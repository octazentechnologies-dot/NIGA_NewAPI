using System.Data;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services;

namespace Niga_Domain.Repositories;

public partial class S4Week4Service : IS4Week4Service
{
    private static readonly string[] CollectMethods = { "CASH", "UPI_OFFLINE", "CARD_POS", "PAY_LINK" };
    private static readonly string[] BloodGroups = { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-", "Unknown" };

    private readonly NIGACentrumContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<S4Week4Service> _logger;

    public S4Week4Service(
        NIGACentrumContext context,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<S4Week4Service> logger)
    {
        _context = context;
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task<bool> PayAtClinicEnabledAsync(int doctorId)
    {
        try
        {
            var fee = await LatestFeeAsync(doctorId);
            return fee == null || fee.PayAtClinicEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consult fee config could not be read for DoctorId={DoctorId}", doctorId);
            return true;
        }
    }

    public async Task<ConsultFeeQuote> ResolveConsultFeesAsync(int doctorId)
    {
        var doctor = await _context.Doctors.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.DeleteStatus);
        if (doctor == null)
            return new ConsultFeeQuote();

        var fee = await LatestFeeOrNullAsync(doctorId);
        return new ConsultFeeQuote
        {
            InClinicFee = fee?.InClinicFee ?? doctor.ConsultFeeInClinic ?? 0,
            TeleFee = fee?.TeleFee ?? doctor.ConsultFeeTele ?? 0
        };
    }

    public async Task<S4ActionResult> GetPublicFeeAsync(int doctorId)
    {
        if (doctorId <= 0)
            return Fail(400, "VALIDATION", "DoctorId is required.");

        var doctor = await _context.Doctors.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.DeleteStatus);
        if (doctor == null)
            return Fail(404, "NOT_FOUND", "Doctor not found.");

        var fee = await LatestFeeOrNullAsync(doctorId);
        var inClinic = fee?.InClinicFee ?? doctor.ConsultFeeInClinic ?? 0;
        var tele = fee?.TeleFee ?? doctor.ConsultFeeTele ?? 0;
        return Ok(new
        {
            success = true,
            data = new
            {
                doctorId,
                inClinicFee = inClinic,
                teleFee = tele,
                instantSurcharge = fee?.InstantSurcharge ?? 0,
                currency = fee?.Currency ?? "INR",
                payAtClinicEnabled = fee?.PayAtClinicEnabled ?? true,
                isVerified = IsVerified(doctor.VerificationStatus),
                source = fee == null ? "profile" : "config"
            }
        });
    }

    public async Task<S4ActionResult> UpsertFeeAsync(FeeUpsertRequest request, S4Caller caller)
    {
        if (request == null)
            return Fail(400, "VALIDATION", "Body is required.");
        if (!caller.IsAdmin && !caller.IsDoctor)
            return Fail(403, "FORBIDDEN", "Only a doctor or admin can set consult fees.");
        if (!caller.OwnsDoctor(request.DoctorId))
            return Fail(403, "FORBIDDEN", "You can update fees only for your own clinic.");
        if (request.InClinicFee < 0 || request.TeleFee < 0 || request.InstantSurcharge < 0
            || request.InClinicFee > 100000 || request.TeleFee > 100000 || request.InstantSurcharge > 100000)
            return Fail(400, "VALIDATION", "Fees must be between 0 and 100000.");
        var currency = (request.Currency ?? "").Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(c => c < 'A' || c > 'Z'))
            return Fail(400, "VALIDATION", "Currency must be a 3-letter code.");
        if (request.EffectiveFrom == null)
            return Fail(400, "VALIDATION", "EffectiveFrom is required.");
        var effective = request.EffectiveFrom.Value.Date;
        var today = DateTime.Today;
        if (effective < today.AddYears(-1) || effective > today.AddYears(1))
            return Fail(400, "VALIDATION", "EffectiveFrom must be within one year of today.");

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == request.DoctorId && !d.DeleteStatus);
        if (doctor == null)
            return Fail(404, "NOT_FOUND", "Doctor not found.");

        var id = await InsertAsync(
            @"INSERT INTO dbo.ConsultFeeConfig
                (DoctorId, InClinicFee, TeleFee, InstantSurcharge, Currency, PayAtClinicEnabled, EffectiveFrom, CreatedBy, CreatedAt)
              VALUES (@DoctorId, @InClinic, @Tele, @Instant, @Currency, @PayAtClinic, @Effective, @By, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@DoctorId", request.DoctorId),
            P("@InClinic", request.InClinicFee),
            P("@Tele", request.TeleFee),
            P("@Instant", request.InstantSurcharge),
            P("@Currency", currency, 3),
            P("@PayAtClinic", request.PayAtClinicEnabled),
            P("@Effective", effective),
            P("@By", caller.UserId),
            P("@At", DateTime.Now));

        doctor.ConsultFeeInClinic = request.InClinicFee;
        doctor.ConsultFeeTele = request.TeleFee;
        doctor.ChangedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Consult fee saved.", data = new { consultFeeConfigId = id } });
    }

    public async Task<S4ActionResult> FeeHistoryAsync(int doctorId, S4Caller caller)
    {
        if (doctorId <= 0)
            return Fail(400, "VALIDATION", "DoctorId is required.");
        if (!caller.IsAccount && !caller.OwnsDoctor(doctorId))
            return Fail(403, "FORBIDDEN", "Fee history is limited to the clinic or account team.");

        var rows = await _context.Database.SqlQuery<FeeRow>($@"
            SELECT ConsultFeeConfigId, DoctorId, InClinicFee, TeleFee, InstantSurcharge, Currency,
                   PayAtClinicEnabled, EffectiveFrom, CreatedAt
            FROM dbo.ConsultFeeConfig
            WHERE DoctorId = {doctorId}
            ORDER BY EffectiveFrom DESC, ConsultFeeConfigId DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> CreateConsultOrderAsync(CreateConsultOrderRequest request, S4Caller caller)
    {
        if (request == null || request.PatientAppId <= 0)
            return Fail(400, "VALIDATION", "PatientAppId is required.");

        var appointment = await LoadAppointmentAsync(request.PatientAppId);
        if (appointment == null)
            return Fail(404, "NOT_FOUND", "Appointment not found.");
        var access = await EnsureAppointmentAccessAsync(appointment, caller);
        if (access != null)
            return access;
        if (S3AppointmentRules.IsCancelled(appointment.Status))
            return Fail(409, "CONFLICT", "A cancelled appointment cannot be paid.");
        if (IsPaid(appointment.PaymentStatus))
            return Fail(409, "CONFLICT", "This appointment is already paid.");

        var doctor = await _context.Doctors.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == appointment.DoctorId && !d.DeleteStatus);
        if (doctor == null)
            return Fail(404, "NOT_FOUND", "Doctor not found.");
        if (caller.IsPatient && !IsVerified(doctor.VerificationStatus))
            return Fail(403, "DOCTOR_UNVERIFIED", "Patient checkout is available only for a verified doctor.");

        var quote = await QuoteConsultAsync(doctor, appointment);
        if (quote.Amount <= 0)
            return Fail(400, "FEE_NOT_CONFIGURED", "Set an in-clinic or tele consult fee before taking payment.");
        if (request.Amount.HasValue && request.Amount.Value != quote.Amount)
            return Fail(400, "AMOUNT_MISMATCH", "Amount is taken from the consult fee configuration.");
        if (request.PayAtClinic && !quote.PayAtClinicEnabled)
            return Fail(400, "PAY_AT_CLINIC_DISABLED", "This doctor does not accept pay at clinic.");

        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        if (key != null && (key.Length > 80 || key.Any(char.IsWhiteSpace)))
            return Fail(400, "VALIDATION", "IdempotencyKey must be a single token up to 80 characters.");
        if (key != null)
        {
            var existing = await FindOrderByKeyAsync(key);
            if (existing != null)
                return Ok(OrderBody(existing, "Existing order returned for this idempotency key."));
        }

        var correlation = Guid.NewGuid().ToString("N");
        if (request.PayAtClinic)
        {
            var orderId = await InsertOrderAsync(new OrderInsert
            {
                Stream = "CONSULT",
                PatientAppId = appointment.PatientAppId,
                DoctorId = appointment.DoctorId,
                PatientId = appointment.PatientId,
                Amount = quote.Amount,
                Currency = quote.Currency,
                Status = "PAY_AT_CLINIC",
                Method = "PAY_AT_CLINIC",
                IdempotencyKey = key,
                CorrelationId = correlation,
                CreatedBy = caller.UserId
            });
            appointment.PayAtClinicAllowed = true;
            appointment.PaymentStatus = "UNPAID";
            appointment.PaymentMethod = "PAY_AT_CLINIC";
            await _context.SaveChangesAsync();
            await SetCorrelationAsync(appointment.PatientAppId, correlation);
            var saved = await LoadOrderAsync(orderId);
            return Ok(OrderBody(saved!, "Pay at clinic recorded. The visit stays unpaid until reception collection."));
        }

        string? gatewayOrderId = null;
        var status = "AWAITING_GATEWAY";
        var gatewayReady = false;
        var keyId = _config["Razorpay:KeyId"];
        var keySecret = _config["Razorpay:KeySecret"];
        if (!string.IsNullOrWhiteSpace(keyId) && !string.IsNullOrWhiteSpace(keySecret))
        {
            gatewayOrderId = await CreateGatewayOrderAsync(keyId, keySecret, quote.Amount, quote.Currency, correlation);
            if (gatewayOrderId == null)
                return Fail(502, "GATEWAY", "The payment gateway did not return an order id.");
            status = "PENDING";
            gatewayReady = true;
        }

        var createdId = await InsertOrderAsync(new OrderInsert
        {
            Stream = "CONSULT",
            PatientAppId = appointment.PatientAppId,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            Amount = quote.Amount,
            Currency = quote.Currency,
            Status = status,
            Method = "ONLINE",
            GatewayOrderId = gatewayOrderId,
            IdempotencyKey = key,
            CorrelationId = correlation,
            CreatedBy = caller.UserId
        });
        await SetCorrelationAsync(appointment.PatientAppId, correlation);
        var created = await LoadOrderAsync(createdId);
        return Ok(new
        {
            success = true,
            message = gatewayReady
                ? "Consult order created. Payment is confirmed only by the webhook."
                : "Consult order stored. Gateway keys are not configured, so checkout cannot start and the visit is not marked paid.",
            gatewayReady,
            keyId = gatewayReady ? keyId : null,
            data = created
        });
    }

    public async Task<S4ActionResult> VerifyPaymentAsync(VerifyPaymentRequest request, S4Caller caller)
    {
        if (request == null || request.PaymentOrderId <= 0)
            return Fail(400, "VALIDATION", "PaymentOrderId is required.");
        var order = await LoadOrderAsync(request.PaymentOrderId);
        if (order == null)
            return Fail(404, "NOT_FOUND", "Payment order not found.");
        var access = await EnsureOrderAccessAsync(order, caller);
        if (access != null)
            return access;
        if (!string.IsNullOrWhiteSpace(request.GatewayPaymentId)
            && !string.Equals(order.GatewayPaymentId, request.GatewayPaymentId, StringComparison.Ordinal))
            return Fail(409, "NOT_CAPTURED", "That gateway payment is not recorded. Client success is not accepted.");

        return Ok(new
        {
            success = true,
            message = "Status is read from the ledger order. This call does not mark a payment paid.",
            data = order
        });
    }

    public async Task<S4ActionResult> AppointmentPaymentAsync(int patientAppId, S4Caller caller)
    {
        if (patientAppId <= 0)
            return Fail(400, "VALIDATION", "PatientAppId is required.");
        var appointment = await LoadAppointmentAsync(patientAppId);
        if (appointment == null)
            return Fail(404, "NOT_FOUND", "Appointment not found.");
        var access = await EnsureAppointmentAccessAsync(appointment, caller);
        if (access != null)
            return access;
        var orders = await _context.Database.SqlQuery<OrderRow>($@"
            SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                   Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
            FROM dbo.PaymentOrder
            WHERE PatientAppId = {patientAppId}
            ORDER BY CreatedAt DESC").ToListAsync();
        return Ok(new
        {
            success = true,
            data = new
            {
                patientAppId,
                paymentStatus = appointment.PaymentStatus,
                paymentMethod = appointment.PaymentMethod,
                orders
            }
        });
    }

    public async Task<S4ActionResult> HandleWebhookAsync(string rawBody, string? signature, string? eventId)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return Fail(400, "VALIDATION", "Webhook body is required.");
        var secret = _config["Razorpay:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return Fail(503, "GATEWAY_NOT_CONFIGURED", "Razorpay webhook secret is not configured.");
        if (!SignatureMatches(rawBody, signature, secret))
            return Fail(401, "WEBHOOK_SIGNATURE", "Webhook signature is invalid.");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return Fail(400, "VALIDATION", "Webhook body is not valid JSON.");
        }

        using (doc)
        {
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("event", out var eventNode) ? eventNode.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(eventType))
                return Fail(400, "VALIDATION", "Webhook event is required.");

            var gatewayEventId = string.IsNullOrWhiteSpace(eventId)
                ? SecurityTokenHash.Sha256Hex(rawBody)
                : eventId.Trim();
            if (await EventExistsAsync(gatewayEventId))
                return Ok(new { success = true, message = "Event already processed.", eventId = gatewayEventId });

            var payment = ReadEntity(root, "payment");
            var refund = ReadEntity(root, "refund");
            var gatewayOrderId = payment?.GetPropertyOrNull("order_id");
            var gatewayPaymentId = payment?.GetPropertyOrNull("id") ?? refund?.GetPropertyOrNull("payment_id");
            var order = await FindOrderByGatewayAsync(gatewayOrderId, gatewayPaymentId);

            await InsertAsync(
                @"INSERT INTO dbo.PaymentEvent (PaymentOrderId, GatewayEventId, EventType, RawJson, At)
                  VALUES (@OrderId, @EventId, @Type, @Json, @At);
                  SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                P("@OrderId", order?.PaymentOrderId),
                P("@EventId", gatewayEventId, 80),
                P("@Type", eventType, 60),
                P("@Json", rawBody, -1),
                P("@At", DateTime.Now));

            if (eventType == "payment.captured")
                await ApplyCapturedAsync(order, payment, gatewayPaymentId);
            else if (eventType == "payment.failed")
                await ApplyFailedAsync(order, gatewayPaymentId);
            else if (eventType == "refund.processed")
                await ApplyRefundWebhookAsync(order, refund);
            else
                _logger.LogInformation("Stored unhandled payment event {EventType}", eventType);

            return Ok(new { success = true, message = "Webhook stored.", eventType });
        }
    }

    public async Task<S4ActionResult> CollectAtReceptionAsync(CollectAtReceptionRequest request, S4Caller caller)
    {
        if (request == null || request.PatientAppId <= 0)
            return Fail(400, "VALIDATION", "PatientAppId is required.");
        if (!caller.IsAdmin && !caller.IsDoctor && !caller.IsReception)
            return Fail(403, "FORBIDDEN", "Only reception or the treating doctor can collect at clinic.");

        var method = (request.Method ?? "").Trim().ToUpperInvariant();
        if (!CollectMethods.Contains(method))
            return Fail(400, "VALIDATION", "Method must be CASH, UPI_OFFLINE, CARD_POS, or PAY_LINK.");

        var appointment = await LoadAppointmentAsync(request.PatientAppId);
        if (appointment == null)
            return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (!caller.OwnsDoctor(appointment.DoctorId))
            return Fail(403, "FORBIDDEN", "This appointment belongs to another clinic.");
        if (S3AppointmentRules.IsCancelled(appointment.Status))
            return Fail(409, "CONFLICT", "A cancelled appointment cannot be collected.");
        if (IsPaid(appointment.PaymentStatus))
            return Fail(409, "CONFLICT", "This appointment is already paid.");

        var doctor = await _context.Doctors.AsNoTracking().FirstAsync(d => d.DoctorId == appointment.DoctorId);
        var quote = await QuoteConsultAsync(doctor, appointment);
        if (quote.Amount <= 0)
            return Fail(400, "FEE_NOT_CONFIGURED", "Set a consult fee before collection.");
        if (request.Amount.HasValue && request.Amount.Value != quote.Amount)
            return Fail(400, "AMOUNT_MISMATCH", "Collected amount must match the configured consult fee.");

        var correlation = Guid.NewGuid().ToString("N");
        var status = method == "PAY_LINK" ? "PAY_LINK" : "COLLECTED";
        var orderId = await InsertOrderAsync(new OrderInsert
        {
            Stream = "CONSULT",
            PatientAppId = appointment.PatientAppId,
            DoctorId = appointment.DoctorId,
            PatientId = appointment.PatientId,
            Amount = quote.Amount,
            Currency = quote.Currency,
            Status = status,
            Method = method,
            CorrelationId = correlation,
            CreatedBy = caller.UserId
        });

        if (method == "PAY_LINK")
        {
            appointment.PaymentMethod = "PAY_LINK";
            appointment.PaymentStatus = string.IsNullOrWhiteSpace(appointment.PaymentStatus) ? "UNPAID" : appointment.PaymentStatus;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                message = "Pay link reserved. SMS is not sent. The visit stays unpaid until collection or webhook capture.",
                data = new { paymentOrderId = orderId, linkToken = correlation, amount = quote.Amount, currency = quote.Currency }
            });
        }

        await MarkConsultCollectedAsync(orderId, appointment, quote.Amount, method, correlation, caller.UserId);
        var gst = await GstOnAsync(quote.Amount);
        return Ok(new
        {
            success = true,
            message = "Collected at reception.",
            receipt = new
            {
                paymentOrderId = orderId,
                patientAppId = appointment.PatientAppId,
                amount = quote.Amount,
                method,
                gstAmount = gst.Amount,
                gstNote = gst.Note,
                currency = quote.Currency
            }
        });
    }

    public async Task<S4ActionResult> CreateMedicinePaymentAsync(CreateMedicinePaymentRequest request, S4Caller caller)
    {
        if (request == null || request.MedicineOrderId <= 0)
            return Fail(400, "VALIDATION", "MedicineOrderId is required.");
        var mode = (request.PayMode ?? "").Trim().ToUpperInvariant();
        if (mode is not ("ONLINE" or "COD"))
            return Fail(400, "VALIDATION", "PayMode must be ONLINE or COD.");

        var order = await LoadMedicineAsync(request.MedicineOrderId);
        if (order == null)
            return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (!caller.IsAdmin && caller.PatientId != order.PatientId)
            return Fail(403, "FORBIDDEN", "Only the patient can pay this medicine order.");
        if (!string.Equals(order.Status, "QUOTED_ACCEPTED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Accept the pharmacy quote before payment.");
        if (order.QuoteAmount is null or <= 0)
            return Fail(409, "CONFLICT", "A quote amount is required.");

        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        if (key != null)
        {
            var existing = await FindOrderByKeyAsync(key);
            if (existing != null)
                return Ok(OrderBody(existing, "Existing medicine payment returned."));
        }

        var correlation = Guid.NewGuid().ToString("N");
        if (mode == "COD")
        {
            var id = await InsertOrderAsync(new OrderInsert
            {
                Stream = "MEDICINE",
                MedicineOrderId = order.MedicineOrderId,
                PatientId = order.PatientId,
                Amount = order.QuoteAmount.Value,
                Currency = "INR",
                Status = "COD",
                Method = "COD",
                IdempotencyKey = key,
                CorrelationId = correlation,
                CreatedBy = caller.UserId
            });
            await SetMedicineStatusAsync(order.MedicineOrderId, "COD_PENDING", "COD selected");
            return Ok(new { success = true, message = "COD recorded. Capture happens when the seller confirms collection.", data = new { paymentOrderId = id, status = "COD" } });
        }

        var keyId = _config["Razorpay:KeyId"];
        var keySecret = _config["Razorpay:KeySecret"];
        string? gatewayOrderId = null;
        var status = "AWAITING_GATEWAY";
        var gatewayReady = false;
        if (!string.IsNullOrWhiteSpace(keyId) && !string.IsNullOrWhiteSpace(keySecret))
        {
            gatewayOrderId = await CreateGatewayOrderAsync(keyId, keySecret, order.QuoteAmount.Value, "INR", correlation);
            if (gatewayOrderId == null)
                return Fail(502, "GATEWAY", "The payment gateway did not return an order id.");
            status = "PENDING";
            gatewayReady = true;
        }

        var paymentId = await InsertOrderAsync(new OrderInsert
        {
            Stream = "MEDICINE",
            MedicineOrderId = order.MedicineOrderId,
            PatientId = order.PatientId,
            Amount = order.QuoteAmount.Value,
            Currency = "INR",
            Status = status,
            Method = "ONLINE",
            GatewayOrderId = gatewayOrderId,
            IdempotencyKey = key,
            CorrelationId = correlation,
            CreatedBy = caller.UserId
        });
        return Ok(new
        {
            success = true,
            gatewayReady,
            message = gatewayReady
                ? "Medicine order created. Paid status waits for the webhook."
                : "Medicine payment stored. Gateway keys are not configured, so the order is not marked paid.",
            data = new { paymentOrderId = paymentId, amount = order.QuoteAmount, currency = "INR" }
        });
    }

    public async Task<S4ActionResult> PatientPaymentsAsync(S4Caller caller)
    {
        if (caller.PatientId is null or <= 0)
            return Fail(403, "FORBIDDEN", "A patient profile is required.");
        var patientId = caller.PatientId.Value;
        var rows = await _context.Database.SqlQuery<OrderRow>($@"
            SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                   Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
            FROM dbo.PaymentOrder
            WHERE PatientId = {patientId}
            ORDER BY CreatedAt DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> PreviewRefundPolicyAsync(long paymentOrderId, S4Caller caller)
    {
        var loaded = await LoadRefundContextAsync(paymentOrderId, caller, requireAccount: false);
        if (loaded.Error != null)
            return loaded.Error;
        var policy = BuildPolicy(loaded.Order!, loaded.Appointment);
        return Ok(new { success = true, data = policy });
    }

    public async Task<S4ActionResult> CreateRefundAsync(CreateRefundRequest request, S4Caller caller)
    {
        if (!caller.IsAccount)
            return Fail(403, "FORBIDDEN", "Refunds are created by the account role.");
        if (request == null || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 3)
            return Fail(400, "VALIDATION", "A refund reason is required.");

        var loaded = await LoadRefundContextAsync(request.PaymentOrderId, caller, requireAccount: true);
        if (loaded.Error != null)
            return loaded.Error;
        var order = loaded.Order!;
        if (order.Status is not ("CAPTURED" or "COLLECTED"))
            return Fail(409, "CONFLICT", "Only a captured or collected payment can be refunded.");

        var policy = BuildPolicy(order, loaded.Appointment);
        if (policy.Policy == "NONE")
            return Fail(409, "REFUND_NONE", "The cancel policy does not allow a refund for this visit.");

        var amount = request.Amount ?? policy.Amount;
        if (amount <= 0 || amount > policy.Amount)
            return Fail(400, "VALIDATION", "Refund amount must be greater than 0 and within the policy amount.");

        var refundId = await InsertAsync(
            @"INSERT INTO dbo.Refund (PaymentOrderId, Amount, Reason, Policy, Status, ByUserId, At)
              VALUES (@OrderId, @Amount, @Reason, @Policy, N'REQUESTED', @By, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@OrderId", order.PaymentOrderId),
            P("@Amount", amount),
            P("@Reason", request.Reason.Trim(), 500),
            P("@Policy", policy.Policy, 20),
            P("@By", caller.UserId),
            P("@At", DateTime.Now));

        if (order.PatientAppId.HasValue)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientAppointment
                SET PaymentStatus = N'REFUND_PENDING'
                WHERE PatientAppId = {order.PatientAppId.Value}");
        }

        return Ok(new
        {
            success = true,
            message = "Refund requested. Gateway refund.processed marks it complete. No bank call is made here.",
            data = new { refundId, amount, policy = policy.Policy }
        });
    }

    public async Task<S4ActionResult> ListRefundsAsync(S4Caller caller)
    {
        if (!caller.IsAccount)
            return Fail(403, "FORBIDDEN", "Refund list is for the account role.");
        var rows = await _context.Database.SqlQuery<RefundRow>($@"
            SELECT RefundId, PaymentOrderId, Amount, Reason, Policy, Status, GatewayRefundId, ByUserId, At
            FROM dbo.Refund
            ORDER BY At DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> GetOrCreateInvoiceAsync(long paymentOrderId, S4Caller caller, bool create)
    {
        var order = await LoadOrderAsync(paymentOrderId);
        if (order == null)
            return Fail(404, "NOT_FOUND", "Payment order not found.");
        var access = await EnsureOrderAccessAsync(order, caller);
        if (access != null)
            return access;

        var existing = await _context.Database.SqlQuery<InvoiceRow>($@"
            SELECT InvoiceId, Number, Series, Stream, PaymentOrderId, GstBreakup, PdfPath, SecureDocumentId, CreatedAt
            FROM dbo.Invoice WHERE PaymentOrderId = {paymentOrderId}").FirstOrDefaultAsync();
        if (existing != null || !create)
        {
            if (existing == null)
                return Fail(404, "NOT_FOUND", "Invoice not found.");
            return Ok(new { success = true, data = existing });
        }

        if (order.Status is not ("CAPTURED" or "COLLECTED"))
            return Fail(409, "CONFLICT", "Invoice is available after payment is captured or collected.");

        var gst = await GstOnAsync(order.Amount);
        var series = order.Stream;
        var invoiceId = await InsertAsync(
            @"INSERT INTO dbo.Invoice (Number, Series, Stream, PaymentOrderId, GstBreakup, CreatedAt)
              VALUES (N'PENDING', @Series, @Stream, @OrderId, @Gst, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@Series", series, 20),
            P("@Stream", order.Stream, 20),
            P("@OrderId", order.PaymentOrderId),
            P("@Gst", gst.Note, 500),
            P("@At", DateTime.Now));
        var number = $"{series}-{DateTime.Now:yyyy}-{invoiceId:D6}";
        var lines = new[]
        {
            "Invoice " + number,
            "Stream " + order.Stream,
            "Amount " + order.Amount.ToString("0.00") + " " + order.Currency,
            gst.Note,
            "Payment order " + order.PaymentOrderId,
            "Correlation " + order.CorrelationId
        };
        var pdf = ClinicalCasePdfBuilder.Build("Homeocentrum invoice", lines);
        var relative = await StorePdfAsync(pdf, number + ".pdf", "Invoice", invoiceId, caller.UserId);
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Invoice
            SET Number = {number}, PdfPath = {relative.Path}, SecureDocumentId = {relative.DocumentId}
            WHERE InvoiceId = {invoiceId}");
        var saved = await _context.Database.SqlQuery<InvoiceRow>($@"
            SELECT InvoiceId, Number, Series, Stream, PaymentOrderId, GstBreakup, PdfPath, SecureDocumentId, CreatedAt
            FROM dbo.Invoice WHERE InvoiceId = {invoiceId}").FirstAsync();
        return Ok(new { success = true, data = saved });
    }

    private async Task ApplyCapturedAsync(OrderRow? order, JsonElement? payment, string? gatewayPaymentId)
    {
        if (order == null)
        {
            await AddExceptionAsync(null, "ORDER_NOT_FOUND", "Captured webhook did not match a payment order.");
            return;
        }
        if (order.Status is "CAPTURED" or "COLLECTED")
            return;

        var paise = payment?.GetPropertyOrNull("amount");
        if (long.TryParse(paise, out var paidPaise))
        {
            var expected = (long)Math.Round(order.Amount * 100m, 0);
            if (paidPaise != expected)
            {
                await AddExceptionAsync(order.PaymentOrderId, "AMOUNT_MISMATCH",
                    "Gateway amount " + paidPaise + " paise does not match order " + expected + ".");
                return;
            }
        }

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PaymentOrder
            SET Status = N'CAPTURED', GatewayPaymentId = {gatewayPaymentId}, Method = ISNULL(Method, N'ONLINE')
            WHERE PaymentOrderId = {order.PaymentOrderId}");

        if (order.Stream == "CONSULT" && order.PatientAppId.HasValue)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientAppointment
                SET PaymentStatus = N'PAID', PaymentMethod = N'ONLINE'
                WHERE PatientAppId = {order.PatientAppId.Value}");
            await InsertAsync(
                @"INSERT INTO dbo.ConsultPayment (PaymentOrderId, PatientAppId, Amount, Method, Status, GstAmount, CreatedAt)
                  VALUES (@OrderId, @AppId, @Amount, N'ONLINE', N'CAPTURED', 0, @At);
                  SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                P("@OrderId", order.PaymentOrderId),
                P("@AppId", order.PatientAppId.Value),
                P("@Amount", order.Amount),
                P("@At", DateTime.Now));
            await WriteLedgerAsync(order, "CREDIT", order.Amount, 0, "Appointment", order.PatientAppId.Value.ToString());
        }
        else if (order.Stream == "MEDICINE" && order.MedicineOrderId.HasValue)
        {
            await SetMedicineStatusAsync(order.MedicineOrderId.Value, "PAID", "Webhook captured");
            await WriteMedicineSplitsAsync(order);
        }
    }

    private async Task ApplyFailedAsync(OrderRow? order, string? gatewayPaymentId)
    {
        if (order == null)
        {
            await AddExceptionAsync(null, "ORDER_NOT_FOUND", "Failed webhook did not match a payment order.");
            return;
        }
        if (order.Status is "CAPTURED" or "COLLECTED")
        {
            await AddExceptionAsync(order.PaymentOrderId, "CAPTURED_THEN_FAILED", "Failure arrived after capture.");
            return;
        }
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PaymentOrder
            SET Status = N'FAILED', GatewayPaymentId = {gatewayPaymentId}
            WHERE PaymentOrderId = {order.PaymentOrderId}");
        if (order.PatientAppId.HasValue)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientAppointment
                SET PaymentStatus = N'FAILED'
                WHERE PatientAppId = {order.PatientAppId.Value} AND ISNULL(PaymentStatus, N'') <> N'PAID'");
        }
    }

    private async Task ApplyRefundWebhookAsync(OrderRow? order, JsonElement? refund)
    {
        if (order == null)
        {
            await AddExceptionAsync(null, "REFUND_ORDER_NOT_FOUND", "Refund webhook did not match a payment order.");
            return;
        }
        var gatewayRefundId = refund?.GetPropertyOrNull("id");
        var open = await _context.Database.SqlQuery<RefundRow>($@"
            SELECT TOP 1 RefundId, PaymentOrderId, Amount, Reason, Policy, Status, GatewayRefundId, ByUserId, At
            FROM dbo.Refund
            WHERE PaymentOrderId = {order.PaymentOrderId} AND Status = N'REQUESTED'
            ORDER BY At").FirstOrDefaultAsync();
        if (open == null)
        {
            await AddExceptionAsync(order.PaymentOrderId, "REFUND_WITHOUT_REQUEST", "Gateway refund had no account request.");
            return;
        }
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Refund
            SET Status = N'PROCESSED', GatewayRefundId = {gatewayRefundId}
            WHERE RefundId = {open.RefundId}");
        await WriteLedgerAsync(order, "DEBIT", open.Amount, 0, "Refund", open.RefundId.ToString());
        if (order.PatientAppId.HasValue)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientAppointment SET PaymentStatus = N'REFUNDED' WHERE PatientAppId = {order.PatientAppId.Value}");
        }
    }

    private async Task MarkConsultCollectedAsync(long orderId, PatientAppointment appointment, decimal amount, string method, string correlation, long userId)
    {
        appointment.PaymentStatus = "PAID";
        appointment.PaymentMethod = method;
        await _context.SaveChangesAsync();
        await SetCorrelationAsync(appointment.PatientAppId, correlation);
        var gst = await GstOnAsync(amount);
        await InsertAsync(
            @"INSERT INTO dbo.ConsultPayment (PaymentOrderId, PatientAppId, Amount, Method, Status, GstAmount, CreatedAt)
              VALUES (@OrderId, @AppId, @Amount, @Method, N'COLLECTED', @Gst, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@OrderId", orderId),
            P("@AppId", appointment.PatientAppId),
            P("@Amount", amount),
            P("@Method", method, 30),
            P("@Gst", gst.Amount),
            P("@At", DateTime.Now));
        var order = await LoadOrderAsync(orderId);
        if (order != null)
            await WriteLedgerAsync(order, "CREDIT", amount, gst.Amount, "Appointment", appointment.PatientAppId.ToString());
        _ = userId;
    }

    private async Task<(OrderRow? Order, PatientAppointment? Appointment, S4ActionResult? Error)> LoadRefundContextAsync(
        long paymentOrderId, S4Caller caller, bool requireAccount)
    {
        if (paymentOrderId <= 0)
            return (null, null, Fail(400, "VALIDATION", "PaymentOrderId is required."));
        var order = await LoadOrderAsync(paymentOrderId);
        if (order == null)
            return (null, null, Fail(404, "NOT_FOUND", "Payment order not found."));
        if (!requireAccount)
        {
            var access = await EnsureOrderAccessAsync(order, caller);
            if (access != null)
                return (null, null, access);
        }
        PatientAppointment? appointment = null;
        if (order.PatientAppId.HasValue)
            appointment = await LoadAppointmentAsync(order.PatientAppId.Value);
        return (order, appointment, null);
    }

    private static RefundPolicy BuildPolicy(OrderRow order, PatientAppointment? appointment)
    {
        if (appointment?.AppointmentDate == null)
            return new RefundPolicy("FULL", order.Amount, "No visit time is stored, so the full captured amount can be refunded.");
        var when = appointment.AppointmentDate.Value.Date;
        if (appointment.AppointmentTime.HasValue)
            when = when.Add(appointment.AppointmentTime.Value.ToTimeSpan());
        if (when <= DateTime.Now)
            return new RefundPolicy("NONE", 0, "The visit time has passed.");
        if (when <= DateTime.Now.AddHours(24))
            return new RefundPolicy("PARTIAL", Math.Round(order.Amount / 2m, 2), "Inside 24 hours, half the amount can be refunded.");
        return new RefundPolicy("FULL", order.Amount, "More than 24 hours before the visit, the full amount can be refunded.");
    }

    private async Task<FeeQuote> QuoteConsultAsync(Doctor doctor, PatientAppointment appointment)
    {
        var fee = await LatestFeeOrNullAsync(doctor.DoctorId);
        var tele = appointment.IsTele == true
            || string.Equals(S3AppointmentRules.NormalizeMode(appointment.ConsultMode), S3AppointmentRules.Tele, StringComparison.Ordinal);
        var amount = tele
            ? fee?.TeleFee ?? doctor.ConsultFeeTele ?? 0
            : fee?.InClinicFee ?? doctor.ConsultFeeInClinic ?? 0;
        var mode = appointment.ConsultMode ?? "";
        if (mode.Contains("instant", StringComparison.OrdinalIgnoreCase))
            amount += fee?.InstantSurcharge ?? 0;
        return new FeeQuote(amount, fee?.Currency ?? "INR", fee?.PayAtClinicEnabled ?? true);
    }

    private async Task<FeeRow?> LatestFeeAsync(int doctorId) => await LatestFeeOrNullAsync(doctorId);

    private async Task<FeeRow?> LatestFeeOrNullAsync(int doctorId)
        => await _context.Database.SqlQuery<FeeRow>($@"
            SELECT TOP 1 ConsultFeeConfigId, DoctorId, InClinicFee, TeleFee, InstantSurcharge, Currency,
                   PayAtClinicEnabled, EffectiveFrom, CreatedAt
            FROM dbo.ConsultFeeConfig
            WHERE DoctorId = {doctorId} AND EffectiveFrom <= CONVERT(date, GETDATE())
            ORDER BY EffectiveFrom DESC, ConsultFeeConfigId DESC").FirstOrDefaultAsync();

    private async Task<string?> CreateGatewayOrderAsync(string keyId, string keySecret, decimal amount, string currency, string receipt)
    {
        var paise = (long)Math.Round(amount * 100m, 0);
        var payload = JsonSerializer.Serialize(new { amount = paise, currency, receipt });
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(keyId + ":" + keySecret));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("https://api.razorpay.com/v1/orders", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Razorpay order failed with status {Status}", (int)response.StatusCode);
            return null;
        }
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private static bool SignatureMatches(string body, string? signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        byte[] actual;
        try
        {
            actual = Convert.FromHexString(signature.Trim());
        }
        catch (FormatException)
        {
            return false;
        }
        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static JsonElement? ReadEntity(JsonElement root, string name)
    {
        if (!root.TryGetProperty("payload", out var payload) || !payload.TryGetProperty(name, out var node))
            return null;
        if (!node.TryGetProperty("entity", out var entity))
            return null;
        return entity;
    }

    private async Task<PatientAppointment?> LoadAppointmentAsync(int patientAppId)
        => await _context.PatientAppointments.FirstOrDefaultAsync(a => a.PatientAppId == patientAppId && a.DeleteStatus != true);

    private async Task<S4ActionResult?> EnsureAppointmentAccessAsync(PatientAppointment appointment, S4Caller caller)
    {
        if (caller.IsAdmin || caller.IsAccount)
            return null;
        if (caller.IsPatient)
            return caller.PatientId == appointment.PatientId ? null : Fail(403, "FORBIDDEN", "This appointment belongs to another patient.");
        if ((caller.IsDoctor || caller.IsReception) && caller.OwnsDoctor(appointment.DoctorId))
            return null;
        return Fail(403, "FORBIDDEN", "You cannot access this appointment.");
    }

    private async Task<S4ActionResult?> EnsureOrderAccessAsync(OrderRow order, S4Caller caller)
    {
        if (caller.IsAdmin || caller.IsAccount)
            return null;
        if (caller.IsPatient && caller.PatientId == order.PatientId)
            return null;
        if (order.DoctorId.HasValue && caller.OwnsDoctor(order.DoctorId.Value) && (caller.IsDoctor || caller.IsReception))
            return null;
        return Fail(403, "FORBIDDEN", "You cannot access this payment.");
    }

    private static bool IsPaid(string? status)
        => string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase);

    private static bool IsVerified(string? status)
        => status != null && (status.Equals("Verified", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Approved", StringComparison.OrdinalIgnoreCase));

    private async Task SetCorrelationAsync(int patientAppId, string correlation)
        => await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PatientAppointment SET CorrelationId = {correlation} WHERE PatientAppId = {patientAppId}");

    private async Task<bool> EventExistsAsync(string eventId)
    {
        var count = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.PaymentEvent WHERE GatewayEventId = {eventId}").FirstAsync();
        return count.Value > 0;
    }

    private async Task<OrderRow?> FindOrderByKeyAsync(string key)
        => await _context.Database.SqlQuery<OrderRow>($@"
            SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                   Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
            FROM dbo.PaymentOrder WHERE IdempotencyKey = {key}").FirstOrDefaultAsync();

    private async Task<OrderRow?> FindOrderByGatewayAsync(string? gatewayOrderId, string? gatewayPaymentId)
    {
        if (!string.IsNullOrWhiteSpace(gatewayOrderId))
        {
            var byOrder = await _context.Database.SqlQuery<OrderRow>($@"
                SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                       Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
                FROM dbo.PaymentOrder WHERE GatewayOrderId = {gatewayOrderId}").FirstOrDefaultAsync();
            if (byOrder != null)
                return byOrder;
        }
        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
            return null;
        return await _context.Database.SqlQuery<OrderRow>($@"
            SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                   Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
            FROM dbo.PaymentOrder WHERE GatewayPaymentId = {gatewayPaymentId}").FirstOrDefaultAsync();
    }

    private async Task<OrderRow?> LoadOrderAsync(long id)
        => await _context.Database.SqlQuery<OrderRow>($@"
            SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                   Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
            FROM dbo.PaymentOrder WHERE PaymentOrderId = {id}").FirstOrDefaultAsync();

    private async Task<long> InsertOrderAsync(OrderInsert row)
        => await InsertAsync(
            @"INSERT INTO dbo.PaymentOrder
                (Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency, Status, Method,
                 GatewayOrderId, IdempotencyKey, CorrelationId, CreatedBy, CreatedAt)
              VALUES
                (@Stream, @AppId, @MedId, @DoctorId, @PatientId, @Amount, @Currency, @Status, @Method,
                 @GatewayOrder, @Idem, @Correlation, @By, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@Stream", row.Stream, 20),
            P("@AppId", row.PatientAppId),
            P("@MedId", row.MedicineOrderId),
            P("@DoctorId", row.DoctorId),
            P("@PatientId", row.PatientId),
            P("@Amount", row.Amount),
            P("@Currency", row.Currency, 3),
            P("@Status", row.Status, 30),
            P("@Method", row.Method, 30),
            P("@GatewayOrder", row.GatewayOrderId, 80),
            P("@Idem", row.IdempotencyKey, 80),
            P("@Correlation", row.CorrelationId, 40),
            P("@By", row.CreatedBy),
            P("@At", DateTime.Now));

    private static object OrderBody(OrderRow order, string message)
        => new { success = true, message, data = order };

    private async Task<(decimal Amount, string Note)> GstOnAsync(decimal amount)
    {
        var tax = await _context.Database.SqlQuery<TaxRow>($@"
            SELECT TOP 1 GstRate, TreatmentExempt FROM dbo.TaxConfig ORDER BY TaxConfigId").FirstOrDefaultAsync();
        if (tax == null || tax.TreatmentExempt)
            return (0, "GST 0. Treatment is exempt until tax config says otherwise.");
        var gst = Math.Round(amount * tax.GstRate / 100m, 2);
        return (gst, "GST " + gst.ToString("0.00") + " at " + tax.GstRate.ToString("0.##") + "%.");
    }

    private async Task<(string Path, long DocumentId)> StorePdfAsync(byte[] pdf, string fileName, string ownerType, long ownerId, long userId, string mime = "application/pdf")
    {
        var root = Path.Combine(_env.ContentRootPath, "Data", "SecureDocuments");
        Directory.CreateDirectory(root);
        var stored = Guid.NewGuid().ToString("N") + "_" + fileName;
        var relative = Path.Combine("Data", "SecureDocuments", stored).Replace('\\', '/');
        await File.WriteAllBytesAsync(Path.Combine(root, stored), pdf);
        var hash = Convert.ToHexString(SHA256.HashData(pdf));
        var doc = new SecureDocument
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            BlobPath = relative,
            FileName = fileName,
            Mime = mime,
            Hash = hash,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.SecureDocuments.Add(doc);
        await _context.SaveChangesAsync();
        return (relative, doc.SecureDocumentId);
    }

    private async Task AddExceptionAsync(long? orderId, string kind, string detail)
        => await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.PaymentException (PaymentOrderId, Kind, Detail, Status, CreatedAt)
            VALUES ({orderId}, {kind}, {detail}, N'OPEN', {DateTime.Now})");

    private async Task WriteLedgerAsync(OrderRow order, string direction, decimal amount, decimal gst, string entityType, string entityId,
        decimal? seller = null, decimal? platform = null, decimal? delivery = null)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.LedgerEntry
                (Stream, Direction, Amount, Gst, Commission, SplitSeller, SplitPlatform, SplitDelivery,
                 EntityType, EntityId, PaymentOrderId, CorrelationId, At)
            VALUES
                ({order.Stream}, {direction}, {amount}, {gst}, {0m}, {seller}, {platform}, {delivery},
                 {entityType}, {entityId}, {order.PaymentOrderId}, {order.CorrelationId}, {DateTime.Now})");
    }

    private async Task WriteMedicineSplitsAsync(OrderRow order)
    {
        var seller = Math.Round(order.Amount * 0.70m, 2);
        var platform = Math.Round(order.Amount * 0.20m, 2);
        var delivery = order.Amount - seller - platform;
        await WriteLedgerAsync(order, "CREDIT", seller, 0, "Pharmacy", order.MedicineOrderId?.ToString() ?? "", seller, platform, delivery);
        await WriteLedgerAsync(order, "CREDIT", platform, 0, "Platform", order.MedicineOrderId?.ToString() ?? "", seller, platform, delivery);
        await WriteLedgerAsync(order, "CREDIT", delivery, 0, "Delivery", order.MedicineOrderId?.ToString() ?? "", seller, platform, delivery);
    }

    private static S4ActionResult Ok(object body) => S4ActionResult.Ok(body);
    private static S4ActionResult Fail(int status, string code, string message) => S4ActionResult.Fail(status, code, message);

    private static S4ActionResult? DateRange(DateTime? from, DateTime? to, out DateTime start, out DateTime end)
    {
        end = (to ?? DateTime.Today).Date.AddDays(1);
        start = (from ?? end.AddDays(-31)).Date;
        if (start >= end)
        {
            start = end;
            return Fail(400, "VALIDATION", "From must be before to.");
        }
        if ((end - start).TotalDays > 366)
            return Fail(400, "VALIDATION", "Date range cannot exceed 366 days.");
        return null;
    }

    private static S4ActionResult? AccountOnly(S4Caller caller)
        => caller.IsAccount ? null : Fail(403, "FORBIDDEN", "This route is for the account role.");

    private async Task<long> InsertAsync(string sql, params SqlParameter[] parameters)
    {
        var connection = _context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var parameter in parameters)
                command.Parameters.Add(parameter);
            var scalar = await command.ExecuteScalarAsync();
            if (scalar == null || scalar == DBNull.Value)
                throw new InvalidOperationException("Insert did not return an id.");
            return Convert.ToInt64(scalar);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static SqlParameter P(string name, object? value, int size = 0)
    {
        if (value is string text)
        {
            var parameter = size < 0
                ? new SqlParameter(name, SqlDbType.NVarChar, -1)
                : new SqlParameter(name, SqlDbType.NVarChar, size == 0 ? 400 : size);
            parameter.Value = (object?)text ?? DBNull.Value;
            return parameter;
        }
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    private sealed class OrderInsert
    {
        public string Stream { get; set; } = "";
        public int? PatientAppId { get; set; }
        public int? MedicineOrderId { get; set; }
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Status { get; set; } = "";
        public string? Method { get; set; }
        public string? GatewayOrderId { get; set; }
        public string? IdempotencyKey { get; set; }
        public string CorrelationId { get; set; } = "";
        public long CreatedBy { get; set; }
    }

    private readonly record struct FeeQuote(decimal Amount, string Currency, bool PayAtClinicEnabled);
    private readonly record struct RefundPolicy(string Policy, decimal Amount, string Reason);
}

file static class JsonElementExtensions
{
    public static string? GetPropertyOrNull(this JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }
}

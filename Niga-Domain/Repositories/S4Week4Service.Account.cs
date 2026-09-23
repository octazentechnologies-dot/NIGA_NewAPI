using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services;

namespace Niga_Domain.Repositories;

public partial class S4Week4Service
{
    public async Task<S4ActionResult> LedgerAsync(string? stream, DateTime? from, DateTime? to, int? doctorId, int page, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var range = DateRange(from, to, out var start, out var end);
        if (range != null) return range;
        var pageNumber = page < 1 ? 1 : page;
        var skip = (pageNumber - 1) * 50;
        var filter = (stream ?? "").Trim().ToUpperInvariant();
        if (filter.Length > 0 && filter is not ("CONSULT" or "SUBSCRIPTION" or "MEDICINE"))
            return Fail(400, "VALIDATION", "Stream must be CONSULT, SUBSCRIPTION, or MEDICINE.");

        var rows = await _context.Database.SqlQuery<LedgerRow>($@"
            SELECT LedgerEntryId, Stream, Direction, Amount, Gst, EntityType, EntityId, PaymentOrderId, CorrelationId, At
            FROM dbo.LedgerEntry
            WHERE At >= {start} AND At < {end}
              AND ({filter} = N'' OR Stream = {filter})
            ORDER BY At DESC
            OFFSET {skip} ROWS FETCH NEXT 50 ROWS ONLY").ToListAsync();
        _ = doctorId;
        return Ok(new { success = true, page = pageNumber, pageSize = 50, data = rows });
    }

    public async Task<S4ActionResult> LedgerExportAsync(string? stream, DateTime? from, DateTime? to, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var range = DateRange(from, to, out var start, out var end);
        if (range != null) return range;
        var count = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.LedgerEntry WHERE At >= {start} AND At < {end}").FirstAsync();
        if (count.Value > 5000)
            return Fail(400, "VALIDATION", "Narrow the date range. Export is limited to 5000 rows.");

        var rows = await _context.Database.SqlQuery<LedgerRow>($@"
            SELECT LedgerEntryId, Stream, Direction, Amount, Gst, EntityType, EntityId, PaymentOrderId, CorrelationId, At
            FROM dbo.LedgerEntry
            WHERE At >= {start} AND At < {end}
            ORDER BY At").ToListAsync();
        var csv = new StringBuilder();
        csv.AppendLine("LedgerEntryId,Stream,Direction,Amount,Gst,EntityType,EntityId,PaymentOrderId,CorrelationId,At");
        foreach (var row in rows)
        {
            csv.Append(row.LedgerEntryId).Append(',')
                .Append(Csv(row.Stream)).Append(',')
                .Append(Csv(row.Direction)).Append(',')
                .Append(row.Amount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Gst.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(row.EntityType)).Append(',')
                .Append(Csv(row.EntityId)).Append(',')
                .Append(row.PaymentOrderId).Append(',')
                .Append(Csv(row.CorrelationId)).Append(',')
                .AppendLine(row.At.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }
        _ = stream;
        return Ok(new { success = true, fileName = "ledger.csv", csv = csv.ToString() });
    }

    public async Task<S4ActionResult> ReconciliationAsync(DateTime? from, DateTime? to, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var range = DateRange(from, to, out var start, out var end);
        if (range != null) return range;
        var rows = await _context.Database.SqlQuery<OrderRow>($@"
            SELECT p.PaymentOrderId, p.Stream, p.PatientAppId, p.MedicineOrderId, p.DoctorId, p.PatientId, p.Amount, p.Currency,
                   p.Status, p.Method, p.GatewayOrderId, p.GatewayPaymentId, p.IdempotencyKey, p.CorrelationId, p.CreatedAt
            FROM dbo.PaymentOrder p
            WHERE p.Stream = N'CONSULT' AND p.CreatedAt >= {start} AND p.CreatedAt < {end}
            ORDER BY p.CreatedAt DESC").ToListAsync();
        var captured = rows.Where(r => r.Status is "CAPTURED" or "COLLECTED").Sum(r => r.Amount);
        return Ok(new { success = true, data = new { count = rows.Count, capturedOrCollected = captured, orders = rows } });
    }

    public async Task<S4ActionResult> MedicineLedgerAsync(DateTime? from, DateTime? to, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var range = DateRange(from, to, out var start, out var end);
        if (range != null) return range;
        var rows = await _context.Database.SqlQuery<LedgerRow>($@"
            SELECT LedgerEntryId, Stream, Direction, Amount, Gst, EntityType, EntityId, PaymentOrderId, CorrelationId, At
            FROM dbo.LedgerEntry
            WHERE Stream = N'MEDICINE' AND At >= {start} AND At < {end}
            ORDER BY At DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> CreateSettlementAsync(SettlementCreateRequest request, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        request ??= new SettlementCreateRequest();
        if (!request.DryRun && !request.Confirm)
            return Fail(400, "VALIDATION", "Set confirm=true to commit a settlement run.");

        var open = await _context.Database.SqlQuery<OpenLedgerRow>($@"
            SELECT LedgerEntryId, Amount, EntityType, EntityId
            FROM dbo.LedgerEntry
            WHERE Direction = N'CREDIT' AND SettlementRunId IS NULL AND EntityType IN (N'Appointment', N'Pharmacy')").ToListAsync();

        if (request.DryRun)
            return Ok(new { success = true, dryRun = true, lineCount = open.Count, amount = open.Sum(x => x.Amount), data = open });

        var runId = await InsertAsync(
            @"INSERT INTO dbo.SettlementRun (Status, CreatedBy, CreatedAt) VALUES (N'COMMITTED', @By, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@By", caller.UserId),
            P("@At", DateTime.Now));

        foreach (var line in open)
        {
            var payeeType = line.EntityType == "Pharmacy" ? "Pharmacy" : "Doctor";
            var payeeId = int.TryParse(line.EntityId, out var parsed) ? parsed : 0;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SettlementLine (SettlementRunId, PayeeType, PayeeId, Amount, LedgerEntryId)
                VALUES ({runId}, {payeeType}, {payeeId}, {line.Amount}, {line.LedgerEntryId})");
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.Payout (PayeeType, PayeeId, Amount, Status, SettlementRunId, CreatedAt)
                VALUES ({payeeType}, {payeeId}, {line.Amount}, N'PENDING', {runId}, {DateTime.Now})");
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.LedgerEntry SET SettlementRunId = {runId} WHERE LedgerEntryId = {line.LedgerEntryId}");
        }

        return Ok(new { success = true, dryRun = false, data = new { settlementRunId = runId, lines = open.Count } });
    }

    public async Task<S4ActionResult> ListSettlementsAsync(S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var rows = await _context.Database.SqlQuery<SettlementRow>($@"
            SELECT SettlementRunId, Status, CreatedBy, CreatedAt FROM dbo.SettlementRun ORDER BY CreatedAt DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> SettlementDetailAsync(long id, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        if (id <= 0) return Fail(400, "VALIDATION", "Settlement id is required.");
        var run = await _context.Database.SqlQuery<SettlementRow>($@"
            SELECT SettlementRunId, Status, CreatedBy, CreatedAt FROM dbo.SettlementRun WHERE SettlementRunId = {id}").FirstOrDefaultAsync();
        if (run == null) return Fail(404, "NOT_FOUND", "Settlement run not found.");
        var lines = await _context.Database.SqlQuery<SettlementLineRow>($@"
            SELECT SettlementLineId, SettlementRunId, PayeeType, PayeeId, Amount, LedgerEntryId
            FROM dbo.SettlementLine WHERE SettlementRunId = {id}").ToListAsync();
        return Ok(new { success = true, data = new { run, lines } });
    }

    public async Task<S4ActionResult> RequestPayoutOtpAsync(long payoutId, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var payout = await LoadPayoutAsync(payoutId);
        if (payout == null) return Fail(404, "NOT_FOUND", "Payout not found.");
        if (!string.Equals(payout.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Only a pending payout can be approved.");
        var code = await IssueOtpAsync("PayoutApprove", "Payout", payoutId.ToString(), caller.UserId);
        return Ok(new { success = true, message = "OTP created for payout approval.", devCode = code });
    }

    public async Task<S4ActionResult> ApprovePayoutAsync(long payoutId, PayoutDecisionRequest request, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        if (!IsOtpShape(request?.Otp))
            return Fail(400, "VALIDATION", "A 6-digit OTP is required.");
        var payout = await LoadPayoutAsync(payoutId);
        if (payout == null) return Fail(404, "NOT_FOUND", "Payout not found.");
        if (!string.Equals(payout.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Only a pending payout can be approved.");
        var otp = await ConsumeOtpAsync("PayoutApprove", "Payout", payoutId.ToString(), request!.Otp);
        if (otp != null) return otp;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Payout
            SET Status = N'APPROVED', DecidedBy = {caller.UserId}, DecidedAt = {DateTime.Now}
            WHERE PayoutId = {payoutId}");
        return Ok(new { success = true, message = "Payout approved. Bank file dispatch is not sent by this API.", data = new { payoutId, status = "APPROVED" } });
    }

    public async Task<S4ActionResult> RejectPayoutAsync(long payoutId, PayoutDecisionRequest request, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        if (request == null || string.IsNullOrWhiteSpace(request.Reason))
            return Fail(400, "VALIDATION", "A reject reason is required.");
        var payout = await LoadPayoutAsync(payoutId);
        if (payout == null) return Fail(404, "NOT_FOUND", "Payout not found.");
        if (!string.Equals(payout.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Only a pending payout can be rejected.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Payout
            SET Status = N'REJECTED', RejectReason = {request.Reason.Trim()}, DecidedBy = {caller.UserId}, DecidedAt = {DateTime.Now}
            WHERE PayoutId = {payoutId}");
        return Ok(new { success = true, data = new { payoutId, status = "REJECTED" } });
    }

    public async Task<S4ActionResult> ListExceptionsAsync(string? status, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var filter = string.IsNullOrWhiteSpace(status) ? "OPEN" : status.Trim().ToUpperInvariant();
        if (filter is not ("OPEN" or "RESOLVED" or "RETRIED"))
            return Fail(400, "VALIDATION", "Status must be OPEN, RESOLVED, or RETRIED.");
        var rows = await _context.Database.SqlQuery<ExceptionRow>($@"
            SELECT PaymentExceptionId, PaymentOrderId, Kind, Detail, Status, CreatedAt
            FROM dbo.PaymentException
            WHERE Status = {filter}
            ORDER BY CreatedAt DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> ExceptionDetailAsync(long id, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var row = await LoadExceptionAsync(id);
        return row == null ? Fail(404, "NOT_FOUND", "Payment exception not found.") : Ok(new { success = true, data = row });
    }

    public async Task<S4ActionResult> RetryExceptionAsync(long id, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var row = await LoadExceptionAsync(id);
        if (row == null) return Fail(404, "NOT_FOUND", "Payment exception not found.");
        if (!string.Equals(row.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Only an open exception can be retried.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PaymentException SET Status = N'RETRIED' WHERE PaymentExceptionId = {id}");
        return Ok(new { success = true, message = "Marked for retry. Gateway capture is still applied only by a valid webhook.", data = new { id, status = "RETRIED" } });
    }

    public async Task<S4ActionResult> ResolveExceptionAsync(long id, ExceptionResolveRequest request, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        if (request == null || string.IsNullOrWhiteSpace(request.Note) || request.Note.Trim().Length < 3)
            return Fail(400, "VALIDATION", "A resolution note is required.");
        var row = await LoadExceptionAsync(id);
        if (row == null) return Fail(404, "NOT_FOUND", "Payment exception not found.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PaymentException
            SET Status = N'RESOLVED', ResolvedBy = {caller.UserId}, ResolvedAt = {DateTime.Now}, ResolutionNote = {request.Note.Trim()}
            WHERE PaymentExceptionId = {id}");
        return Ok(new { success = true, data = new { id, status = "RESOLVED" } });
    }

    public async Task<S4ActionResult> TaxReportAsync(DateTime? from, DateTime? to, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var range = DateRange(from, to, out var start, out var end);
        if (range != null) return range;
        var rows = await _context.Database.SqlQuery<LedgerRow>($@"
            SELECT LedgerEntryId, Stream, Direction, Amount, Gst, EntityType, EntityId, PaymentOrderId, CorrelationId, At
            FROM dbo.LedgerEntry
            WHERE At >= {start} AND At < {end}").ToListAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Stream,Direction,Amount,Gst,At");
        foreach (var row in rows)
            csv.Append(Csv(row.Stream)).Append(',').Append(Csv(row.Direction)).Append(',')
                .Append(row.Amount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Gst.ToString(CultureInfo.InvariantCulture)).Append(',')
                .AppendLine(row.At.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return Ok(new { success = true, gstTotal = rows.Sum(r => r.Gst), csv = csv.ToString(), data = rows });
    }

    public async Task<S4ActionResult> ListPayeesAsync(S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var rows = await _context.Database.SqlQuery<PayeeRow>($@"
            SELECT PayeeId, PayeeType, DoctorId, PharmacyId, AccountName, BankAccount, Ifsc, Pan, KycStatus
            FROM dbo.Payee ORDER BY PayeeId").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> UpdatePayeeAsync(int payeeId, PayeeUpdateRequest request, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        if (request == null) return Fail(400, "VALIDATION", "Body is required.");
        var payee = await _context.Database.SqlQuery<PayeeRow>($@"
            SELECT PayeeId, PayeeType, DoctorId, PharmacyId, AccountName, BankAccount, Ifsc, Pan, KycStatus
            FROM dbo.Payee WHERE PayeeId = {payeeId}").FirstOrDefaultAsync();
        if (payee == null) return Fail(404, "NOT_FOUND", "Payee not found.");

        var bankChanged = request.BankAccount != null && !string.Equals(request.BankAccount, payee.BankAccount, StringComparison.Ordinal);
        if (bankChanged)
        {
            if (!IsOtpShape(request.Otp))
                return Fail(400, "VALIDATION", "Bank changes require a 6-digit OTP.");
            var otp = await ConsumeOtpAsync("PayeeBank", "Payee", payeeId.ToString(), request.Otp!);
            if (otp != null) return otp;
        }
        if (request.Pan != null && request.Pan.Trim().Length is > 0 and not 10)
            return Fail(400, "VALIDATION", "PAN must be 10 characters.");

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Payee
            SET AccountName = COALESCE({request.AccountName}, AccountName),
                BankAccount = COALESCE({request.BankAccount}, BankAccount),
                Ifsc = COALESCE({request.Ifsc}, Ifsc),
                Pan = COALESCE({request.Pan}, Pan),
                KycStatus = N'UPDATED',
                UpdatedAt = {DateTime.Now}
            WHERE PayeeId = {payeeId}");
        return Ok(new { success = true, message = "Payee updated." });
    }

    public async Task<S4ActionResult> RequestPayeeBankOtpAsync(int payeeId, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var exists = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.Payee WHERE PayeeId = {payeeId}").FirstAsync();
        if (exists.Value == 0) return Fail(404, "NOT_FOUND", "Payee not found.");
        var code = await IssueOtpAsync("PayeeBank", "Payee", payeeId.ToString(), caller.UserId);
        return Ok(new { success = true, message = "OTP created for the bank change.", devCode = code });
    }

    public async Task<S4ActionResult> ClinicCollectionsAsync(DateTime? from, DateTime? to, int? doctorId, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        var range = DateRange(from, to, out var start, out var end);
        if (range != null) return range;
        var rows = await _context.Database.SqlQuery<OrderRow>($@"
            SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                   Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
            FROM dbo.PaymentOrder
            WHERE Status = N'COLLECTED'
              AND Method IN (N'CASH', N'UPI_OFFLINE', N'CARD_POS')
              AND CreatedAt >= {start} AND CreatedAt < {end}
              AND ({doctorId ?? 0} = 0 OR DoctorId = {doctorId ?? 0})
            ORDER BY CreatedAt DESC").ToListAsync();
        return Ok(new { success = true, total = rows.Sum(r => r.Amount), data = rows });
    }

    public async Task<S4ActionResult> TrailAsync(int? patientAppId, long? paymentOrderId, int? doctorId, S4Caller caller)
    {
        var deny = AccountOnly(caller);
        if (deny != null) return deny;
        if (patientAppId is null or <= 0 && paymentOrderId is null or <= 0 && doctorId is null or <= 0)
            return Fail(400, "VALIDATION", "Provide an appointment, payment, or doctor id.");
        var orders = await _context.Database.SqlQuery<OrderRow>($@"
            SELECT PaymentOrderId, Stream, PatientAppId, MedicineOrderId, DoctorId, PatientId, Amount, Currency,
                   Status, Method, GatewayOrderId, GatewayPaymentId, IdempotencyKey, CorrelationId, CreatedAt
            FROM dbo.PaymentOrder
            WHERE ({patientAppId ?? 0} = 0 OR PatientAppId = {patientAppId ?? 0})
              AND ({paymentOrderId ?? 0} = 0 OR PaymentOrderId = {paymentOrderId ?? 0})
              AND ({doctorId ?? 0} = 0 OR DoctorId = {doctorId ?? 0})
            ORDER BY CreatedAt DESC").ToListAsync();
        var events = await _context.Database.SqlQuery<ExceptionRow>($@"
            SELECT PaymentExceptionId, PaymentOrderId, Kind, Detail, Status, CreatedAt
            FROM dbo.PaymentException
            WHERE PaymentOrderId IN (
                SELECT PaymentOrderId FROM dbo.PaymentOrder
                WHERE ({patientAppId ?? 0} = 0 OR PatientAppId = {patientAppId ?? 0})
                  AND ({paymentOrderId ?? 0} = 0 OR PaymentOrderId = {paymentOrderId ?? 0})
                  AND ({doctorId ?? 0} = 0 OR DoctorId = {doctorId ?? 0}))").ToListAsync();
        return Ok(new { success = true, data = new { orders, exceptions = events } });
    }

    private async Task<PayoutRow?> LoadPayoutAsync(long id)
        => await _context.Database.SqlQuery<PayoutRow>($@"
            SELECT PayoutId, PayeeType, PayeeId, Amount, Status, SettlementRunId
            FROM dbo.Payout WHERE PayoutId = {id}").FirstOrDefaultAsync();

    private async Task<ExceptionRow?> LoadExceptionAsync(long id)
        => await _context.Database.SqlQuery<ExceptionRow>($@"
            SELECT PaymentExceptionId, PaymentOrderId, Kind, Detail, Status, CreatedAt
            FROM dbo.PaymentException WHERE PaymentExceptionId = {id}").FirstOrDefaultAsync();

    private async Task<string> IssueOtpAsync(string action, string entityType, string entityId, long userId)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        _context.OtpChallenges.Add(new OtpChallenge
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DestinationMasked = "account",
            OtpHash = SecurityTokenHash.Sha256Hex(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _ = userId;
        return code;
    }

    private async Task<S4ActionResult?> ConsumeOtpAsync(string action, string entityType, string entityId, string code)
    {
        var hash = SecurityTokenHash.Sha256Hex(code.Trim());
        var challenge = await _context.OtpChallenges
            .Where(c => c.Action == action && c.EntityType == entityType && c.EntityId == entityId && c.VerifiedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
        if (challenge == null)
            return Fail(400, "OTP_REQUIRED", "Request an OTP before this action.");
        if (challenge.LockedUntil != null && challenge.LockedUntil > DateTime.UtcNow)
            return Fail(429, "OTP_LOCKED", "Too many OTP attempts. Try again later.");
        if (challenge.ExpiresAt < DateTime.UtcNow)
            return Fail(400, "OTP_EXPIRED", "The OTP has expired.");
        if (!string.Equals(challenge.OtpHash, hash, StringComparison.OrdinalIgnoreCase))
        {
            challenge.AttemptCount += 1;
            if (challenge.AttemptCount >= 5)
                challenge.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();
            return Fail(400, "OTP_INVALID", "The OTP is not valid.");
        }
        challenge.VerifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return null;
    }

    private static bool IsOtpShape(string? otp)
        => !string.IsNullOrWhiteSpace(otp) && otp.Trim().Length == 6 && otp.Trim().All(char.IsDigit);

    private static string Csv(string? value)
    {
        var text = value ?? "";
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n'))
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }
}

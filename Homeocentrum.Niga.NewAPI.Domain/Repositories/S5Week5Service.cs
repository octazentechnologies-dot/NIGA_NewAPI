using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

/// <summary>
/// S5 Week 5 — SMS log, notifications, receipt email, and clinic reports.
/// Provider calls run only when the matching key is present in appsettings.json.
/// </summary>
public class S5Week5Service : IS5Week5Service
{
    private readonly NIGACentrumContext _context;
    private readonly IConfiguration _config;

    public S5Week5Service(NIGACentrumContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public Task<S4ActionResult> ListSmsTemplatesAsync(S4Caller caller) => Guard(async () =>
    {
        var deny = Admin(caller);
        if (deny != null) return deny;
        var rows = await _context.Database.SqlQuery<SmsTemplateRow>($@"
            SELECT SmsTemplateId, Code, Body, IsActive, CreatedAt
            FROM dbo.SmsTemplate ORDER BY Code").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> SaveSmsTemplateAsync(SmsTemplateWrite request, S4Caller caller) => Guard(async () =>
    {
        var deny = Admin(caller);
        if (deny != null) return deny;
        var code = (request?.Code ?? "").Trim().ToUpperInvariant();
        var body = (request?.Body ?? "").Trim();
        if (code.Length == 0 || body.Length == 0)
            return S4ActionResult.Fail(400, "VALIDATION", "Code and body are required.");
        if (request!.SmsTemplateId.HasValue && request.SmsTemplateId.Value > 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.SmsTemplate SET Code = {code}, Body = {body}, IsActive = {request.IsActive}
                WHERE SmsTemplateId = {request.SmsTemplateId.Value}");
        }
        else
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SmsTemplate (Code, Body, IsActive) VALUES ({code}, {body}, {request.IsActive})");
        }
        return await ListSmsTemplatesAsync(caller);
    });

    public Task<S4ActionResult> SendSmsAsync(SmsSendRequest request, S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsDoctor && !caller.IsReception)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Clinic staff can send SMS.");
        var mobile = Digits(request?.Mobile);
        if (mobile.Length < 10)
            return S4ActionResult.Fail(400, "VALIDATION", "A mobile number is required.");
        var opted = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.Patient
            WHERE REPLACE(REPLACE(ISNULL(MobileNo,''),' ',''),'-','') LIKE {'%' + mobile}
              AND SmsOptOut = 1").FirstAsync();
        if (opted.Value > 0)
            return S4ActionResult.Fail(409, "OPTED_OUT", "This patient has opted out of SMS.");

        var code = (request?.TemplateCode ?? "").Trim();
        var body = (request?.Body ?? "").Trim();
        if (body.Length == 0 && code.Length > 0)
        {
            var template = await _context.Database.SqlQuery<SmsTemplateRow>($@"
                SELECT SmsTemplateId, Code, Body, IsActive, CreatedAt
                FROM dbo.SmsTemplate WHERE Code = {code} AND IsActive = 1").FirstOrDefaultAsync();
            body = template?.Body ?? "";
        }
        if (body.Length == 0)
            return S4ActionResult.Fail(400, "VALIDATION", "Body or an active template code is required.");
        if (caller.DoctorId.HasValue && code.Length > 0)
        {
            var pref = await _context.Database.SqlQuery<CountRow>($@"
                SELECT TOP 1 CAST(CASE WHEN Enabled = 1 THEN 1 ELSE 0 END AS int) AS Value
                FROM dbo.DoctorSmsPreference
                WHERE DoctorId = {caller.DoctorId.Value} AND TemplateCode = {code}").FirstOrDefaultAsync();
            if (pref != null && pref.Value == 0)
                return S4ActionResult.Fail(409, "EVENT_OFF", "This doctor has turned off SMS for that event.");
        }

        var status = "LOGGED";
        var providerId = "";
        var authKey = _config["Sms:AuthKey"];
        var endpoint = _config["Sms:Endpoint"];
        if (!string.IsNullOrWhiteSpace(authKey) && !string.IsNullOrWhiteSpace(endpoint))
        {
            status = await PostProviderAsync(endpoint, authKey, mobile, body) ? "SENT" : "FAILED";
            providerId = status == "SENT" ? "provider" : "";
        }

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.SmsMessageLog (TemplateCode, Mobile, Body, Status, ProviderMessageId)
            VALUES ({code}, {mobile}, {body}, {status}, {providerId})");
        return S4ActionResult.Ok(new { success = true, status, mobile });
    });

    public Task<S4ActionResult> SmsHistoryAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsAccount)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Account or admin can read SMS history.");
        var rows = await _context.Database.SqlQuery<SmsLogRow>($@"
            SELECT TOP 200 SmsMessageLogId, TemplateCode, Mobile, Body, Status, At
            FROM dbo.SmsMessageLog ORDER BY SmsMessageLogId DESC").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> ListSmsEventsAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsDoctor)
            return S4ActionResult.Fail(403, "FORBIDDEN", "A doctor or admin can read SMS events.");
        var doctorId = caller.DoctorId ?? 0;
        var rows = await _context.Database.SqlQuery<SmsEventRow>($@"
            SELECT t.Code, t.Body, t.IsActive,
                   CAST(ISNULL(p.Enabled, 1) AS bit) AS Enabled
            FROM dbo.SmsTemplate t
            LEFT JOIN dbo.DoctorSmsPreference p
              ON p.TemplateCode = t.Code AND p.DoctorId = {doctorId}
            ORDER BY t.Code").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> SaveSmsPreferenceAsync(DoctorSmsPreferenceWrite request, S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsDoctor || !caller.DoctorId.HasValue)
            return S4ActionResult.Fail(403, "FORBIDDEN", "A doctor can change SMS events.");
        var code = (request?.TemplateCode ?? "").Trim().ToUpperInvariant();
        if (code.Length == 0)
            return S4ActionResult.Fail(400, "VALIDATION", "TemplateCode is required.");
        var enabled = request!.Enabled;
        var doctorId = caller.DoctorId.Value;
        var existing = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.DoctorSmsPreference
            WHERE DoctorId = {doctorId} AND TemplateCode = {code}").FirstAsync();
        if (existing.Value > 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.DoctorSmsPreference SET Enabled = {enabled}
                WHERE DoctorId = {doctorId} AND TemplateCode = {code}");
        }
        else
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.DoctorSmsPreference (DoctorId, TemplateCode, Enabled)
                VALUES ({doctorId}, {code}, {enabled})");
        }
        return await ListSmsEventsAsync(caller);
    });

    public Task<S4ActionResult> RegisterDeviceAsync(DeviceRegisterRequest request, S4Caller caller) => Guard(async () =>
    {
        if (caller.UserId <= 0)
            return S4ActionResult.Fail(401, "UNAUTHORIZED", "Sign in before registering a device.");
        var token = (request?.Token ?? "").Trim();
        if (token.Length < 8)
            return S4ActionResult.Fail(400, "VALIDATION", "A device token is required.");
        if (token.Length > 512) token = token.Substring(0, 512);
        var platform = (request?.Platform ?? "web").Trim().ToLowerInvariant();
        if (platform != "android" && platform != "ios" && platform != "web")
            platform = "web";
        var existing = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.DeviceToken
            WHERE UserId = {caller.UserId} AND Token = {token}").FirstAsync();
        if (existing.Value > 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.DeviceToken SET Platform = {platform}, UpdatedAt = {DateTime.Now}
                WHERE UserId = {caller.UserId} AND Token = {token}");
        }
        else
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.DeviceToken (UserId, Token, Platform)
                VALUES ({caller.UserId}, {token}, {platform})");
        }
        return S4ActionResult.Ok(new { success = true, platform });
    });

    public Task<S4ActionResult> WhatsAppReceiptAsync(WhatsAppReceiptRequest request) => Guard(async () =>
    {
        var id = (request?.MetaMessageId ?? "").Trim();
        if (id.Length == 0)
            return S4ActionResult.Fail(400, "VALIDATION", "MetaMessageId is required.");
        var status = (request?.Status ?? "delivered").Trim();
        var payload = (request?.Payload ?? "").Trim();
        if (payload.Length > 4000) payload = payload.Substring(0, 4000);
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.WhatsAppMessageLog
            SET DeliveryStatus = {status}, ReceiptAt = {DateTime.Now}, ReceiptPayload = {payload}
            WHERE MetaMessageId = {id}");
        return S4ActionResult.Ok(new { success = true, updated });
    });

    public Task<S4ActionResult> WhatsAppBulkAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin) return Admin(caller)!;
        var rows = await _context.Database.SqlQuery<BulkRow>($@"
            SELECT TOP 100 WhatsAppMessageLogId AS Id, MobileNumber, DeliveryStatus, SendStatus, CreatedDate
            FROM dbo.WhatsAppMessageLog
            WHERE IsBulk = 1 AND ISNULL(DeleteStatus,0) = 0
            ORDER BY WhatsAppMessageLogId DESC").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> SendNotificationAsync(NotificationSendRequest request, S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsDoctor)
            return S4ActionResult.Fail(403, "FORBIDDEN", "A doctor or admin can send a notification.");
        var title = (request?.Title ?? "").Trim();
        var body = (request?.Body ?? "").Trim();
        if (request == null || request.UserId <= 0 || title.Length == 0)
            return S4ActionResult.Fail(400, "VALIDATION", "UserId and title are required.");
        var push = await DeliverPushAsync(request.UserId, title, body);
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.AppNotification (UserId, Title, Body, PushStatus)
            VALUES ({request.UserId}, {title}, {body}, {push})");
        return S4ActionResult.Ok(new { success = true, pushStatus = push });
    });

    public Task<S4ActionResult> ListNotificationsAsync(S4Caller caller) => Guard(async () =>
    {
        var rows = await _context.Database.SqlQuery<NoteRow>($@"
            SELECT TOP 100 AppNotificationId, Title, Body, IsRead, PushStatus, CreatedAt
            FROM dbo.AppNotification
            WHERE UserId = {caller.UserId}
            ORDER BY AppNotificationId DESC").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> PatchNotificationAsync(long id, bool isRead, S4Caller caller) => Guard(async () =>
    {
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppNotification SET IsRead = {isRead}
            WHERE AppNotificationId = {id} AND UserId = {caller.UserId}");
        if (updated == 0)
            return S4ActionResult.Fail(404, "NOT_FOUND", "Notification not found.");
        return S4ActionResult.Ok(new { success = true, appNotificationId = id, isRead });
    });

    public Task<S4ActionResult> SendReceiptEmailAsync(long paymentOrderId, S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsAccount)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Account can send a receipt email.");
        var order = await _context.Database.SqlQuery<PayRow>($@"
            SELECT PaymentOrderId, Amount, Currency, Status
            FROM dbo.PaymentOrder WHERE PaymentOrderId = {paymentOrderId}").FirstOrDefaultAsync();
        if (order == null)
            return S4ActionResult.Fail(404, "NOT_FOUND", "Payment not found.");
        if (order.Status is not ("CAPTURED" or "COLLECTED" or "PAID"))
            return S4ActionResult.Fail(409, "NOT_PAID", "A receipt email is sent after the payment is paid.");
        var to = await PatientEmailAsync(paymentOrderId);
        var subject = "Homeocentrum receipt " + order.PaymentOrderId;
        var body = $"Payment {order.PaymentOrderId} is {order.Status} for {order.Amount} {order.Currency}.";
        var status = "LOGGED";
        var smtp = _config.GetSection("smtp").Get<SmtpSettingsModel>();
        if (to.Contains('@') && smtp != null && !string.IsNullOrWhiteSpace(smtp.host))
        {
            var sender = new EmailSenderService();
            var sent = sender.SendMail(new EmailSenderModel
            {
                ToAddress = to,
                Subject = subject,
                Body = body,
                isHtml = false
            }, smtp);
            status = sent ? "SENT" : "FAILED";
        }
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.EmailMessageLog (ToAddress, Subject, Body, Status, PaymentOrderId)
            VALUES ({to}, {subject}, {body}, {status}, {paymentOrderId})");
        return S4ActionResult.Ok(new { success = true, status, paymentOrderId });
    });

    public Task<S4ActionResult> EmailHistoryAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsAccount)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Account can read email history.");
        var rows = await _context.Database.SqlQuery<EmailLogRow>($@"
            SELECT TOP 100 EmailMessageLogId, ToAddress, Subject, Status, PaymentOrderId, At
            FROM dbo.EmailMessageLog ORDER BY EmailMessageLogId DESC").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> OverviewAsync(S4Caller caller) => Guard(async () =>
    {
        var deny = Admin(caller);
        if (deny != null) return deny;
        var appointments = await CountAsync($@"SELECT COUNT(1) AS Value FROM dbo.PatientAppointment WHERE ISNULL(DeleteStatus,0)=0");
        var orders = await CountAsync($@"SELECT COUNT(1) AS Value FROM dbo.MedicineOrder");
        var openFollow = await CountAsync($@"SELECT COUNT(1) AS Value FROM dbo.FollowUpTask WHERE Status = N'OPEN'");
        return S4ActionResult.Ok(new
        {
            success = true,
            data = new { appointments, medicineOrders = orders, openFollowUps = openFollow }
        });
    });

    public Task<S4ActionResult> FollowUpDueAsync(S4Caller caller) => Guard(async () =>
    {
        if (!CanReadClinic(caller))
            return S4ActionResult.Fail(403, "FORBIDDEN", "Clinic staff can read follow-ups.");
        var today = DateTime.Today;
        List<FollowRow> rows;
        if (caller.IsAdmin || !caller.DoctorId.HasValue)
        {
            rows = await _context.Database.SqlQuery<FollowRow>($@"
                SELECT TOP 200 t.FollowUpTaskId, t.Title, t.DueDate, t.Status, p.PatientId, p.DoctorId
                FROM dbo.FollowUpTask t
                INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
                WHERE t.Status = N'OPEN' AND t.DueDate <= {today}
                ORDER BY t.DueDate").ToListAsync();
        }
        else
        {
            var doctorId = caller.DoctorId.Value;
            rows = await _context.Database.SqlQuery<FollowRow>($@"
                SELECT TOP 200 t.FollowUpTaskId, t.Title, t.DueDate, t.Status, p.PatientId, p.DoctorId
                FROM dbo.FollowUpTask t
                INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
                WHERE t.Status = N'OPEN' AND t.DueDate <= {today} AND p.DoctorId = {doctorId}
                ORDER BY t.DueDate").ToListAsync();
        }
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> FollowUpSummaryAsync(S4Caller caller) => Guard(async () =>
    {
        if (!CanReadClinic(caller))
            return S4ActionResult.Fail(403, "FORBIDDEN", "Clinic staff can read follow-ups.");
        var rows = await _context.Database.SqlQuery<GroupRow>($@"
            SELECT Status AS Name, COUNT(1) AS Cnt FROM dbo.FollowUpTask GROUP BY Status").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> ClinicPerformanceAsync(DateTime? from, DateTime? to, S4Caller caller) => Guard(async () =>
    {
        if (!CanReadClinic(caller))
            return S4ActionResult.Fail(403, "FORBIDDEN", "Clinic staff can read performance.");
        var start = (from ?? DateTime.Today.AddDays(-30)).Date;
        var end = (to ?? DateTime.Today).Date.AddDays(1);
        var visits = await _context.Database.SqlQuery<GroupRow>($@"
            SELECT ISNULL(NULLIF(ConsultMode,''), ISNULL(NULLIF(VisitType,''), N'Unknown')) AS Name, COUNT(1) AS Cnt
            FROM dbo.PatientAppointment
            WHERE ISNULL(DeleteStatus,0)=0 AND AppointmentDate >= {start} AND AppointmentDate < {end}
            GROUP BY ISNULL(NULLIF(ConsultMode,''), ISNULL(NULLIF(VisitType,''), N'Unknown'))").ToListAsync();
        var paid = await _context.Database.SqlQuery<MoneyRow>($@"
            SELECT ISNULL(SUM(Amount),0) AS Amount, COUNT(1) AS Cnt
            FROM dbo.PaymentOrder
            WHERE Status = N'PAID' AND CreatedAt >= {start} AND CreatedAt < {end}").FirstAsync();
        return S4ActionResult.Ok(new { success = true, from = start, to = end.AddDays(-1), visits, paid });
    });

    public Task<S4ActionResult> ExportReconciliationAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsAccount)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Account can export reconciliation.");
        var rows = await _context.Database.SqlQuery<PayRow>($@"
            SELECT TOP 500 PaymentOrderId, Amount, Currency, Status
            FROM dbo.PaymentOrder ORDER BY PaymentOrderId DESC").ToListAsync();
        var csv = Csv(new[] { "PaymentOrderId,Amount,Currency,Status" }
            .Concat(rows.Select(r => $"{r.PaymentOrderId},{r.Amount},{r.Currency},{r.Status}")));
        return S4ActionResult.Ok(new { success = true, fileName = "reconciliation.csv", csv });
    });

    public Task<S4ActionResult> ExportSettlementsAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsAccount)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Account can export settlements.");
        var rows = await _context.Database.SqlQuery<RunRow>($@"
            SELECT SettlementRunId, Status, CreatedAt FROM dbo.SettlementRun ORDER BY SettlementRunId DESC").ToListAsync();
        var csv = Csv(new[] { "SettlementRunId,Status,CreatedAt" }
            .Concat(rows.Select(r => $"{r.SettlementRunId},{r.Status},{r.CreatedAt:yyyy-MM-dd}")));
        return S4ActionResult.Ok(new { success = true, fileName = "settlements.csv", csv });
    });

    public Task<S4ActionResult> ExportPayoutsAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsAccount)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Account can export payouts.");
        var rows = await _context.Database.SqlQuery<PayoutRow>($@"
            SELECT PayoutId, PayeeType, PayeeId, Amount, Status, SettlementRunId
            FROM dbo.Payout ORDER BY PayoutId DESC").ToListAsync();
        var csv = Csv(new[] { "PayoutId,PayeeType,PayeeId,Amount,Status,SettlementRunId" }
            .Concat(rows.Select(r => $"{r.PayoutId},{r.PayeeType},{r.PayeeId},{r.Amount},{r.Status},{r.SettlementRunId}")));
        return S4ActionResult.Ok(new { success = true, fileName = "payouts.csv", csv });
    });

    public Task<S4ActionResult> MedicineReportAsync(S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsAdmin && !caller.IsAccount && !caller.IsPharmacy)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Account or pharmacy can read the medicine report.");
        var rows = await _context.Database.SqlQuery<GroupRow>($@"
            SELECT Status AS Name, COUNT(1) AS Cnt FROM dbo.MedicineOrder GROUP BY Status").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> EarningsBucketsAsync(DateTime? from, DateTime? to, S4Caller caller) => Guard(async () =>
    {
        if (!caller.IsDoctor && !caller.IsAdmin)
            return S4ActionResult.Fail(403, "FORBIDDEN", "A doctor can read earnings buckets.");
        var start = (from ?? DateTime.Today.AddMonths(-6)).Date;
        var end = (to ?? DateTime.Today).Date.AddDays(1);
        List<BucketRow> rows;
        if (caller.IsAdmin && !caller.DoctorId.HasValue)
        {
            rows = await _context.Database.SqlQuery<BucketRow>($@"
                SELECT CONVERT(char(7), CreatedAt, 120) AS Bucket, ISNULL(SUM(Amount),0) AS Amount, COUNT(1) AS Cnt
                FROM dbo.PaymentOrder
                WHERE Status = N'PAID' AND CreatedAt >= {start} AND CreatedAt < {end}
                GROUP BY CONVERT(char(7), CreatedAt, 120)
                ORDER BY Bucket").ToListAsync();
        }
        else
        {
            var doctorId = caller.DoctorId ?? 0;
            rows = await _context.Database.SqlQuery<BucketRow>($@"
                SELECT CONVERT(char(7), CreatedAt, 120) AS Bucket, ISNULL(SUM(Amount),0) AS Amount, COUNT(1) AS Cnt
                FROM dbo.PaymentOrder
                WHERE Status = N'PAID' AND DoctorId = {doctorId} AND CreatedAt >= {start} AND CreatedAt < {end}
                GROUP BY CONVERT(char(7), CreatedAt, 120)
                ORDER BY Bucket").ToListAsync();
        }
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> ExportUsersAsync(S4Caller caller) => Guard(async () =>
    {
        var deny = Admin(caller);
        if (deny != null) return deny;
        var roles = await _context.RoleMasters.AsNoTracking()
            .Where(r => !r.DeleteStatus)
            .Select(r => new { r.RoleId, r.RoleName })
            .ToListAsync();
        var doctors = await _context.Doctors.AsNoTracking()
            .Where(d => d.UserId != null && !d.DeleteStatus)
            .Select(d => new { d.UserId, d.VerificationStatus })
            .ToListAsync();
        var users = await _context.UserMasters.AsNoTracking()
            .Where(u => !u.DeleteStatus)
            .OrderBy(u => u.UserId)
            .Select(u => new { u.UserId, u.UserName, u.EmailId, u.MobileNo, u.RoleId, u.UserStatus })
            .Take(2000)
            .ToListAsync();
        var lines = new List<string> { "UserId,UserName,EmailId,MobileNo,Role,UserStatus,VerificationStatus" };
        foreach (var user in users)
        {
            var role = roles.FirstOrDefault(r => r.RoleId == user.RoleId)?.RoleName ?? "";
            var verification = doctors.FirstOrDefault(d => d.UserId.HasValue && d.UserId.Value == user.UserId)?.VerificationStatus ?? "";
            lines.Add($"{user.UserId},{CsvCell(user.UserName)},{CsvCell(user.EmailId)},{CsvCell(user.MobileNo)},{CsvCell(role)},{user.UserStatus},{CsvCell(verification)}");
        }
        return S4ActionResult.Ok(new { success = true, fileName = "users.csv", csv = Csv(lines) });
    });

    public Task<S4ActionResult> ImportUsersAsync(UserImportRequest request, S4Caller caller) => Guard(async () =>
    {
        var deny = Admin(caller);
        if (deny != null) return deny;
        var incoming = request?.Users ?? new List<UserImportRow>();
        var names = incoming
            .Select(u => (u.UserName ?? "").Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToList();
        var existing = await _context.UserMasters.AsNoTracking()
            .Where(u => names.Contains(u.UserName))
            .Select(u => u.UserName)
            .ToListAsync();
        var known = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        return S4ActionResult.Ok(new
        {
            success = true,
            created = 0,
            message = "Import does not create passwords. Unknown names are listed only.",
            existing = names.Where(known.Contains).ToList(),
            unknown = names.Where(n => !known.Contains(n)).ToList()
        });
    });

    public Task<S4ActionResult> SecurityPostureAsync(S4Caller caller)
    {
        var deny = Admin(caller);
        if (deny != null) return Task.FromResult(deny);
        return Task.FromResult(S4ActionResult.Ok(new
        {
            success = true,
            directoryBrowsingEnabled = false,
            secretsStoredIn = "appsettings.json",
            keyVaultConfigured = !string.IsNullOrWhiteSpace(_config["KeyVault:VaultUri"]),
            smsConfigured = !string.IsNullOrWhiteSpace(_config["Sms:AuthKey"]),
            fcmConfigured = !string.IsNullOrWhiteSpace(_config["Fcm:ServerKey"]),
            message = "Live keys are not rotated by this API. Change them in appsettings.json."
        }));
    }

    private async Task<S4ActionResult> Guard(Func<Task<S4ActionResult>> work)
    {
        try
        {
            return await work();
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return S4ActionResult.Fail(503, "SCHEMA", "Run ScriptsAndFiles/S5_Week5/Database scripts/01_S5_Week5_Schema.sql.");
        }
    }

    private async Task<int> CountAsync(FormattableString sql)
    {
        var row = await _context.Database.SqlQuery<CountRow>(sql).FirstAsync();
        return row.Value;
    }

    private static S4ActionResult? Admin(S4Caller caller)
        => caller.IsAdmin ? null : S4ActionResult.Fail(403, "FORBIDDEN", "Admin is required.");

    private static bool CanReadClinic(S4Caller caller)
        => caller.IsAdmin || caller.IsDoctor || caller.IsReception || caller.IsAccount;

    private static string Digits(string? value)
    {
        var raw = value ?? "";
        var chars = raw.Where(char.IsDigit).ToArray();
        var text = new string(chars);
        return text.Length > 10 ? text.Substring(text.Length - 10) : text;
    }

    private async Task<string> PatientEmailAsync(long paymentOrderId)
    {
        var row = await _context.Database.SqlQuery<TextRow>($@"
            SELECT TOP 1 ISNULL(pt.Email, N'') AS Value
            FROM dbo.PaymentOrder po
            LEFT JOIN dbo.Patient pt ON pt.PatientID = po.PatientId
            WHERE po.PaymentOrderId = {paymentOrderId}").FirstOrDefaultAsync();
        return (row?.Value ?? "").Trim();
    }

    /// <summary>COM-03.04 — FCM for android/web tokens when a server key is set. iOS stays logged until an APNs key exists.</summary>
    private async Task<string> DeliverPushAsync(long userId, string title, string body)
    {
        var serverKey = _config["Fcm:ServerKey"];
        var tokens = await _context.Database.SqlQuery<DeviceRow>($@"
            SELECT Token, Platform FROM dbo.DeviceToken WHERE UserId = {userId}").ToListAsync();
        if (tokens.Count == 0 || string.IsNullOrWhiteSpace(serverKey))
            return "LOGGED";
        var attempted = 0;
        var sent = 0;
        foreach (var token in tokens)
        {
            var platform = (token.Platform ?? "").ToLowerInvariant();
            if (platform == "ios")
                continue;
            attempted++;
            if (await PostFcmAsync(serverKey, token.Token, title, body))
                sent++;
        }
        if (attempted == 0)
            return "LOGGED";
        return sent > 0 ? "SENT" : "FAILED";
    }

    private static async Task<bool> PostFcmAsync(string serverKey, string token, string title, string body)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "key=" + serverKey);
            var payload = "{\"to\":\"" + token.Replace("\"", "") + "\",\"notification\":{\"title\":\""
                + title.Replace("\"", "") + "\",\"body\":\"" + body.Replace("\"", "") + "\"}}";
            var response = await client.PostAsync(
                "https://fcm.googleapis.com/fcm/send",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> PostProviderAsync(string endpoint, string authKey, string mobile, string body)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authKey);
            var payload = new StringContent(
                "{\"mobile\":\"" + mobile + "\",\"message\":\"" + body.Replace("\"", "") + "\"}",
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync(endpoint, payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string Csv(IEnumerable<string> lines) => string.Join("\n", lines);

    private static string CsvCell(string? value)
    {
        var text = value ?? "";
        if (text.Contains(',') || text.Contains('"'))
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        return text;
    }

    private sealed class CountRow { public int Value { get; set; } }
    private sealed class TextRow { public string Value { get; set; } = ""; }
    private sealed class DeviceRow
    {
        public string Token { get; set; } = "";
        public string Platform { get; set; } = "";
    }
    private sealed class SmsEventRow
    {
        public string Code { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsActive { get; set; }
        public bool Enabled { get; set; }
    }
    private sealed class SmsTemplateRow
    {
        public int SmsTemplateId { get; set; }
        public string Code { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    private sealed class SmsLogRow
    {
        public long SmsMessageLogId { get; set; }
        public string? TemplateCode { get; set; }
        public string Mobile { get; set; } = "";
        public string Body { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime At { get; set; }
    }
    private sealed class NoteRow
    {
        public long AppNotificationId { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsRead { get; set; }
        public string PushStatus { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
    private sealed class EmailLogRow
    {
        public long EmailMessageLogId { get; set; }
        public string ToAddress { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Status { get; set; } = "";
        public long? PaymentOrderId { get; set; }
        public DateTime At { get; set; }
    }
    private sealed class PayRow
    {
        public long PaymentOrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "";
        public string Status { get; set; } = "";
    }
    private sealed class FollowRow
    {
        public int FollowUpTaskId { get; set; }
        public string Title { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "";
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
    }
    private sealed class GroupRow
    {
        public string Name { get; set; } = "";
        public int Cnt { get; set; }
    }
    private sealed class MoneyRow
    {
        public decimal Amount { get; set; }
        public int Cnt { get; set; }
    }
    private sealed class RunRow
    {
        public long SettlementRunId { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
    private sealed class PayoutRow
    {
        public long PayoutId { get; set; }
        public string PayeeType { get; set; } = "";
        public int PayeeId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public long? SettlementRunId { get; set; }
    }
    private sealed class BucketRow
    {
        public string Bucket { get; set; } = "";
        public decimal Amount { get; set; }
        public int Cnt { get; set; }
    }
    private sealed class BulkRow
    {
        public int Id { get; set; }
        public string? MobileNumber { get; set; }
        public string? DeliveryStatus { get; set; }
        public bool SendStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

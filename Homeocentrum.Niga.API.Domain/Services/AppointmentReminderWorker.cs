using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Data;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Sends appointment reminder SMS the day before the visit (once per appointment).
/// Uses NotificationOutbox EventType APPT_REMIND_{patientAppId} as a dedupe marker.
/// </summary>
public sealed class AppointmentReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<SmsOptions> _sms;
    private readonly ILogger<AppointmentReminderWorker> _logger;

    public AppointmentReminderWorker(
        IServiceScopeFactory scopes,
        IOptions<SmsOptions> sms,
        ILogger<AppointmentReminderWorker> logger)
    {
        _scopes = scopes;
        _sms = sms;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger first run so app startup is not blocked.
        try { await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Appointment reminder sweep failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        var options = _sms.Value ?? new SmsOptions();
        if (!SmsOtpPolicy.RealSmsLive(options))
            return;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var notify = scope.ServiceProvider.GetRequiredService<IAppointmentRescheduleNotifier>();

        var tomorrow = DateTime.Today.AddDays(1);
        var dayAfter = tomorrow.AddDays(1);

        var rows = await db.Database.SqlQuery<ReminderCandidate>($@"
            SELECT TOP 40
                a.PatientAppId,
                a.AppointmentDate,
                CONVERT(varchar(5), a.AppointmentTime, 108) AS AppointmentTime,
                p.MobileNo,
                CAST(ISNULL(p.IsWhatsAppOptIn, 0) AS bit) AS WhatsAppOptIn,
                LTRIM(RTRIM(CONCAT(ISNULL(d.FirstName,''), ' ', ISNULL(d.MiddleName,''), ' ', ISNULL(d.LastName,'')))) AS DoctorName
            FROM dbo.PatientAppointment a
            INNER JOIN dbo.Patient p ON p.PatientId = a.PatientId
            INNER JOIN dbo.Doctor d ON d.DoctorId = a.DoctorId
            WHERE a.DeleteStatus = 0
              AND a.AppointmentDate >= {tomorrow} AND a.AppointmentDate < {dayAfter}
              AND ISNULL(a.Status, N'') NOT IN (N'CANCELLED', N'Cancelled', N'COMPLETED', N'Completed', N'NO_SHOW', N'NoShow')
              AND ISNULL(p.SmsOptOut, 0) = 0
              AND LEN(REPLACE(REPLACE(ISNULL(p.MobileNo,''),' ',''),'-','')) >= 10
              AND NOT EXISTS (
                    SELECT 1 FROM dbo.NotificationOutbox o
                    WHERE o.Channel = N'SMS'
                      AND o.EventType = CONCAT(N'APPT_REMIND_', a.PatientAppId)
                      AND o.Status IN (N'SENT', N'QUEUED', N'PENDING', N'PENDING_KEYS'))
            ORDER BY a.AppointmentTime").ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            var dateLabel = row.AppointmentDate.ToString("dd MMM yyyy");
            var timeLabel = string.IsNullOrWhiteSpace(row.AppointmentTime) ? "" : row.AppointmentTime.Trim();
            var doctor = string.IsNullOrWhiteSpace(row.DoctorName) ? "your doctor" : row.DoctorName.Trim();
            var notice = await notify.NotifyReminderAsync(
                row.MobileNo,
                row.WhatsAppOptIn,
                doctor,
                dateLabel,
                timeLabel,
                cancellationToken);

            var status = notice.Sms == "sent" ? "SENT" : "QUEUED";
            var body = $"Reminder: Homeocentrum appointment with {doctor} on {dateLabel} at {timeLabel}. See you soon.";
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.NotificationOutbox (Channel, EventType, Destination, Body, Status, LastError, CreatedAt)
                VALUES (N'SMS', {("APPT_REMIND_" + row.PatientAppId)}, {row.MobileNo}, {body}, {status}, NULL, {DateTime.Now})",
                cancellationToken);
        }

        if (rows.Count > 0)
            _logger.LogInformation("Appointment reminders processed: {Count}", rows.Count);

        await SendPatientDiaryRemindersAsync(
            db,
            scope.ServiceProvider.GetRequiredService<ISmsSender>(),
            scope.ServiceProvider.GetRequiredService<ISmsTemplateRenderer>(),
            cancellationToken);
    }

    private async Task SendPatientDiaryRemindersAsync(
        NIGACentrumContext db,
        ISmsSender sms,
        ISmsTemplateRenderer templates,
        CancellationToken cancellationToken)
    {
        // Patient self-reminders with NotifyPush — deliver as SMS within a 2-hour due window.
        var from = DateTime.Now.AddMinutes(-30);
        var to = DateTime.Now.AddHours(2);
        var diary = await db.Database.SqlQuery<DiaryReminderRow>($@"
            SELECT TOP 20
                r.PatientReminderId,
                r.Title,
                r.DueAt,
                p.MobileNo
            FROM dbo.PatientReminder r
            INNER JOIN dbo.Patient p ON p.PatientId = r.PatientId
            WHERE r.IsDone = 0
              AND ISNULL(r.NotifyPush, 0) = 1
              AND r.DueAt >= {from} AND r.DueAt < {to}
              AND ISNULL(p.SmsOptOut, 0) = 0
              AND LEN(REPLACE(REPLACE(ISNULL(p.MobileNo,''),' ',''),'-','')) >= 10
              AND NOT EXISTS (
                    SELECT 1 FROM dbo.NotificationOutbox o
                    WHERE o.Channel = N'SMS'
                      AND o.EventType = CONCAT(N'DIARY_REMIND_', r.PatientReminderId)
                      AND o.Status IN (N'SENT', N'QUEUED', N'PENDING', N'PENDING_KEYS'))
            ORDER BY r.DueAt").ToListAsync(cancellationToken);

        foreach (var row in diary)
        {
            var body = await templates.RenderAsync("REMINDER", new Dictionary<string, string?>
            {
                ["title"] = row.Title,
                ["due"] = row.DueAt.ToString("dd MMM yyyy HH:mm")
            }, cancellationToken);
            var sent = await sms.SendAsync(row.MobileNo ?? "", body, cancellationToken);
            var status = sent ? "SENT" : "QUEUED";
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.NotificationOutbox (Channel, EventType, Destination, Body, Status, LastError, CreatedAt)
                VALUES (N'SMS', {("DIARY_REMIND_" + row.PatientReminderId)}, {row.MobileNo}, {body}, {status}, NULL, {DateTime.Now})",
                cancellationToken);
        }
    }

    private sealed class ReminderCandidate
    {
        public int PatientAppId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public string? MobileNo { get; set; }
        public bool WhatsAppOptIn { get; set; }
        public string? DoctorName { get; set; }
    }

    private sealed class DiaryReminderRow
    {
        public int PatientReminderId { get; set; }
        public string Title { get; set; } = "";
        public DateTime DueAt { get; set; }
        public string? MobileNo { get; set; }
    }
}

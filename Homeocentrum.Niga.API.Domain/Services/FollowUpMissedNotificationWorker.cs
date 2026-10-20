using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Helpers;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// When a follow-up due date has passed and the task is still OPEN (missed),
/// notify the doctor in-app + FCM once per task (dedupe via NotificationOutbox).
/// Also notifies when a clinic visit is marked NOT ARRIVED for a patient with an open follow-up due today or earlier.
/// </summary>
public sealed class FollowUpMissedNotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<FollowUpMissedNotificationWorker> _logger;

    public FollowUpMissedNotificationWorker(
        IServiceScopeFactory scopes,
        ILogger<FollowUpMissedNotificationWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepMissedFollowUpsAsync(stoppingToken);
                await SweepMissedAppointmentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Follow-up missed notification sweep failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task SweepMissedFollowUpsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationSender>();

        var today = DateTime.Today;
        var rows = await db.Database.SqlQuery<MissedFollowUpRow>($@"
            SELECT TOP 40
                t.FollowUpTaskId,
                t.Title,
                t.DueDate,
                p.PatientId,
                p.DoctorId,
                d.UserId AS DoctorUserId,
                LTRIM(RTRIM(ISNULL(pt.PatientName, N''))) AS PatientName
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
            INNER JOIN dbo.Doctor d ON d.DoctorId = p.DoctorId
            LEFT JOIN dbo.Patient pt ON pt.PatientID = p.PatientId
            WHERE t.Status = N'OPEN'
              AND t.DueDate < {today}
              AND d.UserId IS NOT NULL
              AND NOT EXISTS (
                    SELECT 1 FROM dbo.NotificationOutbox o
                    WHERE o.EventType = CONCAT(N'FOLLOW_UP_MISS_', t.FollowUpTaskId)
                      AND o.Status IN (N'SENT', N'QUEUED', N'PENDING', N'LOGGED'))
            ORDER BY t.DueDate").ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (!row.DoctorUserId.HasValue || row.DoctorUserId.Value <= 0)
                continue;

            var patientLabel = string.IsNullOrWhiteSpace(row.PatientName)
                ? $"Patient #{row.PatientId}"
                : row.PatientName.Trim();
            var dueLabel = row.DueDate.ToString("dd MMM yyyy");
            var title = "Follow-up missed";
            var body = $"{patientLabel}: {row.Title} was due {dueLabel} and is still open.";
            var deepLink = "/doctor/follow-ups";
            var userId = (long)row.DoctorUserId.Value;

            var pushResult = await push.SendToUserAsync(
                userId,
                title,
                body,
                new Dictionary<string, string>
                {
                    ["type"] = "FOLLOW_UP_MISSED",
                    ["followUpTaskId"] = row.FollowUpTaskId.ToString(),
                    ["deepLink"] = deepLink,
                },
                "FOLLOW_UP_MISSED",
                cancellationToken);

            var pushStatus = string.IsNullOrWhiteSpace(pushResult.Status) ? "LOGGED" : pushResult.Status.ToUpperInvariant();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.AppNotification (UserId, Title, Body, PushStatus, Type, DeepLink)
                VALUES ({userId}, {title}, {body}, {pushStatus}, N'FOLLOW_UP_MISSED', {deepLink})",
                cancellationToken);

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.NotificationOutbox (Channel, EventType, Destination, Body, Status, LastError, CreatedAt)
                VALUES (N'PUSH', {("FOLLOW_UP_MISS_" + row.FollowUpTaskId)}, {userId.ToString()}, {body}, N'SENT', NULL, {DateTime.Now})",
                cancellationToken);
        }

        if (rows.Count > 0)
            _logger.LogInformation("Follow-up missed notifications sent: {Count}", rows.Count);
    }

    /// <summary>
    /// Clinic visits marked NOT ARRIVED (missed) — notify the treating doctor once per appointment.
    /// </summary>
    private async Task SweepMissedAppointmentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationSender>();

        var from = DateTime.Today.AddDays(-2);
        var to = DateTime.Today.AddDays(1);

        var rows = await db.Database.SqlQuery<MissedAppointmentRow>($@"
            SELECT TOP 40
                a.PatientAppId,
                a.DoctorId,
                d.UserId AS DoctorUserId,
                a.AppointmentDate,
                CONVERT(varchar(5), a.AppointmentTime, 108) AS AppointmentTime,
                LTRIM(RTRIM(ISNULL(pt.PatientName, N''))) AS PatientName
            FROM dbo.PatientAppointment a
            INNER JOIN dbo.Doctor d ON d.DoctorId = a.DoctorId
            LEFT JOIN dbo.Patient pt ON pt.PatientID = a.PatientId
            WHERE ISNULL(a.DeleteStatus, 0) = 0
              AND a.AppointmentDate >= {from} AND a.AppointmentDate < {to}
              AND UPPER(ISNULL(a.Status, N'')) IN (N'NOT ARRIVED', N'NO_SHOW', N'NOSHOW')
              AND d.UserId IS NOT NULL
              AND (
                    EXISTS (
                        SELECT 1 FROM dbo.FollowUpPlan fp
                        INNER JOIN dbo.FollowUpTask ft ON ft.FollowUpPlanId = fp.FollowUpPlanId
                        WHERE fp.PatientId = a.PatientId AND fp.DoctorId = a.DoctorId
                          AND ft.Status = N'OPEN' AND ft.DueDate <= a.AppointmentDate)
                    OR UPPER(ISNULL(a.VisitType, N'')) LIKE N'%FOLLOW%'
                  )
              AND NOT EXISTS (
                    SELECT 1 FROM dbo.NotificationOutbox o
                    WHERE o.EventType = CONCAT(N'APPT_MISS_', a.PatientAppId)
                      AND o.Status IN (N'SENT', N'QUEUED', N'PENDING', N'LOGGED'))
            ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC").ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (!row.DoctorUserId.HasValue || row.DoctorUserId.Value <= 0)
                continue;

            var patientLabel = string.IsNullOrWhiteSpace(row.PatientName)
                ? "A patient"
                : row.PatientName.Trim();
            var when = NotificationClock.FormatVisit(row.AppointmentDate, row.AppointmentTime);
            var title = "Follow-up appointment missed";
            var body = $"{patientLabel} did not arrive for the follow-up visit on {when}.";
            var deepLink = "/doctor/follow-ups";
            var userId = (long)row.DoctorUserId.Value;

            var pushResult = await push.SendToUserAsync(
                userId,
                title,
                body,
                new Dictionary<string, string>
                {
                    ["type"] = "APPT_MISSED",
                    ["patientAppId"] = row.PatientAppId.ToString(),
                    ["deepLink"] = deepLink,
                },
                "APPT_MISSED",
                cancellationToken);

            var pushStatus = string.IsNullOrWhiteSpace(pushResult.Status) ? "LOGGED" : pushResult.Status.ToUpperInvariant();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.AppNotification (UserId, Title, Body, PushStatus, Type, DeepLink)
                VALUES ({userId}, {title}, {body}, {pushStatus}, N'APPT_MISSED', {deepLink})",
                cancellationToken);

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.NotificationOutbox (Channel, EventType, Destination, Body, Status, LastError, CreatedAt)
                VALUES (N'PUSH', {("APPT_MISS_" + row.PatientAppId)}, {userId.ToString()}, {body}, N'SENT', NULL, {DateTime.Now})",
                cancellationToken);
        }

        if (rows.Count > 0)
            _logger.LogInformation("Missed follow-up appointment notifications sent: {Count}", rows.Count);
    }

    private sealed class MissedFollowUpRow
    {
        public int FollowUpTaskId { get; set; }
        public string Title { get; set; } = "";
        public DateTime DueDate { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? DoctorUserId { get; set; }
        public string? PatientName { get; set; }
    }

    private sealed class MissedAppointmentRow
    {
        public int PatientAppId { get; set; }
        public int DoctorId { get; set; }
        public int? DoctorUserId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public string? PatientName { get; set; }
    }
}

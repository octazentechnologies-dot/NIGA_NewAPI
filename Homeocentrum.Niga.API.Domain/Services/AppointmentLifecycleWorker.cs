using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Appointment grace lifecycle:
/// 1) T-15 / exact-time reminders
/// 2) Mark OVERDUE when slot time passes (24h reschedule window)
/// 3) Grace-soon warning (~4h before GraceEndsAt)
/// 4) Mark EXPIRED when grace ends (RefundEligible=0, no reschedule)
/// </summary>
public sealed class AppointmentLifecycleWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AppointmentLifecycleWorker> _logger;

    public AppointmentLifecycleWorker(
        IServiceScopeFactory scopes,
        ILogger<AppointmentLifecycleWorker> logger)
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
                if (FeatureFlags.Current.EnableAppointmentGraceLifecycle
                    && FeatureFlags.Current.EnableBackgroundJobs)
                {
                    await EnsureSchemaAsync(stoppingToken);
                    await SendSlotProximityNotificationsAsync(stoppingToken);
                    await MarkOverdueAsync(stoppingToken);
                    await SendGraceSoonNotificationsAsync(stoppingToken);
                    await MarkExpiredAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Appointment lifecycle sweep failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.PatientAppointment', N'OverdueAt') IS NULL
                ALTER TABLE dbo.PatientAppointment ADD OverdueAt datetime NULL;
            IF COL_LENGTH(N'dbo.PatientAppointment', N'GraceEndsAt') IS NULL
                ALTER TABLE dbo.PatientAppointment ADD GraceEndsAt datetime NULL;
            IF COL_LENGTH(N'dbo.PatientAppointment', N'ExpiredAt') IS NULL
                ALTER TABLE dbo.PatientAppointment ADD ExpiredAt datetime NULL;
            IF COL_LENGTH(N'dbo.PatientAppointment', N'RefundEligible') IS NULL
                ALTER TABLE dbo.PatientAppointment ADD RefundEligible bit NULL;
            """, ct);
    }

    private async Task SendSlotProximityNotificationsAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var inbox = scope.ServiceProvider.GetRequiredService<IClinicInboxFanout>();
        var now = DateTime.Now;

        // T-15: slot between now+14 and now+16 minutes (1-min poll window).
        var t15From = now.AddMinutes(14);
        var t15To = now.AddMinutes(16);
        await NotifyProximityAsync(db, inbox, "APPT_T15_", t15From, t15To,
            "Appointment in 15 minutes",
            (paid, when) => paid
                ? $"Your visit at {when} starts in about 15 minutes. Please join or arrive on time. Your payment is on file."
                : $"Your visit at {when} starts in about 15 minutes. Please join or arrive on time.",
            ct);

        // Exact time: slot between now-1 and now+1 minute.
        var t0From = now.AddMinutes(-1);
        var t0To = now.AddMinutes(1);
        await NotifyProximityAsync(db, inbox, "APPT_T0_", t0From, t0To,
            "Appointment starting now",
            (paid, when) => paid
                ? $"Your visit at {when} is starting now. Please check in or join. Your payment is on file."
                : $"Your visit at {when} is starting now. Please check in or join.",
            ct);
    }

    private static async Task NotifyProximityAsync(
        NIGACentrumContext db,
        IClinicInboxFanout inbox,
        string eventPrefix,
        DateTime from,
        DateTime to,
        string title,
        Func<bool, string, string> bodyFactory,
        CancellationToken ct)
    {
        var rows = await db.Database.SqlQuery<LifecycleCandidate>($@"
            SELECT TOP 40
                a.PatientAppId,
                a.PatientId,
                a.DoctorId,
                a.AppointmentDate,
                CONVERT(varchar(5), a.AppointmentTime, 108) AS AppointmentTime,
                a.PaymentStatus,
                LTRIM(RTRIM(ISNULL(pt.PatientName, N''))) AS PatientName
            FROM dbo.PatientAppointment a
            LEFT JOIN dbo.Patient pt ON pt.PatientId = a.PatientId
            WHERE ISNULL(a.DeleteStatus, 0) = 0
              AND a.AppointmentDate IS NOT NULL
              AND UPPER(ISNULL(a.Status, N'')) NOT IN (
                    N'CANCELLED', N'COMPLETED', N'EXPIRED', N'OVERDUE', N'NOT ARRIVED', N'NO_SHOW', N'NOSHOW')
              AND DATEADD(minute,
                    DATEDIFF(minute, 0, CAST(ISNULL(a.AppointmentTime, '00:00') AS datetime)),
                    CAST(a.AppointmentDate AS datetime)) >= {from}
              AND DATEADD(minute,
                    DATEDIFF(minute, 0, CAST(ISNULL(a.AppointmentTime, '00:00') AS datetime)),
                    CAST(a.AppointmentDate AS datetime)) < {to}
              AND NOT EXISTS (
                    SELECT 1 FROM dbo.NotificationOutbox o
                    WHERE o.EventType = CONCAT({eventPrefix}, a.PatientAppId)
                      AND o.Status IN (N'SENT', N'QUEUED', N'PENDING', N'LOGGED'))
            ORDER BY a.AppointmentDate, a.AppointmentTime").ToListAsync(ct);

        foreach (var row in rows)
        {
            var when = NotificationClock.FormatVisit(row.AppointmentDate, row.AppointmentTime);
            var paid = string.Equals(row.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);
            var body = bodyFactory(paid, when);
            await inbox.NotifyPatientAsync(row.PatientId, title, body, eventPrefix.TrimEnd('_'), "/patientdashboard", ct);
            await MarkOutboxAsync(db, eventPrefix + row.PatientAppId, row.PatientId.ToString(), body, ct);
        }
    }

    private async Task MarkOverdueAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var inbox = scope.ServiceProvider.GetRequiredService<IClinicInboxFanout>();
        var now = DateTime.Now;

        var rows = await db.Database.SqlQuery<LifecycleCandidate>($@"
            SELECT TOP 40
                a.PatientAppId,
                a.PatientId,
                a.DoctorId,
                a.AppointmentDate,
                CONVERT(varchar(5), a.AppointmentTime, 108) AS AppointmentTime,
                a.PaymentStatus,
                LTRIM(RTRIM(ISNULL(pt.PatientName, N''))) AS PatientName
            FROM dbo.PatientAppointment a
            LEFT JOIN dbo.Patient pt ON pt.PatientId = a.PatientId
            WHERE ISNULL(a.DeleteStatus, 0) = 0
              AND a.AppointmentDate IS NOT NULL
              AND UPPER(ISNULL(a.Status, N'')) NOT IN (
                    N'CANCELLED', N'COMPLETED', N'EXPIRED', N'OVERDUE')
              AND (
                    UPPER(ISNULL(a.Status, N'')) IN (
                        N'WAITING', N'WALK-IN', N'E-CONSULT', N'REMAINING', N'EARLY', N'READY', N'NOT ARRIVED', N'NO_SHOW', N'NOSHOW')
                    OR a.Status IS NULL OR LTRIM(RTRIM(a.Status)) = N''
                  )
              AND DATEADD(minute,
                    DATEDIFF(minute, 0, CAST(ISNULL(a.AppointmentTime, '00:00') AS datetime)),
                    CAST(a.AppointmentDate AS datetime)) < {now}
            ORDER BY a.AppointmentDate, a.AppointmentTime").ToListAsync(ct);

        foreach (var row in rows)
        {
            var slotAt = AppointmentLifecycle.ComputeSlotAt(row.AppointmentDate, ParseTime(row.AppointmentTime))
                         ?? row.AppointmentDate.Date;
            var graceEnds = AppointmentLifecycle.ComputeGraceEndsAt(slotAt);
            var overdueAt = now;

            var updated = await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientAppointment
                SET Status = {S3AppointmentRules.Overdue},
                    OverdueAt = ISNULL(OverdueAt, {overdueAt}),
                    GraceEndsAt = ISNULL(GraceEndsAt, {graceEnds})
                WHERE PatientAppId = {row.PatientAppId}
                  AND ISNULL(DeleteStatus, 0) = 0
                  AND UPPER(ISNULL(Status, N'')) NOT IN (N'CANCELLED', N'COMPLETED', N'EXPIRED', N'OVERDUE')", ct);
            if (updated == 0)
                continue;

            var when = NotificationClock.FormatVisit(row.AppointmentDate, row.AppointmentTime);
            var paid = string.Equals(row.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);
            var patientBody = paid
                ? $"You missed your visit at {when}. You have 24 hours to reschedule and keep your payment. After that the booking expires with no refund."
                : $"You missed your visit at {when}. You have 24 hours to reschedule. After that the booking expires.";
            var patientLabel = string.IsNullOrWhiteSpace(row.PatientName) ? "A patient" : row.PatientName.Trim();
            var clinicBody = $"{patientLabel} missed the visit at {when}. 24h reschedule grace is open.";

            await inbox.NotifyPatientAsync(row.PatientId, "Appointment overdue", patientBody, "APPT_OVERDUE", "/patientdashboard", ct);
            await inbox.NotifyDoctorAndReceptionAsync(
                row.DoctorId,
                "Appointment overdue",
                clinicBody,
                "APPT_OVERDUE",
                cancellationToken: ct);
            await MarkOutboxAsync(db, "APPT_OVERDUE_" + row.PatientAppId, row.PatientId.ToString(), patientBody, ct);
        }

        if (rows.Count > 0)
            _logger.LogInformation("Appointment OVERDUE marked: {Count}", rows.Count);
    }

    private async Task SendGraceSoonNotificationsAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var inbox = scope.ServiceProvider.GetRequiredService<IClinicInboxFanout>();
        var now = DateTime.Now;
        var warnFrom = now.AddHours(AppointmentLifecycle.GraceWarningHoursBeforeEnd - 0.05);
        var warnTo = now.AddHours(AppointmentLifecycle.GraceWarningHoursBeforeEnd + 0.05);

        // Notify when GraceEndsAt is ~4h ahead (window ±3 min via 1-min poll → use hour band).
        var rows = await db.Database.SqlQuery<LifecycleCandidate>($@"
            SELECT TOP 40
                a.PatientAppId,
                a.PatientId,
                a.DoctorId,
                a.AppointmentDate,
                CONVERT(varchar(5), a.AppointmentTime, 108) AS AppointmentTime,
                a.PaymentStatus,
                LTRIM(RTRIM(ISNULL(pt.PatientName, N''))) AS PatientName
            FROM dbo.PatientAppointment a
            LEFT JOIN dbo.Patient pt ON pt.PatientId = a.PatientId
            WHERE ISNULL(a.DeleteStatus, 0) = 0
              AND UPPER(ISNULL(a.Status, N'')) IN (N'OVERDUE', N'NOT ARRIVED')
              AND a.GraceEndsAt IS NOT NULL
              AND a.GraceEndsAt >= {now.AddHours(3.5)}
              AND a.GraceEndsAt < {now.AddHours(4.5)}
              AND NOT EXISTS (
                    SELECT 1 FROM dbo.NotificationOutbox o
                    WHERE o.EventType = CONCAT(N'APPT_GRACE_SOON_', a.PatientAppId)
                      AND o.Status IN (N'SENT', N'QUEUED', N'PENDING', N'LOGGED'))
            ORDER BY a.GraceEndsAt").ToListAsync(ct);

        foreach (var row in rows)
        {
            var paid = string.Equals(row.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);
            var body = paid
                ? "About 4 hours left to reschedule your missed visit and keep your payment. After that there is no refund."
                : "About 4 hours left to reschedule your missed visit before it expires.";
            await inbox.NotifyPatientAsync(row.PatientId, "Reschedule window ending soon", body, "APPT_GRACE_SOON", "/patientdashboard", ct);
            await MarkOutboxAsync(db, "APPT_GRACE_SOON_" + row.PatientAppId, row.PatientId.ToString(), body, ct);
        }

        _ = warnFrom;
        _ = warnTo;
    }

    private async Task MarkExpiredAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var inbox = scope.ServiceProvider.GetRequiredService<IClinicInboxFanout>();
        var now = DateTime.Now;

        var rows = await db.Database.SqlQuery<LifecycleCandidate>($@"
            SELECT TOP 40
                a.PatientAppId,
                a.PatientId,
                a.DoctorId,
                a.AppointmentDate,
                CONVERT(varchar(5), a.AppointmentTime, 108) AS AppointmentTime,
                a.PaymentStatus,
                LTRIM(RTRIM(ISNULL(pt.PatientName, N''))) AS PatientName
            FROM dbo.PatientAppointment a
            LEFT JOIN dbo.Patient pt ON pt.PatientId = a.PatientId
            WHERE ISNULL(a.DeleteStatus, 0) = 0
              AND UPPER(ISNULL(a.Status, N'')) IN (N'OVERDUE', N'NOT ARRIVED')
              AND a.GraceEndsAt IS NOT NULL
              AND a.GraceEndsAt <= {now}
            ORDER BY a.GraceEndsAt").ToListAsync(ct);

        foreach (var row in rows)
        {
            var updated = await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientAppointment
                SET Status = {S3AppointmentRules.Expired},
                    ExpiredAt = {now},
                    RefundEligible = 0
                WHERE PatientAppId = {row.PatientAppId}
                  AND ISNULL(DeleteStatus, 0) = 0
                  AND UPPER(ISNULL(Status, N'')) IN (N'OVERDUE', N'NOT ARRIVED')
                  AND GraceEndsAt IS NOT NULL
                  AND GraceEndsAt <= {now}", ct);
            if (updated == 0)
                continue;

            var when = NotificationClock.FormatVisit(row.AppointmentDate, row.AppointmentTime);
            var paid = string.Equals(row.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);
            var patientBody = paid
                ? $"Your visit at {when} has expired. Reschedule is closed and the payment is not refundable per policy. Please book a new appointment if needed."
                : $"Your visit at {when} has expired. Please book a new appointment if you still need care.";
            var patientLabel = string.IsNullOrWhiteSpace(row.PatientName) ? "A patient" : row.PatientName.Trim();

            await inbox.NotifyPatientAsync(row.PatientId, "Appointment expired", patientBody, "APPT_EXPIRED", "/patientdashboard", ct);
            await inbox.NotifyDoctorAndReceptionAsync(
                row.DoctorId,
                "Appointment expired",
                $"{patientLabel}'s missed visit at {when} expired after the 24h grace window.",
                "APPT_EXPIRED",
                cancellationToken: ct);
            await MarkOutboxAsync(db, "APPT_EXPIRED_" + row.PatientAppId, row.PatientId.ToString(), patientBody, ct);
        }

        if (rows.Count > 0)
            _logger.LogInformation("Appointment EXPIRED marked: {Count}", rows.Count);
    }

    private static async Task MarkOutboxAsync(
        NIGACentrumContext db,
        string eventType,
        string destination,
        string body,
        CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.NotificationOutbox (Channel, EventType, Destination, Body, Status, LastError, CreatedAt)
            VALUES (N'PUSH', {eventType}, {destination}, {body}, N'SENT', NULL, {DateTime.Now})", ct);
    }

    private static TimeSpan? ParseTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        return TimeSpan.TryParse(raw.Trim(), out var t) ? t : null;
    }

    private sealed class LifecycleCandidate
    {
        public int PatientAppId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PatientName { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Realtime;

namespace Homeocentrum.Niga.API.Domain.Services;

public sealed class ClinicInboxFanout : IClinicInboxFanout
{
    private readonly NIGACentrumContext _context;
    private readonly IPushNotificationSender _push;
    private readonly IClinicRealtimeNotifier _realtime;
    private readonly IPatientRealtimeNotifier _patientRealtime;
    private readonly ILogger<ClinicInboxFanout> _logger;

    public ClinicInboxFanout(
        NIGACentrumContext context,
        IPushNotificationSender push,
        IClinicRealtimeNotifier realtime,
        IPatientRealtimeNotifier patientRealtime,
        ILogger<ClinicInboxFanout> logger)
    {
        _context = context;
        _push = push;
        _realtime = realtime;
        _patientRealtime = patientRealtime;
        _logger = logger;
    }

    public async Task NotifyDoctorAndReceptionAsync(
        int doctorId,
        string title,
        string body,
        string action,
        string? doctorDeepLink = "/doctordashboard",
        string? receptionDeepLink = "/reception/appointments",
        CancellationToken cancellationToken = default)
    {
        if (doctorId <= 0)
            return;

        try
        {
            var doctorLink = string.IsNullOrWhiteSpace(doctorDeepLink)
                ? AppNotificationInbox.DoctorHomeLink()
                : doctorDeepLink;

            long? doctorUserId = await _context.Doctors.AsNoTracking()
                .Where(d => d.DoctorId == doctorId && !d.DeleteStatus)
                .Select(d => d.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            await NotifyDoctorUserAsync(
                doctorId,
                title,
                body,
                action,
                doctorLink,
                cancellationToken);

            var staffIds = await _context.DoctorReceptionStaffs.AsNoTracking()
                .Where(s => s.DoctorId == doctorId && !s.DeleteStatus && s.IsActive)
                .Select(s => (long)s.ReceptionStaffId)
                .ToListAsync(cancellationToken);

            var receptionLink = string.IsNullOrWhiteSpace(receptionDeepLink)
                ? "/reception/appointments"
                : receptionDeepLink;
            var type = AppNotificationInbox.MapType(action, null, title, body);
            // Reception JWT subject is ReceptionStaffId; never also fan-out to the doctor's UserId
            // or the doctor inbox gets two identical "New appointment" rows.
            foreach (var staffId in staffIds.Distinct()
                         .Where(id => !doctorUserId.HasValue || id != doctorUserId.Value))
            {
                await SendAsync(staffId, title, body, action, type, receptionLink, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clinic inbox fan-out failed for DoctorId={DoctorId} Action={Action}", doctorId, action);
        }
    }

    public async Task NotifyPharmacyPartnerAsync(
        int? pharmacyPartnerId,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (pharmacyPartnerId is null or <= 0)
            return;

        try
        {
            var userId = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 CAST(UserId AS bigint) AS Id FROM dbo.PharmacyPartner
                WHERE PharmacyPartnerId = {pharmacyPartnerId.Value} AND Status = N'ACTIVE'").FirstOrDefaultAsync(cancellationToken);
            if (userId == null || userId.Id <= 0)
                return;

            await SendAsync(
                userId.Id,
                title,
                body,
                "medicine_status",
                "MEDICINE",
                "/pharmacy/orders",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pharmacy inbox fan-out failed for PharmacyPartnerId={Id}", pharmacyPartnerId);
        }
    }

    public async Task NotifyDoctorUserAsync(
        int doctorId,
        string title,
        string body,
        string action,
        string? deepLink,
        CancellationToken cancellationToken = default)
    {
        if (doctorId <= 0)
            return;

        try
        {
            var doctorUserId = await _context.Doctors.AsNoTracking()
                .Where(d => d.DoctorId == doctorId && !d.DeleteStatus)
                .Select(d => d.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (!doctorUserId.HasValue || doctorUserId.Value <= 0)
                return;

            var type = AppNotificationInbox.MapType(action, null, title, body);
            var link = string.IsNullOrWhiteSpace(deepLink)
                ? AppNotificationInbox.DoctorHomeLink()
                : deepLink;

            await SendAsync(
                doctorUserId.Value,
                title,
                body,
                action,
                type,
                link,
                cancellationToken);

            await PublishInboxRealtimeAsync(doctorId, action, title, body, link, type, cancellationToken);
            await PublishAppointmentRealtimeAsync(doctorId, action, title, body, link, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Doctor inbox send failed for DoctorId={DoctorId} Action={Action}", doctorId, action);
        }
    }

    public async Task NotifyPatientAsync(
        int patientId,
        string title,
        string body,
        string action,
        string? deepLink,
        CancellationToken cancellationToken = default)
    {
        if (patientId <= 0)
            return;

        try
        {
            var mapped = await _context.PatientUserMaps.AsNoTracking()
                .Where(m => m.PatientId == patientId && !m.DeleteStatus)
                .OrderByDescending(m => m.IsPrimary)
                .Select(m => (long?)m.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            var userId = mapped ?? 0;
            if (userId <= 0)
            {
                var mobile = await _context.Patients.AsNoTracking()
                    .Where(p => p.PatientId == patientId)
                    .Select(p => p.MobileNo)
                    .FirstOrDefaultAsync(cancellationToken);
                userId = await _push.ResolveUserIdByMobileAsync(mobile, cancellationToken) ?? 0;
            }

            if (userId <= 0)
                return;

            var link = string.IsNullOrWhiteSpace(deepLink)
                ? AppNotificationInbox.MapDeepLink(action, null) ?? "/patientdashboard"
                : deepLink;
            var type = AppNotificationInbox.MapType(action, null, title, body);
            await SendAsync(userId, title, body, action, type, link, cancellationToken);
            await PublishPatientRealtimeAsync(userId, action, title, body, link, type, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Patient inbox send failed for PatientId={PatientId} Action={Action}", patientId, action);
        }
    }

    private async Task SendAsync(
        long userId,
        string title,
        string body,
        string action,
        string type,
        string deepLink,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
            return;

        // Stable per user+action+deepLink so payment confirm after booking cannot enqueue
        // a second push outbox / inbox for the same appointment.
        var deliveryId = AppNotificationInbox.StableDeliveryId(action, userId, deepLink, title, body);

        await _push.SendToUserAsync(
            userId,
            title,
            body,
            new Dictionary<string, string>
            {
                ["action"] = action,
                ["type"] = type,
                ["deepLink"] = deepLink,
                ["deliveryId"] = deliveryId.ToString("D")
            },
            action,
            cancellationToken);
    }

    private async Task PublishInboxRealtimeAsync(
        int doctorId,
        string action,
        string title,
        string body,
        string deepLink,
        string type,
        CancellationToken cancellationToken)
    {
        try
        {
            await _realtime.InboxChangedAsync(doctorId, new
            {
                eventId = Guid.NewGuid().ToString("N"),
                schemaVersion = 1,
                eventType = string.IsNullOrWhiteSpace(action) ? "inbox_changed" : action.Trim(),
                doctorId,
                title,
                body,
                deepLink,
                type,
                occurredAtUtc = DateTime.UtcNow
            }, cancellationToken);

            if (string.Equals(type, "FOLLOW_UP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(action, "FOLLOW_UP_MISSED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(action, "follow_up", StringComparison.OrdinalIgnoreCase))
            {
                await _realtime.FollowUpChangedAsync(doctorId, new
                {
                    eventId = Guid.NewGuid().ToString("N"),
                    schemaVersion = 1,
                    eventType = "follow_up_changed",
                    doctorId,
                    occurredAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inbox realtime publish failed for DoctorId={DoctorId}", doctorId);
        }
    }

    private async Task PublishAppointmentRealtimeAsync(
        int doctorId,
        string action,
        string title,
        string body,
        string? deepLink,
        CancellationToken cancellationToken)
    {
        var type = AppNotificationInbox.MapType(action, null, title, body);
        if (!string.Equals(type, "APPOINTMENT", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            await _realtime.AppointmentChangedAsync(doctorId, new
            {
                eventId = Guid.NewGuid().ToString("N"),
                schemaVersion = 1,
                eventType = string.IsNullOrWhiteSpace(action) ? "appointment_changed" : action.Trim(),
                doctorId,
                title,
                body,
                deepLink = string.IsNullOrWhiteSpace(deepLink)
                    ? AppNotificationInbox.DoctorHomeLink()
                    : deepLink,
                occurredAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment realtime publish failed for DoctorId={DoctorId}", doctorId);
        }
    }

    private async Task PublishPatientRealtimeAsync(
        long userId,
        string action,
        string title,
        string body,
        string deepLink,
        string type,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var payload = new
        {
            eventId,
            schemaVersion = 1,
            eventType = string.IsNullOrWhiteSpace(action) ? "inbox_changed" : action.Trim(),
            title,
            body,
            deepLink,
            type,
            occurredAtUtc = DateTime.UtcNow
        };

        try
        {
            await _patientRealtime.InboxChangedAsync(userId, payload, cancellationToken);
            if (string.Equals(type, "APPOINTMENT", StringComparison.OrdinalIgnoreCase))
                await _patientRealtime.AppointmentChangedAsync(userId, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Patient realtime publish failed for UserId={UserId}", userId);
        }
    }

    private sealed class IdRow
    {
        public long Id { get; set; }
    }
}

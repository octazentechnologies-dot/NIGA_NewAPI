using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Homeocentrum.Niga.API.Domain.Realtime;

public sealed class ClinicRealtimeNotifier : IClinicRealtimeNotifier
{
    private readonly IHubContext<ClinicRealtimeHub> _hub;
    private readonly ILogger<ClinicRealtimeNotifier> _logger;

    public ClinicRealtimeNotifier(
        IHubContext<ClinicRealtimeHub> hub,
        ILogger<ClinicRealtimeNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task AppointmentRequestChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
        => SendAsync(doctorId, ClinicRealtimeHub.ClientMethod, payload, cancellationToken);

    public Task AppointmentChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
        => SendAsync(doctorId, ClinicRealtimeHub.AppointmentChangedMethod, payload, cancellationToken);

    public Task InboxChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
        => SendAsync(doctorId, ClinicRealtimeHub.InboxChangedMethod, payload, cancellationToken);

    public Task ReminderChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
        => SendAsync(doctorId, ClinicRealtimeHub.ReminderChangedMethod, payload, cancellationToken);

    public Task ReminderDueAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
        => SendAsync(doctorId, ClinicRealtimeHub.ReminderDueMethod, payload, cancellationToken);

    public Task InstantOfferChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
        => SendAsync(doctorId, ClinicRealtimeHub.InstantOfferChangedMethod, payload, cancellationToken);

    public Task FollowUpChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
        => SendAsync(doctorId, ClinicRealtimeHub.FollowUpChangedMethod, payload, cancellationToken);

    private async Task SendAsync(
        int doctorId,
        string method,
        object payload,
        CancellationToken cancellationToken)
    {
        if (doctorId <= 0 || payload == null || string.IsNullOrWhiteSpace(method))
            return;
        try
        {
            await _hub.Clients
                .Group(ClinicRealtimeHub.DoctorGroup(doctorId))
                .SendAsync(method, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clinic realtime {Method} failed for doctor {DoctorId}", method, doctorId);
        }
    }
}

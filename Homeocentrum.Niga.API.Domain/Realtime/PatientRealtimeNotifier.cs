using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Homeocentrum.Niga.API.Domain.Realtime;

public sealed class PatientRealtimeNotifier : IPatientRealtimeNotifier
{
    private readonly IHubContext<PatientRealtimeHub> _hub;
    private readonly ILogger<PatientRealtimeNotifier> _logger;

    public PatientRealtimeNotifier(
        IHubContext<PatientRealtimeHub> hub,
        ILogger<PatientRealtimeNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task AppointmentChangedAsync(long userId, object payload, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || payload == null) return;
        try
        {
            await _hub.Clients
                .Group(PatientRealtimeHub.UserGroup(userId))
                .SendAsync(PatientRealtimeHub.AppointmentChangedMethod, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Patient appointment realtime failed for UserId={UserId}", userId);
        }
    }

    public async Task InboxChangedAsync(long userId, object payload, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || payload == null) return;
        try
        {
            await _hub.Clients
                .Group(PatientRealtimeHub.UserGroup(userId))
                .SendAsync(PatientRealtimeHub.InboxChangedMethod, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Patient inbox realtime failed for UserId={UserId}", userId);
        }
    }
}

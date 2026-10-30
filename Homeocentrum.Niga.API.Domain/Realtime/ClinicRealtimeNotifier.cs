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

    public async Task AppointmentRequestChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (doctorId <= 0 || payload == null) return;
        try
        {
            await _hub.Clients
                .Group(ClinicRealtimeHub.DoctorGroup(doctorId))
                .SendAsync(ClinicRealtimeHub.ClientMethod, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clinic realtime push failed for doctor {DoctorId}", doctorId);
        }
    }

    public async Task AppointmentChangedAsync(
        int doctorId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (doctorId <= 0 || payload == null) return;
        try
        {
            await _hub.Clients
                .Group(ClinicRealtimeHub.DoctorGroup(doctorId))
                .SendAsync(ClinicRealtimeHub.AppointmentChangedMethod, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Appointment realtime push failed for doctor {DoctorId}", doctorId);
        }
    }
}

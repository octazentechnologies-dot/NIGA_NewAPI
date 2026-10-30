namespace Homeocentrum.Niga.API.Domain.Realtime;

/// <summary>
/// Pushes clinic inbox events to connected doctor browsers (SignalR).
/// Not an external HTTP webhook — in-app realtime so the UI need not poll.
/// </summary>
public interface IClinicRealtimeNotifier
{
    Task AppointmentRequestChangedAsync(int doctorId, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Book / reschedule / cancel / confirm and other appointment calendar changes.
    /// Doctor dashboard and schedule clients refresh from this event.
    /// </summary>
    Task AppointmentChangedAsync(int doctorId, object payload, CancellationToken cancellationToken = default);
}

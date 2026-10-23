namespace Homeocentrum.Niga.API.Domain.Realtime;

/// <summary>
/// Pushes clinic inbox events to connected doctor browsers (SignalR).
/// Not an external HTTP webhook — in-app realtime so the UI need not poll.
/// </summary>
public interface IClinicRealtimeNotifier
{
    Task AppointmentRequestChangedAsync(int doctorId, object payload, CancellationToken cancellationToken = default);
}

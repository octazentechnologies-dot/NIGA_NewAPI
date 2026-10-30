namespace Homeocentrum.Niga.API.Domain.Realtime;

public interface IPatientRealtimeNotifier
{
    Task AppointmentChangedAsync(long userId, object payload, CancellationToken cancellationToken = default);
    Task InboxChangedAsync(long userId, object payload, CancellationToken cancellationToken = default);
}

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

    /// <summary>Bell / AppNotification inbox changed for this clinic.</summary>
    Task InboxChangedAsync(int doctorId, object payload, CancellationToken cancellationToken = default);

    /// <summary>Doctor reminder list changed (CRUD / done / snooze).</summary>
    Task ReminderChangedAsync(int doctorId, object payload, CancellationToken cancellationToken = default);

    /// <summary>A reminder is due now — includes toast fields so the client need not refetch first.</summary>
    Task ReminderDueAsync(int doctorId, object payload, CancellationToken cancellationToken = default);

    /// <summary>Instant consult offer queue changed for this doctor.</summary>
    Task InstantOfferChangedAsync(int doctorId, object payload, CancellationToken cancellationToken = default);

    /// <summary>Follow-up due / open task set changed.</summary>
    Task FollowUpChangedAsync(int doctorId, object payload, CancellationToken cancellationToken = default);
}

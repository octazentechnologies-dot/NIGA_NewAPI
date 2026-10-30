namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Drains dbo.PushNotificationOutbox (FCM only — not SMS NotificationOutbox).
/// </summary>
public interface IPushOutboxProcessor
{
    /// <summary>Claim and process a batch. Returns number of rows handled.</summary>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);
}

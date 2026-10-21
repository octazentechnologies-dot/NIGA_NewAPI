using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Services;

public sealed class PushDeliveryResult
{
    /// <summary>sent | logged | skipped | failed</summary>
    public string Status { get; set; } = "skipped";

    public int TokenCount { get; set; }

    public int SentCount { get; set; }

    public string? Detail { get; set; }
}

/// <summary>
/// Common FCM push for Website / Android / iOS device tokens stored on DevicePushToken.
/// One user may have many devices; every active token for that UserId is targeted.
/// </summary>
public interface IPushNotificationSender
{
    /// <summary>
    /// Send to all registered devices for <paramref name="userId"/>.
    /// Writes a <c>FirebaseNotificationLog</c> row and a per-user <c>AppNotification</c> inbox
    /// row (web bell). Safe to call on every successful API action.
    /// </summary>
    Task<PushDeliveryResult> SendToUserAsync(
        long userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        string? action = null,
        CancellationToken cancellationToken = default);

    /// <summary>Resolve login UserId from a patient mobile (Patient → PatientUserMap).</summary>
    Task<long?> ResolveUserIdByMobileAsync(string? mobile, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete tokens on logout (all devices, or one token when provided).</summary>
    Task ClearUserTokensAsync(long userId, string? token = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert or refresh a device token. Same token or same (user + deviceType + deviceId) updates in place.
    /// </summary>
    Task<DevicePushToken> UpsertTokenAsync(
        long userId,
        string token,
        string? deviceType,
        string? deviceId,
        CancellationToken cancellationToken = default);

    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);
}

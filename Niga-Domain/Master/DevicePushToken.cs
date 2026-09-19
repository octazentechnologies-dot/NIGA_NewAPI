using System;

namespace Niga_Domain.Master;

/// <summary>DMO-03.02 / COM-03 — FCM or APNs device token for the JWT user.</summary>
public class DevicePushToken
{
    public long DevicePushTokenId { get; set; }
    public long UserId { get; set; }
    public string Platform { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool DeleteStatus { get; set; }
}

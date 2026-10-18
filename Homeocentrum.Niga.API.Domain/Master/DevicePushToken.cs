using System;

namespace Homeocentrum.Niga.API.Domain.Master;

/// <summary>FCM device token for a login (Website / Android / iOS).</summary>
public class DevicePushToken
{
    public long DevicePushTokenId { get; set; }
    public long UserId { get; set; }

    /// <summary>Legacy channel label (FCM). Prefer <see cref="DeviceType"/>.</summary>
    public string Platform { get; set; } = "FCM";

    /// <summary>Website | Android | iOS</summary>
    public string DeviceType { get; set; } = "Website";

    public string Token { get; set; } = null!;
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool DeleteStatus { get; set; }
}

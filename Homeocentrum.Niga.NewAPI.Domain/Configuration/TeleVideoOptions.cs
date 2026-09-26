namespace Homeocentrum.Niga.NewAPI.Domain.Configuration;

/// <summary>
/// Tele video vendor. Vendor=Stub until Agora/Twilio/Daily keys are provided.
/// Client still uses Token / Rejoin / DeviceCheck / WaitingRoom / JoinFailure APIs.
/// No SignalR — clients poll GetSession / Tele/Queue.
/// </summary>
public class TeleVideoOptions
{
    public const string SectionName = "TeleVideo";

    /// <summary>Stub | Agora | Twilio | Daily</summary>
    public string Vendor { get; set; } = "Stub";

    public int TokenTtlMinutes { get; set; } = 60;

    public AgoraTeleOptions Agora { get; set; } = new();

    public TwilioTeleOptions Twilio { get; set; } = new();

    public DailyTeleOptions Daily { get; set; } = new();
}

public class AgoraTeleOptions
{
    public string AppId { get; set; } = string.Empty;
    public string AppCertificate { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppCertificate);
}

public class TwilioTeleOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string ApiKeySid { get; set; } = string.Empty;
    public string ApiKeySecret { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(ApiKeySid)
        && !string.IsNullOrWhiteSpace(ApiKeySecret);
}

public class DailyTeleOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Domain);
}

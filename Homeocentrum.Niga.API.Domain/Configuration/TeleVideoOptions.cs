namespace Homeocentrum.Niga.API.Domain.Configuration;

/// <summary>
/// Tele video vendor. Vendor=Stub until Agora/Twilio/Daily keys are provided.
/// Client still uses Token / Rejoin / DeviceCheck / WaitingRoom / JoinFailure APIs.
/// No SignalR — clients poll GetSession / Tele/Queue.
/// </summary>
public class TeleVideoOptions
{
    public const string SectionName = "TeleVideo";

    /// <summary>Stub | Agora | Twilio | Daily | 100ms (alias HundredMs)</summary>
    public string Vendor { get; set; } = "Stub";

    public int TokenTtlMinutes { get; set; } = 60;

    public AgoraTeleOptions Agora { get; set; } = new();

    public TwilioTeleOptions Twilio { get; set; } = new();

    public DailyTeleOptions Daily { get; set; } = new();

    public HundredMsTeleOptions HundredMs { get; set; } = new();
}

/// <summary>
/// 100ms.live (dashboard → Developer). AccessKey/AppSecret sign the management and app JWTs;
/// TemplateId decides which roles exist in each room. DoctorRole/PatientRole must match template role names.
/// </summary>
public class HundredMsTeleOptions
{
    public string AccessKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>in | us | eu | auto</summary>
    public string Region { get; set; } = "in";

    public string ApiBaseUrl { get; set; } = "https://api.100ms.live/v2";
    public string DoctorRole { get; set; } = "host";
    public string PatientRole { get; set; } = "guest";

    /// <summary>Optional 100ms app subdomain (e.g. "homeocentrum") for the prebuilt meeting link.</summary>
    public string Subdomain { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(AppSecret)
        && !string.IsNullOrWhiteSpace(TemplateId);
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

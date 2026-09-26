namespace Homeocentrum.Niga.NewAPI.Domain.Configuration;

/// <summary>
/// SMS delivery. Fill Msg91 or Twilio keys when ready; Provider=Stub until then.
/// </summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>Stub | Msg91 | Twilio</summary>
    public string Provider { get; set; } = "Stub";

    public bool Enabled { get; set; } = true;

    public string DefaultCountryDialCode { get; set; } = "91";

    public Msg91SmsOptions Msg91 { get; set; } = new();

    public TwilioSmsOptions Twilio { get; set; } = new();
}

public class Msg91SmsOptions
{
    public string AuthKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Route { get; set; } = "4";
    /// <summary>India DLT template id for OTP (optional; plain text used when empty).</summary>
    public string OtpTemplateId { get; set; } = string.Empty;
    public string DltEntityId { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AuthKey) && !string.IsNullOrWhiteSpace(SenderId);
}

public class TwilioSmsOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(FromNumber);
}

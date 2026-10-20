using System;

namespace Homeocentrum.Niga.API.Domain.Configuration;

/// <summary>
/// SMS delivery via Twilio (or Stub). WhatsApp settings live under <see cref="WhatsAppMetaOptions"/>.
/// </summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>Stub | Twilio</summary>
    public string Provider { get; set; } = "Stub";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// true = real OTP mode: Twilio send, API never returns <c>devCode</c>.
    /// false = dummy/dev mode: no provider SMS. The API returns <c>devCode</c> for the UI to verify.
    /// Exposed on GET /api/Public/AppConfig as <c>realSms</c>.
    /// </summary>
    public bool RealSms { get; set; }

    public string DefaultCountryDialCode { get; set; } = "91";

    /// <summary>
    /// Minutes after API/IIS process start before background SMS workers send.
    /// Prevents outbox/reminder blasts on deploy or app-pool recycle. OTP/API sends are unaffected.
    /// </summary>
    public int StartupGraceMinutes { get; set; } = 5;

    public TwilioSmsOptions Twilio { get; set; } = new();
}

/// <summary>Twilio SMS credentials only. WhatsApp From / ContentSid are under WhatsAppMeta.</summary>
public class TwilioSmsOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;

    /// <summary>
    /// null = auto-detect from Twilio Account.type (Trial → templates, Full → custom Body).
    /// true = force trial templates. false = force custom real messages (upgraded account).
    /// </summary>
    public bool? UseTrialTemplates { get; set; }

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(FromNumber);
}

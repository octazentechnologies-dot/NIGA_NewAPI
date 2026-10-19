using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.API.Domain.Configuration;

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

    /// <summary>
    /// Minutes after API/IIS process start before background SMS workers send.
    /// Prevents outbox/reminder blasts on deploy or app-pool recycle. OTP/API sends are unaffected.
    /// </summary>
    public int StartupGraceMinutes { get; set; } = 5;

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

    /// <summary>
    /// WhatsApp sender (whatsapp:+E.164). Empty = whatsapp: + FromNumber (same Twilio number).
    /// Use Twilio sandbox From (whatsapp:+14155238886) when testing with the sandbox.
    /// </summary>
    public string WhatsAppFromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Default / fallback Twilio Content template SID for WhatsApp (trial sample Appointment Reminders).
    /// Used when category-specific SID is empty or the send fails on a free/trial account.
    /// </summary>
    public string WhatsAppDefaultContentSid { get; set; } = "HXfe5ab5f00277942d4d4200328b4d403c";

    /// <summary>
    /// Optional ContentSid per campaign category / notice key (HospitalService, OffersDiscount, HealthTips, NOTICE).
    /// Empty value falls back to WhatsAppDefaultContentSid.
    /// </summary>
    public Dictionary<string, string> WhatsAppContentSids { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// true = try freeform Body first (upgraded accounts). On failure, fall back to ContentSid sample.
    /// false = always send ContentSid (required on Twilio WhatsApp trial).
    /// </summary>
    public bool WhatsAppTryFreeformBodyFirst { get; set; }

    /// <summary>
    /// null = auto-detect from Twilio Account.type (Trial → templates, Full → custom Body).
    /// true = force trial templates. false = force custom real messages (upgraded account).
    /// </summary>
    public bool? UseTrialTemplates { get; set; }

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(FromNumber);

    public string ResolveWhatsAppContentSid(string? categoryOrKey = null)
    {
        if (!string.IsNullOrWhiteSpace(categoryOrKey)
            && WhatsAppContentSids != null
            && WhatsAppContentSids.TryGetValue(categoryOrKey.Trim(), out var mapped)
            && !string.IsNullOrWhiteSpace(mapped))
        {
            return mapped.Trim();
        }

        return string.IsNullOrWhiteSpace(WhatsAppDefaultContentSid)
            ? "HXfe5ab5f00277942d4d4200328b4d403c"
            : WhatsAppDefaultContentSid.Trim();
    }
}

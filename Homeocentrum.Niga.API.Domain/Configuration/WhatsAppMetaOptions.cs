using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.API.Domain.Configuration;

/// <summary>
/// WhatsApp campaigns and notices — single config section.
/// Provider Twilio reuses Sms:Twilio AccountSid / AuthToken / FromNumber; Meta uses the Meta fields below.
/// </summary>
public class WhatsAppMetaOptions
{
    public const string SectionName = "WhatsAppMeta";

    /// <summary>
    /// Twilio | Meta. Twilio reuses Sms:Twilio credentials. Meta uses AccessToken + PhoneNumberId below.
    /// </summary>
    public string Provider { get; set; } = "Twilio";

    public string AccessToken { get; set; } = string.Empty;

    public string PhoneNumberId { get; set; } = string.Empty;

    public string BusinessAccountId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "v23.0";

    /// <summary>
    /// Dial code without + (e.g. 91 for India). Used when numbers are sent as 10-digit local format.
    /// </summary>
    public string DefaultCountryDialCode { get; set; } = "91";

    /// <summary>
    /// Meta app secret. Used to verify X-Hub-Signature-256 on delivery receipt POSTs.
    /// Empty = receipt webhook returns 503.
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// Token Meta sends on GET hub.verify_token when registering the callback URL.
    /// Must match the value configured in Meta → WhatsApp → Configuration → Webhook.
    /// </summary>
    public string VerifyToken { get; set; } = string.Empty;

    /// <summary>
    /// WhatsApp sender (whatsapp:+E.164). Empty = whatsapp: + Sms:Twilio:FromNumber.
    /// </summary>
    public string WhatsAppFromNumber { get; set; } = string.Empty;

    /// <summary>
    /// Default / fallback Twilio Content template SID for WhatsApp (trial sample Appointment Reminders).
    /// </summary>
    public string WhatsAppDefaultContentSid { get; set; } = "HXfe5ab5f00277942d4d4200328b4d403c";

    /// <summary>
    /// Optional ContentSid per campaign category / notice key (HospitalService, OffersDiscount, HealthTips, NOTICE).
    /// </summary>
    public Dictionary<string, string> WhatsAppContentSids { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// true = try freeform Body first (upgraded accounts). On failure, fall back to ContentSid sample.
    /// false = always send ContentSid (required on Twilio WhatsApp trial).
    /// </summary>
    public bool WhatsAppTryFreeformBodyFirst { get; set; }

    public bool IsMetaProvider() =>
        string.Equals(Provider?.Trim(), "Meta", StringComparison.OrdinalIgnoreCase);

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(AccessToken)
            && !string.IsNullOrWhiteSpace(PhoneNumberId)
            && !string.IsNullOrWhiteSpace(ApiVersion);
    }

    public bool IsReceiptWebhookConfigured() =>
        !string.IsNullOrWhiteSpace(AppSecret);

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

namespace Homeocentrum.Niga.API.Domain.Configuration;

public class WhatsAppMetaOptions
{
    public const string SectionName = "WhatsAppMeta";

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

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(AccessToken)
            && !string.IsNullOrWhiteSpace(PhoneNumberId)
            && !string.IsNullOrWhiteSpace(ApiVersion);
    }

    public bool IsReceiptWebhookConfigured() =>
        !string.IsNullOrWhiteSpace(AppSecret);
}

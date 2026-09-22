namespace Niga_Domain.Configuration;

public class WhatsAppMetaOptions
{
    public const string SectionName = "WhatsAppMeta";

    // TODO S5: confirm AccessToken is still valid (already in New-API appsettings WhatsAppMeta). S5 will use it.

    public string AccessToken { get; set; } = string.Empty;

    public string PhoneNumberId { get; set; } = string.Empty;

    public string BusinessAccountId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "v23.0";

    /// <summary>
    /// Dial code without + (e.g. 91 for India). Used when numbers are sent as 10-digit local format.
    /// </summary>
    public string DefaultCountryDialCode { get; set; } = "91";

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(AccessToken)
            && !string.IsNullOrWhiteSpace(PhoneNumberId)
            && !string.IsNullOrWhiteSpace(ApiVersion);
    }
}

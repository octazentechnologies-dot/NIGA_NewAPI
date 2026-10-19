using System.Text.Json;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>
/// Single source for GET /api/Public/AppConfig and GET /api/App/Config.
/// Support contacts come from configuration. Instant consult / medicine online pay
/// are always available to the app (or gated from patient profile) — not AppConfig flags.
/// </summary>
public static class AppConfigPayload
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public static object Build(IConfiguration configuration)
    {
        // Sms:RealSms — true = production OTP (no devCode). false = dummy: SMS may still send + API returns devCode.
        var realSms = configuration.GetValue("Sms:RealSms", false);

        return new
        {
            success = true,
            data = new
            {
                razorpayKeyId = Blank(configuration["Razorpay:KeyId"]),
                siteUrl = Blank(configuration["ConfigurationModel:SiteUrl"]),
                supportPhone = Blank(configuration["Support:Phone"]),
                supportWhatsApp = Blank(configuration["Support:WhatsApp"]),
                supportEmail = Blank(configuration["Support:Email"]),
                realSms,
                smsMode = realSms ? "real" : "dummy"
            }
        };
    }

    public static string ToJsonExample(IConfiguration configuration)
        => JsonSerializer.Serialize(Build(configuration), JsonOptions);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

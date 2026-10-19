using Microsoft.Extensions.Options;

namespace Homeocentrum.Niga.API.Domain.Configuration;

/// <summary>
/// Real vs dummy OTP mode.
/// <see cref="SmsOptions.RealSms"/> true → production (no <c>devCode</c> in API).
/// false → dummy: SMS can still go out via Twilio, and <c>devCode</c> is returned for UI/testing.
/// </summary>
public static class SmsOtpPolicy
{
    public static bool RealSmsLive(SmsOptions? options)
    {
        if (options == null || !options.Enabled || !options.RealSms)
            return false;

        var provider = (options.Provider ?? "").Trim();
        if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            return options.Twilio?.IsConfigured() == true;
        if (provider.Equals("Msg91", StringComparison.OrdinalIgnoreCase))
            return options.Msg91?.IsConfigured() == true;
        return false;
    }

    public static bool RealSmsLive(IOptions<SmsOptions> options)
        => RealSmsLive(options?.Value);

    /// <summary>Include plaintext OTP in JSON only when not in real SMS mode.</summary>
    public static string? DevCodeOrNull(SmsOptions? options, string code)
        => RealSmsLive(options) ? null : code;
}

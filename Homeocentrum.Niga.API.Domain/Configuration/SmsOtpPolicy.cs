using Microsoft.Extensions.Options;

namespace Homeocentrum.Niga.API.Domain.Configuration;

/// <summary>
/// When a real SMS provider is live, OTP codes must not be returned in API responses.
/// When SMS is off or not configured, <c>devCode</c> may be returned for local testing.
/// </summary>
public static class SmsOtpPolicy
{
    public static bool RealSmsLive(SmsOptions? options)
    {
        if (options == null || !options.Enabled)
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

    /// <summary>Include plaintext OTP in JSON only when SMS is not live.</summary>
    public static string? DevCodeOrNull(SmsOptions? options, string code)
        => RealSmsLive(options) ? null : code;
}

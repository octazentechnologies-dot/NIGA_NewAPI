using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Homeocentrum.Niga.API.Domain.Security;

/// <summary>
/// Doctor-app → web portal bridge. The URL carries a one-time code only.
/// The web session is a chain of 20-minute tokens inside a 5-hour cap.
/// </summary>
public static class WebHandoffPolicy
{
    public const string TokenUseClaim = "token_use";
    public const string TokenUseValue = "web_handoff";
    public const string SidClaim = "handoff_sid";
    public const string AbsoluteExpClaim = "handoff_abs";

    public const string PurposePatientBoard = "patient-board";
    public const string PurposeDoctorDashboard = "doctor-dashboard";

    public const string DefaultReturnUrl = "nigadoctor://portal-return";

    public static readonly TimeSpan CodeLifetime = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(20);
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(5);
    public static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan CreateWindow = TimeSpan.FromMinutes(5);

    public const int MaxCreatesPerWindow = 10;
    public const int RenewAfterSeconds = 600;
    public const int AbsoluteWarningSeconds = 900;

    public static string CreateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncoder.Encode(bytes);
    }

    public static string? NormalizePurpose(string? purpose)
    {
        var value = (purpose ?? "").Trim().ToLowerInvariant().Replace("_", "-");
        if (value is "patient-board" or "patientboard")
            return PurposePatientBoard;
        if (value is "doctor-dashboard" or "doctordashboard" or "dashboard")
            return PurposeDoctorDashboard;
        return null;
    }

    /// <summary>Open-redirect guard. Only the Doctor app scheme and the portal return page.</summary>
    public static bool IsAllowedReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        var value = url.Trim();
        if (value.Length > 300 || value.Contains('\r') || value.Contains('\n') || value.Contains('\\'))
            return false;
        if (value.StartsWith("nigadoctor://portal-return", StringComparison.OrdinalIgnoreCase))
        {
            var rest = value["nigadoctor://portal-return".Length..];
            return rest.Length == 0 || rest[0] is '?' or '#';
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;
        if (uri.UserInfo.Length > 0)
            return false;

        var local = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        var portal = uri.Host.Equals("nigaui.homeocentrum.com", StringComparison.OrdinalIgnoreCase);
        if (uri.Scheme == "https" && portal)
            return IsReturnPath(uri);
        if (uri.Scheme == "http" && local)
            return IsReturnPath(uri);
        return false;
    }

    public static string ResolveReturnUrl(string? requested)
    {
        var value = requested?.Trim();
        return IsAllowedReturnUrl(value) ? value! : DefaultReturnUrl;
    }

    public static DateTime CapExpiry(DateTime nowUtc, DateTime absoluteExpiresUtc)
    {
        var next = nowUtc.Add(AccessTokenLifetime);
        return next < absoluteExpiresUtc ? next : absoluteExpiresUtc;
    }

    private static bool IsReturnPath(Uri uri)
    {
        var path = uri.AbsolutePath.TrimEnd('/');
        return path.Equals("/auth/app-return", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/app-return", StringComparison.OrdinalIgnoreCase);
    }
}

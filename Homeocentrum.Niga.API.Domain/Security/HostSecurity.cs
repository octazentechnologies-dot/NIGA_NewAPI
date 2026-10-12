namespace Homeocentrum.Niga.API.Domain.Security;

/// <summary>
/// Shared rules for the API host: one rate bucket per user (or per IP when anonymous),
/// open paths, and cookie CSRF. The clinic is one tenant; these rules do not invent a second clinic.
/// </summary>
public static class HostSecurity
{
    public static bool IsOpenPath(string? path)
    {
        var value = path ?? "";
        return value.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Signed-in calls share one bucket. Anonymous calls share a bucket per socket IP.</summary>
    public static string RatePartition(string? userId, string? ip)
    {
        if (!string.IsNullOrWhiteSpace(userId) && userId != "0")
            return "u:" + userId.Trim();
        var address = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip.Trim();
        return "ip:" + address;
    }

    /// <summary>
    /// CSRF applies to a cookie-carried session. A Bearer token is not sent by a foreign page, so it is exempt.
    /// CookieCsrf:Enabled in appsettings turns the whole check off when false.
    /// </summary>
    public static bool NeedsCsrfCheck(string? method, bool hasBearer, bool hasCookie, bool enabled = true)
    {
        if (!enabled || hasBearer || !hasCookie)
            return false;
        return method is "POST" or "PUT" or "PATCH" or "DELETE";
    }
}

/// <summary>In-process request counters for the admin metrics endpoint. Reset when the process restarts.</summary>
public static class ApiTraffic
{
    private static long _total;
    private static long _serverErrors;

    public static void Hit(int statusCode)
    {
        Interlocked.Increment(ref _total);
        if (statusCode >= 500)
            Interlocked.Increment(ref _serverErrors);
    }

    public static (long Total, long ServerErrors) Snapshot()
        => (Interlocked.Read(ref _total), Interlocked.Read(ref _serverErrors));
}

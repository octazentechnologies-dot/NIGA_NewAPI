using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Homeocentrum.Niga.API.Domain.Security;

/// <summary>
/// Failed-login throttle per user name and per client IP (in-memory, per API instance).
/// Five failures for one user name, or thirty from one IP, within 15 minutes block further attempts for 15 minutes.
/// </summary>
public static class LoginThrottle
{
    public const int MaxFailuresPerUser = 5;
    public const int MaxFailuresPerIp = 30;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan Block = TimeSpan.FromMinutes(15);

    private sealed class Entry
    {
        public int Failures;
        public DateTime WindowStartUtc;
        public DateTime BlockedUntilUtc;
    }

    private static readonly ConcurrentDictionary<string, Entry> Entries = new(StringComparer.Ordinal);

    public static TimeSpan? BlockedFor(string? userName, string? ip, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        TimeSpan? longest = null;
        foreach (var key in Keys(userName, ip))
        {
            if (Entries.TryGetValue(key, out var e))
            {
                lock (e)
                {
                    if (e.BlockedUntilUtc > now && (longest == null || e.BlockedUntilUtc - now > longest))
                        longest = e.BlockedUntilUtc - now;
                }
            }
        }
        return longest;
    }

    public static void RecordFailure(string? userName, string? ip, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (Entries.Count > 50_000)
            Purge(now);
        foreach (var key in Keys(userName, ip))
        {
            var isUser = key.StartsWith("u:", StringComparison.Ordinal);
            var limit = isUser ? MaxFailuresPerUser : MaxFailuresPerIp;
            var e = Entries.GetOrAdd(key, _ => new Entry { WindowStartUtc = now });
            var blockStarted = false;
            lock (e)
            {
                if (now - e.WindowStartUtc > Window)
                {
                    e.WindowStartUtc = now;
                    e.Failures = 0;
                }
                e.Failures++;
                if (e.Failures >= limit)
                {
                    blockStarted = e.Failures == limit;
                    e.BlockedUntilUtc = now + Block;
                }
            }
            if (blockStarted)
            {
                Logging.AppFileLog.SendOpsAlert("login-block|" + key, isUser ? "Sign-in blocked for a user name" : "Sign-in blocked for a client IP",
                    new Dictionary<string, string>
                    {
                        [isUser ? "User name" : "Client IP"] = key[(key.IndexOf(':') + 1)..],
                        ["Failed attempts"] = $"{limit} within {(int)Window.TotalMinutes} minutes",
                        ["Blocked until (local)"] = (now + Block).ToLocalTime().ToString("dd-MMM-yyyy HH:mm"),
                        ["Meaning"] = isUser ? "Password guessing against one account" : "Many accounts tried from one address",
                    });
            }
        }
    }

    public static void RecordSuccess(string? userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
            Entries.TryRemove(UserKey(userName), out _);
    }

    public static void ResetAll() => Entries.Clear();

    private static IEnumerable<string> Keys(string? userName, string? ip)
    {
        if (!string.IsNullOrWhiteSpace(userName)) yield return UserKey(userName);
        if (!string.IsNullOrWhiteSpace(ip)) yield return "ip:" + ip.Trim();
    }

    private static string UserKey(string userName) => "u:" + userName.Trim().ToLowerInvariant();

    private static void Purge(DateTime now)
    {
        foreach (var pair in Entries)
        {
            lock (pair.Value)
            {
                if (pair.Value.BlockedUntilUtc < now && now - pair.Value.WindowStartUtc > Window)
                    Entries.TryRemove(pair.Key, out _);
            }
        }
    }
}

/// <summary>Applies <see cref="LoginThrottle"/> to a login action. The user name is read from the action's model (UserName / Email / MobileNo).</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LoginThrottleAttribute : ActionFilterAttribute
{
    private const string UserKey = "__loginThrottleUser";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userName = ReadUserName(context.ActionArguments.Values);
        context.HttpContext.Items[UserKey] = userName;
        var wait = LoginThrottle.BlockedFor(userName, ClientIp(context.HttpContext));
        if (wait != null)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling(wait.Value.TotalMinutes));
            context.HttpContext.Response.Headers["Retry-After"] = ((int)wait.Value.TotalSeconds).ToString();
            context.Result = new ObjectResult(new
            {
                success = false,
                code = "TOO_MANY_FAILED_LOGINS",
                message = $"Too many failed sign-in attempts. Try again in {minutes} minute(s)."
            })
            { StatusCode = StatusCodes.Status429TooManyRequests };
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var userName = context.HttpContext.Items[UserKey] as string;
        var status = (context.Result as IStatusCodeActionResult)?.StatusCode ?? (context.Exception == null ? 200 : 500);
        if (status == StatusCodes.Status401Unauthorized)
            LoginThrottle.RecordFailure(userName, ClientIp(context.HttpContext));
        else if (status >= 200 && status < 300)
            LoginThrottle.RecordSuccess(userName);
    }

    /// <summary>
    /// Loopback means the caller came through a local proxy or tunnel that hid the real address, so every user
    /// would share one IP bucket; only the per-user-name limit applies then.
    /// </summary>
    private static string? ClientIp(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip == null) return null;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        return System.Net.IPAddress.IsLoopback(ip) ? null : ip.ToString();
    }

    private static string? ReadUserName(IEnumerable<object?> arguments)
    {
        foreach (var arg in arguments)
        {
            if (arg == null) continue;
            if (arg is string s) return s;
            foreach (var name in new[] { "UserName", "Username", "Email", "MobileNo", "Mobile" })
            {
                var value = arg.GetType().GetProperty(name)?.GetValue(arg) as string;
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
        }
        return null;
    }
}

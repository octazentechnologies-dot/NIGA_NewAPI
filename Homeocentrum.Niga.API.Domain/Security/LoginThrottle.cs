using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Homeocentrum.Niga.API.Domain.Security;

/// <summary>
/// Counts failed sign-ins per user name (in-memory, per API instance).
/// There is no wait / lockout: the user may keep trying.
/// After more than <see cref="PasswordResetHintAfterFailures"/> failures, the login response
/// asks them to change the password via forget password.
/// </summary>
public static class LoginThrottle
{
    /// <summary>Wrong attempts above this number get the forget-password message.</summary>
    public const int PasswordResetHintAfterFailures = 15;

    public const string ForgetPasswordMessage =
        "Please change password via forget password";

    private sealed class Entry
    {
        public int Failures;
    }

    // Case-sensitive keys: Tufan_Patient and tufan_patient are different counters.
    private static readonly ConcurrentDictionary<string, Entry> Entries = new(StringComparer.Ordinal);

    public static int FailureCount(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return 0;
        return Entries.TryGetValue(UserKey(userName), out var e) ? e.Failures : 0;
    }

    public static bool ShouldSuggestPasswordReset(string? userName)
        => FailureCount(userName) > PasswordResetHintAfterFailures;

    public static void RecordFailure(string? userName, string? ip = null, DateTime? nowUtc = null)
    {
        _ = ip;
        _ = nowUtc;
        if (string.IsNullOrWhiteSpace(userName))
            return;
        if (Entries.Count > 50_000)
            Purge();
        var e = Entries.GetOrAdd(UserKey(userName), _ => new Entry());
        lock (e)
        {
            e.Failures++;
        }
    }

    public static void RecordSuccess(string? userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
            Entries.TryRemove(UserKey(userName), out _);
    }

    public static void ResetAll() => Entries.Clear();

    private static string UserKey(string userName) => "u:" + userName.Trim();

    private static void Purge()
    {
        // Soft cap only: drop entries that have never exceeded the hint threshold.
        foreach (var pair in Entries)
        {
            lock (pair.Value)
            {
                if (pair.Value.Failures <= PasswordResetHintAfterFailures)
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
        context.HttpContext.Items[UserKey] = ReadUserName(context.ActionArguments.Values);
        // No wait / lockout — always allow the login attempt.
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var userName = context.HttpContext.Items[UserKey] as string;
        var status = (context.Result as IStatusCodeActionResult)?.StatusCode ?? (context.Exception == null ? 200 : 500);
        if (status == StatusCodes.Status401Unauthorized)
        {
            LoginThrottle.RecordFailure(userName);
            if (LoginThrottle.ShouldSuggestPasswordReset(userName))
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = LoginThrottle.ForgetPasswordMessage
                })
                { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
        else if (status >= 200 && status < 300)
        {
            LoginThrottle.RecordSuccess(userName);
        }
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Niga_Domain.DTOs;
using Niga_Domain.Services;

namespace Niga_Domain.Logging
{
    public class ErrorAlertOptions
    {
        /// <summary>When false, runtime alert emails are not sent.</summary>
        public bool Enabled { get; set; } = true;
        public string[] Recipients { get; set; } = Array.Empty<string>();
        public int CooldownMinutes { get; set; } = 10;
    }

    public class FileLogOptions
    {
        /// <summary>When false, no files are written under Logs/.</summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Writes INFO / WARN / ERROR / CRITICAL (and DEBUG) under {ContentRoot}/Logs.
    /// Emails runtime failures to ErrorAlert:Recipients when ErrorAlert:Enabled is true.
    /// </summary>
    public static class AppFileLog
    {
        private static readonly object Gate = new();
        private static readonly ConcurrentDictionary<string, DateTime> LastAlert = new();
        private static readonly AsyncLocal<Dictionary<string, string>?> RequestSnapshot = new();
        private static string _root = Path.Combine(AppContext.BaseDirectory, "Logs");
        private static string _application = "NIGA New-API";
        private static ErrorAlertOptions _alert = new();
        private static FileLogOptions _file = new();
        private static SmtpSettingsModel? _smtp;

        public static string LogsDirectory => _root;
        public static bool IsFileEnabled => _file?.Enabled == true;
        public static bool IsAlertEnabled => _alert?.Enabled == true;

        public static void SetRequestSnapshot(Dictionary<string, string>? details)
            => RequestSnapshot.Value = details;

        public static void ClearRequestSnapshot()
            => RequestSnapshot.Value = null;

        public static void Initialize(string contentRoot, IConfiguration config, string? applicationName = null)
        {
            _application = string.IsNullOrWhiteSpace(applicationName) ? "NIGA New-API (Niga-Web)" : applicationName;
            _root = Path.Combine(contentRoot ?? AppContext.BaseDirectory, "Logs");
            _file = config.GetSection("FileLog").Get<FileLogOptions>() ?? new FileLogOptions();
            _alert = config.GetSection("ErrorAlert").Get<ErrorAlertOptions>() ?? new ErrorAlertOptions();
            if (_alert.Recipients == null || _alert.Recipients.Length == 0)
            {
                _alert.Recipients = config.GetSection("ErrorAlert:Recipients").GetChildren()
                    .Select(c => c.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToArray()!;
            }
            _smtp = config.GetSection("smtp").Get<SmtpSettingsModel>();
            if (IsFileEnabled)
            {
                Directory.CreateDirectory(_root);
                Write("app", "INFO", "AppFileLog",
                    "File logging started at " + _root + " FileLog.Enabled=" + _file.Enabled + " ErrorAlert.Enabled=" + _alert.Enabled,
                    sendAlert: false);
            }
        }

        public static void Write(string kind, string level, string category, string message, Exception? ex = null,
            IDictionary<string, string>? details = null, bool sendAlert = true)
        {
            try
            {
                details = MergeSnapshot(details);
                if (IsFileEnabled)
                {
                    Directory.CreateDirectory(_root);
                    var day = DateTime.Now.ToString("yyyyMMdd");
                    var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var line = stamp + "\t" + level + "\t" + category + "\t" + Sanitize(message);
                    if (details != null)
                    {
                        foreach (var kv in details)
                        {
                            if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value))
                                continue;
                            line += "\t" + kv.Key + "=" + Sanitize(kv.Value);
                        }
                    }
                    if (ex != null)
                        line += Environment.NewLine + ex;

                    lock (Gate)
                    {
                        File.AppendAllText(Path.Combine(_root, $"niga-all-{day}.log"), line + Environment.NewLine);
                        File.AppendAllText(Path.Combine(_root, $"niga-{kind}-{day}.log"), line + Environment.NewLine);
                    }
                }

                if (sendAlert && IsFailureLevel(level))
                    TryEmail(level, category, message, ex, details);
            }
            catch
            {
                // Logging must never throw.
            }
        }

        public static void Audit(string action, string entity, long? userId, string? role, string? extra = null)
        {
            Write("audit", "INFO", entity,
                $"user={userId} role={role} action={action} {extra}".Trim(),
                sendAlert: false);
        }

        public static Dictionary<string, string> BaseDetails()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Application"] = _application,
                ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "",
                ["Machine"] = Environment.MachineName,
                ["ProcessId"] = Process.GetCurrentProcess().Id.ToString(),
                ["LoggedAtLocal"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ["LoggedAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "Z",
                ["OccurredAtLocal"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ["OccurredAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "Z",
                ["ServerTimeZone"] = TimeZoneInfo.Local.DisplayName,
                ["LogsDirectory"] = _root
            };
        }

        private static IDictionary<string, string>? MergeSnapshot(IDictionary<string, string>? details)
        {
            var snap = RequestSnapshot.Value;
            if (snap == null || snap.Count == 0)
                return details;
            var merged = new Dictionary<string, string>(snap, StringComparer.OrdinalIgnoreCase);
            if (details != null)
            {
                foreach (var kv in details)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                        merged[kv.Key] = kv.Value;
                }
            }
            return merged;
        }

        private static bool IsFailureLevel(string level)
        {
            return string.Equals(level, "ERROR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(level, "CRITICAL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(level, "FAIL", StringComparison.OrdinalIgnoreCase);
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\r", " ").Replace("\n", " | ");
        }

        private static void TryEmail(string level, string category, string message, Exception? ex,
            IDictionary<string, string>? details)
        {
            if (!IsAlertEnabled || _alert.Recipients == null || _alert.Recipients.Length == 0 || _smtp == null)
                return;

            var key = (level + "|" + category + "|" + (message ?? "")).GetHashCode().ToString();
            var cooldown = TimeSpan.FromMinutes(_alert.CooldownMinutes <= 0 ? 10 : _alert.CooldownMinutes);
            var now = DateTime.UtcNow;
            if (LastAlert.TryGetValue(key, out var prev) && now - prev < cooldown)
                return;
            LastAlert[key] = now;

            var merged = BaseDetails();
            merged["Level"] = level ?? "";
            merged["Category"] = category ?? "";
            if (details != null)
            {
                foreach (var kv in details)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null)
                        merged[kv.Key] = kv.Value;
                }
            }

            var who = First(merged, "UserName", "DisplayName", "User", "ClientUserName");
            var browser = First(merged, "Browser");
            var device = First(merged, "DeviceName");
            var subject = "[" + _application + "] " + level + " — " + category;
            if (!string.IsNullOrWhiteSpace(who))
                subject += " user=" + who;
            if (!string.IsNullOrWhiteSpace(browser))
                subject += " " + browser;
            if (!string.IsNullOrWhiteSpace(device))
                subject += " / " + device;
            if (merged.TryGetValue("Method", out var method) && merged.TryGetValue("Path", out var path))
                subject += " " + method + " " + Truncate(path, 80);

            var body = BuildAlertHtml(level, category, message, ex, merged);
            var sender = new EmailSenderService();
            foreach (var to in _alert.Recipients.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var ok = sender.SendMail(new EmailSenderModel
                {
                    ToAddress = to.Trim(),
                    Subject = Truncate(subject, 180),
                    Body = body,
                    isHtml = true
                }, _smtp);
                if (!ok)
                {
                    Write("errors", "WARN", "ErrorAlert",
                        "Alert email failed to " + to + ": " + sender.LastError,
                        sendAlert: false);
                }
            }
        }

        private static string BuildAlertHtml(string level, string category, string message, Exception? ex,
            IDictionary<string, string> details)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='font-family:Segoe UI,Arial,sans-serif;font-size:13px;color:#222'>");
            sb.Append("<h2 style='margin:0 0 8px'>Homeocentrum runtime ").Append(WebUtility.HtmlEncode(level)).Append("</h2>");
            sb.Append("<p style='margin:0 0 12px'>Who hit this, when, from which browser/device, and the detailed error log.</p>");

            RenderSection(sb, "Who", details, "UserName", "DisplayName", "User", "UserId", "Role", "DoctorId", "DoctorUserId",
                "ClientUserName", "ClientUserId", "ClientRole", "Authenticated");
            RenderSection(sb, "When", details, "OccurredAtLocal", "OccurredAtUtc", "LoggedAtLocal", "LoggedAtUtc",
                "ServerTimeZone", "TimeZone", "ElapsedMs");
            RenderSection(sb, "Where", details, "Application", "Environment", "Host", "Scheme", "Path", "Method",
                "ClientUrl", "Href", "Referrer", "RemoteIp", "ForwardedFor", "Machine", "TraceId", "Status");
            RenderSection(sb, "Browser / device", details, "Browser", "BrowserVersion", "Os", "DeviceType", "DeviceName",
                "Platform", "Screen", "Language", "UserAgent");

            sb.Append("<h3>Message</h3><pre style='white-space:pre-wrap;background:#f8f8f8;padding:10px;border:1px solid #ddd'>")
                .Append(WebUtility.HtmlEncode(message ?? "")).Append("</pre>");

            sb.Append("<h3>Detailed error log</h3>");
            if (ex != null)
            {
                sb.Append("<pre style='white-space:pre-wrap;background:#fff4f4;padding:10px;border:1px solid #e0b0b0'>")
                    .Append(WebUtility.HtmlEncode(ex.ToString())).Append("</pre>");
                var inner = ex.InnerException;
                var n = 1;
                while (inner != null && n <= 5)
                {
                    sb.Append("<h4>Inner exception ").Append(n).Append("</h4><pre style='white-space:pre-wrap;background:#fff4f4;padding:10px;border:1px solid #e0b0b0'>")
                        .Append(WebUtility.HtmlEncode(inner.ToString())).Append("</pre>");
                    inner = inner.InnerException;
                    n++;
                }
            }
            if (details.TryGetValue("Stack", out var stack) && !string.IsNullOrWhiteSpace(stack))
            {
                sb.Append("<h4>Client stack</h4><pre style='white-space:pre-wrap;background:#fff4f4;padding:10px;border:1px solid #e0b0b0'>")
                    .Append(WebUtility.HtmlEncode(stack)).Append("</pre>");
            }
            if (details.TryGetValue("ComponentStack", out var cstack) && !string.IsNullOrWhiteSpace(cstack))
            {
                sb.Append("<h4>React component stack</h4><pre style='white-space:pre-wrap;background:#fff4f4;padding:10px;border:1px solid #e0b0b0'>")
                    .Append(WebUtility.HtmlEncode(cstack)).Append("</pre>");
            }

            var tail = ReadRecentLogTail();
            if (!string.IsNullOrWhiteSpace(tail))
            {
                sb.Append("<h4>Recent log file tail</h4><pre style='white-space:pre-wrap;background:#111;color:#eee;padding:10px;border:1px solid #333;max-height:320px;overflow:auto'>")
                    .Append(WebUtility.HtmlEncode(tail)).Append("</pre>");
            }

            sb.Append("<h3>All diagnostic fields</h3>");
            sb.Append("<table cellpadding='6' cellspacing='0' style='border-collapse:collapse;border:1px solid #ccc'>");
            foreach (var kv in details)
            {
                sb.Append("<tr><td style='border:1px solid #ccc;background:#f6f6f6;white-space:nowrap'><b>")
                    .Append(WebUtility.HtmlEncode(kv.Key))
                    .Append("</b></td><td style='border:1px solid #ccc'>")
                    .Append(WebUtility.HtmlEncode(kv.Value ?? ""))
                    .Append("</td></tr>");
            }
            sb.Append("</table>");
            sb.Append("<p>Log files: ").Append(WebUtility.HtmlEncode(_root)).Append("</p></div>");
            return sb.ToString();
        }

        private static void RenderSection(StringBuilder sb, string title, IDictionary<string, string> details, params string[] keys)
        {
            sb.Append("<h3>").Append(WebUtility.HtmlEncode(title)).Append("</h3>");
            sb.Append("<table cellpadding='6' cellspacing='0' style='border-collapse:collapse;border:1px solid #ccc;margin-bottom:12px'>");
            foreach (var key in keys)
            {
                details.TryGetValue(key, out var value);
                sb.Append("<tr><td style='border:1px solid #ccc;background:#f6f6f6;white-space:nowrap'><b>")
                    .Append(WebUtility.HtmlEncode(key))
                    .Append("</b></td><td style='border:1px solid #ccc'>")
                    .Append(WebUtility.HtmlEncode(value ?? ""))
                    .Append("</td></tr>");
            }
            sb.Append("</table>");
        }

        private static string First(IDictionary<string, string> details, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (details.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return "";
        }

        private static string ReadRecentLogTail()
        {
            try
            {
                if (!IsFileEnabled) return "";
                var day = DateTime.Now.ToString("yyyyMMdd");
                var path = Path.Combine(_root, "niga-errors-" + day + ".log");
                if (!File.Exists(path))
                    path = Path.Combine(_root, "niga-all-" + day + ".log");
                if (!File.Exists(path)) return "";
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length > 12000)
                    fs.Seek(-12000, SeekOrigin.End);
                using var reader = new StreamReader(fs, Encoding.UTF8, true, 1024, true);
                var text = reader.ReadToEnd();
                var lines = text.Replace("\r\n", "\n").Split('\n');
                var take = Math.Min(40, lines.Length);
                return string.Join("\n", lines, lines.Length - take, take).Trim();
            }
            catch
            {
                return "";
            }
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text ?? "";
            return text.Substring(0, max) + "…";
        }
    }

    public sealed class AppFileLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new AppFileLogger(categoryName);
        public void Dispose() { }
    }

    internal sealed class AppFileLogger : ILogger
    {
        private readonly string _category;
        public AppFileLogger(string category) => _category = category;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None) return false;
            var microsoft = _category.StartsWith("Microsoft.", StringComparison.Ordinal)
                || _category.StartsWith("System.", StringComparison.Ordinal);
            if (microsoft && logLevel < LogLevel.Warning) return false;
            if (AppFileLog.IsFileEnabled && logLevel >= LogLevel.Debug) return true;
            if (AppFileLog.IsAlertEnabled && logLevel >= LogLevel.Error) return true;
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var level = logLevel switch
            {
                LogLevel.Critical => "CRITICAL",
                LogLevel.Error => "ERROR",
                LogLevel.Warning => "WARN",
                LogLevel.Debug => "DEBUG",
                LogLevel.Trace => "TRACE",
                _ => "INFO"
            };
            var kind = exception != null || logLevel >= LogLevel.Error ? "errors" : "app";
            AppFileLog.Write(kind, level, _category, formatter(state, exception), exception);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

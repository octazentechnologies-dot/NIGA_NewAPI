using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Domain.Logging
{
    public class ErrorAlertOptions
    {
        /// <summary>When false, runtime alert emails are not sent.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When true, one matrix email is sent at local midnight for the calendar day that just ended.
        /// Independent of <see cref="Enabled"/>: instant alerts and the daily matrix can be switched separately.
        /// </summary>
        public bool DailyMatrixEnabled { get; set; } = true;

        public string[] Recipients { get; set; } = Array.Empty<string>();
        public int CooldownMinutes { get; set; } = 10;

        /// <summary>When true, 4xx / WARN / slow requests also email (UI axios, API, performance).</summary>
        public bool AlertOnWarn { get; set; } = true;

        /// <summary>Successful API calls slower than this (ms) email a performance WARN. 0 disables.</summary>
        public int SlowRequestMs { get; set; } = 3000;
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

        /// <summary>Logs/22-Sep-2026 — one folder per local calendar day.</summary>
        public static string DayDirectory(DateTime day)
            => Path.Combine(_root, day.ToString("dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture));
        public static bool IsFileEnabled => _file?.Enabled == true;
        public static bool IsAlertEnabled => _alert?.Enabled == true;

        /// <summary>ErrorAlert:DailyMatrixEnabled. Default on when the section is missing.</summary>
        public static bool IsDailyMatrixEnabled => _alert?.DailyMatrixEnabled == true;
        public static bool IsWarnAlertEnabled => _alert?.AlertOnWarn != false;
        public static int SlowRequestMilliseconds => _alert?.SlowRequestMs > 0 ? _alert.SlowRequestMs : 3000;

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
                MoveTodayFilesIntoDayFolder();
                Write("app", "INFO", "AppFileLog",
                    "File logging started at " + DayDirectory(DateTime.Now) + " FileLog.Enabled=" + _file.Enabled + " ErrorAlert.Enabled=" + _alert.Enabled,
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
                    var now = DateTime.Now;
                    var folder = DayDirectory(now);
                    Directory.CreateDirectory(folder);
                    var day = now.ToString("yyyyMMdd");
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

                    var type = LogFileType(kind, category);
                    lock (Gate)
                    {
                        File.AppendAllText(Path.Combine(folder, LogFileName(type, day)), line + Environment.NewLine);
                    }
                }

                if (sendAlert && IsFailureLevel(level) && !ShouldSkipAlert(category, message, ex, details))
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
                ["LogsDirectory"] = DayDirectory(DateTime.Now)
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
            if (string.Equals(level, "ERROR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(level, "CRITICAL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(level, "FAIL", StringComparison.OrdinalIgnoreCase))
                return true;
            if (IsWarnAlertEnabled && string.Equals(level, "WARN", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static bool ShouldSkipAlert(string category, string message, Exception? ex,
            IDictionary<string, string>? details)
        {
            var text = (message ?? "") + " " + (ex?.Message ?? "") + " " + (ex?.GetType().Name ?? "");
            if (text.IndexOf("address already in use", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("Hosting failed to start", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (text.IndexOf("BackgroundService failed", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (ex is OperationCanceledException || ex is TaskCanceledException)
                return true;
            if (!string.IsNullOrEmpty(category) && category.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
                return true;
            if (details != null)
            {
                if (details.TryGetValue("Path", out var skipPath))
                {
                    if (skipPath.IndexOf("/json/version", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                    if (skipPath.IndexOf("/Diagnostics/", StringComparison.OrdinalIgnoreCase) >= 0
                        && skipPath.IndexOf("ClientError", StringComparison.OrdinalIgnoreCase) < 0)
                        return true;
                }
                details.TryGetValue("Status", out var status);
                details.TryGetValue("HasBearer", out var hasBearer);
                details.TryGetValue("Authenticated", out var authenticated);
                if (status == "401"
                    && !string.Equals(hasBearer, "yes", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(authenticated, "yes", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (status == "503")
                {
                    var pth = details.TryGetValue("Path", out var pathVal) ? pathVal : "";
                    if (pth.IndexOf("/Payments/Webhook", StringComparison.OrdinalIgnoreCase) >= 0
                        || text.IndexOf("GATEWAY_NOT_CONFIGURED", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            if (string.Equals(category, "Microsoft.Extensions.Hosting.Internal.Host", StringComparison.OrdinalIgnoreCase)
                && text.IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
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

            string? traceId = null;
            details?.TryGetValue("TraceId", out traceId);
            var key = !string.IsNullOrWhiteSpace(traceId)
                ? ("trace|" + traceId.Trim())
                : (level + "|" + category + "|" + (message ?? ""));
            var now = DateTime.UtcNow;
            if (_alert.CooldownMinutes > 0)
            {
                var cooldown = TimeSpan.FromMinutes(_alert.CooldownMinutes);
                if (LastAlert.TryGetValue(key, out var prev) && now - prev < cooldown)
                    return;
            }
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

            var source = string.Equals(category, "UI", StringComparison.OrdinalIgnoreCase) ? "UI" : "New API";
            merged["Source"] = source;
            var subject = "Homeocentrum Runtime ERROR - " + source + " - " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

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

        /// <summary>
        /// Midnight matrix. Subject stays "Homeocentrum Runtime ERROR - {host} - {local time}".
        /// The type split (UI vs this API) and the counts are in the body.
        /// </summary>
        public static void SendDailyMatrix(DateTime day, string hostSource)
        {
            if (!IsDailyMatrixEnabled || _alert.Recipients == null || _alert.Recipients.Length == 0 || _smtp == null)
                return;

            var rows = DailyIssueMatrix.Read(_root, day, hostSource);
            var body = DailyIssueMatrix.ToHtml(day, hostSource, rows);
            var subject = "Homeocentrum Runtime ERROR - " + hostSource + " - " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
            var sender = new EmailSenderService();
            foreach (var to in _alert.Recipients.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var ok = sender.SendMail(new EmailSenderModel
                {
                    ToAddress = to.Trim(),
                    Subject = subject,
                    Body = body,
                    isHtml = true
                }, _smtp);
                if (!ok)
                {
                    Write("errors", "WARN", "ErrorAlert",
                        "Daily matrix email failed to " + to + ": " + sender.LastError,
                        sendAlert: false);
                }
            }
        }

        private static readonly string[] WhoKeys =
        {
            "UserName", "DisplayName", "User", "UserId", "Role", "DoctorId", "DoctorUserId",
            "ClientUserName", "ClientUserId", "ClientRole", "Authenticated", "HasBearer"
        };

        private static readonly string[] WhenKeys =
        {
            "OccurredAtLocal", "OccurredAtUtc", "LoggedAtLocal", "LoggedAtUtc", "ServerTimeZone", "TimeZone", "ElapsedMs"
        };

        private static readonly string[] WhereKeys =
        {
            "Application", "Environment", "Host", "Scheme", "Path", "Method", "ClientUrl", "Href", "Referrer",
            "RemoteIp", "ForwardedFor", "Machine", "ProcessId", "TraceId", "Status", "ContentType", "LogsDirectory"
        };

        private static readonly string[] BrowserKeys =
        {
            "Browser", "BrowserVersion", "Os", "DeviceType", "DeviceName", "Platform", "Screen", "Language", "UserAgent"
        };

        private static string BuildAlertHtml(string level, string category, string message, Exception? ex,
            IDictionary<string, string> details)
        {
            var accent = string.Equals(level, "WARN", StringComparison.OrdinalIgnoreCase) ? "#b45309" : "#b91c1c";
            var shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head>");
            sb.Append("<body style=\"margin:0;padding:0;background:#e2e8f0;\">");
            sb.Append("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#e2e8f0\"><tr><td align=\"center\" style=\"padding:16px;\">");
            sb.Append("<table width=\"680\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#ffffff\" style=\"width:680px;max-width:100%;border:1px solid #94a3b8;\">");

            sb.Append("<tr><td bgcolor=\"").Append(accent).Append("\" style=\"padding:16px 20px;font-family:Segoe UI,Arial,sans-serif;color:#ffffff;\">");
            sb.Append("<div style=\"font-size:20px;font-weight:700;\">Homeocentrum Runtime ").Append(WebUtility.HtmlEncode((level ?? "ERROR").ToUpperInvariant())).Append("</div>");
            sb.Append("<div style=\"font-size:13px;margin-top:6px;\">Error log, audit log, and full diagnostic dump</div>");
            sb.Append("</td></tr>");

            OpenSectionCell(sb);
            RenderKvTable(sb, "Summary", new[]
            {
                Kv("Source", Val(details, "Source", "New API")),
                Kv("Level", Val(details, "Level", level)),
                Kv("Category", Val(details, "Category", category)),
                Kv("Application", Dash(Val(details, "Application"))),
                Kv("When (local)", Dash(First(details, "OccurredAtLocal", "LoggedAtLocal"))),
                Kv("Method / path", Dash(JoinVals(Val(details, "Method"), Val(details, "Path")))),
                Kv("Status", Dash(Val(details, "Status"))),
                Kv("TraceId", Dash(Val(details, "TraceId"))),
                Kv("User", Dash(JoinVals(First(details, "UserName", "User", "ClientUserName"), Val(details, "Role")))),
            }, shown);
            CloseSectionCell(sb);

            OpenSectionCell(sb);
            RenderNamedSection(sb, "Who", details, WhoKeys, shown, "Authenticated", "HasBearer");
            CloseSectionCell(sb);
            OpenSectionCell(sb);
            RenderNamedSection(sb, "When", details, WhenKeys, shown, "LoggedAtLocal");
            CloseSectionCell(sb);
            OpenSectionCell(sb);
            RenderNamedSection(sb, "Where", details, WhereKeys, shown, "Path", "Method", "Host");
            CloseSectionCell(sb);
            OpenSectionCell(sb);
            RenderNamedSection(sb, "Browser / device", details, BrowserKeys, shown);
            CloseSectionCell(sb);

            AppendPreBlock(sb, "Message", message);
            if (ex != null)
            {
                var detail = new StringBuilder();
                detail.Append(ex.ToString());
                var inner = ex.InnerException;
                var n = 1;
                while (inner != null && n <= 5)
                {
                    detail.Append("\n\n--- Inner exception ").Append(n).Append(" ---\n").Append(inner);
                    inner = inner.InnerException;
                    n++;
                }
                AppendPreBlock(sb, "Exception / stack", detail.ToString(), "#fff7ed", "#7c2d12");
            }

            if (details.TryGetValue("Stack", out var stack) && !string.IsNullOrWhiteSpace(stack))
                AppendPreBlock(sb, "Client stack", stack, "#fff7ed", "#7c2d12");
            if (details.TryGetValue("ComponentStack", out var cstack) && !string.IsNullOrWhiteSpace(cstack))
                AppendPreBlock(sb, "React component stack", cstack, "#fff7ed", "#7c2d12");
            if (details.TryGetValue("ClientError", out var ce) && !string.IsNullOrWhiteSpace(ce))
                AppendPreBlock(sb, "Client error", ce);

            shown.Add("Stack");
            shown.Add("ComponentStack");
            shown.Add("ClientError");
            shown.Add("Level");
            shown.Add("Category");

            AppendPreBlock(sb, "API log (Homeocentrum_api, latest)", ReadKindTail("api", 80, 12000), "#0f172a", "#e2e8f0");
            AppendPreBlock(sb, "Other log (Homeocentrum_other, latest)", ReadKindTail("other", 60, 8000), "#0f172a", "#e2e8f0");

            var leftover = details
                .Where(kv => !shown.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => Kv(kv.Key, kv.Value))
                .ToArray();
            OpenSectionCell(sb);
            RenderKvTable(sb, "Other diagnostic fields", leftover, shown);
            CloseSectionCell(sb);

            sb.Append("<tr><td style=\"padding:8px 20px 20px;font-family:Segoe UI,Arial,sans-serif;font-size:11px;color:#64748b;\">Log files: ")
                .Append(WebUtility.HtmlEncode(_root)).Append("</td></tr>");
            sb.Append("</table></td></tr></table></body></html>");
            return sb.ToString();
        }

        private static KeyValuePair<string, string> Kv(string key, string? value)
            => new(key, value ?? "");

        private static void OpenSectionCell(StringBuilder sb)
            => sb.Append("<tr><td style=\"padding:10px 20px 0;font-family:Segoe UI,Arial,sans-serif;font-size:13px;color:#0f172a;\">");

        private static void CloseSectionCell(StringBuilder sb)
            => sb.Append("</td></tr>");

        private static void RenderNamedSection(StringBuilder sb, string title, IDictionary<string, string> details,
            string[] keys, HashSet<string> shown, params string[] always)
        {
            var rows = new List<KeyValuePair<string, string>>();
            foreach (var key in keys)
            {
                var value = Val(details, key);
                var keep = !string.IsNullOrWhiteSpace(value) || always.Any(a => string.Equals(a, key, StringComparison.OrdinalIgnoreCase));
                if (keep)
                    rows.Add(Kv(key, string.IsNullOrWhiteSpace(value) ? "—" : value));
            }
            RenderKvTable(sb, title, rows.ToArray(), shown);
        }

        private static void RenderKvTable(StringBuilder sb, string title, KeyValuePair<string, string>[] rows, HashSet<string> shown)
        {
            sb.Append("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"1\" bordercolor=\"#cbd5e1\" bgcolor=\"#ffffff\" style=\"width:100%;border-collapse:collapse;margin:0 0 10px;\">");
            sb.Append("<tr><td colspan=\"2\" bgcolor=\"#0f172a\" style=\"background:#0f172a;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:13px;font-weight:700;padding:8px 10px;\">")
                .Append(WebUtility.HtmlEncode(title)).Append("</td></tr>");
            var any = false;
            foreach (var row in rows)
            {
                shown.Add(row.Key);
                if (string.IsNullOrWhiteSpace(row.Value)) continue;
                any = true;
                sb.Append("<tr>");
                sb.Append("<td width=\"200\" bgcolor=\"#f8fafc\" valign=\"top\" style=\"width:200px;background:#f8fafc;padding:7px 10px;font-family:Segoe UI,Arial,sans-serif;font-size:12px;font-weight:700;color:#334155;\">")
                    .Append(WebUtility.HtmlEncode(row.Key)).Append("</td>");
                sb.Append("<td width=\"480\" valign=\"top\" style=\"width:480px;padding:7px 10px;font-family:Segoe UI,Arial,sans-serif;font-size:12px;color:#0f172a;word-break:break-word;\">")
                    .Append(WebUtility.HtmlEncode(row.Value)).Append("</td>");
                sb.Append("</tr>");
            }
            if (!any)
            {
                sb.Append("<tr><td colspan=\"2\" style=\"padding:8px 10px;font-family:Segoe UI,Arial,sans-serif;font-size:12px;color:#64748b;\">No values captured</td></tr>");
            }
            sb.Append("</table>");
        }

        private static void AppendPreBlock(StringBuilder sb, string title, string? body, string bg = "#f8fafc", string color = "#0f172a")
        {
            var text = string.IsNullOrWhiteSpace(body) ? "(none for today)" : body;
            sb.Append("<tr><td style=\"padding:10px 20px 0;font-family:Segoe UI,Arial,sans-serif;font-size:13px;color:#0f172a;\">");
            sb.Append("<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"1\" bordercolor=\"#cbd5e1\" style=\"width:100%;border-collapse:collapse;margin:0 0 10px;\">");
            sb.Append("<tr><td bgcolor=\"#0f172a\" style=\"background:#0f172a;color:#ffffff;font-family:Segoe UI,Arial,sans-serif;font-size:13px;font-weight:700;padding:8px 10px;\">")
                .Append(WebUtility.HtmlEncode(title)).Append("</td></tr>");
            sb.Append("<tr><td bgcolor=\"").Append(bg).Append("\" style=\"background:").Append(bg)
                .Append(";padding:10px;font-family:Consolas,Courier New,monospace;font-size:11px;line-height:1.45;color:")
                .Append(color).Append(";white-space:pre-wrap;word-break:break-word;\">")
                .Append(WebUtility.HtmlEncode(text)).Append("</td></tr>");
            sb.Append("</table></td></tr>");
        }

        private static string Val(IDictionary<string, string> details, string key, string? fallback = null)
        {
            if (details != null && details.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
            return fallback ?? "";
        }

        private static string JoinVals(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a)) return b ?? "";
            if (string.IsNullOrWhiteSpace(b)) return a;
            return a + "  " + b;
        }

        private static string Dash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
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

        private static string ReadKindTail(string kind, int maxLines, int maxBytes)
        {
            try
            {
                if (!IsFileEnabled) return "File logging is disabled.";
                var day = DateTime.Now.ToString("yyyyMMdd");
                var path = Path.Combine(DayDirectory(DateTime.Now), LogFileName(kind, day));
                if (!File.Exists(path))
                    return "No " + LogFileName(kind, day) + " for today.";
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length > maxBytes)
                    fs.Seek(-maxBytes, SeekOrigin.End);
                using var reader = new StreamReader(fs, Encoding.UTF8, true, 1024, true);
                var text = reader.ReadToEnd();
                var lines = text.Replace("\r\n", "\n").Split('\n');
                var take = Math.Min(maxLines, lines.Length);
                var body = string.Join("\n", lines, lines.Length - take, take).Trim();
                return string.IsNullOrWhiteSpace(body) ? "Homeocentrum_" + kind + " log is empty today." : body;
            }
            catch (Exception ex)
            {
                return "Could not read Homeocentrum_" + kind + " log: " + ex.Message;
            }
        }

        /// <summary>Homeocentrum_api_yyyyMMdd.log or Homeocentrum_other_yyyyMMdd.log.</summary>
        private static string LogFileName(string type, string day)
            => "Homeocentrum_" + type + "_" + day + ".log";

        private static string LogFileType(string kind, string category)
        {
            if (string.Equals(category, "UI", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "ui", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "other", StringComparison.OrdinalIgnoreCase))
                return "other";
            return "api";
        }

        /// <summary>Files already written in Logs/ today move into Logs/dd-MMM-yyyy/.</summary>
        private static void MoveTodayFilesIntoDayFolder()
        {
            try
            {
                var day = DateTime.Now.ToString("yyyyMMdd");
                var folder = DayDirectory(DateTime.Now);
                Directory.CreateDirectory(folder);
                foreach (var path in Directory.GetFiles(_root, "niga-*-" + day + ".log")
                    .Concat(Directory.GetFiles(_root, "Homeocentrum_*_" + day + ".log")))
                {
                    var dest = Path.Combine(folder, Path.GetFileName(path));
                    if (File.Exists(dest))
                    {
                        File.AppendAllText(dest, File.ReadAllText(path));
                        File.Delete(path);
                    }
                    else
                    {
                        File.Move(path, dest);
                    }
                }
            }
            catch
            {
                // A locked file stays in Logs/ until the next start.
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

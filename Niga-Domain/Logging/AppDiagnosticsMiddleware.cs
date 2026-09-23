using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Niga_Domain.Logging
{
    /// <summary>Logs unhandled exceptions and API responses with status &gt;= 400 into Logs/.</summary>
    public sealed class AppDiagnosticsMiddleware
    {
        private readonly RequestDelegate _next;

        public AppDiagnosticsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            ApplyCorrelation(context);
            AppFileLog.SetRequestSnapshot(RequestDetails(context, null, 0));
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();
                var status = context.Response?.StatusCode ?? 0;
                var path = context.Request.Path.Value ?? "";
                if (IsSkipped(path))
                    return;
                if (status >= 400)
                {
                    var level = status >= 500 && status != 503 ? "ERROR" : "WARN";
                    var kind = level == "ERROR" ? "errors" : "api";
                    AppFileLog.Write(kind, level, "Http",
                        $"{status} {context.Request.Method} {path}{context.Request.QueryString} user={context.User?.Identity?.Name} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                        details: RequestDetails(context, status, sw.ElapsedMilliseconds));
                }
                else if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
                {
                    AppFileLog.Write("api", "INFO", "Http",
                        $"{status} {context.Request.Method} {path} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                        details: RequestDetails(context, status, sw.ElapsedMilliseconds),
                        sendAlert: false);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                var path = context.Request.Path.Value ?? "";
                AppFileLog.Write("errors", "ERROR", "Http",
                    $"UNHANDLED {context.Request.Method} {path}{context.Request.QueryString} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                    ex,
                    RequestDetails(context, 500, sw.ElapsedMilliseconds));
                await ApiProblem.WriteAsync(context, 500, "Something went wrong. Please try again.");
            }
            finally
            {
                AppFileLog.ClearRequestSnapshot();
            }
        }

        private static void ApplyCorrelation(HttpContext context)
        {
            var incoming = context.Request.Headers["X-Correlation-Id"].ToString();
            if (IsSafeTrace(incoming))
                context.TraceIdentifier = incoming.Trim();
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier ?? "";
                return Task.CompletedTask;
            });
        }

        private static bool IsSafeTrace(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 80) return false;
            foreach (var ch in value.Trim())
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')) return false;
            }
            return true;
        }

        private static bool IsSkipped(string path)
        {
            return path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                || path.IndexOf("/Diagnostics/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static Dictionary<string, string> RequestDetails(HttpContext context, int? status, long elapsedMs)
        {
            var details = AppFileLog.BaseDetails();
            details["TraceId"] = context.TraceIdentifier ?? "";
            details["Method"] = context.Request.Method ?? "";
            details["Path"] = (context.Request.Path.Value ?? "") + context.Request.QueryString;
            details["Status"] = status?.ToString() ?? "";
            details["ElapsedMs"] = elapsedMs.ToString();
            details["OccurredAtLocal"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            details["OccurredAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "Z";
            details["ContentType"] = context.Request.ContentType ?? "";
            ClientEnvironment.ApplyUser(details, context.User);
            ClientEnvironment.ApplyRequestPlace(details, context);
            ClientEnvironment.ApplyUserAgent(details, context.Request.Headers["User-Agent"].ToString());
            return details;
        }
    }
}

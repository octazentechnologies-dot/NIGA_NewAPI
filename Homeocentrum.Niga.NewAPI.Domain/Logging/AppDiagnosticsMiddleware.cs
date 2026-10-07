using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Homeocentrum.Niga.NewAPI.Domain.Logging
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
                        details: RequestDetails(context, status, sw.ElapsedMilliseconds),
                        sendAlert: level == "ERROR");
                }
                else if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
                {
                    var slow = AppFileLog.SlowRequestMilliseconds;
                    var isSlow = slow > 0 && sw.ElapsedMilliseconds >= slow;
                    AppFileLog.Write(isSlow ? "perf" : "api", isSlow ? "WARN" : "INFO", isSlow ? "Performance" : "Http",
                        $"{status} {context.Request.Method} {path} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                        details: RequestDetails(context, status, sw.ElapsedMilliseconds),
                        sendAlert: isSlow);
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                var path = context.Request.Path.Value ?? "";
                var errorId = Errors.SafeError.NewErrorId();
                var details = RequestDetails(context, 500, sw.ElapsedMilliseconds);
                details["ErrorId"] = errorId;
                AppFileLog.Write("errors", "ERROR", "Http",
                    $"UNHANDLED errorId={errorId} {context.Request.Method} {path}{context.Request.QueryString} {sw.ElapsedMilliseconds}ms trace={context.TraceIdentifier}",
                    ex,
                    details);
                await WriteSafeErrorAsync(context, errorId);
            }
            finally
            {
                AppFileLog.ClearRequestSnapshot();
            }
        }

        private static async Task WriteSafeErrorAsync(HttpContext context, string errorId)
        {
            if (context.Response.HasStarted)
                return;
            context.Response.Clear();
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var body = new Errors.SafeErrorBody { ErrorId = errorId, TraceId = context.TraceIdentifier };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
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
                || path.Equals("/json/version", StringComparison.OrdinalIgnoreCase)
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

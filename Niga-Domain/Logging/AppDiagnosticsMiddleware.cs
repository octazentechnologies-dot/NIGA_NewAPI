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
            AppFileLog.SetRequestSnapshot(RequestDetails(context, null, 0));
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();
                var status = context.Response?.StatusCode ?? 0;
                if (status >= 400)
                {
                    var path = context.Request.Path.Value ?? "";
                    if (path.IndexOf("/Diagnostics/", StringComparison.OrdinalIgnoreCase) >= 0)
                        return;
                    var level = status >= 500 ? "ERROR" : "WARN";
                    var kind = status >= 500 ? "errors" : "api";
                    AppFileLog.Write(kind, level, "Http",
                        $"{status} {context.Request.Method} {path}{context.Request.QueryString} user={context.User?.Identity?.Name} {sw.ElapsedMilliseconds}ms",
                        details: RequestDetails(context, status, sw.ElapsedMilliseconds));
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                var path = context.Request.Path.Value ?? "";
                AppFileLog.Write("errors", "ERROR", "Http",
                    $"UNHANDLED {context.Request.Method} {path}{context.Request.QueryString} {sw.ElapsedMilliseconds}ms",
                    ex,
                    RequestDetails(context, 500, sw.ElapsedMilliseconds));
                throw;
            }
            finally
            {
                AppFileLog.ClearRequestSnapshot();
            }
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

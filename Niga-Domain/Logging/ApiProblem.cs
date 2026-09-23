using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Niga_Domain.Logging
{
    /// <summary>
    /// One error shape for every API: success, a short message, the trace id, and field errors.
    /// </summary>
    public static class ApiProblem
    {
        public static object Body(HttpContext context, string message, IDictionary<string, string[]>? errors = null)
        {
            return new
            {
                success = false,
                message,
                traceId = context.TraceIdentifier,
                errors
            };
        }

        public static async Task WriteAsync(HttpContext context, int status, string message, IDictionary<string, string[]>? errors = null)
        {
            if (context.Response.HasStarted)
                return;
            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            var json = ToJson(context.TraceIdentifier, message, errors);
            await context.Response.WriteAsync(json);
        }

        public static IActionResult Validation(ActionContext context)
        {
            var errors = context.ModelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage) ? "This value is not valid." : error.ErrorMessage).ToArray());
            return new BadRequestObjectResult(Body(context.HttpContext, "Please check the highlighted fields.", errors));
        }

        public static string ToJson(string? traceId, string message, IDictionary<string, string[]>? errors)
        {
            var sb = new StringBuilder();
            sb.Append("{\"success\":false,\"message\":");
            sb.Append(Quote(message));
            sb.Append(",\"traceId\":");
            sb.Append(Quote(traceId));
            sb.Append(",\"errors\":");
            if (errors == null || errors.Count == 0)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append('{');
                var first = true;
                foreach (var pair in errors)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(Quote(pair.Key));
                    sb.Append(":[");
                    for (var i = 0; i < pair.Value.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(Quote(pair.Value[i]));
                    }
                    sb.Append(']');
                }
                sb.Append('}');
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string Quote(string? value)
        {
            if (value == null) return "null";
            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", " ")
                .Replace("\n", " ") + "\"";
        }
    }
}

using System.Security.Cryptography;
using Homeocentrum.Niga.NewAPI.Domain.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Homeocentrum.Niga.NewAPI.Domain.Errors
{
    /// <summary>
    /// The only body a client sees for a server failure. Internal details go to the error log under the same error id.
    /// </summary>
    public sealed class SafeErrorBody
    {
        public bool Success { get; init; }
        public int Status { get; init; } = 500;
        public string Message { get; init; } = SafeError.GenericMessage;
        public string ErrorId { get; init; } = "";
        public string? TraceId { get; init; }
    }

    public static class SafeError
    {
        public const string GenericMessage = "Something went wrong on our side. Please try again. If it keeps happening, contact support with the error id.";

        public static string NewErrorId()
            => "E" + DateTime.UtcNow.ToString("yyMMdd") + "-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(4));

        /// <summary>Logs the exception with a new error id and returns the body to send.</summary>
        public static SafeErrorBody Capture(Exception ex, HttpContext? context, string? where = null, int status = 500)
        {
            var errorId = NewErrorId();
            Log(errorId, ex.GetType().Name, ex, context, where);
            return new SafeErrorBody { Status = status, ErrorId = errorId, TraceId = context?.TraceIdentifier };
        }

        /// <summary>Logs a raw failure text that was about to reach the client and returns the safe body instead.</summary>
        public static SafeErrorBody CaptureText(string? text, HttpContext? context, int status, string? where = null)
        {
            var errorId = NewErrorId();
            Log(errorId, text, null, context, where);
            return new SafeErrorBody { Status = status, ErrorId = errorId, TraceId = context?.TraceIdentifier };
        }

        private static void Log(string errorId, string? summary, Exception? ex, HttpContext? context, string? where)
        {
            var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ErrorId"] = errorId,
            };
            if (context != null)
            {
                details["TraceId"] = context.TraceIdentifier ?? "";
                details["Method"] = context.Request.Method ?? "";
                details["Path"] = context.Request.Path.Value ?? "";
            }
            AppFileLog.Write("errors", "ERROR", where ?? "ServerError",
                $"errorId={errorId} {summary}".Trim(), ex, details);
        }

        public static ObjectResult ServerError(this ControllerBase controller, Exception ex, int status = 500)
        {
            var where = controller.ControllerContext?.ActionDescriptor?.DisplayName;
            return new ObjectResult(Capture(ex, controller.HttpContext, where, status)) { StatusCode = status };
        }
    }

    /// <summary>
    /// Safety net for any 5xx result that still carries raw text or an exception message. The text is logged
    /// with an error id and the client receives <see cref="SafeErrorBody"/>.
    /// </summary>
    public sealed class SafeServerErrorResultFilter : IAlwaysRunResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            switch (context.Result)
            {
                case ObjectResult obj when (obj.StatusCode ?? 200) >= 500 && obj.Value is not SafeErrorBody:
                    var status = obj.StatusCode ?? 500;
                    if (IsCodedFailure(obj.Value))
                        return;
                    context.Result = new ObjectResult(SafeError.CaptureText(Describe(obj.Value), context.HttpContext, status,
                        context.ActionDescriptor.DisplayName)) { StatusCode = status };
                    break;
                case ContentResult content when (content.StatusCode ?? 200) >= 500:
                    var code = content.StatusCode ?? 500;
                    context.Result = new ObjectResult(SafeError.CaptureText(content.Content, context.HttpContext, code,
                        context.ActionDescriptor.DisplayName)) { StatusCode = code };
                    break;
            }
        }

        public void OnResultExecuted(ResultExecutedContext context) { }

        /// <summary>5xx bodies built by the API itself (missing table, gateway not configured) carry a fixed code and message, never exception text.</summary>
        private static bool IsCodedFailure(object? value)
        {
            if (value == null) return false;
            var code = value.GetType().GetProperty("code") ?? value.GetType().GetProperty("Code");
            return code?.GetValue(value) is string s && !string.IsNullOrWhiteSpace(s);
        }

        private static string Describe(object? value)
        {
            if (value == null) return "(empty 5xx body)";
            if (value is string s) return s;
            try { return System.Text.Json.JsonSerializer.Serialize(value); }
            catch { return value.GetType().Name; }
        }
    }
}

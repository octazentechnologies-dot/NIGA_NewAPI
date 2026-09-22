using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Logging;

namespace Niga_Domain.API.Controllers
{
    [Route("api/Diagnostics")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        public class ClientErrorRequest
        {
            public string? Source { get; set; }
            public string? Url { get; set; }
            public int? Status { get; set; }
            public string? Method { get; set; }
            public string? Message { get; set; }
            public string? Stack { get; set; }
            public string? UserName { get; set; }
            public string? UserId { get; set; }
            public string? Role { get; set; }
            public string? UserAgent { get; set; }
            public string? ComponentStack { get; set; }
            public string? Browser { get; set; }
            public string? Os { get; set; }
            public string? DeviceType { get; set; }
            public string? DeviceName { get; set; }
            public string? Platform { get; set; }
            public string? Screen { get; set; }
            public string? Language { get; set; }
            public string? TimeZone { get; set; }
            public string? Href { get; set; }
            public string? Referrer { get; set; }
            public string? DisplayName { get; set; }
        }

        /// <summary>SPA / runtime client failures land in Logs/niga-ui-*.log and email on 5xx / window errors.</summary>
        [HttpPost("ClientError")]
        [AllowAnonymous]
        public IActionResult ClientError([FromBody] ClientErrorRequest request)
        {
            request ??= new ClientErrorRequest();
            var status = request.Status ?? 0;
            var level = status >= 500 || string.Equals(request.Source, "window", StringComparison.OrdinalIgnoreCase)
                ? "ERROR" : "WARN";
            var kind = level == "ERROR" ? "errors" : "ui";
            var details = AppDiagnosticsMiddleware.RequestDetails(HttpContext, status, 0);
            details["ClientSource"] = request.Source ?? "";
            details["ClientUrl"] = request.Url ?? "";
            details["Href"] = request.Href ?? request.Url ?? "";
            details["ClientMethod"] = request.Method ?? "";
            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                details["ClientUserName"] = request.UserName;
                details["UserName"] = request.UserName;
                details["User"] = request.UserName;
            }
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
                details["DisplayName"] = request.DisplayName;
            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                details["ClientUserId"] = request.UserId;
                details["UserId"] = request.UserId;
            }
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                details["ClientRole"] = request.Role;
                details["Role"] = request.Role;
            }
            if (!string.IsNullOrWhiteSpace(request.UserAgent))
                ClientEnvironment.ApplyUserAgent(details, request.UserAgent);
            if (!string.IsNullOrWhiteSpace(request.Browser))
                details["Browser"] = request.Browser;
            if (!string.IsNullOrWhiteSpace(request.Os))
                details["Os"] = request.Os;
            if (!string.IsNullOrWhiteSpace(request.DeviceType))
                details["DeviceType"] = request.DeviceType;
            if (!string.IsNullOrWhiteSpace(request.DeviceName))
                details["DeviceName"] = request.DeviceName;
            if (!string.IsNullOrWhiteSpace(request.Platform))
                details["Platform"] = request.Platform;
            if (!string.IsNullOrWhiteSpace(request.Screen))
                details["Screen"] = request.Screen;
            if (!string.IsNullOrWhiteSpace(request.Language))
                details["Language"] = request.Language;
            if (!string.IsNullOrWhiteSpace(request.TimeZone))
                details["TimeZone"] = request.TimeZone;
            if (!string.IsNullOrWhiteSpace(request.Referrer))
                details["Referrer"] = request.Referrer;
            if (!string.IsNullOrWhiteSpace(request.Stack))
                details["Stack"] = request.Stack;
            if (!string.IsNullOrWhiteSpace(request.ComponentStack))
                details["ComponentStack"] = request.ComponentStack;

            AppFileLog.Write(kind, level, "UI",
                $"{request.Method} {request.Url} status={status} src={request.Source} user={request.UserName} browser={request.Browser} device={request.DeviceName} {request.Message}",
                details: details);
            return Ok(new { success = true });
        }
    }
}

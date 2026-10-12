using Homeocentrum.Niga.API.Domain.Logging;
using Homeocentrum.Niga.API.Domain.Security.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.API.Domain.Security.Uploads;

/// <summary>
/// Runs before every action that receives multipart files: size limit, content signature check, executable/script
/// block, archive (macro / embedded executable) check and antivirus scan. A rejected file never reaches the action.
/// </summary>
public sealed class UploadSecurityFilter : IAsyncActionFilter
{
    private readonly IAntivirusScanner _scanner;
    private readonly ISecurityAuditLog _audit;
    private readonly long _maxBytes;

    public UploadSecurityFilter(IAntivirusScanner scanner, ISecurityAuditLog audit, IConfiguration config)
    {
        _scanner = scanner;
        _audit = audit;
        _maxBytes = Math.Max(1, config.GetValue("UploadSecurity:MaxFileMegabytes", 100)) * 1024L * 1024L;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!request.HasFormContentType)
        {
            await next();
            return;
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(context.HttpContext.RequestAborted);
        }
        catch (InvalidDataException)
        {
            context.Result = Reject(context, 413, "The upload is too large or malformed.");
            return;
        }

        foreach (var file in form.Files)
        {
            var rejection = await CheckAsync(context, file);
            if (rejection != null)
            {
                context.Result = rejection;
                return;
            }
        }

        await next();
    }

    private async Task<IActionResult?> CheckAsync(ActionExecutingContext context, IFormFile file)
    {
        if (file.Length <= 0)
            return Reject(context, 400, "The file is empty.");
        if (file.Length > _maxBytes)
            return Reject(context, 413, $"The file is larger than {_maxBytes / (1024 * 1024)} MB.");

        byte[] bytes;
        await using (var stream = file.OpenReadStream())
        using (var buffer = new MemoryStream((int)Math.Min(file.Length, int.MaxValue)))
        {
            await stream.CopyToAsync(buffer, context.HttpContext.RequestAborted);
            bytes = buffer.ToArray();
        }

        var verdict = UploadGuard.Inspect(bytes.AsSpan(0, Math.Min(bytes.Length, 8192)), file.FileName);
        if (verdict.Allowed && verdict.DetectedType == "zip")
        {
            using var archive = new MemoryStream(bytes, writable: false);
            verdict = UploadGuard.InspectArchive(archive);
        }
        if (!verdict.Allowed)
        {
            AppFileLog.Write("api", "WARN", "UploadGuard",
                $"Upload blocked: {verdict.Reason} detected={verdict.DetectedType} ext={Path.GetExtension(file.FileName)} bytes={file.Length}",
                sendAlert: false);
            await _audit.WriteAsync(SecurityAuditEvents.UploadBlocked, context.HttpContext, outcome: "BLOCKED",
                detail: $"{verdict.Reason} detected={verdict.DetectedType} ext={Path.GetExtension(file.FileName)}");
            return Reject(context, 415, verdict.Reason ?? "This file type is not allowed.");
        }

        var scan = await _scanner.ScanAsync(bytes, file.FileName, context.HttpContext.RequestAborted);
        if (scan.Status == AntivirusStatus.Infected)
        {
            AppFileLog.Write("errors", "ERROR", "Antivirus",
                $"Upload rejected by antivirus engine={scan.Engine} ext={Path.GetExtension(file.FileName)} bytes={file.Length}");
            await _audit.WriteAsync(SecurityAuditEvents.UploadMalware, context.HttpContext, outcome: "BLOCKED",
                detail: $"engine={scan.Engine} ext={Path.GetExtension(file.FileName)}");
            AppFileLog.SendOpsAlert("malware", "Malware upload blocked", new Dictionary<string, string>
            {
                ["Engine"] = scan.Engine ?? "",
                ["Extension"] = Path.GetExtension(file.FileName),
                ["Bytes"] = file.Length.ToString(),
                ["Path"] = context.HttpContext.Request.Path,
                ["UserId"] = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
                ["Client IP"] = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
            }, cooldownMinutes: 5);
            return Reject(context, 422, "The file failed the virus scan and was not saved.");
        }
        if (scan.Status == AntivirusStatus.Unavailable && _scanner.Enabled)
        {
            AppFileLog.Write("api", "WARN", "Antivirus",
                $"Antivirus not available engine={scan.Engine} detail={scan.Detail} failClosed={_scanner.FailClosed}", sendAlert: false);
            AppFileLog.SendOpsAlert("antivirus-unavailable", "Antivirus scanning unavailable", new Dictionary<string, string>
            {
                ["Engine"] = scan.Engine ?? "",
                ["Detail"] = scan.Detail ?? "",
                ["Effect"] = _scanner.FailClosed ? "Uploads are refused (503) until scanning works" : "Uploads are accepted without a virus scan",
            }, cooldownMinutes: 60);
            if (_scanner.FailClosed)
                return Reject(context, 503, "File scanning is not available right now. Please try again later.");
        }
        return null;
    }

    private static IActionResult Reject(ActionExecutingContext context, int status, string message)
        => status >= 500
            ? new ObjectResult(new { success = false, code = "UPLOAD_SCAN_UNAVAILABLE", message, traceId = context.HttpContext.TraceIdentifier }) { StatusCode = status }
            : new ObjectResult(ApiProblem.Body(context.HttpContext, message)) { StatusCode = status };
}

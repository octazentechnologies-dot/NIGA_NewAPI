using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// WhatsApp delivery: Meta delivery receipts, bulk send status, and audience counts.
/// Provider keys stay in appsettings.json and are never returned.
/// </summary>
[ApiController]
[Authorize]
public class WhatsAppDeliveryController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<WhatsAppDeliveryController> _logger;
    private readonly IConfiguration _configuration;

    public WhatsAppDeliveryController(IS5Week5Service s5, NIGACentrumContext context, ILogger<WhatsAppDeliveryController> logger, IConfiguration configuration)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Meta delivery receipt. Signed with X-Hub-Signature-256 = HMAC-SHA256(raw body, WhatsAppMeta:AppSecret).
    /// Unsigned receipts are never accepted; without an AppSecret the webhook is closed (503).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/api/WhatsApp/Receipts")]
    public async Task<IActionResult> Receipt()
    {
        string raw;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
            raw = await reader.ReadToEndAsync();

        var secret = _configuration["WhatsAppMeta:AppSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, code = "WEBHOOK_NOT_CONFIGURED", message = "Receipt webhook is not configured." });
        if (!MetaSignatureValid(raw, Request.Headers["X-Hub-Signature-256"].ToString(), secret))
        {
            return Unauthorized(new { success = false, message = "Invalid signature." });
        }

        WhatsAppReceiptRequest? request;
        try { request = System.Text.Json.JsonSerializer.Deserialize<WhatsAppReceiptRequest>(raw, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)); }
        catch (System.Text.Json.JsonException) { request = null; }
        if (request == null)
            return BadRequest(new { success = false, message = "Invalid receipt body." });
        return await Done(_s5.WhatsAppReceiptAsync(request));
    }

    private static bool MetaSignatureValid(string raw, string header, string secret)
    {
        const string prefix = "sha256=";
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        var expected = Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(secret), System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(expected), System.Text.Encoding.ASCII.GetBytes(header[prefix.Length..].Trim().ToLowerInvariant()));
    }

    [HttpGet("/api/WhatsApp/Bulk")]
    public Task<IActionResult> Bulk() => Done(_s5.WhatsAppBulkAsync(Caller()));

    [HttpGet("/api/WhatsApp/Audience")]
    public Task<IActionResult> WhatsAppAudience() => Done(_s5.WhatsAppAudienceAsync(Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

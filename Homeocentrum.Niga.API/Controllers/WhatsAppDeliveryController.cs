using System.Text;
using System.Text.Json;
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
    /// Meta webhook subscription handshake.
    /// Meta calls GET with hub.mode=subscribe, hub.verify_token, hub.challenge.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/api/WhatsApp/Receipts")]
    public IActionResult VerifyReceiptWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expected = _configuration["WhatsAppMeta:VerifyToken"];
        if (string.IsNullOrWhiteSpace(expected))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, code = "WEBHOOK_NOT_CONFIGURED", message = "Receipt webhook verify token is not configured." });

        if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(verifyToken, expected, StringComparison.Ordinal)
            || string.IsNullOrEmpty(challenge))
        {
            return Unauthorized(new { success = false, message = "Webhook verification failed." });
        }

        return Content(challenge, "text/plain", Encoding.UTF8);
    }

    /// <summary>
    /// Meta delivery receipt. Signed with X-Hub-Signature-256 = HMAC-SHA256(raw body, WhatsAppMeta:AppSecret).
    /// Accepts Meta Cloud API nested payloads and the flat { metaMessageId, status, payload } shape.
    /// Unsigned receipts are never accepted; without an AppSecret the webhook is closed (503).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/api/WhatsApp/Receipts")]
    public async Task<IActionResult> Receipt()
    {
        string raw;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            raw = await reader.ReadToEndAsync();

        var secret = _configuration["WhatsAppMeta:AppSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, code = "WEBHOOK_NOT_CONFIGURED", message = "Receipt webhook is not configured." });
        if (!MetaSignatureValid(raw, Request.Headers["X-Hub-Signature-256"].ToString(), secret))
        {
            return Unauthorized(new { success = false, message = "Invalid signature." });
        }

        List<WhatsAppReceiptRequest> receipts;
        try
        {
            receipts = ParseReceiptPayload(raw);
        }
        catch (JsonException)
        {
            return BadRequest(new { success = false, message = "Invalid receipt body." });
        }

        if (receipts.Count == 0)
            return BadRequest(new { success = false, message = "Invalid receipt body." });

        return await Done(_s5.WhatsAppReceiptBatchAsync(receipts));
    }

    /// <summary>
    /// Parses Meta Cloud API webhook JSON (entry[].changes[].value.statuses[]) or a flat receipt DTO.
    /// </summary>
    internal static List<WhatsAppReceiptRequest> ParseReceiptPayload(string raw)
    {
        var results = new List<WhatsAppReceiptRequest>();
        if (string.IsNullOrWhiteSpace(raw))
            return results;

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (root.TryGetProperty("entry", out var entryEl) && entryEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entryEl.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var change in changes.EnumerateArray())
                {
                    if (!change.TryGetProperty("value", out var value))
                        continue;
                    if (!value.TryGetProperty("statuses", out var statuses) || statuses.ValueKind != JsonValueKind.Array)
                        continue;
                    foreach (var status in statuses.EnumerateArray())
                    {
                        var id = status.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                        var st = status.TryGetProperty("status", out var stEl) ? stEl.GetString() : null;
                        if (string.IsNullOrWhiteSpace(id))
                            continue;
                        results.Add(new WhatsAppReceiptRequest
                        {
                            MetaMessageId = id,
                            Status = st ?? "delivered",
                            Payload = status.GetRawText(),
                        });
                    }
                }
            }

            if (results.Count > 0)
                return results;
        }

        // Flat body used by internal tests / legacy callers.
        var flat = JsonSerializer.Deserialize<WhatsAppReceiptRequest>(raw, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (!string.IsNullOrWhiteSpace(flat?.MetaMessageId))
            results.Add(flat!);

        return results;
    }

    private static bool MetaSignatureValid(string raw, string header, string secret)
    {
        const string prefix = "sha256=";
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        var expected = Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(header[prefix.Length..].Trim().ToLowerInvariant()));
    }

    [HttpGet("/api/WhatsApp/Bulk")]
    public Task<IActionResult> Bulk() => Done(_s5.WhatsAppBulkAsync(Caller()));

    [HttpGet("/api/WhatsApp/Audience")]
    public Task<IActionResult> WhatsAppAudience() => Done(_s5.WhatsAppAudienceAsync(Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

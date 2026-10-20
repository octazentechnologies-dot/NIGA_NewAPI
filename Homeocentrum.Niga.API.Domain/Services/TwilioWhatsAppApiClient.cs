using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Twilio WhatsApp — credentials from Sms:Twilio; From / ContentSid from WhatsAppMeta (single place).
/// Trial/free accounts must use ContentSid templates; freeform Body is tried only when configured,
/// then falls back to the sample Appointment Reminders ContentSid so a message still triggers.
/// </summary>
public sealed class TwilioWhatsAppApiClient : IWhatsAppMetaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly TwilioSmsOptions _twilio;
    private readonly WhatsAppMetaOptions _whatsApp;
    private readonly ILogger<TwilioWhatsAppApiClient> _logger;

    public TwilioWhatsAppApiClient(
        HttpClient httpClient,
        IOptions<SmsOptions> smsOptions,
        IOptions<WhatsAppMetaOptions> whatsAppOptions,
        ILogger<TwilioWhatsAppApiClient> logger)
    {
        _httpClient = httpClient;
        _twilio = smsOptions.Value?.Twilio ?? new TwilioSmsOptions();
        _whatsApp = whatsAppOptions.Value ?? new WhatsAppMetaOptions();
        _logger = logger;
    }

    public Task<(bool Success, string? MediaId, string? ErrorMessage)> UploadImageAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<(bool, string?, string?)>((
            false,
            null,
            "Twilio WhatsApp image send needs a public MediaUrl. Remove the image or use Meta Provider."));
    }

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendTextMessageAsync(
        string recipientNumber,
        string messageText,
        CancellationToken cancellationToken = default)
    {
        return SendOutboundAsync(
            recipientNumber,
            messageText,
            contentSid: _whatsApp.ResolveWhatsAppContentSid("NOTICE"),
            mediaUrl: null,
            cancellationToken);
    }

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendImageMessageAsync(
        string recipientNumber,
        string mediaId,
        string caption,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mediaId)
            || (!mediaId.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !mediaId.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            // Still deliver via sample ContentSid so the campaign triggers on trial.
            return SendOutboundAsync(
                recipientNumber,
                caption,
                contentSid: _whatsApp.ResolveWhatsAppContentSid(),
                mediaUrl: null,
                cancellationToken);
        }

        return SendOutboundAsync(
            recipientNumber,
            caption,
            contentSid: _whatsApp.ResolveWhatsAppContentSid(),
            mediaUrl: mediaId.Trim(),
            cancellationToken);
    }

    public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendTemplateMessageAsync(
        string recipientNumber,
        string metaTemplateName,
        string languageCode,
        IReadOnlyList<string>? bodyParameters = null,
        CancellationToken cancellationToken = default)
    {
        // HX… = Twilio ContentSid; otherwise treat name as a category key (HospitalService, etc.).
        string contentSid;
        if (!string.IsNullOrWhiteSpace(metaTemplateName)
            && metaTemplateName.Trim().StartsWith("HX", StringComparison.OrdinalIgnoreCase))
        {
            contentSid = metaTemplateName.Trim();
        }
        else
        {
            contentSid = _whatsApp.ResolveWhatsAppContentSid(metaTemplateName);
        }

        var body = bodyParameters is { Count: > 0 }
            ? string.Join("\n", bodyParameters)
            : metaTemplateName ?? string.Empty;

        return SendOutboundAsync(recipientNumber, body, contentSid, mediaUrl: null, cancellationToken);
    }

    private async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendOutboundAsync(
        string recipientNumber,
        string? messageText,
        string contentSid,
        string? mediaUrl,
        CancellationToken cancellationToken)
    {
        if (!_twilio.IsConfigured())
        {
            return (false, null, "Sms:Twilio AccountSid, AuthToken, and FromNumber are required for WhatsApp.");
        }

        var to = FormatWhatsAppAddress(recipientNumber);
        var from = ResolveWhatsAppFrom();
        if (string.IsNullOrWhiteSpace(to))
        {
            return (false, null, "Recipient mobile number is missing.");
        }

        var fallbackSid = _whatsApp.ResolveWhatsAppContentSid();
        var primarySid = string.IsNullOrWhiteSpace(contentSid) ? fallbackSid : contentSid.Trim();

        // Upgraded accounts may prefer freeform Body; on failure fall back to ContentSid sample.
        if (_whatsApp.WhatsAppTryFreeformBodyFirst && !string.IsNullOrWhiteSpace(messageText))
        {
            var freeform = await PostAsync(to, from, body: messageText, contentSid: null, mediaUrl, cancellationToken);
            if (freeform.Success)
                return freeform;

            _logger.LogWarning(
                "Twilio WhatsApp freeform Body failed; falling back to ContentSid. Error={Error}",
                freeform.ErrorMessage);
        }

        var primary = await PostAsync(to, from, body: null, contentSid: primarySid, mediaUrl: null, cancellationToken);
        if (primary.Success)
            return primary;

        // Free/trial or unknown template → sample ContentSid so a message still triggers.
        if (!string.Equals(primarySid, fallbackSid, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Twilio WhatsApp ContentSid {Sid} failed; falling back to sample {Fallback}. Error={Error}",
                primarySid,
                fallbackSid,
                primary.ErrorMessage);
            var fallback = await PostAsync(to, from, body: null, contentSid: fallbackSid, mediaUrl: null, cancellationToken);
            if (fallback.Success)
                return fallback;
            return (false, null, fallback.ErrorMessage ?? primary.ErrorMessage);
        }

        return primary;
    }

    private async Task<(bool Success, string? MessageId, string? ErrorMessage)> PostAsync(
        string to,
        string from,
        string? body,
        string? contentSid,
        string? mediaUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var sid = _twilio.AccountSid.Trim();
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json";
            var form = new Dictionary<string, string>
            {
                ["To"] = to,
                ["From"] = from
            };

            if (!string.IsNullOrWhiteSpace(contentSid))
            {
                form["ContentSid"] = contentSid.Trim();
                form["ContentVariables"] = BuildContentVariables(body);
            }
            else
            {
                form["Body"] = body ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(mediaUrl))
                    form["MediaUrl"] = mediaUrl;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(form)
            };
            var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sid}:{_twilio.AuthToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var err = ExtractTwilioError(responseBody) ?? $"Twilio WhatsApp send failed ({(int)response.StatusCode}).";
                _logger.LogError(
                    "Twilio WhatsApp failed. Status={Status} Mode={Mode} ToMasked={Masked} Body={Body}",
                    response.StatusCode,
                    contentSid != null ? "ContentSid" : "Body",
                    PhoneNormalizer.Mask(to),
                    responseBody);
                return (false, null, err);
            }

            string? messageSid = null;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("sid", out var sidEl))
                    messageSid = sidEl.GetString();
            }
            catch
            {
                // ignore
            }

            _logger.LogInformation(
                "Twilio WhatsApp accepted. Mode={Mode} ContentSid={ContentSid} ToMasked={Masked} MessageSid={Sid}",
                contentSid != null ? "ContentSid" : "Body",
                contentSid,
                PhoneNormalizer.Mask(to),
                messageSid);
            return (true, messageSid, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio WhatsApp network failure. ToMasked={Masked}", PhoneNormalizer.Mask(to));
            return (false, null, $"Twilio WhatsApp network failure: {ex.Message}");
        }
    }

    /// <summary>
    /// Appointment Reminders sample uses {{1}}=date, {{2}}=time. Other templates ignore extra keys.
    /// </summary>
    private static string BuildContentVariables(string? messageText)
    {
        var now = DateTime.Now;
        var vars = new Dictionary<string, string>
        {
            ["1"] = now.ToString("dd MMM yyyy"),
            ["2"] = now.ToString("h:mm tt")
        };

        if (!string.IsNullOrWhiteSpace(messageText))
        {
            var snippet = messageText.Trim();
            if (snippet.Length > 80)
                snippet = snippet[..80];
            // Keep keys 1/2 for the trial sample; stash campaign text in 3/4 for custom templates.
            vars["3"] = snippet;
            vars["4"] = "Homeocentrum";
        }

        return JsonSerializer.Serialize(vars);
    }

    private string ResolveWhatsAppFrom()
    {
        var overrideFrom = _whatsApp.WhatsAppFromNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(overrideFrom))
        {
            return overrideFrom.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
                ? overrideFrom
                : "whatsapp:" + (overrideFrom.StartsWith("+") ? overrideFrom : "+" + overrideFrom.TrimStart('+'));
        }

        var from = (_twilio.FromNumber ?? string.Empty).Trim();
        if (from.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            return from;
        if (!from.StartsWith("+", StringComparison.Ordinal))
            from = "+" + from.TrimStart('+');
        return "whatsapp:" + from;
    }

    /// <summary>
    /// Build whatsapp:+E.164. Do not use PhoneNormalizer.Digits — that strips the 91 country code
    /// and produced whatsapp:+7768… (invalid) instead of whatsapp:+917768… (Twilio 572002).
    /// </summary>
    private static string FormatWhatsAppAddress(string? recipientNumber)
    {
        if (string.IsNullOrWhiteSpace(recipientNumber))
            return string.Empty;

        var raw = recipientNumber.Trim();
        if (raw.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
        {
            var rest = raw["whatsapp:".Length..].Trim();
            if (rest.StartsWith("+", StringComparison.Ordinal))
                return "whatsapp:" + rest;
            var restDigits = KeepDigits(rest);
            return string.IsNullOrEmpty(restDigits) ? string.Empty : "whatsapp:+" + restDigits;
        }

        var digits = KeepDigits(raw);
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        // 10-digit India local → add 91 (same rule as FormatForWhatsAppApi).
        if (digits.Length == 10)
            digits = "91" + digits;

        return "whatsapp:+" + digits;
    }

    private static string KeepDigits(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string? ExtractTwilioError(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                var text = msg.GetString();
                if (doc.RootElement.TryGetProperty("code", out var codeEl))
                {
                    var code = codeEl.ValueKind == JsonValueKind.Number
                        ? codeEl.GetInt32().ToString()
                        : codeEl.GetString();
                    if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(text))
                        return $"Twilio {code}: {text}";
                }
                return text;
            }
        }
        catch
        {
            // fall through
        }

        return responseBody.Length > 280 ? responseBody[..280] : responseBody;
    }
}

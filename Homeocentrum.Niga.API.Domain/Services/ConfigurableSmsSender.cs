using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Routes SMS by Sms:Provider.
/// Full Twilio account → custom Body (real OTP / appointment text).
/// Trial → Twilio predefined templates (sms_2fa, …); OTP code is read from the response and stored for verify.
/// </summary>
public sealed class ConfigurableSmsSender : ISmsSender
{
    private static readonly Regex TrialOtpRegex = new(
        @"(?:code|otp)\s*(?:is|:)?\s*(\d{4,8})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SmsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly INotificationOutbox _outbox;
    private readonly ILogger<ConfigurableSmsSender> _logger;
    private readonly object _trialLock = new();
    private bool? _cachedIsTrial;

    public ConfigurableSmsSender(
        IOptions<SmsOptions> options,
        IHttpClientFactory httpClientFactory,
        INotificationOutbox outbox,
        ILogger<ConfigurableSmsSender> logger)
    {
        _options = options.Value ?? new SmsOptions();
        _httpClientFactory = httpClientFactory;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(string destination, string message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SMS disabled in config. DestinationMasked={Masked}", PhoneNormalizer.Mask(destination));
            return SmsSendResult.Fail();
        }

        // RealSms false = dummy OTP mode. Do not call Twilio; callers return devCode instead.
        if (!_options.RealSms)
        {
            _logger.LogInformation(
                "SMS dummy mode (RealSms false). Provider send skipped. DestinationMasked={Masked}",
                PhoneNormalizer.Mask(destination));
            return SmsSendResult.Fail();
        }

        var to = NormalizeTo(destination);
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("SMS skipped — empty destination.");
            return SmsSendResult.Fail();
        }

        var provider = (_options.Provider ?? "Stub").Trim();
        try
        {
            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase) && _options.Twilio.IsConfigured())
                return await SendTwilioAsync(to, message, cancellationToken);

            await _outbox.EnqueueAsync("SMS", provider, to, message ?? "", "PENDING_KEYS",
                "Fill Sms:Twilio in API appsettings. Row will send after keys are set.");
            _logger.LogInformation(
                "SMS queued PENDING_KEYS ({Provider}). DestinationMasked={Masked} Length={Length}",
                provider,
                PhoneNormalizer.Mask(to),
                (message ?? string.Empty).Length);
            return provider.Equals("Stub", StringComparison.OrdinalIgnoreCase)
                ? SmsSendResult.Ok(providerLabel: "Stub")
                : SmsSendResult.Fail();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send failed. Provider={Provider} DestinationMasked={Masked}", provider, PhoneNormalizer.Mask(to));
            return SmsSendResult.Fail();
        }
    }

    private string NormalizeTo(string destination)
    {
        var digits = PhoneNormalizer.Digits(destination);
        if (digits.Length == 10 && !string.IsNullOrWhiteSpace(_options.DefaultCountryDialCode))
            return _options.DefaultCountryDialCode.Trim() + digits;
        return digits;
    }

    private async Task<SmsSendResult> SendTwilioAsync(string to, string message, CancellationToken cancellationToken)
    {
        var useTrial = await ResolveUseTrialTemplatesAsync(cancellationToken);
        var result = await PostTwilioMessageAsync(to, useTrial ? MapTrialTemplateName(message) : (message ?? ""), useTrial, cancellationToken);

        // Custom body rejected by Trial → flip to trial templates and retry once.
        if (!result.Accepted && !useTrial && result.ProviderLabel == "572006")
        {
            lock (_trialLock) { _cachedIsTrial = true; }
            _logger.LogWarning("Twilio returned 572006 — switching to trial templates for this account.");
            result = await PostTwilioMessageAsync(to, MapTrialTemplateName(message), useTrial: true, cancellationToken);
        }

        return result;
    }

    private async Task<bool> ResolveUseTrialTemplatesAsync(CancellationToken cancellationToken)
    {
        if (_options.Twilio.UseTrialTemplates.HasValue)
            return _options.Twilio.UseTrialTemplates.Value;

        lock (_trialLock)
        {
            if (_cachedIsTrial.HasValue)
                return _cachedIsTrial.Value;
        }

        var isTrial = await DetectTwilioTrialAccountAsync(cancellationToken);
        lock (_trialLock) { _cachedIsTrial = isTrial; }
        _logger.LogInformation("Twilio account auto-detect: IsTrial={IsTrial}", isTrial);
        return isTrial;
    }

    private async Task<bool> DetectTwilioTrialAccountAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sid = _options.Twilio.AccountSid;
            var client = _httpClientFactory.CreateClient("SmsVendor");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.twilio.com/2010-04-01/Accounts/{sid}.json");
            var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sid}:{_options.Twilio.AuthToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
            using var response = await client.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return true; // fail-safe: trial-safe mode
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("type", out var typeEl))
                return string.Equals(typeEl.GetString(), "Trial", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Twilio account type detect failed; using trial-safe mode.");
        }
        return true;
    }

    private async Task<SmsSendResult> PostTwilioMessageAsync(
        string to,
        string bodyValue,
        bool useTrial,
        CancellationToken cancellationToken)
    {
        var sid = _options.Twilio.AccountSid;
        var client = _httpClientFactory.CreateClient("SmsVendor");
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json";
        var form = new Dictionary<string, string>
        {
            ["To"] = to.StartsWith("+") ? to : "+" + to,
            ["From"] = _options.Twilio.FromNumber,
            ["Body"] = bodyValue
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form)
        };
        var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sid}:{_options.Twilio.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Twilio SMS failed. Status={Status} Body={Body}", response.StatusCode, responseBody);
            var label = responseBody.Contains("572006", StringComparison.Ordinal) ? "572006" : null;
            return new SmsSendResult { Accepted = false, ProviderLabel = label };
        }

        string? messageSid = null;
        string? embeddedOtp = null;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("sid", out var sidEl))
                messageSid = sidEl.GetString();
            if (useTrial && string.Equals(bodyValue, "sms_2fa", StringComparison.OrdinalIgnoreCase))
                embeddedOtp = ExtractEmbeddedOtp(responseBody);
        }
        catch { /* ignore */ }

        _logger.LogInformation(
            "Twilio SMS accepted. DestinationMasked={Masked} Mode={Mode} TemplateOrCustom={Body} EmbeddedOtp={HasOtp}",
            PhoneNormalizer.Mask(to),
            useTrial ? "Trial" : "Full",
            useTrial ? bodyValue : "(custom)",
            !string.IsNullOrEmpty(embeddedOtp));

        return SmsSendResult.Ok(
            embeddedOtp,
            messageSid,
            usedTrialTemplate: useTrial,
            providerLabel: useTrial ? "TwilioTrial" : "Twilio");
    }

    internal static string MapTrialTemplateName(string? message)
    {
        var text = (message ?? "").ToLowerInvariant();
        if (text.Contains("verification code")
            || text.Contains("otp")
            || text.Contains("approval code")
            || text.Contains("accept code")
            || text.Contains("bank-change code")
            || text.Contains("do not share"))
            return "sms_2fa";
        if (text.Contains("appointment")
            || text.Contains("reschedule")
            || text.Contains("cancelled")
            || text.Contains("canceled")
            || text.Contains("reminder")
            || text.Contains("check in")
            || text.Contains("checked in")
            || text.Contains("slot opened")
            || text.Contains("tele consultation")
            || text.Contains("follow-up")
            || text.Contains("follow up"))
            return "sms_appointment_reminders";
        if (text.Contains("medicine")
            || text.Contains("order")
            || text.Contains("prescription")
            || text.Contains("payment")
            || text.Contains("pay for"))
            return "sms_order_confirmation";
        return "sms_account_alerts";
    }

    private static string? ExtractEmbeddedOtp(string twilioJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(twilioJson);
            if (doc.RootElement.TryGetProperty("body", out var bodyEl))
            {
                var body = bodyEl.GetString() ?? "";
                var match = TrialOtpRegex.Match(body);
                if (match.Success)
                    return match.Groups[1].Value;
                var digits = Regex.Match(body, @"\b(\d{6})\b");
                if (digits.Success)
                    return digits.Groups[1].Value;
            }
        }
        catch { }
        return null;
    }
}

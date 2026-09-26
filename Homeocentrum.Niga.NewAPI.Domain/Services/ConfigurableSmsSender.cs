using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Domain.Services;

/// <summary>
/// Routes SMS by Sms:Provider. Empty keys → stub log (still returns true so OTP/flows continue).
/// Fill Msg91 or Twilio in appsettings when ready — no code change required.
/// </summary>
public sealed class ConfigurableSmsSender : ISmsSender
{
    private readonly SmsOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly INotificationOutbox _outbox;
    private readonly ILogger<ConfigurableSmsSender> _logger;

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

    public async Task<bool> SendAsync(string destination, string message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SMS disabled in config. DestinationMasked={Masked}", PhoneNormalizer.Mask(destination));
            return false;
        }

        var to = NormalizeTo(destination);
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("SMS skipped — empty destination.");
            return false;
        }

        var provider = (_options.Provider ?? "Stub").Trim();
        try
        {
            if (provider.Equals("Msg91", StringComparison.OrdinalIgnoreCase) && _options.Msg91.IsConfigured())
                return await SendMsg91Async(to, message, cancellationToken);

            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase) && _options.Twilio.IsConfigured())
                return await SendTwilioAsync(to, message, cancellationToken);

            await _outbox.EnqueueAsync("SMS", provider, to, message ?? "", "PENDING_KEYS",
                "Fill Sms:Msg91 or Sms:Twilio in New-API appsettings. Row will send after keys are set.");
            _logger.LogInformation(
                "SMS queued PENDING_KEYS ({Provider}). DestinationMasked={Masked} Length={Length}",
                provider,
                PhoneNormalizer.Mask(to),
                (message ?? string.Empty).Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send failed. Provider={Provider} DestinationMasked={Masked}", provider, PhoneNormalizer.Mask(to));
            return false;
        }
    }

    private string NormalizeTo(string destination)
    {
        var digits = PhoneNormalizer.Digits(destination);
        if (digits.Length == 10 && !string.IsNullOrWhiteSpace(_options.DefaultCountryDialCode))
            return _options.DefaultCountryDialCode.Trim() + digits;
        return digits;
    }

    private async Task<bool> SendMsg91Async(string to, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("SmsVendor");
        var url =
            "https://api.msg91.com/api/sendhttp.php"
            + "?authkey=" + Uri.EscapeDataString(_options.Msg91.AuthKey)
            + "&mobiles=" + Uri.EscapeDataString(to)
            + "&message=" + Uri.EscapeDataString(message ?? string.Empty)
            + "&sender=" + Uri.EscapeDataString(_options.Msg91.SenderId)
            + "&route=" + Uri.EscapeDataString(_options.Msg91.Route);
        if (!string.IsNullOrWhiteSpace(_options.Msg91.OtpTemplateId))
            url += "&DLT_TE_ID=" + Uri.EscapeDataString(_options.Msg91.OtpTemplateId);
        if (!string.IsNullOrWhiteSpace(_options.Msg91.DltEntityId))
            url += "&entityid=" + Uri.EscapeDataString(_options.Msg91.DltEntityId);

        using var response = await client.GetAsync(url, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Msg91 SMS failed. Status={Status} Body={Body}", response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("Msg91 SMS accepted. DestinationMasked={Masked}", PhoneNormalizer.Mask(to));
        return true;
    }

    private async Task<bool> SendTwilioAsync(string to, string message, CancellationToken cancellationToken)
    {
        var sid = _options.Twilio.AccountSid;
        var client = _httpClientFactory.CreateClient("SmsVendor");
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json";
        var form = new Dictionary<string, string>
        {
            ["To"] = to.StartsWith("+") ? to : "+" + to,
            ["From"] = _options.Twilio.FromNumber,
            ["Body"] = message ?? string.Empty
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form)
        };
        var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{sid}:{_options.Twilio.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Twilio SMS failed. Status={Status} Body={Body}", response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("Twilio SMS accepted. DestinationMasked={Masked}", PhoneNormalizer.Mask(to));
        return true;
    }
}

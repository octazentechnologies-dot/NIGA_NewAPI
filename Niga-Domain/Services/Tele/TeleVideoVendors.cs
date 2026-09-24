using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;

namespace Niga_Domain.Services.Tele;

/// <summary>
/// Default until TeleVideo:Vendor is set and vendor keys are filled.
/// Returns a signed opaque join token for API rejoin validation; no real A/V media.
/// </summary>
public sealed class StubTeleVideoVendor : ITeleVideoVendor
{
    private readonly TeleVideoOptions _options;
    private readonly ILogger<StubTeleVideoVendor> _logger;

    public StubTeleVideoVendor(IOptions<TeleVideoOptions> options, ILogger<StubTeleVideoVendor> logger)
    {
        _options = options.Value ?? new TeleVideoOptions();
        _logger = logger;
    }

    public string VendorName => "Stub";

    public Task<TeleVideoIssueResult> IssueTokenAsync(TeleVideoIssueRequest request, CancellationToken cancellationToken = default)
    {
        var ttl = request.TtlMinutes > 0 ? request.TtlMinutes : Math.Max(5, _options.TokenTtlMinutes);
        var expires = DateTime.UtcNow.AddMinutes(ttl);
        var payload =
            $"{request.AppointmentId:N}|{request.RoomId}|{request.Role}|{request.UserAccountId}|{expires:O}|stub";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            + "."
            + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload + "|niga-stub"))).ToLowerInvariant();

        _logger.LogInformation(
            "Tele Stub token issued. AppointmentId={AppointmentId} RoomId={RoomId} Role={Role}",
            request.AppointmentId, request.RoomId, request.Role);

        return Task.FromResult(new TeleVideoIssueResult
        {
            Vendor = VendorName,
            RoomId = request.RoomId,
            Token = token,
            ExpiresAtUtc = expires,
            ClientConfig = new Dictionary<string, string>
            {
                ["mode"] = "stub",
                ["hint"] = "No media plane. Set TeleVideo:Vendor + vendor keys for Agora/Twilio/Daily."
            }
        });
    }
}

/// <summary>
/// Agora RTC token (AccessToken2-lite: uses AppId + AppCertificate HMAC).
/// When keys empty, falls back to Stub behaviour with Vendor=Agora marker.
/// </summary>
public sealed class AgoraTeleVideoVendor : ITeleVideoVendor
{
    private readonly TeleVideoOptions _options;
    private readonly StubTeleVideoVendor _stub;
    private readonly ILogger<AgoraTeleVideoVendor> _logger;

    public AgoraTeleVideoVendor(
        IOptions<TeleVideoOptions> options,
        StubTeleVideoVendor stub,
        ILogger<AgoraTeleVideoVendor> logger)
    {
        _options = options.Value ?? new TeleVideoOptions();
        _stub = stub;
        _logger = logger;
    }

    public string VendorName => "Agora";

    public async Task<TeleVideoIssueResult> IssueTokenAsync(TeleVideoIssueRequest request, CancellationToken cancellationToken = default)
    {
        var agora = _options.Agora;
        if (!agora.IsConfigured())
        {
            _logger.LogWarning("Agora keys missing — issuing stub token with Agora client config shell.");
            var stub = await _stub.IssueTokenAsync(request, cancellationToken);
            var cfg = new Dictionary<string, string>(stub.ClientConfig ?? new Dictionary<string, string>())
            {
                ["vendor"] = "Agora",
                ["appId"] = agora.AppId ?? string.Empty,
                ["channel"] = request.RoomId,
                ["uid"] = request.UserAccountId.ToString(),
                ["ready"] = "false"
            };
            return new TeleVideoIssueResult
            {
                Vendor = VendorName,
                RoomId = stub.RoomId,
                Token = stub.Token,
                ExpiresAtUtc = stub.ExpiresAtUtc,
                ClientConfig = cfg
            };
        }

        var ttl = request.TtlMinutes > 0 ? request.TtlMinutes : Math.Max(5, _options.TokenTtlMinutes);
        var expires = DateTime.UtcNow.AddMinutes(ttl);
        var expireUnix = new DateTimeOffset(expires).ToUnixTimeSeconds();
        var privilegeExpire = expireUnix;
        var uid = (uint)(request.UserAccountId % uint.MaxValue);
        var token = AgoraRtcTokenBuilder.BuildTokenWithUid(
            agora.AppId,
            agora.AppCertificate,
            request.RoomId,
            uid,
            expireUnix,
            privilegeExpire);

        _logger.LogInformation(
            "Agora token issued. AppointmentId={AppointmentId} Channel={Channel} Uid={Uid}",
            request.AppointmentId, request.RoomId, uid);

        return new TeleVideoIssueResult
        {
            Vendor = VendorName,
            RoomId = request.RoomId,
            Token = token,
            ExpiresAtUtc = expires,
            ClientConfig = new Dictionary<string, string>
            {
                ["vendor"] = "Agora",
                ["appId"] = agora.AppId,
                ["channel"] = request.RoomId,
                ["uid"] = uid.ToString(),
                ["ready"] = "true"
            }
        };
    }
}

/// <summary>Minimal Agora RTC AccessToken (001) builder for channel join.</summary>
internal static class AgoraRtcTokenBuilder
{
    private const ushort KJoinChannel = 1;
    private const ushort KPublishAudio = 2;
    private const ushort KPublishVideo = 3;
    private const ushort KPublishDataStream = 4;

    public static string BuildTokenWithUid(
        string appId,
        string appCertificate,
        string channelName,
        uint uid,
        long privilegeExpiredTs,
        long tokenExpiredTs)
    {
        // AccessToken version "006" + appId + CRC + content (simplified DynamicKey5 / AccessToken)
        // Production: prefer official agora-access-token NuGet. This is a compact HMAC token for join.
        var message = $"{appId}{channelName}{uid}{privilegeExpiredTs}{tokenExpiredTs}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appCertificate));
        var sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return $"006{appId}{sig}.{uid}.{privilegeExpiredTs}.{KJoinChannel}.{KPublishAudio}.{KPublishVideo}.{KPublishDataStream}";
    }
}

/// <summary>
/// Twilio Video / Daily.co shells — issue opaque tokens + client config when keys present;
/// otherwise stub with vendor metadata so SPA can switch SDKs later.
/// </summary>
public sealed class ConfigurableTeleVideoVendor : ITeleVideoVendor
{
    private readonly TeleVideoOptions _options;
    private readonly StubTeleVideoVendor _stub;
    private readonly AgoraTeleVideoVendor _agora;
    private readonly ILogger<ConfigurableTeleVideoVendor> _logger;

    public ConfigurableTeleVideoVendor(
        IOptions<TeleVideoOptions> options,
        StubTeleVideoVendor stub,
        AgoraTeleVideoVendor agora,
        ILogger<ConfigurableTeleVideoVendor> logger)
    {
        _options = options.Value ?? new TeleVideoOptions();
        _stub = stub;
        _agora = agora;
        _logger = logger;
    }

    public string VendorName
    {
        get
        {
            var v = (_options.Vendor ?? "Stub").Trim();
            return string.IsNullOrEmpty(v) ? "Stub" : v;
        }
    }

    public Task<TeleVideoIssueResult> IssueTokenAsync(TeleVideoIssueRequest request, CancellationToken cancellationToken = default)
    {
        var vendor = VendorName;
        if (vendor.Equals("Agora", StringComparison.OrdinalIgnoreCase))
            return _agora.IssueTokenAsync(request, cancellationToken);

        if (vendor.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
            return IssueTwilioShellAsync(request, cancellationToken);

        if (vendor.Equals("Daily", StringComparison.OrdinalIgnoreCase))
            return IssueDailyShellAsync(request, cancellationToken);

        return _stub.IssueTokenAsync(request, cancellationToken);
    }

    private async Task<TeleVideoIssueResult> IssueTwilioShellAsync(TeleVideoIssueRequest request, CancellationToken cancellationToken)
    {
        var stub = await _stub.IssueTokenAsync(request, cancellationToken);
        var tw = _options.Twilio;
        var ready = tw.IsConfigured();
        if (!ready)
            _logger.LogWarning("Twilio Video keys missing — stub token with Twilio client shell.");

        return new TeleVideoIssueResult
        {
            Vendor = "Twilio",
            RoomId = stub.RoomId,
            Token = ready ? $"TWILIO_PENDING.{stub.Token}" : stub.Token,
            ExpiresAtUtc = stub.ExpiresAtUtc,
            ClientConfig = new Dictionary<string, string>
            {
                ["vendor"] = "Twilio",
                ["accountSid"] = tw.AccountSid ?? string.Empty,
                ["room"] = request.RoomId,
                ["identity"] = request.UserAccountId.ToString(),
                ["ready"] = ready ? "true" : "false",
                ["hint"] = ready
                    ? "Wire Twilio Video JWT (AccessToken) in ConfigurableTeleVideoVendor when enabling live A/V."
                    : "Set TeleVideo:Twilio AccountSid / ApiKeySid / ApiKeySecret."
            }
        };
    }

    private async Task<TeleVideoIssueResult> IssueDailyShellAsync(TeleVideoIssueRequest request, CancellationToken cancellationToken)
    {
        var stub = await _stub.IssueTokenAsync(request, cancellationToken);
        var daily = _options.Daily;
        var ready = daily.IsConfigured();
        if (!ready)
            _logger.LogWarning("Daily.co keys missing — stub token with Daily client shell.");

        var roomUrl = ready && !string.IsNullOrWhiteSpace(daily.Domain)
            ? $"https://{daily.Domain.TrimEnd('/')}/{request.RoomId}"
            : string.Empty;

        return new TeleVideoIssueResult
        {
            Vendor = "Daily",
            RoomId = stub.RoomId,
            Token = ready ? $"DAILY_PENDING.{stub.Token}" : stub.Token,
            ExpiresAtUtc = stub.ExpiresAtUtc,
            ClientConfig = new Dictionary<string, string>
            {
                ["vendor"] = "Daily",
                ["domain"] = daily.Domain ?? string.Empty,
                ["roomUrl"] = roomUrl,
                ["ready"] = ready ? "true" : "false",
                ["hint"] = ready
                    ? "Call Daily REST /meeting-tokens when enabling live A/V; ApiKey is configured."
                    : "Set TeleVideo:Daily ApiKey + Domain."
            }
        };
    }
}

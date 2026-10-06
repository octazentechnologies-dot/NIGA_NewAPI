using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.Tele;

/// <summary>
/// 100ms.live: creates (or reuses) a room per appointment and issues an app auth token for the
/// mapped role. Clients join with the 100ms SDK using ClientConfig["roomId"] + Token.
/// When keys are missing or the 100ms API fails, returns the stub token with ready=false.
/// </summary>
public sealed class HundredMsTeleVideoVendor : ITeleVideoVendor
{
    public const string HttpClientName = "HundredMs";

    private static readonly Regex RoomNameInvalid = new("[^a-zA-Z0-9._:-]", RegexOptions.Compiled);

    private readonly TeleVideoOptions _options;
    private readonly StubTeleVideoVendor _stub;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HundredMsTeleVideoVendor> _logger;
    private readonly ConcurrentDictionary<string, string> _roomIds = new(StringComparer.Ordinal);

    public HundredMsTeleVideoVendor(
        IOptions<TeleVideoOptions> options,
        StubTeleVideoVendor stub,
        IHttpClientFactory httpClientFactory,
        ILogger<HundredMsTeleVideoVendor> logger)
    {
        _options = options.Value ?? new TeleVideoOptions();
        _stub = stub;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string VendorName => "100ms";

    public async Task<TeleVideoIssueResult> IssueTokenAsync(TeleVideoIssueRequest request, CancellationToken cancellationToken = default)
    {
        var hms = _options.HundredMs ?? new HundredMsTeleOptions();
        var role = string.Equals(request.Role, "doctor", StringComparison.OrdinalIgnoreCase)
            ? (string.IsNullOrWhiteSpace(hms.DoctorRole) ? "host" : hms.DoctorRole.Trim())
            : (string.IsNullOrWhiteSpace(hms.PatientRole) ? "guest" : hms.PatientRole.Trim());
        var userId = $"{(request.Role ?? "user").ToLowerInvariant()}-{request.UserAccountId}";

        if (!hms.IsConfigured())
        {
            _logger.LogWarning("100ms keys missing — issuing stub token with 100ms client config shell.");
            return await StubShellAsync(request, role, userId, "Set TeleVideo:HundredMs AccessKey / AppSecret / TemplateId.", cancellationToken);
        }

        string roomId;
        try
        {
            roomId = await EnsureRoomAsync(hms, request.RoomId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "100ms room create failed. AppointmentId={AppointmentId} RoomId={RoomId}", request.AppointmentId, request.RoomId);
            return await StubShellAsync(request, role, userId, "100ms room could not be created. Check AccessKey / AppSecret / TemplateId / Region.", cancellationToken);
        }

        var ttl = request.TtlMinutes > 0 ? request.TtlMinutes : Math.Max(5, _options.TokenTtlMinutes);
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(ttl);
        var token = SignHs256(hms.AppSecret, new Dictionary<string, object>
        {
            ["access_key"] = hms.AccessKey,
            ["room_id"] = roomId,
            ["user_id"] = userId,
            ["role"] = role,
            ["type"] = "app",
            ["version"] = 2,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expires.ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString()
        });

        _logger.LogInformation(
            "100ms token issued. AppointmentId={AppointmentId} Room={RoomId} HmsRoom={HmsRoomId} Role={Role}",
            request.AppointmentId, request.RoomId, roomId, role);

        return new TeleVideoIssueResult
        {
            Vendor = VendorName,
            RoomId = request.RoomId,
            Token = token,
            ExpiresAtUtc = expires.UtcDateTime,
            ClientConfig = new Dictionary<string, string>
            {
                ["vendor"] = VendorName,
                ["roomId"] = roomId,
                ["role"] = role,
                ["userId"] = userId,
                ["subdomain"] = hms.Subdomain ?? string.Empty,
                ["ready"] = "true"
            }
        };
    }

    private async Task<string> EnsureRoomAsync(HundredMsTeleOptions hms, string roomName, CancellationToken cancellationToken)
    {
        var name = RoomNameInvalid.Replace(roomName ?? string.Empty, "-");
        if (string.IsNullOrWhiteSpace(name)) name = $"room-{Guid.NewGuid():N}";
        if (_roomIds.TryGetValue(name, out var cached)) return cached;

        var body = new Dictionary<string, object>
        {
            ["name"] = name,
            ["description"] = "Homeocentrum tele consult",
            ["template_id"] = hms.TemplateId
        };
        if (!string.IsNullOrWhiteSpace(hms.Region) && !hms.Region.Equals("auto", StringComparison.OrdinalIgnoreCase))
            body["region"] = hms.Region.Trim();

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{hms.ApiBaseUrl.TrimEnd('/')}/rooms")
        {
            Content = JsonContent.Create(body)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ManagementToken(hms));

        using var response = await client.SendAsync(message, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"100ms POST /rooms returned {(int)response.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("100ms POST /rooms response had no id.");

        _roomIds[name] = id;
        return id;
    }

    private static string ManagementToken(HundredMsTeleOptions hms)
    {
        var now = DateTimeOffset.UtcNow;
        return SignHs256(hms.AppSecret, new Dictionary<string, object>
        {
            ["access_key"] = hms.AccessKey,
            ["type"] = "management",
            ["version"] = 2,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(10).ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString()
        });
    }

    private static string SignHs256(string secret, IDictionary<string, object> payload)
    {
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = "HS256", ["typ"] = "JWT" }));
        var body = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signingInput = $"{header}.{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return $"{signingInput}.{Base64Url(hmac.ComputeHash(Encoding.ASCII.GetBytes(signingInput)))}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private async Task<TeleVideoIssueResult> StubShellAsync(
        TeleVideoIssueRequest request, string role, string userId, string hint, CancellationToken cancellationToken)
    {
        var stub = await _stub.IssueTokenAsync(request, cancellationToken);
        return new TeleVideoIssueResult
        {
            Vendor = VendorName,
            RoomId = stub.RoomId,
            Token = stub.Token,
            ExpiresAtUtc = stub.ExpiresAtUtc,
            ClientConfig = new Dictionary<string, string>
            {
                ["mode"] = "stub",
                ["vendor"] = VendorName,
                ["roomId"] = string.Empty,
                ["role"] = role,
                ["userId"] = userId,
                ["ready"] = "false",
                ["hint"] = hint
            }
        };
    }
}

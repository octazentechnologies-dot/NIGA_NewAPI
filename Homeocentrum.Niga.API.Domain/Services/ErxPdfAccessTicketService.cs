using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Short-lived HMAC tickets so a mobile browser can open GET /api/Erx/{id}/Pdf without a Bearer header.
/// Mint only after the caller is authorized. Validate checks expiry, signature, and erx id.
/// Reusable until expiry so browser redirects/prefetches do not break the open (TTL is the main window).
/// </summary>
public interface IErxPdfAccessTicketService
{
    TimeSpan Ttl { get; }

    /// <summary>Creates an opaque ticket bound to erx + user. <paramref name="patientView"/> drives remedy-name reveal rules.</summary>
    (string Ticket, DateTime ExpiresAtUtc) Mint(int erxId, long userId, bool patientView);

    /// <summary>Validates the ticket. Returns false if expired, forged, or erx mismatch.</summary>
    bool TryRedeem(string? ticket, int erxId, out long userId, out bool patientView, out string error);
}

public sealed class ErxPdfAccessTicketService : IErxPdfAccessTicketService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(2);
    private readonly byte[] _key;

    public ErxPdfAccessTicketService(IConfiguration configuration)
    {
        _key = Security.JwtSettings.SigningKey(configuration).Key;
    }

    public TimeSpan Ttl => DefaultTtl;

    public (string Ticket, DateTime ExpiresAtUtc) Mint(int erxId, long userId, bool patientView)
    {
        var expiresAt = DateTime.UtcNow.Add(Ttl);
        var exp = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        var jti = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var patientFlag = patientView ? "1" : "0";
        var payload = $"{erxId}.{userId}.{exp}.{patientFlag}.{jti}";
        var sig = Sign(payload);
        var ticket = Base64UrlEncode(Encoding.UTF8.GetBytes(payload)) + "." + sig;
        return (ticket, expiresAt);
    }

    public bool TryRedeem(string? ticket, int erxId, out long userId, out bool patientView, out string error)
    {
        userId = 0;
        patientView = false;
        error = "";
        if (string.IsNullOrWhiteSpace(ticket))
        {
            error = "Ticket is required.";
            return false;
        }

        var parts = ticket.Trim().Split('.', 2);
        if (parts.Length != 2)
        {
            error = "Invalid ticket format.";
            return false;
        }

        byte[] payloadBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
        }
        catch
        {
            error = "Invalid ticket encoding.";
            return false;
        }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var expectedSig = Sign(payload);
        if (!FixedTimeEqualsHex(expectedSig, parts[1]))
        {
            error = "Invalid ticket signature.";
            return false;
        }

        var fields = payload.Split('.');
        if (fields.Length != 5
            || !int.TryParse(fields[0], out var ticketErxId)
            || !long.TryParse(fields[1], out userId)
            || !long.TryParse(fields[2], out var expUnix)
            || (fields[3] is not ("0" or "1"))
            || string.IsNullOrWhiteSpace(fields[4]))
        {
            error = "Invalid ticket payload.";
            return false;
        }

        if (ticketErxId != erxId)
        {
            error = "Ticket does not match this prescription.";
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
        {
            error = "Ticket expired.";
            return false;
        }

        patientView = fields[3] == "1";
        return true;
    }

    private string Sign(string payload)
        => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes("ErxPdf|" + payload)));

    private static bool FixedTimeEqualsHex(string expectedHex, string actual)
    {
        if (string.IsNullOrWhiteSpace(actual))
            return false;
        try
        {
            var a = Convert.FromHexString(expectedHex);
            var b = Convert.FromHexString(actual.Trim());
            return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }
        catch
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}

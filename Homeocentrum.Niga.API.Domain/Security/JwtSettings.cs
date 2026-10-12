using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Homeocentrum.Niga.API.Domain.Security;

/// <summary>
/// One place for JWT issuer, audience and signing key. The web UI signs in through the Old API and sends
/// that token here, so the Old API issuer stays trusted. Both APIs share the signing key and the audience.
/// </summary>
public static class JwtSettings
{
    public const string DefaultIssuer = "Homeocentrum.Niga.API";
    public const string OldApiIssuer = "Homeocentrum.Niga.OldAPI";
    public const string DefaultAudience = "Homeocentrum.Niga.Client";

    public static string Issuer(IConfiguration config)
        => NonEmpty(config["JWT:Issuer"]) ?? DefaultIssuer;

    public static string Audience(IConfiguration config)
        => NonEmpty(config["JWT:Audience"]) ?? DefaultAudience;

    public static string[] ValidIssuers(IConfiguration config)
    {
        var configured = config.GetSection("JWT:ValidIssuers").GetChildren()
            .Select(c => NonEmpty(c.Value))
            .Where(v => v != null)
            .Select(v => v!)
            .ToList();
        if (configured.Count == 0)
            configured.AddRange(new[] { Issuer(config), OldApiIssuer });
        if (!configured.Contains(Issuer(config), StringComparer.Ordinal))
            configured.Add(Issuer(config));
        return configured.Distinct(StringComparer.Ordinal).ToArray();
    }

    public static SymmetricSecurityKey SigningKey(IConfiguration config)
    {
        var key = config["TokenKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("TokenKey must be set in appsettings.json and be at least 32 characters.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public static TokenValidationParameters Validation(IConfiguration config, bool validateLifetime = true)
        => new()
        {
            ValidateIssuer = true,
            ValidIssuers = ValidIssuers(config),
            ValidateAudience = true,
            ValidAudience = Audience(config),
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningKey(config),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };

    private static string? NonEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

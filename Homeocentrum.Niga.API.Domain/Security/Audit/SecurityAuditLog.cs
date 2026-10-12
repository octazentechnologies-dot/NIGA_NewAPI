using System.Data;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Homeocentrum.Niga.API.Domain.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.API.Domain.Security.Audit;

public static class SecurityAuditEvents
{
    public const string Login = "LOGIN";
    public const string TokenIssued = "TOKEN_ISSUED";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string Logout = "LOGOUT";
    public const string OtpRequested = "OTP_REQUESTED";
    public const string PasswordReset = "PASSWORD_RESET";
    public const string PasswordChange = "PASSWORD_CHANGE";
    public const string AdminPasswordSet = "ADMIN_PASSWORD_SET";
    public const string PayoutOtpRequested = "PAYOUT_OTP_REQUESTED";
    public const string PayoutOtpVerify = "PAYOUT_OTP_VERIFY";
    public const string PayeeBankOtpRequested = "PAYEE_BANK_OTP_REQUESTED";
    public const string PatientDataExport = "PATIENT_DATA_EXPORT";
    public const string FinanceExport = "FINANCE_EXPORT";
    public const string UploadBlocked = "UPLOAD_BLOCKED";
    public const string UploadMalware = "UPLOAD_MALWARE";
    public const string ConsentNoticePublished = "CONSENT_NOTICE_PUBLISHED";
    public const string GuardianConsent = "GUARDIAN_CONSENT";
}

public interface ISecurityAuditLog
{
    Task WriteAsync(string eventType, HttpContext? context, string outcome = "SUCCESS", long? actorUserId = null,
        string? actorRole = null, string? subject = null, string? detail = null);
}

public sealed record SecurityAuditVerifyResult(long RowsChecked, long? FirstBrokenId, string? Problem, string? HeadHash);

/// <summary>
/// Append-only, hash-chained security audit trail (script 09). The API signs each row with an HMAC whose key
/// never reaches the database; the database chains RowHash over the previous row. Writing never fails a request.
/// </summary>
public sealed class SecurityAuditLog : ISecurityAuditLog
{
    public const string SourceApi = "API";
    private readonly string _connectionString;
    private readonly SecurityAuditKeys _keys;

    public SecurityAuditLog(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
        _keys = SecurityAuditKeys.From(config);
    }

    public async Task WriteAsync(string eventType, HttpContext? context, string outcome = "SUCCESS", long? actorUserId = null,
        string? actorRole = null, string? subject = null, string? detail = null)
    {
        if (!Configuration.FeatureFlags.Current.EnableAuditLogging)
            return;
        try
        {
            var user = context?.User;
            actorUserId ??= ReadUserId(user);
            actorRole ??= user?.FindFirst("RoleName")?.Value ?? user?.FindFirst(ClaimTypes.Role)?.Value;
            var row = SecurityAuditRow.Create(
                DateTime.UtcNow, eventType, outcome, actorUserId, actorRole,
                subject == null ? null : LogRedactor.Redact(subject),
                SourceApi, MaskIp(context?.Connection.RemoteIpAddress), context?.TraceIdentifier,
                detail == null ? null : LogRedactor.Redact(detail), _keys.CurrentKeyId);
            var mac = SecurityAuditRow.ComputeMac(_keys.Current, row.Canonical);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand("dbo.usp_SecurityAudit_Append", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add("@OccurredAtUtc", SqlDbType.DateTime2).Value = row.OccurredAtUtc;
            command.Parameters.Add("@EventType", SqlDbType.VarChar, 60).Value = row.EventType;
            command.Parameters.Add("@Outcome", SqlDbType.VarChar, 20).Value = row.Outcome;
            command.Parameters.Add("@ActorUserId", SqlDbType.BigInt).Value = (object?)row.ActorUserId ?? DBNull.Value;
            command.Parameters.Add("@ActorRole", SqlDbType.NVarChar, 50).Value = (object?)row.ActorRole ?? DBNull.Value;
            command.Parameters.Add("@Subject", SqlDbType.NVarChar, 150).Value = (object?)row.Subject ?? DBNull.Value;
            command.Parameters.Add("@SourceApi", SqlDbType.VarChar, 20).Value = row.SourceApi;
            command.Parameters.Add("@ClientIp", SqlDbType.VarChar, 64).Value = (object?)row.ClientIp ?? DBNull.Value;
            command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar, 80).Value = (object?)row.CorrelationId ?? DBNull.Value;
            command.Parameters.Add("@Detail", SqlDbType.NVarChar, 500).Value = (object?)row.Detail ?? DBNull.Value;
            command.Parameters.Add("@KeyId", SqlDbType.VarChar, 20).Value = row.KeyId;
            command.Parameters.Add("@Canonical", SqlDbType.NVarChar, 2000).Value = row.Canonical;
            command.Parameters.Add("@Mac", SqlDbType.Char, 64).Value = mac;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            AppFileLog.Write("errors", "WARN", "SecurityAudit", $"Security audit write failed for {eventType}. Run script 09 if the table is missing.", ex, null, sendAlert: false);
        }
    }

    /// <summary>Walks the whole chain and re-checks every HMAC and hash.</summary>
    public async Task<SecurityAuditVerifyResult> VerifyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(
            "SELECT SecurityAuditLogId, OccurredAtUtc, EventType, Outcome, ActorUserId, ActorRole, Subject, SourceApi, ClientIp, " +
            "CorrelationId, Detail, KeyId, Mac, PrevHash, RowHash FROM dbo.SecurityAuditLog ORDER BY SecurityAuditLogId", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var expectedPrev = new string('0', 64);
        long count = 0;
        string? head = null;
        while (await reader.ReadAsync(cancellationToken))
        {
            count++;
            var id = reader.GetInt64(0);
            var row = new SecurityAuditRow(
                DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt64(4), NullableString(reader, 5), NullableString(reader, 6),
                reader.GetString(7), NullableString(reader, 8), NullableString(reader, 9), NullableString(reader, 10), reader.GetString(11));
            var mac = reader.GetString(12);
            var prev = reader.GetString(13);
            var rowHash = reader.GetString(14);

            if (!string.Equals(prev, expectedPrev, StringComparison.OrdinalIgnoreCase))
                return new SecurityAuditVerifyResult(count, id, "PrevHash does not match the previous row (row removed or reordered).", head);
            if (!_keys.TryGet(row.KeyId, out var key))
                return new SecurityAuditVerifyResult(count, id, $"Unknown audit key '{row.KeyId}'. Add it to SecurityAudit:PreviousKeys.", head);
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(SecurityAuditRow.ComputeMac(key, row.Canonical)), Encoding.ASCII.GetBytes(mac.ToUpperInvariant())))
                return new SecurityAuditVerifyResult(count, id, "Row fields were changed (HMAC mismatch).", head);
            if (!string.Equals(SecurityAuditRow.ComputeRowHash(prev, row.Canonical, mac), rowHash, StringComparison.OrdinalIgnoreCase))
                return new SecurityAuditVerifyResult(count, id, "RowHash does not match.", head);
            expectedPrev = rowHash;
            head = rowHash;
        }
        return new SecurityAuditVerifyResult(count, null, null, head);
    }

    private static string? NullableString(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static long? ReadUserId(ClaimsPrincipal? user)
    {
        var raw = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("nameid")?.Value;
        return long.TryParse(raw, out var id) && id > 0 ? id : null;
    }

    /// <summary>Keeps the network, drops the host part: IPv4 /24, IPv6 /48.</summary>
    public static string? MaskIp(IPAddress? ip)
    {
        if (ip == null) return null;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        var bytes = ip.GetAddressBytes();
        if (bytes.Length == 4) { bytes[3] = 0; return new IPAddress(bytes) + "/24"; }
        for (var i = 6; i < bytes.Length; i++) bytes[i] = 0;
        return new IPAddress(bytes) + "/48";
    }
}

/// <summary>Row fields in the exact form that is signed and chained. Old API builds the same canonical string.</summary>
public sealed record SecurityAuditRow(DateTime OccurredAtUtc, string EventType, string Outcome, long? ActorUserId, string? ActorRole,
    string? Subject, string SourceApi, string? ClientIp, string? CorrelationId, string? Detail, string KeyId)
{
    public static SecurityAuditRow Create(DateTime atUtc, string eventType, string outcome, long? actorUserId, string? actorRole,
        string? subject, string sourceApi, string? clientIp, string? correlationId, string? detail, string keyId)
    {
        var at = new DateTime(atUtc.Ticks - atUtc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        return new SecurityAuditRow(at, Clip(eventType, 60)!, Clip(outcome, 20)!, actorUserId, Clip(actorRole, 50), Clip(subject, 150),
            Clip(sourceApi, 20)!, Clip(clientIp, 64), Clip(correlationId, 80), Clip(detail, 500), Clip(keyId, 20)!);
    }

    public string Canonical => string.Join("|", "v1",
        OccurredAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
        EventType, Outcome, ActorUserId?.ToString(CultureInfo.InvariantCulture) ?? "", ActorRole ?? "", Subject ?? "",
        SourceApi, ClientIp ?? "", CorrelationId ?? "", Detail ?? "", KeyId);

    public static string ComputeMac(byte[] key, string canonical)
        => Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(canonical)));

    public static string ComputeRowHash(string prev, string canonical, string mac)
        => Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(prev + "|" + canonical + "|" + mac)));

    private static string? Clip(string? value, int max)
    {
        if (value == null) return null;
        var clean = value.Replace('|', '/').Replace('\r', ' ').Replace('\n', ' ').Trim();
        return clean.Length <= max ? clean : clean[..max];
    }
}

/// <summary>
/// HMAC keys for the audit trail. SecurityAudit:HmacKey (with SecurityAudit:KeyId) is preferred; without it the key is
/// derived from TokenKey so both APIs agree. Retired keys stay in SecurityAudit:PreviousKeys for verification;
/// an entry whose id starts with "tk" holds a retired TokenKey and is derived the same way.
/// </summary>
public sealed class SecurityAuditKeys
{
    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.OrdinalIgnoreCase);
    public string CurrentKeyId { get; private set; } = "tk1";
    public byte[] Current => _keys[CurrentKeyId];

    public bool TryGet(string keyId, out byte[] key) => _keys.TryGetValue(keyId, out key!);

    public static SecurityAuditKeys From(IConfiguration config)
    {
        var keys = new SecurityAuditKeys();
        var tokenKey = config["TokenKey"] ?? "";
        if (tokenKey.Length > 0)
            keys._keys["tk1"] = DeriveFromTokenKey(tokenKey);
        var explicitKey = config["SecurityAudit:HmacKey"];
        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            var id = config["SecurityAudit:KeyId"];
            keys.CurrentKeyId = string.IsNullOrWhiteSpace(id) ? "k1" : id.Trim();
            keys._keys[keys.CurrentKeyId] = Encoding.UTF8.GetBytes(explicitKey);
        }
        foreach (var previous in config.GetSection("SecurityAudit:PreviousKeys").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(previous.Value))
                keys._keys[previous.Key] = previous.Key.StartsWith("tk", StringComparison.OrdinalIgnoreCase)
                    ? DeriveFromTokenKey(previous.Value)
                    : Encoding.UTF8.GetBytes(previous.Value);
        }
        if (!keys._keys.ContainsKey(keys.CurrentKeyId))
            throw new InvalidOperationException("No security audit key: set TokenKey or SecurityAudit:HmacKey.");
        return keys;
    }

    private static byte[] DeriveFromTokenKey(string tokenKey)
        => HMACSHA256.HashData(Encoding.UTF8.GetBytes(tokenKey), Encoding.UTF8.GetBytes("homeocentrum-security-audit-v1"));
}

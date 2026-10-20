using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Sends FCM via Firebase Admin SDK to every active token in dbo.DevicePushToken for a user.
/// Does nothing when Firebase:Enabled is false or credentials are missing (tokens still save).
/// </summary>
public sealed class FirebasePushNotificationSender : IPushNotificationSender
{
    private static int _schemaReady;
    private readonly NIGACentrumContext _context;
    private readonly FirebaseOptions _options;
    private readonly IHostEnvironment _env;
    private readonly ILogger<FirebasePushNotificationSender> _logger;
    private readonly object _initLock = new();
    private bool _appReady;

    public FirebasePushNotificationSender(
        NIGACentrumContext context,
        IOptions<FirebaseOptions> options,
        IHostEnvironment env,
        ILogger<FirebasePushNotificationSender> logger)
    {
        _context = context;
        _options = options.Value ?? new FirebaseOptions();
        _env = env;
        _logger = logger;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _schemaReady) == 1)
            return;

        await _context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.DevicePushToken (
                    DevicePushTokenId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_DevicePushToken PRIMARY KEY,
                    UserId bigint NOT NULL,
                    Platform nvarchar(20) NOT NULL CONSTRAINT DF_DevicePushToken_Platform DEFAULT (N'FCM'),
                    DeviceType nvarchar(20) NOT NULL CONSTRAINT DF_DevicePushToken_DeviceType DEFAULT (N'Website'),
                    Token nvarchar(512) NOT NULL,
                    DeviceId nvarchar(100) NULL,
                    CreatedAt datetime NOT NULL,
                    UpdatedAt datetime NOT NULL,
                    DeleteStatus bit NOT NULL CONSTRAINT DF_DevicePushToken_Delete DEFAULT (0)
                );
                CREATE INDEX IX_DevicePushToken_User_Token ON dbo.DevicePushToken (UserId, Token);
                CREATE INDEX IX_DevicePushToken_User_Device ON dbo.DevicePushToken (UserId, DeviceType, DeviceId);
            END
            ELSE IF COL_LENGTH(N'dbo.DevicePushToken', N'DeviceType') IS NULL
            BEGIN
                ALTER TABLE dbo.DevicePushToken ADD DeviceType nvarchar(20) NOT NULL
                    CONSTRAINT DF_DevicePushToken_DeviceType2 DEFAULT (N'Website');
            END

            IF OBJECT_ID(N'dbo.FirebaseNotificationLog', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.FirebaseNotificationLog (
                    FirebaseNotificationLogId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_FirebaseNotificationLog PRIMARY KEY,
                    UserId bigint NOT NULL,
                    Action nvarchar(80) NOT NULL,
                    Title nvarchar(200) NOT NULL,
                    Body nvarchar(2000) NOT NULL,
                    TokenCount int NOT NULL CONSTRAINT DF_FcmLog_TokenCount DEFAULT (0),
                    SentCount int NOT NULL CONSTRAINT DF_FcmLog_SentCount DEFAULT (0),
                    Status nvarchar(20) NOT NULL,
                    Detail nvarchar(1000) NULL,
                    CreatedAt datetime NOT NULL
                );
                CREATE INDEX IX_FirebaseNotificationLog_User_Created
                    ON dbo.FirebaseNotificationLog (UserId, CreatedAt DESC);
            END
            """, cancellationToken);
        Volatile.Write(ref _schemaReady, 1);
    }

    public async Task<PushDeliveryResult> SendToUserAsync(
        long userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        string? action = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var result = new PushDeliveryResult();
        var actionKey = string.IsNullOrWhiteSpace(action)
            ? (data != null && data.TryGetValue("action", out var a) ? a : "general")
            : action.Trim();

        if (userId <= 0)
        {
            result.Status = "skipped";
            result.Detail = "No user id for push.";
            await WriteLogAsync(userId, actionKey, title, body, result, cancellationToken);
            return result;
        }

        var tokens = await _context.DevicePushTokens.AsNoTracking()
            .Where(d => d.UserId == userId && !d.DeleteStatus && d.Token != null && d.Token != "")
            .Select(d => new { d.DevicePushTokenId, d.Token, d.DeviceType })
            .ToListAsync(cancellationToken);

        result.TokenCount = tokens.Count;
        if (tokens.Count == 0)
        {
            result.Status = "skipped";
            result.Detail = "No device token registered.";
            await WriteLogAsync(userId, actionKey, title, body, result, cancellationToken);
            return result;
        }

        if (!_options.Enabled || !_options.IsConfigured() || !EnsureApp())
        {
            result.Status = "logged";
            result.Detail = "Firebase push is off or not configured. Tokens are saved; message was not sent.";
            _logger.LogInformation(
                "Push logged (not sent). UserId={UserId} Tokens={Count} Action={Action} Title={Title}",
                userId, tokens.Count, actionKey, title);
            await WriteLogAsync(userId, actionKey, title, body, result, cancellationToken);
            return result;
        }

        var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (data != null)
        {
            foreach (var kv in data)
                payload[kv.Key] = kv.Value ?? string.Empty;
        }
        if (!payload.ContainsKey("action"))
            payload["action"] = actionKey;

        var sent = 0;
        var failed = 0;
        foreach (var row in tokens)
        {
            try
            {
                var message = new Message
                {
                    Token = row.Token,
                    Notification = new Notification
                    {
                        Title = title ?? "Homeocentrum",
                        Body = body ?? string.Empty
                    },
                    Data = payload
                };
                await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);
                sent++;
            }
            catch (FirebaseMessagingException ex) when (
                ex.MessagingErrorCode == MessagingErrorCode.Unregistered
                || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                failed++;
                _logger.LogWarning(ex, "FCM token invalid; clearing DevicePushTokenId={Id}", row.DevicePushTokenId);
                await SoftDeleteTokenAsync(row.DevicePushTokenId, cancellationToken);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "FCM send failed for DevicePushTokenId={Id}", row.DevicePushTokenId);
            }
        }

        result.SentCount = sent;
        if (sent > 0)
        {
            result.Status = "sent";
            result.Detail = $"Push sent to {sent} device(s).";
        }
        else
        {
            result.Status = "failed";
            result.Detail = failed > 0 ? "Push failed for all devices." : "Push was not sent.";
        }

        await WriteLogAsync(userId, actionKey, title, body, result, cancellationToken);
        return result;
    }

    public async Task<long?> ResolveUserIdByMobileAsync(string? mobile, CancellationToken cancellationToken = default)
    {
        var digits = PhoneNormalizer.Digits(mobile);
        if (digits.Length < 8)
            return null;

        var patients = await _context.Patients.AsNoTracking()
            .Where(p => p.DeleteStatus != true && p.MobileNo != null)
            .Select(p => new { p.PatientId, p.MobileNo })
            .ToListAsync(cancellationToken);
        var patient = patients.FirstOrDefault(p => PhoneNormalizer.EqualsNormalized(p.MobileNo, digits));
        if (patient == null)
            return null;

        var userId = await _context.PatientUserMaps.AsNoTracking()
            .Where(m => m.PatientId == patient.PatientId && !m.DeleteStatus)
            .OrderByDescending(m => m.IsPrimary)
            .Select(m => (long?)m.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId.HasValue && userId.Value > 0)
            return userId;

        var users = await _context.UserMasters.AsNoTracking()
            .Where(u => !u.DeleteStatus && u.MobileNo != null && u.IsUserActivated == true)
            .Select(u => new { u.UserId, u.MobileNo })
            .ToListAsync(cancellationToken);
        var match = users.FirstOrDefault(u => PhoneNormalizer.EqualsNormalized(u.MobileNo, digits));
        return match?.UserId;
    }

    public async Task ClearUserTokensAsync(long userId, string? token = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return;

        await EnsureSchemaAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var query = _context.DevicePushTokens.Where(d => d.UserId == userId && !d.DeleteStatus);
        var trimmed = token?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
            query = query.Where(d => d.Token == trimmed);

        var rows = await query.ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            row.DeleteStatus = true;
            row.UpdatedAt = now;
        }

        if (rows.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task WriteLogAsync(
        long userId,
        string action,
        string? title,
        string? body,
        PushDeliveryResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            _context.FirebaseNotificationLogs.Add(new FirebaseNotificationLog
            {
                UserId = userId,
                Action = (action ?? "general").Trim(),
                Title = Truncate(title ?? "", 200),
                Body = Truncate(body ?? "", 2000),
                TokenCount = result.TokenCount,
                SentCount = result.SentCount,
                Status = result.Status,
                Detail = Truncate(result.Detail, 1000),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FirebaseNotificationLog write failed.");
        }
    }

    private async Task SoftDeleteTokenAsync(long id, CancellationToken cancellationToken)
    {
        var row = await _context.DevicePushTokens
            .FirstOrDefaultAsync(d => d.DevicePushTokenId == id, cancellationToken);
        if (row == null)
            return;
        row.DeleteStatus = true;
        row.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private bool EnsureApp()
    {
        if (_appReady && FirebaseApp.DefaultInstance != null)
            return true;

        lock (_initLock)
        {
            if (_appReady && FirebaseApp.DefaultInstance != null)
                return true;

            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    _appReady = true;
                    return true;
                }

                GoogleCredential credential;
                if (!string.IsNullOrWhiteSpace(_options.CredentialsJson))
                {
                    credential = GoogleCredential.FromJson(_options.CredentialsJson);
                }
                else if (!string.IsNullOrWhiteSpace(_options.CredentialsPath))
                {
                    var path = _options.CredentialsPath.Trim();
                    if (!Path.IsPathRooted(path))
                        path = Path.Combine(_env.ContentRootPath, path);
                    if (!File.Exists(path))
                    {
                        _logger.LogWarning("Firebase credentials file not found: {Path}", path);
                        return false;
                    }

                    credential = GoogleCredential.FromFile(path);
                }
                else
                {
                    return false;
                }

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = _options.ProjectId.Trim()
                });
                _appReady = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase Admin init failed.");
                return false;
            }
        }
    }

    private static string Truncate(string? value, int max)
    {
        var s = value ?? "";
        return s.Length <= max ? s : s[..max];
    }
}

/// <summary>Normalizes client device-type strings to Website | Android | iOS.</summary>
public static class DeviceTypeNormalizer
{
    public static string Normalize(string? value)
    {
        var raw = (value ?? string.Empty).Trim();
        if (raw.Length == 0)
            return "Website";

        var compact = raw.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
        if (compact is "ios" or "mobileios" or "iphone" or "ipad" or "apns")
            return "iOS";
        if (compact is "android" or "mobileandroid" or "mobile")
            return "Android";
        if (compact is "website" or "web" or "browser" or "fcm")
            return "Website";
        return "Website";
    }

    public static bool IsAllowed(string? value)
    {
        var n = Normalize(value);
        return n is "Website" or "Android" or "iOS";
    }
}

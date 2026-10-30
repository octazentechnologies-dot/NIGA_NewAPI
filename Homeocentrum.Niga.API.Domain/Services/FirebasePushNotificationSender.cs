using System.Text.Json;
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
/// Queues FCM via dbo.PushNotificationOutbox and writes AppNotification inbox immediately.
/// <see cref="IPushOutboxProcessor"/> drains the outbox off the HTTP thread.
/// Does not claim the user saw the message — provider acceptance ≠ read receipt.
/// </summary>
public sealed class FirebasePushNotificationSender : IPushNotificationSender, IPushOutboxProcessor
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
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

            IF OBJECT_ID(N'dbo.AppNotification', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AppNotification (
                    AppNotificationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppNotification PRIMARY KEY,
                    UserId bigint NOT NULL,
                    Title nvarchar(160) NOT NULL,
                    Body nvarchar(1000) NOT NULL,
                    IsRead bit NOT NULL CONSTRAINT DF_AppNotification_IsRead DEFAULT (0),
                    PushStatus nvarchar(30) NOT NULL,
                    CreatedAt datetime NOT NULL CONSTRAINT DF_AppNotification_CreatedAt DEFAULT (GETDATE()),
                    Type nvarchar(40) NULL,
                    DeepLink nvarchar(300) NULL
                );
                CREATE INDEX IX_AppNotification_User ON dbo.AppNotification (UserId, CreatedAt DESC);
            END
            ELSE
            BEGIN
                IF COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL
                    ALTER TABLE dbo.AppNotification ADD Type nvarchar(40) NULL;
                IF COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL
                    ALTER TABLE dbo.AppNotification ADD DeepLink nvarchar(300) NULL;
            END

            IF OBJECT_ID(N'dbo.PushNotificationOutbox', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PushNotificationOutbox (
                    PushNotificationOutboxId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PushNotificationOutbox PRIMARY KEY,
                    DeliveryId uniqueidentifier NOT NULL,
                    UserId bigint NOT NULL,
                    Action nvarchar(80) NOT NULL,
                    NotificationType nvarchar(40) NULL,
                    Title nvarchar(200) NOT NULL,
                    Body nvarchar(2000) NOT NULL,
                    DataJson nvarchar(max) NULL,
                    Status nvarchar(20) NOT NULL CONSTRAINT DF_PushOutbox_Status DEFAULT (N'PENDING'),
                    AttemptCount int NOT NULL CONSTRAINT DF_PushOutbox_Attempt DEFAULT (0),
                    NextAttemptAt datetime2 NOT NULL CONSTRAINT DF_PushOutbox_NextAttempt DEFAULT (SYSUTCDATETIME()),
                    LockedAt datetime2 NULL,
                    LockedBy nvarchar(100) NULL,
                    LastError nvarchar(1000) NULL,
                    ProviderAcceptedCount int NOT NULL CONSTRAINT DF_PushOutbox_Accepted DEFAULT (0),
                    TokenCount int NOT NULL CONSTRAINT DF_PushOutbox_TokenCount DEFAULT (0),
                    CreatedAt datetime2 NOT NULL CONSTRAINT DF_PushOutbox_CreatedAt DEFAULT (SYSUTCDATETIME()),
                    UpdatedAt datetime2 NOT NULL CONSTRAINT DF_PushOutbox_UpdatedAt DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT UQ_PushNotificationOutbox_DeliveryId UNIQUE (DeliveryId)
                );
                CREATE INDEX IX_PushNotificationOutbox_Drain
                    ON dbo.PushNotificationOutbox (Status, NextAttemptAt) INCLUDE (UserId, AttemptCount);
                CREATE INDEX IX_PushNotificationOutbox_User_Created
                    ON dbo.PushNotificationOutbox (UserId, CreatedAt DESC);
            END
            """, cancellationToken);
        // Align runtime ensure with 07_PushNotificationOutbox.sql when table already existed
        // without the secondary index (idempotent).
        await _context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.PushNotificationOutbox', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_PushNotificationOutbox_User_Created'
                      AND object_id = OBJECT_ID(N'dbo.PushNotificationOutbox'))
                CREATE INDEX IX_PushNotificationOutbox_User_Created
                    ON dbo.PushNotificationOutbox (UserId, CreatedAt DESC);
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

        var type = AppNotificationInbox.MapType(actionKey, data, title, body);
        if (!await IsCategoryPushAllowedAsync(userId, type, cancellationToken))
        {
            result.Status = "skipped";
            result.Detail = $"User opted out of {type} push.";
            await WriteLogAsync(userId, actionKey, title, body, result, cancellationToken);
            await WriteInboxAsync(userId, actionKey, title, body, data, result, cancellationToken);
            return result;
        }

        var tokenCount = await _context.DevicePushTokens.AsNoTracking()
            .CountAsync(d => d.UserId == userId && !d.DeleteStatus && d.Token != null && d.Token != "", cancellationToken);
        result.TokenCount = tokenCount;

        // Inbox is written immediately (read state ≠ push delivery). Push is queued.
        result.Status = "queued";
        result.Detail = tokenCount == 0
            ? "Inbox saved; no device tokens — push outbox will skip send."
            : "Inbox saved; push queued for background delivery.";
        await WriteInboxAsync(userId, actionKey, title, body, data, result, cancellationToken);

        var deliveryId = ResolveDeliveryId(data);
        var dataJson = SerializeData(data, actionKey);
        var enqueued = await TryEnqueueAsync(
            deliveryId,
            userId,
            actionKey,
            type,
            title ?? "Homeocentrum",
            body ?? "",
            dataJson,
            tokenCount,
            cancellationToken);

        if (!enqueued)
        {
            result.Status = "queued";
            result.Detail = "Duplicate DeliveryId — existing outbox row retained (idempotent).";
        }

        await WriteLogAsync(userId, actionKey, title, body, result, cancellationToken);
        return result;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var workerId = Truncate($"{Environment.MachineName}:{Guid.NewGuid():N}", 100);
        var claimed = await ClaimBatchAsync(workerId, cancellationToken);
        if (claimed.Count == 0)
            return 0;

        var handled = 0;
        foreach (var row in claimed)
        {
            handled++;
            try
            {
                await ProcessOneAsync(row, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push outbox row {Id} failed unexpectedly", row.PushNotificationOutboxId);
                await ScheduleRetryAsync(row, "Unexpected worker error.", cancellationToken);
            }
        }

        return handled;
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

    public async Task<DevicePushToken> UpsertTokenAsync(
        long userId,
        string token,
        string? deviceType,
        string? deviceId,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var trimmed = (token ?? "").Trim();
        if (trimmed.Length > 512)
            trimmed = trimmed[..512];
        var type = DeviceTypeNormalizer.Normalize(deviceType);
        var id = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        if (id != null && id.Length > 100)
            id = id[..100];
        var now = DateTime.UtcNow;

        var row = await _context.DevicePushTokens
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Token == trimmed && !d.DeleteStatus, cancellationToken);

        if (row == null && !string.IsNullOrWhiteSpace(id))
        {
            row = await _context.DevicePushTokens
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId
                    && d.DeviceType == type
                    && d.DeviceId == id
                    && !d.DeleteStatus, cancellationToken);
        }

        if (row == null)
        {
            row = await _context.DevicePushTokens
                .FirstOrDefaultAsync(d => d.UserId == userId && d.Token == trimmed && d.DeleteStatus, cancellationToken);
        }

        if (row == null)
        {
            row = new DevicePushToken
            {
                UserId = userId,
                Platform = "FCM",
                DeviceType = type,
                Token = trimmed,
                DeviceId = id,
                CreatedAt = now,
                UpdatedAt = now,
                DeleteStatus = false
            };
            _context.DevicePushTokens.Add(row);
        }
        else
        {
            row.Token = trimmed;
            row.DeviceType = type;
            row.Platform = "FCM";
            if (!string.IsNullOrWhiteSpace(id))
                row.DeviceId = id;
            row.UpdatedAt = now;
            row.DeleteStatus = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return row;
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

    /// <summary>
    /// Honors dbo.UserNotificationPreference category flags. Missing row = all enabled
    /// except marketing (defaults match table DF_Unp_*). QuietHoursEnabled alone has no
    /// start/end columns yet — it does not suppress push until a window is defined.
    /// </summary>
    private async Task<bool> IsCategoryPushAllowedAsync(
        long userId,
        string type,
        CancellationToken cancellationToken)
    {
        try
        {
            var pref = await _context.Database.SqlQuery<PreferenceFlagsRow>($@"
                SELECT TOP 1
                    CAST(Appointments AS bit) AS Appointments,
                    CAST(Prescriptions AS bit) AS Prescriptions,
                    CAST(Medicines AS bit) AS Medicines,
                    CAST(Marketing AS bit) AS Marketing
                FROM dbo.UserNotificationPreference
                WHERE UserId = {userId}").FirstOrDefaultAsync(cancellationToken);

            if (pref == null)
                return !string.Equals(type, "MARKETING", StringComparison.OrdinalIgnoreCase);

            return (type ?? "").Trim().ToUpperInvariant() switch
            {
                "APPOINTMENT" => pref.Appointments,
                "PRESCRIPTION" => pref.Prescriptions,
                "MEDICINE" => pref.Medicines,
                "MARKETING" => pref.Marketing,
                _ => true
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Notification preference lookup failed for UserId={UserId}; allowing push", userId);
            return true;
        }
    }

    private sealed class PreferenceFlagsRow
    {
        public bool Appointments { get; set; }
        public bool Prescriptions { get; set; }
        public bool Medicines { get; set; }
        public bool Marketing { get; set; }
    }

    private sealed class OutboxClaimRow
    {
        public long PushNotificationOutboxId { get; set; }
        public Guid DeliveryId { get; set; }
        public long UserId { get; set; }
        public string Action { get; set; } = "";
        public string? NotificationType { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string? DataJson { get; set; }
        public int AttemptCount { get; set; }
    }

    private static Guid ResolveDeliveryId(IReadOnlyDictionary<string, string>? data)
    {
        if (data != null)
        {
            foreach (var key in new[] { "deliveryId", "eventId", "DeliveryId", "EventId" })
            {
                if (data.TryGetValue(key, out var raw) && Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty)
                    return parsed;
            }
        }
        return Guid.NewGuid();
    }

    private static string? SerializeData(IReadOnlyDictionary<string, string>? data, string actionKey)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (data != null)
        {
            foreach (var kv in data)
                map[kv.Key] = kv.Value ?? "";
        }
        if (!map.ContainsKey("action"))
            map["action"] = actionKey;
        return JsonSerializer.Serialize(map, JsonOpts);
    }

    private async Task<bool> TryEnqueueAsync(
        Guid deliveryId,
        long userId,
        string action,
        string type,
        string title,
        string body,
        string? dataJson,
        int tokenCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                IF NOT EXISTS (SELECT 1 FROM dbo.PushNotificationOutbox WHERE DeliveryId = {deliveryId})
                INSERT INTO dbo.PushNotificationOutbox
                    (DeliveryId, UserId, Action, NotificationType, Title, Body, DataJson, Status, AttemptCount, NextAttemptAt, TokenCount, CreatedAt, UpdatedAt)
                VALUES
                    ({deliveryId}, {userId}, {Truncate(action, 80)}, {Truncate(type, 40)}, {Truncate(title, 200)}, {Truncate(body, 2000)},
                     {dataJson}, N'PENDING', 0, SYSUTCDATETIME(), {tokenCount}, SYSUTCDATETIME(), SYSUTCDATETIME())",
                cancellationToken);
            return affected > 0;
        }
        catch (Exception ex)
        {
            // Unique DeliveryId race → idempotent success.
            _logger.LogDebug(ex, "Push outbox enqueue race for DeliveryId={DeliveryId}", deliveryId);
            return false;
        }
    }

    private async Task<List<OutboxClaimRow>> ClaimBatchAsync(string workerId, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE o
            SET Status = N'PROCESSING',
                LockedAt = SYSUTCDATETIME(),
                LockedBy = {workerId},
                UpdatedAt = SYSUTCDATETIME()
            FROM dbo.PushNotificationOutbox o
            INNER JOIN (
                SELECT TOP (20) PushNotificationOutboxId
                FROM dbo.PushNotificationOutbox WITH (READPAST, UPDLOCK, ROWLOCK)
                WHERE Status IN (N'PENDING', N'FAILED')
                  AND NextAttemptAt <= SYSUTCDATETIME()
                  AND (LockedAt IS NULL OR LockedAt < DATEADD(minute, -5, SYSUTCDATETIME()))
                ORDER BY PushNotificationOutboxId
            ) x ON x.PushNotificationOutboxId = o.PushNotificationOutboxId", cancellationToken);

        return await _context.Database.SqlQuery<OutboxClaimRow>($@"
            SELECT PushNotificationOutboxId, DeliveryId, UserId, Action, NotificationType, Title, Body, DataJson, AttemptCount
            FROM dbo.PushNotificationOutbox
            WHERE Status = N'PROCESSING' AND LockedBy = {workerId}
            ORDER BY PushNotificationOutboxId").ToListAsync(cancellationToken);
    }

    private async Task ProcessOneAsync(OutboxClaimRow row, CancellationToken cancellationToken)
    {
        var type = string.IsNullOrWhiteSpace(row.NotificationType)
            ? AppNotificationInbox.MapType(row.Action, null, row.Title, row.Body)
            : row.NotificationType!;

        // Preference changes after queue: re-check at send time.
        if (!await IsCategoryPushAllowedAsync(row.UserId, type, cancellationToken))
        {
            await MarkOutboxAsync(row.PushNotificationOutboxId, "SKIPPED", 0, 0, "User opted out at send time.", clearLock: true, cancellationToken);
            return;
        }

        var tokens = await _context.DevicePushTokens.AsNoTracking()
            .Where(d => d.UserId == row.UserId && !d.DeleteStatus && d.Token != null && d.Token != "")
            .Select(d => new { d.DevicePushTokenId, d.Token })
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            await MarkOutboxAsync(row.PushNotificationOutboxId, "SKIPPED", 0, 0, "No device tokens.", clearLock: true, cancellationToken);
            return;
        }

        if (!_options.Enabled)
        {
            await ScheduleRetryAsync(row, "Firebase:Enabled is false.", cancellationToken);
            return;
        }

        if (!_options.IsConfigured())
        {
            await ScheduleRetryAsync(row, "Firebase ProjectId/CredentialsPath (or CredentialsJson) not set.", cancellationToken);
            return;
        }

        if (!EnsureApp())
        {
            // File missing / invalid JSON — retry until fixed or DEAD (do not invent credentials).
            await ScheduleRetryAsync(
                row,
                "Firebase credentials file missing or invalid (check CredentialsPath under ContentRoot).",
                cancellationToken);
            return;
        }

        var payload = DeserializeData(row.DataJson, row.Action);
        var sent = 0;
        var transientFail = 0;
        foreach (var tokenRow in tokens)
        {
            try
            {
                var titleText = row.Title;
                var bodyText = row.Body;
                var message = new Message
                {
                    Token = tokenRow.Token,
                    Notification = new Notification { Title = titleText, Body = bodyText },
                    Data = payload,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Title = titleText,
                            Body = bodyText,
                            Sound = "default",
                            ChannelId = "homeocentrum"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps { Sound = "default", ContentAvailable = true, Badge = 1 }
                    },
                    Webpush = new WebpushConfig
                    {
                        Notification = new WebpushNotification
                        {
                            Title = titleText,
                            Body = bodyText,
                            Icon = "/favicon.ico"
                        }
                    }
                };
                await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);
                sent++;
            }
            catch (FirebaseMessagingException ex) when (
                ex.MessagingErrorCode == MessagingErrorCode.Unregistered
                || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                _logger.LogWarning(ex, "FCM token invalid; clearing DevicePushTokenId={Id}", tokenRow.DevicePushTokenId);
                await SoftDeleteTokenAsync(tokenRow.DevicePushTokenId, cancellationToken);
            }
            catch (Exception ex)
            {
                transientFail++;
                _logger.LogWarning(ex, "FCM send failed for DevicePushTokenId={Id}", tokenRow.DevicePushTokenId);
            }
        }

        if (sent > 0)
        {
            await MarkOutboxAsync(
                row.PushNotificationOutboxId,
                "SENT",
                sent,
                tokens.Count,
                $"Provider accepted {sent} of {tokens.Count} token(s).",
                clearLock: true,
                cancellationToken);
            return;
        }

        if (transientFail > 0)
        {
            await ScheduleRetryAsync(row, "Transient FCM failure for all tokens.", cancellationToken);
            return;
        }

        // All tokens invalid / removed — permanent for this delivery.
        await MarkOutboxAsync(
            row.PushNotificationOutboxId,
            "FAILED",
            0,
            tokens.Count,
            "No valid tokens remaining after permanent FCM errors.",
            clearLock: true,
            cancellationToken);
    }

    private async Task ScheduleRetryAsync(OutboxClaimRow row, string error, CancellationToken cancellationToken)
    {
        var nextAttempt = row.AttemptCount + 1;
        if (PushOutboxBackoff.IsDead(nextAttempt))
        {
            await MarkOutboxAsync(
                row.PushNotificationOutboxId,
                "DEAD",
                0,
                row.AttemptCount,
                Truncate($"Dead-letter after {nextAttempt} attempts. {error}", 1000),
                clearLock: true,
                cancellationToken);
            return;
        }

        var delay = PushOutboxBackoff.DelayAfterFailure(row.AttemptCount);
        var nextAt = DateTime.UtcNow.Add(delay);
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PushNotificationOutbox
            SET Status = N'FAILED',
                AttemptCount = {nextAttempt},
                NextAttemptAt = {nextAt},
                LockedAt = NULL,
                LockedBy = NULL,
                LastError = {Truncate(error, 1000)},
                UpdatedAt = SYSUTCDATETIME()
            WHERE PushNotificationOutboxId = {row.PushNotificationOutboxId}", cancellationToken);
    }

    private Task MarkOutboxAsync(
        long id,
        string status,
        int accepted,
        int tokenCount,
        string? error,
        bool clearLock,
        CancellationToken cancellationToken)
    {
        if (clearLock)
        {
            return _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PushNotificationOutbox
                SET Status = {status},
                    ProviderAcceptedCount = {accepted},
                    TokenCount = {tokenCount},
                    LastError = {error},
                    LockedAt = NULL,
                    LockedBy = NULL,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE PushNotificationOutboxId = {id}", cancellationToken);
        }

        return _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PushNotificationOutbox
            SET Status = {status},
                ProviderAcceptedCount = {accepted},
                TokenCount = {tokenCount},
                LastError = {error},
                UpdatedAt = SYSUTCDATETIME()
            WHERE PushNotificationOutboxId = {id}", cancellationToken);
    }

    private static Dictionary<string, string> DeserializeData(string? dataJson, string action)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["action"] = action };
        if (string.IsNullOrWhiteSpace(dataJson))
            return map;
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(dataJson, JsonOpts);
            if (parsed == null) return map;
            foreach (var kv in parsed)
                map[kv.Key] = kv.Value ?? "";
        }
        catch
        {
            /* keep action only */
        }
        return map;
    }

    private async Task WriteInboxAsync(
        long userId,
        string action,
        string? title,
        string? body,
        IReadOnlyDictionary<string, string>? data,
        PushDeliveryResult result,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
            return;

        try
        {
            var type = AppNotificationInbox.MapType(action, data, title, body);
            var deepLink = AppNotificationInbox.MapDeepLink(action, data);
            var pushStatus = string.IsNullOrWhiteSpace(result.Status)
                ? "SKIPPED"
                : result.Status.Trim().ToUpperInvariant();
            var inboxTitle = Truncate(string.IsNullOrWhiteSpace(title) ? "Homeocentrum" : title, 160);
            var inboxBody = Truncate(body ?? "", 1000);
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.AppNotification (UserId, Title, Body, IsRead, PushStatus, CreatedAt, Type, DeepLink)
                VALUES ({userId}, {inboxTitle}, {inboxBody}, 0, {pushStatus}, {DateTime.Now}, {type}, {deepLink})",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AppNotification inbox write failed for UserId={UserId}.", userId);
        }
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

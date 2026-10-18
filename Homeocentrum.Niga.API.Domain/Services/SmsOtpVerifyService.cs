using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Domain.Services;

public sealed class SmsOtpVerifyService : ISmsOtpVerifyService
{
    private static int _schemaReady;
    private const int MaxAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IServiceScopeFactory _scopes;

    public SmsOtpVerifyService(IServiceScopeFactory scopes)
    {
        _scopes = scopes;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _schemaReady) == 1)
            return;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.SmsOtpVerify', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.SmsOtpVerify (
                    SmsOtpVerifyId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SmsOtpVerify PRIMARY KEY,
                    Purpose nvarchar(50) NOT NULL,
                    MobileDigits nvarchar(20) NOT NULL,
                    DestinationMasked nvarchar(100) NOT NULL,
                    OtpHash nvarchar(128) NOT NULL,
                    Provider nvarchar(30) NULL,
                    TwilioMessageSid nvarchar(64) NULL,
                    ExpiresAt datetime NOT NULL,
                    AttemptCount int NOT NULL CONSTRAINT DF_SmsOtpVerify_Attempt DEFAULT (0),
                    LockedUntil datetime NULL,
                    CreatedAt datetime NOT NULL,
                    VerifiedAt datetime NULL,
                    OtpChallengeId bigint NULL
                );
                CREATE INDEX IX_SmsOtpVerify_Mobile_Purpose
                    ON dbo.SmsOtpVerify (MobileDigits, Purpose, VerifiedAt, SmsOtpVerifyId DESC);
            END
            """, cancellationToken);
        Volatile.Write(ref _schemaReady, 1);
    }

    public async Task<SmsOtpVerifyStoreResult> StoreAsync(
        string purpose,
        string mobileDigits,
        string plainOtp,
        string? provider,
        string? twilioMessageSid,
        int ttlMinutes,
        long? otpChallengeId = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var digits = PhoneNormalizer.Digits(mobileDigits);
        var masked = digits.Length > 4 ? "******" + digits[^4..] : "****";
        var expires = DateTime.UtcNow.AddMinutes(ttlMinutes < 1 ? 10 : ttlMinutes);
        var hash = SecurityTokenHash.Sha256Hex(plainOtp.Trim());

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();

        // Invalidate prior open OTPs for same purpose+mobile.
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.SmsOtpVerify
            SET VerifiedAt = {DateTime.UtcNow}
            WHERE MobileDigits = {digits} AND Purpose = {purpose.Trim()} AND VerifiedAt IS NULL",
            cancellationToken);

        var row = new SmsOtpVerify
        {
            Purpose = purpose.Trim(),
            MobileDigits = digits,
            DestinationMasked = masked,
            OtpHash = hash,
            Provider = provider,
            TwilioMessageSid = twilioMessageSid,
            ExpiresAt = expires,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow,
            OtpChallengeId = otpChallengeId
        };
        db.SmsOtpVerifies.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return new SmsOtpVerifyStoreResult
        {
            SmsOtpVerifyId = row.SmsOtpVerifyId,
            ExpiresAt = row.ExpiresAt,
            DestinationMasked = masked
        };
    }

    public async Task<SmsOtpVerifyCheckResult> VerifyAsync(
        string purpose,
        string mobileDigits,
        string plainOtp,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        var digits = PhoneNormalizer.Digits(mobileDigits);
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();

        var row = await db.SmsOtpVerifies
            .Where(x => x.MobileDigits == digits && x.Purpose == purpose.Trim() && x.VerifiedAt == null)
            .OrderByDescending(x => x.SmsOtpVerifyId)
            .FirstOrDefaultAsync(cancellationToken);

        return await ConsumeAsync(db, row, plainOtp, cancellationToken);
    }

    public async Task<SmsOtpVerifyCheckResult> VerifyByIdAsync(
        long smsOtpVerifyId,
        string plainOtp,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var row = await db.SmsOtpVerifies
            .FirstOrDefaultAsync(x => x.SmsOtpVerifyId == smsOtpVerifyId, cancellationToken);
        return await ConsumeAsync(db, row, plainOtp, cancellationToken);
    }

    private static async Task<SmsOtpVerifyCheckResult> ConsumeAsync(
        NIGACentrumContext db,
        SmsOtpVerify? row,
        string plainOtp,
        CancellationToken cancellationToken)
    {
        if (row == null)
            return SmsOtpVerifyCheckResult.Fail("No pending SMS OTP.");
        if (row.VerifiedAt != null)
            return SmsOtpVerifyCheckResult.Fail("OTP already used.");
        if (row.ExpiresAt < DateTime.UtcNow)
            return SmsOtpVerifyCheckResult.Fail("OTP expired.");
        if (row.LockedUntil != null && row.LockedUntil > DateTime.UtcNow)
            return SmsOtpVerifyCheckResult.Fail("OTP locked. Try later.");

        row.AttemptCount += 1;
        var ok = string.Equals(
            row.OtpHash,
            SecurityTokenHash.Sha256Hex((plainOtp ?? "").Trim()),
            StringComparison.OrdinalIgnoreCase);
        if (!ok)
        {
            if (row.AttemptCount >= MaxAttempts)
                row.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            await db.SaveChangesAsync(cancellationToken);
            return SmsOtpVerifyCheckResult.Fail("Invalid OTP.");
        }

        row.VerifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Keep legacy OtpChallenge in sync when linked (booking session).
        if (row.OtpChallengeId is > 0)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.OtpChallenge
                SET VerifiedAt = {DateTime.UtcNow}
                WHERE OtpChallengeId = {row.OtpChallengeId.Value} AND VerifiedAt IS NULL",
                cancellationToken);
        }

        return SmsOtpVerifyCheckResult.Ok(row.SmsOtpVerifyId, row.OtpChallengeId);
    }
}

namespace Homeocentrum.Niga.API.Domain.Services;

public interface ISmsOtpVerifyService
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    /// <summary>Store the OTP that was actually sent (custom or Twilio trial embedded).</summary>
    Task<SmsOtpVerifyStoreResult> StoreAsync(
        string purpose,
        string mobileDigits,
        string plainOtp,
        string? provider,
        string? twilioMessageSid,
        int ttlMinutes,
        long? otpChallengeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Compare the code from the phone SMS to the latest pending row for purpose+mobile.</summary>
    Task<SmsOtpVerifyCheckResult> VerifyAsync(
        string purpose,
        string mobileDigits,
        string plainOtp,
        CancellationToken cancellationToken = default);

    Task<SmsOtpVerifyCheckResult> VerifyByIdAsync(
        long smsOtpVerifyId,
        string plainOtp,
        CancellationToken cancellationToken = default);
}

public sealed class SmsOtpVerifyStoreResult
{
    public long SmsOtpVerifyId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string DestinationMasked { get; init; } = "";
}

public sealed class SmsOtpVerifyCheckResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public long? SmsOtpVerifyId { get; init; }
    public long? OtpChallengeId { get; init; }

    public static SmsOtpVerifyCheckResult Ok(long id, long? challengeId = null) =>
        new() { Success = true, SmsOtpVerifyId = id, OtpChallengeId = challengeId };

    public static SmsOtpVerifyCheckResult Fail(string error) =>
        new() { Success = false, Error = error };
}

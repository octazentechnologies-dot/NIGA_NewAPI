namespace Homeocentrum.Niga.API.Domain.Master;

/// <summary>
/// Real-time SMS OTP store. Code is hashed; verify compares the phone SMS against this row.
/// </summary>
public class SmsOtpVerify
{
    public long SmsOtpVerifyId { get; set; }

    /// <summary>PatientAuth | Login | GrantCaregiver | PayoutApprove | …</summary>
    public string Purpose { get; set; } = "";

    /// <summary>Normalized mobile digits used for lookup.</summary>
    public string MobileDigits { get; set; } = "";

    public string DestinationMasked { get; set; } = "";

    public string OtpHash { get; set; } = "";

    /// <summary>Twilio | TwilioTrial | Msg91 | Stub</summary>
    public string? Provider { get; set; }

    public string? TwilioMessageSid { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    /// <summary>Optional link to legacy OtpChallenge row (booking session id).</summary>
    public long? OtpChallengeId { get; set; }
}

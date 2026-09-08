using System;

namespace Niga_Domain.Master;

/// <summary>SEC-07.01 — OTP challenge (hashed code; rate-limit / lockout fields).</summary>
public partial class OtpChallenge
{
    public long OtpChallengeId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string DestinationMasked { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }
}

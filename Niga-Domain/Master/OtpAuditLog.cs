using System;

namespace Niga_Domain.Master;

/// <summary>SEC-07.01 — OTP audit (masked destination).</summary>
public partial class OtpAuditLog
{
    public long OtpAuditLogId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string ToMasked { get; set; } = null!;

    public bool Success { get; set; }

    public DateTime At { get; set; }

    public long? ActorUserId { get; set; }
}

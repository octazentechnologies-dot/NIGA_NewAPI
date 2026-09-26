using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

/// <summary>SEC-08.01 — Mutating action audit trail.</summary>
public partial class AuditEvent
{
    public long AuditEventId { get; set; }

    public long? ActorUserId { get; set; }

    public string? Role { get; set; }

    public string Action { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public string? OldJson { get; set; }

    public string? NewJson { get; set; }

    public DateTime At { get; set; }

    public string? CorrelationId { get; set; }
}

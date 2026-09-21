using System;

namespace Niga_Domain.Master;

/// <summary>SEC-06.01 — Subject consent grant/withdraw (no clinical content).</summary>
public partial class ConsentRecord
{
    public long ConsentRecordId { get; set; }

    public int ConsentTypeId { get; set; }

    /// <summary>Patient | User | Caregiver | …</summary>
    public string SubjectType { get; set; } = null!;

    public long SubjectId { get; set; }

    public long? GrantedByUserId { get; set; }

    public DateTime GrantedAt { get; set; }

    public DateTime? WithdrawnAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Notes { get; set; }

    public virtual ConsentType? ConsentType { get; set; }
}

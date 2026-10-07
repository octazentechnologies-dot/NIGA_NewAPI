using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

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

    /// <summary>The notice the person agreed to.</summary>
    public int? ConsentNoticeId { get; set; }

    public string? NoticeVersion { get; set; }

    public string? NoticeLanguage { get; set; }

    public string? NoticeSha256 { get; set; }

    /// <summary>True when the patient was under 18 and a parent or guardian consented for them.</summary>
    public bool GrantedForMinor { get; set; }

    public long? GuardianUserId { get; set; }

    public string? GuardianName { get; set; }

    public string? GuardianRelationship { get; set; }

    /// <summary>FamilyAccount | InClinic | Otp</summary>
    public string? GuardianVerificationMethod { get; set; }

    /// <summary>family:{id}, caregiver:{id}, staff:{userId};idProof:{type}, or otp:{challengeId}.</summary>
    public string? GuardianVerificationRef { get; set; }

    public string? GuardianMobileMasked { get; set; }

    public virtual ConsentType? ConsentType { get; set; }

    public virtual ConsentNotice? ConsentNotice { get; set; }
}

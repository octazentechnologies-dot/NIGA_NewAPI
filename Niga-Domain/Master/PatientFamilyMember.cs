namespace Niga_Domain.Master;

/// <summary>M16 CON-01 — Family member under one patient account (member is a real Patient row).</summary>
public class PatientFamilyMember
{
    public long FamilyMemberId { get; set; }
    public long OwnerUserId { get; set; }
    public int OwnerPatientId { get; set; }
    public int MemberPatientId { get; set; }
    public int? RelationId { get; set; }
    public string Relation { get; set; } = null!;
    public bool DeleteStatus { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime EnteredDate { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? ChangedDate { get; set; }
}

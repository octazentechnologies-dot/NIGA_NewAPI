namespace Homeocentrum.Niga.NewAPI.Domain.Master;

/// <summary>M16 — Links a Patient-role UserMaster row to a clinical PatientId (primary self).</summary>
public class PatientUserMap
{
    public long PatientUserMapId { get; set; }
    public long UserId { get; set; }
    public int PatientId { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool DeleteStatus { get; set; }
    public string? EnteredBy { get; set; }
    public DateTime EnteredDate { get; set; }
}

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

/// <summary>M16 CON-02 — Caregiver may act for a patient while grant is active.</summary>
public class CaregiverAuthorization
{
    public long CaregiverAuthorizationId { get; set; }
    public int PatientId { get; set; }
    public long CaregiverUserId { get; set; }
    public long? GrantedByUserId { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string Scope { get; set; } = "booking";
    public bool DeleteStatus { get; set; }
}

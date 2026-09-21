using System;

namespace Niga_Domain.Master;

/// <summary>WEB-09.01 / TRU-01 — credential review status for a doctor.</summary>
public class DoctorVerification
{
    public int DoctorVerificationId { get; set; }
    public int DoctorId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ReviewerNote { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime EnteredDate { get; set; }
    public DateTime? ChangedDate { get; set; }
    public bool DeleteStatus { get; set; }
}

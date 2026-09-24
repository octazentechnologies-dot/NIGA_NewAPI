using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

/// <summary>WEB-09.01 / TRU-01 — qualification / registration files for review.</summary>
public class DoctorCredentialDocument
{
    public int DoctorCredentialDocumentId { get; set; }
    public int DoctorId { get; set; }
    public int? DoctorVerificationId { get; set; }
    public string DocumentType { get; set; } = "Other";
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public DateTime EnteredDate { get; set; }
    public bool DeleteStatus { get; set; }
}

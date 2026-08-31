using System;

namespace Niga_Domain.Master;

public partial class AudioCaseConsentLog
{
    public long ConsentLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long PatientId { get; set; }

    public long DoctorUserId { get; set; }

    public string ConsentType { get; set; } = null!;

    public string ConsentTextVersion { get; set; } = null!;

    public bool ConsentGiven { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime EnteredDate { get; set; }
}

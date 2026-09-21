using System;

namespace Niga_Domain.Master;

/// <summary>
/// Audio case-taking consent log (session-scoped).
/// SEC-06.03 — Do not fork a second telemedicine consent model.
/// Phase 11 writes TeleRecording into ConsentRecord (ConsentType master); reuse this pattern / ConsentRecord APIs.
/// </summary>
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

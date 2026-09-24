using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AudioCaseDoctorActionLog
{
    public long DoctorActionLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long DoctorUserId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? Notes { get; set; }

    public string? IpAddress { get; set; }

    public DateTime EnteredDate { get; set; }
}

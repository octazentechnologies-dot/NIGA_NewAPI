using System;

namespace Niga_Domain.Master;

public partial class AudioCaseRetentionLog
{
    public long RetentionLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? Reason { get; set; }

    public string PerformedBy { get; set; } = null!;

    public string? DetailsJson { get; set; }

    public DateTime EnteredDate { get; set; }
}

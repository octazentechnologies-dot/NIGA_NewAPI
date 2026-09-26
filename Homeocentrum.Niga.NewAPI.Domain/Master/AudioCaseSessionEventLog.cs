using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AudioCaseSessionEventLog
{
    public long EventLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string EventType { get; set; } = null!;

    public string EventStatus { get; set; } = null!;

    public string? Message { get; set; }

    public string? DetailsJson { get; set; }

    public int? DurationMs { get; set; }

    public string? CorrelationId { get; set; }

    public string? IpAddress { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }
}

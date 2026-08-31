namespace Niga_Domain.Master;

public partial class AudioCaseIntelligenceLog
{
    public long IntelligenceLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string? CorrelationId { get; set; }

    public string StageName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Message { get; set; }

    public string? DetailsJson { get; set; }

    public int? LatencyMs { get; set; }

    public string EngineVersion { get; set; } = "v2";

    public DateTime EnteredDate { get; set; }
}

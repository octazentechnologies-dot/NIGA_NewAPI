namespace Niga_Domain.Master;

public partial class AiMonitoringDailySnapshot
{
    public long SnapshotId { get; set; }

    public DateTime SnapshotDate { get; set; }

    public string EngineVersion { get; set; } = "all";

    public int SessionsAnalyzed { get; set; }

    public int FeedbackCount { get; set; }

    public decimal? PrecisionScore { get; set; }

    public decimal? RecallScore { get; set; }

    public decimal? DoctorAcceptanceRate { get; set; }

    public int HallucinationCount { get; set; }

    public decimal? HallucinationRate { get; set; }

    public decimal? EmbeddingFreshnessHours { get; set; }

    public decimal? RubricEmbeddingCoverage { get; set; }

    public decimal? ConceptEmbeddingCoverage { get; set; }

    public decimal? TranscriptCoverage { get; set; }

    public decimal? AverageConfidence { get; set; }

    public decimal? PrimaryRubricAccuracy { get; set; }

    public decimal? F1Score { get; set; }

    public string? MetricsJson { get; set; }

    public DateTime CalculatedDate { get; set; }
}

public partial class AiMonitoringAuditLog
{
    public long AuditLogId { get; set; }

    public string EventType { get; set; } = null!;

    public string Operation { get; set; } = null!;

    public int? ActorUserId { get; set; }

    public Guid? CorrelationId { get; set; }

    public string? DetailsJson { get; set; }

    public int? DurationMs { get; set; }

    public bool Success { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public DateTime EnteredDate { get; set; }
}

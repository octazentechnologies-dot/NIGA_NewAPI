namespace Niga_Domain.DTOs;

public static class AiMonitoringMetricKeys
{
    public const string Precision = "Precision";

    public const string Recall = "Recall";

    public const string DoctorAcceptance = "DoctorAcceptance";

    public const string Hallucinations = "Hallucinations";

    public const string TranscriptCoverage = "TranscriptCoverage";

    public const string AverageConfidence = "AverageConfidence";

    public const string PrimaryRubricAccuracy = "PrimaryRubricAccuracy";

    public const string F1Score = "F1Score";

    public const string HallucinationRate = "HallucinationRate";
}

public class AiMonitoringKpiCardModel
{
    public string MetricKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public decimal? Value { get; set; }

    public string? Unit { get; set; }

    public string TrendDirection { get; set; } = "Flat";

    public decimal? TrendDelta { get; set; }

    public string Status { get; set; } = "Normal";
}

public class AiMonitoringDashboardOverviewModel
{
    public string EngineVersion { get; set; } = "v10";

    public int DaysAnalyzed { get; set; }

    public int SessionsAnalyzed { get; set; }

    public int FeedbackCount { get; set; }

    public List<AiMonitoringKpiCardModel> Kpis { get; set; } = new();

    public AiMonitoringEmbeddingHealthModel EmbeddingHealth { get; set; } = new();

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AiMonitoringChartPointModel
{
    public DateTime TimestampUtc { get; set; }

    public string Label { get; set; } = string.Empty;

    public decimal? Value { get; set; }

    public int? Count { get; set; }
}

public class AiMonitoringChartSeriesModel
{
    public string MetricKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string ChartType { get; set; } = "line";

    public string? Unit { get; set; }

    public List<AiMonitoringChartPointModel> Points { get; set; } = new();
}

public class AiMonitoringTrendsModel
{
    public int DaysRequested { get; set; }

    public string Granularity { get; set; } = "daily";

    public List<AiMonitoringChartSeriesModel> Series { get; set; } = new();

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AiMonitoringEmbeddingHealthModel
{
    public Guid? CurrentEmbeddingVersionId { get; set; }

    public string? CurrentVersionCode { get; set; }

    public string? ModelName { get; set; }

    public DateTime? LastRubricEmbeddingUpdateUtc { get; set; }

    public DateTime? LastConceptEmbeddingUpdateUtc { get; set; }

    public decimal? EmbeddingFreshnessHours { get; set; }

    public int TotalActiveRubrics { get; set; }

    public int EmbeddedRubrics { get; set; }

    public decimal? RubricEmbeddingCoverage { get; set; }

    public int EmbeddedConcepts { get; set; }

    public decimal? ConceptEmbeddingCoverage { get; set; }

    public string FreshnessStatus { get; set; } = "Unknown";

    public string CoverageStatus { get; set; } = "Unknown";
}

public class AiMonitoringHallucinationSummaryModel
{
    public int DaysAnalyzed { get; set; }

    public int TotalValidationEvents { get; set; }

    public int HallucinationCount { get; set; }

    public decimal? HallucinationRate { get; set; }

    public int RejectedRubricCount { get; set; }

    public List<AiMonitoringChartPointModel> DailyTrend { get; set; } = new();

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AiMonitoringSnapshotRefreshResultModel
{
    public DateTime SnapshotDate { get; set; }

    public string EngineVersion { get; set; } = "all";

    public bool Created { get; set; }

    public bool Updated { get; set; }

    public AiMonitoringDailySnapshotModel? Snapshot { get; set; }
}

public class AiMonitoringDailySnapshotModel
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

    public DateTime CalculatedDateUtc { get; set; }
}

public class AiMonitoringAuditLogEntryModel
{
    public long AuditLogId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public int? ActorUserId { get; set; }

    public Guid? CorrelationId { get; set; }

    public int? DurationMs { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime EnteredDateUtc { get; set; }
}

public class AiMonitoringAuditLogModel
{
    public List<AiMonitoringAuditLogEntryModel> Entries { get; set; } = new();

    public int TotalCount { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

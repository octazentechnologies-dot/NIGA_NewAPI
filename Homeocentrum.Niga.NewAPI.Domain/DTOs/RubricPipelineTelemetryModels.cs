namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

/// <summary>
/// Stage A baseline telemetry — no PHI/transcript content.
/// Writes use the session engine stamp (e.g. fast-f), not a hardcoded pipeline label.
/// </summary>
public static class RubricPipelineTelemetryConstants
{
    /// <summary>Legacy stamp from Stage A. Do not use for new writes.</summary>
    public const string EngineVersion = "base-a";
    public const string SummaryStageName = "PipelineBaselineSummary";
}

public class RubricPipelineStageMetric
{
    public string StageName { get; set; } = string.Empty;

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public long ElapsedMilliseconds { get; set; }

    public string EngineVersion { get; set; } = RubricPipelineTelemetryConstants.EngineVersion;

    public Guid CaseSessionId { get; set; }

    public int? CandidateCount { get; set; }

    public int? FinalRubricCount { get; set; }

    public int LlmCallCountDelta { get; set; }

    public int EmbeddingCallCountDelta { get; set; }

    public int SqlQueryCountDelta { get; set; }

    public int CacheHitCountDelta { get; set; }

    public int CacheMissCountDelta { get; set; }

    public string Status { get; set; } = "Success";

    public string? Message { get; set; }
}

public class RubricPipelineTelemetrySummary
{
    public Guid CaseSessionId { get; set; }

    public string? CorrelationId { get; set; }

    public string EngineVersion { get; set; } = RubricPipelineTelemetryConstants.EngineVersion;

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public long TotalElapsedMilliseconds { get; set; }

    public int LlmCallCount { get; set; }

    public int EmbeddingCallCount { get; set; }

    public int SqlQueryCount { get; set; }

    public int CacheHitCount { get; set; }

    public int CacheMissCount { get; set; }

    public int CandidateCount { get; set; }

    public int FinalRubricCount { get; set; }

    public int DbBackedRubricCount { get; set; }

    public int AiOnlyRubricCount { get; set; }

    public List<RubricPipelineStageMetric> Stages { get; set; } = new();

    /// <summary>Stage name → elapsed ms for quick dashboards.</summary>
    public Dictionary<string, long> StageElapsedMs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Entities;

public sealed class EciBenchmarkVersion
{
    [Key]
    public Guid BenchmarkVersionId { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public string VersionCode { get; set; } = "v1";

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EciBenchmarkCase
{
    [Key]
    public Guid BenchmarkCaseId { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string CaseId { get; set; } = string.Empty;

    public string? ChiefComplaint { get; set; }

    public string Transcript { get; set; } = string.Empty;

    public string? CleanConversation { get; set; }

    /// <summary>JSON of structured symptoms (no rubrics). Stored for reproducibility.</summary>
    public string? AiStructuredSymptomsJson { get; set; }

    public string? RadarOpusExport { get; set; }

    public string? ReferenceSoftwareExport { get; set; }

    public string? DoctorApprovedRubricsJson { get; set; }

    public string? DoctorApprovedRemedy { get; set; }

    public string? ExpectedRemediesJson { get; set; }

    public string DifficultyLevel { get; set; } = "Medium";

    public string? MedicalSpecialty { get; set; }

    public string? Comments { get; set; }

    public Guid BenchmarkVersionId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class EciBenchmarkExpectedRubric
{
    [Key]
    public Guid ExpectedRubricId { get; set; } = Guid.NewGuid();

    public Guid BenchmarkCaseId { get; set; }

    public int? SubSectionId { get; set; }

    [MaxLength(512)]
    public string RubricText { get; set; } = string.Empty;

    public int? ExpectedRankHint { get; set; }

    public string? Source { get; set; } // RadarOpus | Expert | DoctorApproved
}

public sealed class EciBenchmarkExpectedRemedy
{
    [Key]
    public Guid ExpectedRemedyId { get; set; } = Guid.NewGuid();

    public Guid BenchmarkCaseId { get; set; }

    [MaxLength(128)]
    public string RemedyName { get; set; } = string.Empty;

    public string? Source { get; set; }
}

public sealed class EciBenchmarkRun
{
    [Key]
    public Guid BenchmarkRunId { get; set; } = Guid.NewGuid();

    public Guid BenchmarkVersionId { get; set; }

    [MaxLength(32)]
    public string EngineVersion { get; set; } = "v8.0";

    public string WeightConfigJson { get; set; } = "{}";

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public string? Notes { get; set; }
}

public sealed class EciBenchmarkRunRubric
{
    [Key]
    public Guid RunRubricId { get; set; } = Guid.NewGuid();

    public Guid BenchmarkRunId { get; set; }

    public Guid BenchmarkCaseId { get; set; }

    public int SubSectionId { get; set; }

    public string RubricText { get; set; } = string.Empty;

    public int Rank { get; set; }

    public decimal Score { get; set; }

    public string? Explainability { get; set; }

    public int CandidateCount { get; set; }
}

public sealed class EciBenchmarkRunMetrics
{
    [Key]
    public Guid MetricsId { get; set; } = Guid.NewGuid();

    public Guid BenchmarkRunId { get; set; }

    public int CasesEvaluated { get; set; }

    public decimal Precision { get; set; }

    public decimal Recall { get; set; }

    public decimal F1Score { get; set; }

    public decimal Top1Accuracy { get; set; }

    public decimal Top3Accuracy { get; set; }

    public decimal Top5Accuracy { get; set; }

    public decimal Top10Accuracy { get; set; }

    public decimal Mrr { get; set; }

    public decimal Ndcg { get; set; }

    public decimal AvgRank { get; set; }

    public decimal AvgLatencyMs { get; set; }

    public decimal AvgSearchMs { get; set; }

    public decimal AvgRankingMs { get; set; }

    public decimal AvgExtractionMs { get; set; }

    public decimal AvgTotalMs { get; set; }

    public string ReportJson { get; set; } = "{}";
}

public sealed class EciBenchmarkPerRubricAnalysis
{
    [Key]
    public Guid AnalysisId { get; set; } = Guid.NewGuid();

    public Guid BenchmarkRunId { get; set; }

    public Guid BenchmarkCaseId { get; set; }

    public string ExpectedRubric { get; set; } = string.Empty;

    public int? ExpectedRubricId { get; set; }

    public string? ProducedRubric { get; set; }

    public int? ProducedRank { get; set; }

    public string FailureClass { get; set; } = string.Empty;

    public string? FailureReason { get; set; }

    public string? Evidence { get; set; }

    public decimal? Score { get; set; }

    public int CandidateCount { get; set; }
}


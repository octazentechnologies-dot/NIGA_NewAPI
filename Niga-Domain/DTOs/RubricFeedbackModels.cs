using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs;

public class AudioCaseRubricFeedbackRequestModel
{
    [Required]
    public string FeedbackType { get; set; } = string.Empty;

    public int? SubSectionId { get; set; }

    [Required]
    public string RubricName { get; set; } = string.Empty;

    public string? OriginalMatchLayer { get; set; }

    public int? CorrectedSubSectionId { get; set; }

    public string? Reason { get; set; }

    public string? RejectReasonStage { get; set; }

    public string? RejectReasonNote { get; set; }

    public decimal? ConfidenceAtFeedback { get; set; }

    public string? EngineVersion { get; set; }

    /// <summary>Homeopathic concept that produced this rubric suggestion (optional — resolved from session graph when omitted).</summary>
    public string? SourceConceptName { get; set; }

    /// <summary>Clinical concept linked to the suggestion (optional).</summary>
    public string? ClinicalConceptName { get; set; }
}

public class AudioCaseRubricFeedbackResultModel
{
    public long FeedbackId { get; set; }

    public Guid SessionId { get; set; }

    public string FeedbackType { get; set; } = string.Empty;

    public bool LearningApplied { get; set; }

    public int LearningSignalsApplied { get; set; }

    public DoctorLearningAppliedSummaryModel? LearningSummary { get; set; }

    public AudioCaseRubricBenchmarkSnapshotModel? SessionBenchmark { get; set; }
}

public class AudioCaseRubricBenchmarkSnapshotModel
{
    public int AiSuggestedCount { get; set; }

    public int DoctorAcceptedCount { get; set; }

    public int DoctorRejectedCount { get; set; }

    public int DoctorCorrectedCount { get; set; }

    public bool? PrimaryInTop5 { get; set; }

    public decimal? PrecisionScore { get; set; }

    public decimal? RecallScore { get; set; }

    public decimal? F1Score { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public decimal? FalsePositiveRate { get; set; }

    public decimal? ConfidenceCalibration { get; set; }

    public string EngineVersion { get; set; } = "v2";

    public DateTime CalculatedDateUtc { get; set; }
}

public class RubricBenchmarkSummaryModel
{
    public int TotalSessionsBenchmarked { get; set; }

    public int TotalFeedbackCount { get; set; }

    public int GoldCaseCount { get; set; }

    public decimal? AcceptanceRate7Day { get; set; }

    public decimal? AcceptanceRate30Day { get; set; }

    public decimal? PrimaryInTop5Rate7Day { get; set; }

    public decimal? PrimaryInTop5Rate30Day { get; set; }

    public decimal? FalsePositiveRate30Day { get; set; }

    public decimal? F1Score30Day { get; set; }

    public decimal? V1AcceptanceRate30Day { get; set; }

    public decimal? V2AcceptanceRate30Day { get; set; }

    public DateTime? LastGateTimestamp { get; set; }

    public decimal? LastGateTop5Accuracy { get; set; }

    public decimal? DeltaTop5VsLastGate { get; set; }

    public decimal? DeltaAcceptanceVsLastGate { get; set; }

    public List<RubricBenchmarkTopRejectedModel> TopRejectedRubrics { get; set; } = new();

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public int FastFSessionCount { get; set; }

    public int FastFFeedbackCount { get; set; }

    public decimal? FastFAcceptanceRate { get; set; }

    public decimal? FastFPrimaryInTop5Rate { get; set; }

    public decimal? FastFFalsePositiveRate { get; set; }

    public decimal? FastFPrecisionAt1 { get; set; }

    public decimal? FastFPrecisionAt3 { get; set; }

    public decimal? FastFPrecisionAt5 { get; set; }

    public decimal? FastFRecallAt5 { get; set; }

    public decimal? FastFPrecisionAt10 { get; set; }
}

public class RubricBenchmarkTopRejectedModel
{
    public string RubricName { get; set; } = string.Empty;

    public int? SubSectionId { get; set; }

    public int RejectionCount { get; set; }

    public string? MostCommonMatchLayer { get; set; }
}

public class RubricBenchmarkTrendPointModel
{
    public DateTime WeekStartUtc { get; set; }

    public string EngineVersion { get; set; } = "v2";

    public int SessionCount { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public decimal? PrimaryInTop5Rate { get; set; }

    public decimal? FalsePositiveRate { get; set; }
}

public class RubricBenchmarkTrendsModel
{
    public List<RubricBenchmarkTrendPointModel> Weeks { get; set; } = new();

    public List<RubricBenchmarkTrendPointModel> FastFWeeks { get; set; } = new();

    public int WeeksRequested { get; set; }
}

public class RubricFeedbackQueueItemModel
{
    public string ItemType { get; set; } = string.Empty;

    public long EntityId { get; set; }

    public string DisplayText { get; set; } = string.Empty;

    public int? SubSectionId { get; set; }

    public string? SubSectionName { get; set; }

    public int UsageCount { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public int RejectionCount { get; set; }

    public string? Language { get; set; }
}

public class RubricFeedbackQueueModel
{
    public List<RubricFeedbackQueueItemModel> LowAcceptanceAliases { get; set; } = new();

    public List<RubricFeedbackQueueItemModel> LowAcceptanceMetaphors { get; set; } = new();

    public List<RubricBenchmarkTopRejectedModel> TopRejectedRubrics { get; set; } = new();
}

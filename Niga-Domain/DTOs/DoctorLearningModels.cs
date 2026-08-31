namespace Niga_Domain.DTOs;

public static class DoctorLearningTypes
{
    public const string ConceptRubricMapping = "ConceptRubricMapping";

    public const string ConceptRanking = "ConceptRanking";

    public const string ClinicalRelevance = "ClinicalRelevance";

    public const string ConfidenceCalibration = "ConfidenceCalibration";
}

public static class RejectReasonStages
{
    public const string Meaning = "Meaning";
    public const string Metaphor = "Metaphor";
    public const string ClinicalConcept = "ClinicalConcept";
    public const string HomeopathicConcept = "HomeopathicConcept";
    public const string RubricMapping = "RubricMapping";
    public const string Other = "Other";

    public static bool IsValid(string? value) =>
        value is not null && value is
            Meaning or Metaphor or ClinicalConcept or HomeopathicConcept or RubricMapping or Other;
}

public class DoctorLearningFeedbackContext
{
    public string? HomeopathicConceptName { get; set; }

    public string? ClinicalConceptName { get; set; }

    public int? SubSectionId { get; set; }

    public int? CorrectedSubSectionId { get; set; }

    public string? ResolvedConceptName =>
        !string.IsNullOrWhiteSpace(HomeopathicConceptName)
            ? HomeopathicConceptName
            : ClinicalConceptName;
}

public class DoctorLearningWeightsSnapshot
{
    public static DoctorLearningWeightsSnapshot Empty { get; } = new();

    public Dictionary<string, decimal> ConceptRanking { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> ClinicalRelevance { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<(string Concept, int RubricId), decimal> ConceptRubricMapping { get; set; } = new();

    public Dictionary<int, decimal> RubricAcceptanceRates { get; set; } = new();
}

public class DoctorLearningAppliedSummaryModel
{
    public string? ConceptName { get; set; }

    public int SignalsWritten { get; set; }

    public string FeedbackType { get; set; } = string.Empty;

    public int? SubSectionId { get; set; }

    public int? CorrectedSubSectionId { get; set; }
}

public class DoctorLearningSummaryModel
{
    public string EngineVersion { get; set; } = "v9";

    public int TotalLearningSignals { get; set; }

    public int TotalFeedbackCount { get; set; }

    public int AcceptedCount { get; set; }

    public int RejectedCount { get; set; }

    public int CorrectedCount { get; set; }

    public int DistinctConceptsLearned { get; set; }

    public int DistinctRubricMappingsLearned { get; set; }

    public decimal? AverageAcceptanceRate { get; set; }

    public List<DoctorLearningTopConceptModel> TopBoostedConcepts { get; set; } = new();

    public List<DoctorLearningTopMappingModel> TopConceptRubricMappings { get; set; } = new();

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DoctorLearningTopConceptModel
{
    public string ConceptName { get; set; } = string.Empty;

    public decimal AccumulatedWeight { get; set; }

    public string LearningType { get; set; } = string.Empty;
}

public class DoctorLearningTopMappingModel
{
    public string ConceptName { get; set; } = string.Empty;

    public int SubSectionId { get; set; }

    public decimal AccumulatedWeight { get; set; }
}

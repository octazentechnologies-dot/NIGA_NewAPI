namespace Niga_Domain.DTOs;

public static class RubricEvidenceChainStepKeys
{
    public const string Transcript = "Transcript";

    public const string Meaning = "Meaning";

    public const string ClinicalConcept = "ClinicalConcept";

    public const string HomeopathicConcept = "HomeopathicConcept";

    public const string EmbeddingMatch = "EmbeddingMatch";

    public const string Rubric = "Rubric";

    public const string Confidence = "Confidence";

    public const string ValidationResult = "ValidationResult";

    public const string DoctorFeedback = "DoctorFeedback";
}

public class RubricEvidenceChainStepModel
{
    public string StepKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Value { get; set; }

    public decimal? Score { get; set; }

    public string Status { get; set; } = "Complete";

    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class RubricEnterpriseEvidenceChainModel
{
    public string EngineVersion { get; set; } = "v8";

    public int SubSectionId { get; set; }

    public string RubricName { get; set; } = string.Empty;

    public bool IsComplete { get; set; }

    public List<RubricEvidenceChainStepModel> Steps { get; set; } = new();

    public string DisplayChain =>
        string.Join(" → ", Steps.Select(s => s.Label));

    public RubricEvidenceChainStepModel? GetStep(string stepKey) =>
        Steps.FirstOrDefault(s => string.Equals(s.StepKey, stepKey, StringComparison.OrdinalIgnoreCase));
}

public class RubricEvidenceChainEnrichmentContext
{
    public Guid? SessionId { get; set; }

    public string Transcript { get; set; } = string.Empty;

    public ConceptGraphFullModel? Graph { get; set; }

    public IReadOnlyList<RubricCandidateModel> Candidates { get; set; } = Array.Empty<RubricCandidateModel>();

    public IReadOnlyList<RubricEnterpriseValidationReport> ValidationReports { get; set; } =
        Array.Empty<RubricEnterpriseValidationReport>();

    public IReadOnlyList<ClinicalConceptModel> Concepts { get; set; } = Array.Empty<ClinicalConceptModel>();

    public IReadOnlyDictionary<int, AudioCaseRubricFeedbackSummaryModel> DoctorFeedbackBySubSectionId { get; set; } =
        new Dictionary<int, AudioCaseRubricFeedbackSummaryModel>();
}

public class AudioCaseRubricFeedbackSummaryModel
{
    public string FeedbackType { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public decimal? ConfidenceAtFeedback { get; set; }

    public DateTime EnteredDate { get; set; }

    public int? CorrectedSubSectionId { get; set; }
}

public class RubricEvidenceChainSessionResult
{
    public Guid SessionId { get; set; }

    public string EngineVersion { get; set; } = "v8";

    public List<RubricEnterpriseEvidenceChainModel> Chains { get; set; } = new();
}

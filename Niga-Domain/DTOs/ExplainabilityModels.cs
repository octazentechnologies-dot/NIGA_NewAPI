namespace Niga_Domain.DTOs;

public class RubricExplainabilityModel
{
    public string? PatientStatement { get; set; }

    public string? ClinicalMeaning { get; set; }

    public string? HomeopathicMeaning { get; set; }

    public string? WhySuggested { get; set; }

    public string? MatchLayer { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string? RubricTier { get; set; }

    public bool ReviewRequired { get; set; }

    public List<string> CausationChain { get; set; } = new();

    public Guid? SourceConceptId { get; set; }

    public RubricEvidenceChainModel? EvidenceChain { get; set; }

    public decimal? QualityScore { get; set; }

    public List<string> ValidationFlags { get; set; } = new();
}

public class ClinicalInferenceLogModel
{
    public Guid? SourceConceptId { get; set; }

    public string InferredRubricName { get; set; } = string.Empty;

    public int? SubSectionId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? SourceSymptom { get; set; }

    public decimal Confidence { get; set; }
}

public class ClinicalInferenceResult
{
    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public List<ClinicalInferenceLogModel> Logs { get; set; } = new();
}

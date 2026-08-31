namespace Niga_Domain.DTOs;

using Niga_Domain.Configuration;

public class PatientClinicalContext
{
    public int PatientId { get; set; }

    /// <summary>0 = male, 1 = female (GenderMaster convention).</summary>
    public int? Gender { get; set; }

    public int? AgeYears { get; set; }

    public string? PatientName { get; set; }
}

public class PrimarySymptomModel
{
    public string Text { get; set; } = string.Empty;

    public Guid? SourceConceptId { get; set; }

    public string Source { get; set; } = "ChiefComplaint";

    public decimal Confidence { get; set; } = 1m;
}

public class RubricEvidenceChainModel
{
    public List<string> PatientStatements { get; set; } = new();

    public List<string> ClinicalMeanings { get; set; } = new();

    public List<Guid> SourceConceptIds { get; set; } = new();

    public string? TranscriptExcerpt { get; set; }

    public decimal EvidenceStrength { get; set; }

    public string? MatchedSymptomPhrase { get; set; }
}

public class RubricValidationIssueModel
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int PenaltyPoints { get; set; }

    public bool IsHardReject { get; set; }
}

public class RubricValidationResultModel
{
    public bool IsAccepted { get; set; }

    public decimal QualityScore { get; set; }

    public List<RubricValidationIssueModel> Issues { get; set; } = new();
}

public class ClinicalValidationContext
{
    public PatientClinicalContext? Patient { get; set; }

    public string Transcript { get; set; } = string.Empty;

    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public List<CausationLinkModel> CausationLinks { get; set; } = new();

    public List<AudioCaseSymptomModel> Symptoms { get; set; } = new();

    public AudioCaseSummaryModel? Summary { get; set; }

    public PrimarySymptomModel? PrimarySymptom { get; set; }

    public Guid? SessionId { get; set; }
}

public class ClinicalValidationResult
{
    public List<AudioCaseSuggestedRubricModel> AcceptedRubrics { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> RejectedRubrics { get; set; } = new();

    public PrimarySymptomModel? PrimarySymptom { get; set; }

    public int RejectedCount => RejectedRubrics.Count;

    public bool UsedEnterprisePipeline { get; set; }

    public List<RubricEnterpriseValidationReport> ValidationReports { get; set; } = new();
}

public class RubricValidationStepContext
{
    public AudioCaseSuggestedRubricModel Rubric { get; set; } = new();

    public ClinicalValidationContext Context { get; set; } = new();

    public ClinicalConceptModel? LinkedConcept { get; set; }

    public RubricEvidenceChainModel EvidenceChain { get; set; } = new();

    public PrimarySymptomModel PrimarySymptom { get; set; } = new();

    public RubricIntelligenceOptions Options { get; set; } = new();
}

public class ValidationStepResult
{
    public string StepName { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public RubricValidationIssueModel? Issue { get; set; }
}

public class RubricEnterpriseValidationReport
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public bool PassedAllSteps { get; set; }

    public List<ValidationStepResult> Steps { get; set; } = new();

    public List<RubricValidationIssueModel> Issues { get; set; } = new();
}

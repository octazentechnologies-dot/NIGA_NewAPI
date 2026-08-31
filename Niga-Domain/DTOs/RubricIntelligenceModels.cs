namespace Niga_Domain.DTOs;

public class RubricIntelligenceMetaModel
{
    public string EngineVersion { get; set; } = "v1";

    public bool RequireManualApprovalForSuggestedRubrics { get; set; }

    public bool V2Enabled { get; set; }

    public bool RollbackToV1Only { get; set; }
}

public class RubricIntelligenceAnalysisResult
{
    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public List<AudioCaseSymptomModel> EnhancedSymptoms { get; set; } = new();

    public string EngineVersion { get; set; } = "v2";

    public bool RequiresManualApproval { get; set; } = true;

    public List<string> StagesCompleted { get; set; } = new();

    public List<CausationLinkModel> CausationLinks { get; set; } = new();

    public PrimarySymptomModel? PrimarySymptom { get; set; }

    public int ValidationRejectedCount { get; set; }
}

public class RubricIntelligenceHealthModel
{
    public bool V2Enabled { get; set; }

    public bool RollbackToV1Only { get; set; }

    public bool RequiresManualApproval { get; set; }

    public bool EnableRepertoryMapping { get; set; }

    public bool HasRuntimeOverride { get; set; }

    public string EngineVersion { get; set; } = "v1";

    public string Status { get; set; } = "Healthy";

    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;

    public RepertoryMappingStatusModel? RepertoryMapping { get; set; }

    public RubricIntelligenceRolloutStatusModel? RolloutStatus { get; set; }

    public bool EnableFastClinicalRetrievalPipeline { get; set; }

    public string FastPipelineEngineVersion { get; set; } = "fast-f";

    public bool EnableEciV8Engine { get; set; }

    public bool EnableV6ClinicalReasoningEngine { get; set; }

    public bool EnableV7RepertoryIntelligenceEngine { get; set; }

    public string? AppVersion { get; set; }

    public string? GitSha { get; set; }
}

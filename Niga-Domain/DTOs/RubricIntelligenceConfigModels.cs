namespace Niga_Domain.DTOs;

public class RubricIntelligenceConfigModel
{
    public bool EnableV2 { get; set; }

    public bool EnableV2ForAllDoctors { get; set; }

    public bool RequireManualApprovalForAllAiRubrics { get; set; }

    public bool RollbackToV1Only { get; set; }

    public bool EnableRepertoryMapping { get; set; }

    public bool EnableEmbeddingSearch { get; set; }

    public bool EnableClinicalInference { get; set; }

    public bool EnableDoctorFeedbackLearning { get; set; }

    public bool EnableV3ConceptGraph { get; set; }

    public bool EnableV3ShadowMode { get; set; }

    public bool IsV3Active { get; set; }

    public bool IsV2Active { get; set; }

    public bool RequiresManualApproval { get; set; }

    public bool HasRuntimeOverride { get; set; }

    public DateTime? RuntimeOverrideUpdatedUtc { get; set; }

    public int? RuntimeOverrideUpdatedByUserId { get; set; }

    public bool EnableFastClinicalRetrievalPipeline { get; set; }

    public string FastPipelineEngineVersion { get; set; } = "fast-f";

    public bool EnableEciV8Engine { get; set; }

    public bool EnableV6ClinicalReasoningEngine { get; set; }

    public bool EnableV7RepertoryIntelligenceEngine { get; set; }

    public string? AppVersion { get; set; }
}

public class RubricIntelligenceConfigUpdateModel
{
    public bool? EnableV2 { get; set; }

    public bool? RollbackToV1Only { get; set; }

    public bool? EnableRepertoryMapping { get; set; }

    public bool? StrictConceptGatedDiscovery { get; set; }

    public bool? DualLanguageForSensationSegments { get; set; }

    public bool? EnableV2ForAllDoctors { get; set; }

    public bool? EnableV3ConceptGraph { get; set; }

    public string? RolloutNotes { get; set; }
}

public class AiRolloutGateCreateModel
{
    public string FlagName { get; set; } = string.Empty;

    public DateTime BenchmarkRunUtc { get; set; }

    public decimal? Top5Accuracy { get; set; }

    public decimal? DoctorAcceptanceRate { get; set; }

    public string? Notes { get; set; }
}

public class RubricIntelligenceRolloutGateModel
{
    public string GateCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public bool Passed { get; set; }

    public string Detail { get; set; } = string.Empty;
}

public class RubricIntelligenceRolloutStatusModel
{
    public bool IsV2Active { get; set; }

    public bool RollbackToV1Only { get; set; }

    public bool ReadyForProductionRollout { get; set; }

    public int GatesPassed { get; set; }

    public int GatesTotal { get; set; }

    public decimal? AcceptanceRate30Day { get; set; }

    public decimal? PrimaryInTop5Rate30Day { get; set; }

    public int GoldCaseCount { get; set; }

    public int SessionsBenchmarked30Day { get; set; }

    public DateTime? LastGateTimestamp { get; set; }

    public decimal? LastGateTop5Accuracy { get; set; }

    public decimal? DeltaTop5VsLastGate { get; set; }

    public decimal? DeltaAcceptanceVsLastGate { get; set; }

    public List<string> FlagGateWarnings { get; set; } = new();

    public List<RubricIntelligenceRolloutGateModel> Gates { get; set; } = new();

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

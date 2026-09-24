namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AiPatientMeaning
{
    public long PatientMeaningId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string RawStatement { get; set; } = null!;

    public string NormalizedMeaning { get; set; } = null!;

    public string LanguageCode { get; set; } = "en";

    public decimal Confidence { get; set; }

    public int SequenceOrder { get; set; }

    public string ModelVersion { get; set; } = "v3-m1";

    public DateTime EnteredDate { get; set; }

    public long? ParentMeaningId { get; set; }

    public long? SymptomBlockId { get; set; }

    public string? SymptomCategory { get; set; }
}

public partial class AiMetaphorResolution
{
    public long MetaphorResolutionId { get; set; }

    public long PatientMeaningId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string Expression { get; set; } = null!;

    public string? LiteralMeaning { get; set; }

    public string ClinicalMeaning { get; set; } = null!;

    public decimal Confidence { get; set; }

    public string Source { get; set; } = "ai";

    public string ModelVersion { get; set; } = "v3-m2";

    public DateTime EnteredDate { get; set; }

    /// <summary>Task 2/3: genuine figurative expression detected.</summary>
    public bool IsMetaphor { get; set; }

    /// <summary>Task 3: clinical meaning grounded in AISensationOntology.</summary>
    public bool GroundedInOntology { get; set; }

    public long? OntologyId { get; set; }
}

/// <summary>Task 3: repertory-derived sensation/causation patterns extracted from SubSectionMaster (read-only ref).</summary>
public partial class AiSensationOntology
{
    public long OntologyId { get; set; }

    public string Pattern { get; set; } = null!;

    public int SubSectionId { get; set; }

    public string? SubSectionName { get; set; }

    public string SensationCategory { get; set; } = null!;

    public DateTime ExtractedAt { get; set; }

    public bool IsActive { get; set; } = true;
}

public partial class AiClinicalConceptV3
{
    public long ClinicalConceptId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long? PatientMeaningId { get; set; }

    public string ConceptName { get; set; } = null!;

    public string Domain { get; set; } = null!;

    public decimal Confidence { get; set; }

    public string ModelVersion { get; set; } = "v3-m3";

    public DateTime EnteredDate { get; set; }

    public string? SymptomCategory { get; set; }
}

public partial class AiHomeopathicConcept
{
    public long HomeopathicConceptId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long ClinicalConceptId { get; set; }

    public string ConceptName { get; set; } = null!;

    public string Importance { get; set; } = null!;

    public string? SymptomClass { get; set; }

    public bool IsSRP { get; set; }

    public decimal Weight { get; set; } = 1m;

    public decimal Confidence { get; set; }

    public string ModelVersion { get; set; } = "v3-m4";

    public DateTime EnteredDate { get; set; }

    public long? ClusterId { get; set; }
}

public partial class AiConceptGraphEdge
{
    public long ConceptGraphEdgeId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string FromNodeType { get; set; } = null!;

    public long FromNodeId { get; set; }

    public string ToNodeType { get; set; } = null!;

    public long ToNodeId { get; set; }

    public string EdgeType { get; set; } = null!;

    public decimal Weight { get; set; } = 1m;

    public decimal Confidence { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiRubricDiscovery
{
    public long RubricDiscoveryId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long HomeopathicConceptId { get; set; }

    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = null!;

    public string? MatchReason { get; set; }

    public string DiscoveryMethod { get; set; } = null!;

    public decimal Confidence { get; set; }

    public string ModelVersion { get; set; } = "v3-m5";

    public DateTime EnteredDate { get; set; }

    public string? RubricTier { get; set; }
}

public partial class AiRubricEvidence
{
    public long RubricEvidenceId { get; set; }

    public long RubricDiscoveryId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string EvidenceChainJson { get; set; } = null!;

    public bool IsComplete { get; set; }

    public decimal CoverageScore { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiRubricValidationV3
{
    public long RubricValidationId { get; set; }

    public long RubricDiscoveryId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string ValidationStatus { get; set; } = null!;

    public decimal? QualityScore { get; set; }

    public string? ValidationFlagsJson { get; set; }

    public DateTime ValidatedAt { get; set; }
}

public partial class AiRubricConfidence
{
    public long RubricConfidenceId { get; set; }

    public long RubricDiscoveryId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public decimal FinalScore { get; set; }

    public int RankOrder { get; set; }

    public string? Tier { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiDoctorFeedback
{
    public long DoctorFeedbackId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long? RubricDiscoveryId { get; set; }

    public string Action { get; set; } = null!;

    public string? Reason { get; set; }

    public string? RejectReasonStage { get; set; }

    public string? RejectReasonNote { get; set; }

    public int DoctorUserId { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiCaseLearning
{
    public long CaseLearningId { get; set; }

    public Guid SourceSessionId { get; set; }

    public string LearningType { get; set; } = null!;

    public string FromConcept { get; set; } = null!;

    public int? ToRubricSubSectionId { get; set; }

    public decimal WeightDelta { get; set; }

    public string? ContextJson { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiReasoningAudit
{
    public long ReasoningAuditId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string PipelineStage { get; set; } = null!;

    public string ModelId { get; set; } = null!;

    public string? RequestJson { get; set; }

    public string? ResponseJson { get; set; }

    public int? LatencyMs { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiConceptMappingBootstrap
{
    public int ConceptMappingBootstrapId { get; set; }

    public string HomeopathicConceptPattern { get; set; } = null!;

    public string SubSectionNamePattern { get; set; } = null!;

    public string? Domain { get; set; }

    public int PriorityOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EnteredDate { get; set; }
}

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public static class V6PipelineStageNames
{
    public const string TranscriptCleanup = "TranscriptCleanup";
    public const string SpeakerCorrection = "SpeakerCorrection";
    public const string TemporalSegmentation = "TemporalSegmentation";
    public const string SymptomExtraction = "SymptomExtraction";
    public const string SymptomNormalization = "SymptomNormalization";
    public const string ClinicalOntologyMapping = "ClinicalOntologyMapping";
    public const string HomeopathicOntologyMapping = "HomeopathicOntologyMapping";
    public const string RubricOntologyMapping = "RubricOntologyMapping";
    public const string SqlRepertorySearch = "SqlRepertorySearch";
    public const string HierarchyExpansion = "HierarchyExpansion";
    public const string EmbeddingSearch = "EmbeddingSearch";
    public const string CandidateMerge = "CandidateMerge";
    public const string EvidenceAggregation = "EvidenceAggregation";
    public const string ClinicalValidation = "ClinicalValidation";
    public const string RubricRanking = "RubricRanking";
    public const string FinalSelection = "FinalSelection";
}

public static class V6SqlSearchStageNames
{
    public const string ExactMatch = "ExactMatch";
    public const string LikeMatch = "LikeMatch";
    public const string NormalizedMatch = "NormalizedMatch";
    public const string SynonymAlias = "SynonymAlias";
    public const string BootstrapMapping = "BootstrapMapping";
    public const string HotspotSearch = "HotspotSearch";
    public const string ParentRubric = "ParentRubric";
    public const string ChildRubric = "ChildRubric";
    public const string SiblingRubric = "SiblingRubric";
    public const string CrossReference = "CrossReference";
    public const string OntologyLink = "OntologyLink";
    public const string EmbeddingSimilarity = "EmbeddingSimilarity";
}

public class V6ClinicalSymptomUnit
{
    public long? HomeopathicConceptId { get; set; }

    public string ClinicalLabel { get; set; } = string.Empty;

    public string HomeopathicLabel { get; set; } = string.Empty;

    public string SymptomClass { get; set; } = "Clinical";

    public string? TemporalContext { get; set; }

    public string? Modality { get; set; }

    public string? Etiology { get; set; }

    public string? Concomitant { get; set; }

    public string? TranscriptEvidence { get; set; }

    public decimal Confidence { get; set; }

    public bool IsSRP { get; set; }

    public string? ConceptTier { get; set; }
}

public class V6SqlSearchHit
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public string SearchStage { get; set; } = string.Empty;

    public string MatchPath { get; set; } = string.Empty;

    public decimal SqlConfidence { get; set; }

    public int HierarchyDepth { get; set; }
}

public class V6SymptomDiscoveryAudit
{
    public string SymptomLabel { get; set; } = string.Empty;

    public long? HomeopathicConceptId { get; set; }

    public List<V6SqlSearchHit> SqlHits { get; set; } = new();

    public List<int> KnowledgeGraphSubSectionIds { get; set; } = new();

    public List<int> EmbeddingSubSectionIds { get; set; } = new();

    public string? ValidationResult { get; set; }

    public string OutputCategory { get; set; } = HybridCompletionOutputCategories.RepertoryDatabase;

    public List<string> StageLog { get; set; } = new();
}

public class V6ClinicalReasoningRequest
{
    public Guid SessionId { get; set; }

    public string Transcript { get; set; } = string.Empty;

    public ConceptGraphFullModel Graph { get; set; } = new();

    public IReadOnlyList<RubricDiscoveryNodeModel> GlobalDiscoveries { get; set; } = Array.Empty<RubricDiscoveryNodeModel>();

    public IReadOnlyList<RubricCandidateModel> EmbeddingCandidates { get; set; } = Array.Empty<RubricCandidateModel>();

    public IReadOnlyList<KgRubricPathModel> KnowledgeGraphPaths { get; set; } = Array.Empty<KgRubricPathModel>();
}

public class V6ClinicalReasoningResult
{
    public bool Success { get; set; }

    public string EngineVersion { get; set; } = "v6.0";

    public List<V6ClinicalSymptomUnit> SymptomUnits { get; set; } = new();

    public List<RubricDiscoveryNodeModel> Discoveries { get; set; } = new();

    public List<V6SymptomDiscoveryAudit> AuditTrail { get; set; } = new();

    public List<string> StagesCompleted { get; set; } = new();

    public string? Error { get; set; }
}

public class V6RubricExplainabilityModel
{
    public string? TranscriptEvidence { get; set; }

    public string? ClinicalReasoning { get; set; }

    public string? OntologyPath { get; set; }

    public string? SqlMatchPath { get; set; }

    public string? HierarchyPath { get; set; }

    public decimal? EmbeddingScore { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public decimal? ValidationScore { get; set; }

    public string? FinalExplanation { get; set; }

    public List<string> SearchStages { get; set; } = new();
}

public class V6BenchmarkCase
{
    public string CaseId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> ExpectedRubricPatterns { get; set; } = new();

    public List<string> ForbiddenGenericPatterns { get; set; } = new();
}

public class V6BenchmarkReport
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public int CasesEvaluated { get; set; }

    public decimal SqlRubricRecall { get; set; }

    public decimal SqlRubricPrecision { get; set; }

    public decimal ExpertAgreementRate { get; set; }

    public decimal DuplicateRate { get; set; }

    public decimal FalsePositiveRate { get; set; }

    public decimal AiConceptUsefulness { get; set; }

    public List<string> IdentifiedGaps { get; set; } = new();

    public List<string> ProposedImprovements { get; set; } = new();

    public List<V6BenchmarkCaseResult> CaseResults { get; set; } = new();
}

public class V6BenchmarkCaseResult
{
    public string CaseId { get; set; } = string.Empty;

    public int ExpectedCount { get; set; }

    public int MatchedCount { get; set; }

    public int ProducedCount { get; set; }

    public decimal Recall { get; set; }

    public decimal Precision { get; set; }

    public List<string> MatchedPatterns { get; set; } = new();

    public List<string> MissedPatterns { get; set; } = new();

    public List<string> UnexpectedRubrics { get; set; } = new();
}

public class V6BenchmarkEvaluateRequest
{
    public string CaseId { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string>? ExpectedRubricPatterns { get; set; }

    public List<string>? ForbiddenGenericPatterns { get; set; }

    /// <summary>Rubrics produced by running the V6 pipeline on this case (for offline evaluation).</summary>
    public List<AudioCaseSuggestedRubricModel>? ProducedRubrics { get; set; }
}

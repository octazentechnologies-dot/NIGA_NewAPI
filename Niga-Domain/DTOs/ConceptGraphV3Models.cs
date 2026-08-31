namespace Niga_Domain.DTOs;

public class PatientMeaningNodeModel
{
    public long? PatientMeaningId { get; set; }

    public string MeaningId { get; set; } = Guid.NewGuid().ToString("N");

    public string RawStatement { get; set; } = string.Empty;

    public string NormalizedMeaning { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = "en";

    public decimal Confidence { get; set; }

    public int SequenceOrder { get; set; }

    public string ModelVersion { get; set; } = "v3-m1";

    public long? ParentMeaningId { get; set; }

    public long? SymptomBlockId { get; set; }

    public string? SymptomCategory { get; set; }

    /// <summary>Task 4: English translation when dual-language path is active.</summary>
    public string? EnglishTranslation { get; set; }

    /// <summary>Task 4: original-language text for sensation/emotion-bearing utterances.</summary>
    public string? OriginalLanguageText { get; set; }

    public bool IsSensationBearing { get; set; }
}

public class PatientMeaningGraphResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public int LatencyMs { get; set; }

    public List<PatientMeaningNodeModel> Meanings { get; set; } = new();

    public string ModelVersion { get; set; } = "v3-m1";
}

public class ConceptGraphMeaningGraphModel
{
    public Guid SessionId { get; set; }

    public string EngineVersion { get; set; } = "v3";

    public List<PatientMeaningNodeModel> Meanings { get; set; } = new();

    public int MeaningCount => Meanings.Count;
}

public class ConceptGraphPhaseResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public string EngineVersion { get; set; } = "v3";

    public List<string> StagesCompleted { get; set; } = new();

    public PatientMeaningGraphResult? MeaningGraph { get; set; }
}

public class AiReasoningAuditModel
{
    public string PipelineStage { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string? RequestJson { get; set; }

    public string? ResponseJson { get; set; }

    public int? LatencyMs { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }
}

public class PatientMeaningExtractionGptModel
{
    public List<PatientMeaningExtractionItemGptModel> Meanings { get; set; } = new();
}

public class PatientMeaningExtractionItemGptModel
{
    public string? RawStatement { get; set; }

    public string? NormalizedMeaning { get; set; }

    public string? LanguageCode { get; set; }

    public decimal Confidence { get; set; }

    public bool IsSensationBearing { get; set; }
}

/// <summary>Task 4: optional dual-language context for sensation-bearing meaning nodes.</summary>
public class DualLanguageMeaningContext
{
    public string EnglishTranscript { get; set; } = string.Empty;

    public string? OriginalLanguageTranscript { get; set; }

    public string? LanguageCode { get; set; }

    public List<DualLanguageSensationHint> SensationHints { get; set; } = new();
}

public class DualLanguageSensationHint
{
    public string EnglishPhrase { get; set; } = string.Empty;

    public string? OriginalLanguageText { get; set; }

    public string? LanguageCode { get; set; }
}

public class MetaphorResolutionNodeModel
{
    public long? MetaphorResolutionId { get; set; }

    public long? PatientMeaningId { get; set; }

    public string Expression { get; set; } = string.Empty;

    public string? LiteralMeaning { get; set; }

    public string ClinicalMeaning { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    /// <summary>True only when a genuine non-literal expression was detected (Task 2).</summary>
    public bool IsMetaphor { get; set; }

    /// <summary>Task 3: true when clinical meaning was grounded in AISensationOntology.</summary>
    public bool GroundedInOntology { get; set; }

    public long? OntologyId { get; set; }

    public string ModelVersion { get; set; } = "v3-m2";
}

public static class ConceptTierLabels
{
    public const string Primary = "Primary";

    public const string Secondary = "Secondary";

    public const string Supporting = "Supporting";
}

public class ClinicalConceptNodeModel
{
    public long? ClinicalConceptId { get; set; }

    public long? PatientMeaningId { get; set; }

    public int MeaningIndex { get; set; } = -1;

    public string ConceptName { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    public string ModelVersion { get; set; } = "v3-m3";

    /// <summary>Task 2: "Literal" (DerivesClinical) or "Metaphor" (Interprets).</summary>
    public string InterpretationSource { get; set; } = "Literal";

    public string? SymptomCategory { get; set; }

    public string? ConceptTier { get; set; }
}

public class HomeopathicConceptNodeModel
{
    public long? HomeopathicConceptId { get; set; }

    public long? ClinicalConceptId { get; set; }

    public int ClinicalConceptIndex { get; set; } = -1;

    public string ConceptName { get; set; } = string.Empty;

    public string Importance { get; set; } = "Medium";

    public string? SymptomClass { get; set; }

    public bool IsSRP { get; set; }

    public decimal Weight { get; set; } = 1m;

    public decimal Confidence { get; set; }

    public string ModelVersion { get; set; } = "v3-m4";

    public long? ClusterId { get; set; }

    public string? EvidenceSpan { get; set; }

    public string? ConceptTier { get; set; }

    public string? Category { get; set; }
}

public class RubricDiscoveryNodeModel
{
    public long? RubricDiscoveryId { get; set; }

    public long? HomeopathicConceptId { get; set; }

    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public string? MatchReason { get; set; }

    public string DiscoveryMethod { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    public decimal? QualityScore { get; set; }

    public string? ValidationStatus { get; set; }

    public string? ValidationFlagsJson { get; set; }

    public string? RubricTier { get; set; }

    public RubricEvidenceChainV3Model? EvidenceChain { get; set; }

    public RubricEnterpriseEvidenceChainModel? EnterpriseEvidenceChain { get; set; }
}

public class RubricEvidenceChainV3Model
{
    public string? TranscriptStatement { get; set; }

    public string? PatientMeaning { get; set; }

    public string? ClinicalConcept { get; set; }

    public string? HomeopathicConcept { get; set; }

    public string? RubricName { get; set; }

    public decimal Confidence { get; set; }

    public bool IsComplete { get; set; }
}

public class ConceptGraphEdgeModel
{
    public string FromNodeType { get; set; } = string.Empty;

    public string FromNodeKey { get; set; } = string.Empty;

    public long? FromNodeId { get; set; }

    public string ToNodeType { get; set; } = string.Empty;

    public string ToNodeKey { get; set; } = string.Empty;

    public long? ToNodeId { get; set; }

    public string EdgeType { get; set; } = string.Empty;

    public decimal Weight { get; set; } = 1m;

    public decimal Confidence { get; set; }
}

public class TieredConceptNodeModel
{
    public string ConceptTier { get; set; } = ConceptTierLabels.Supporting;

    public string Category { get; set; } = string.Empty;

    public string ClinicalConceptName { get; set; } = string.Empty;

    public string HomeopathicConceptName { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    public decimal Weight { get; set; } = 1m;

    public bool IsSRP { get; set; }

    public int ClinicalConceptIndex { get; set; } = -1;

    public int HomeopathicConceptIndex { get; set; } = -1;

    public int? MeaningIndex { get; set; }

    public string? EvidenceSpan { get; set; }

    public string? SymptomClass { get; set; }
}

public class MultiConceptDiscoveryResult
{
    public List<ClinicalConceptNodeModel> ClinicalConcepts { get; set; } = new();

    public List<HomeopathicConceptNodeModel> HomeopathicConcepts { get; set; } = new();

    public List<ConceptGraphEdgeModel> Edges { get; set; } = new();

    public List<TieredConceptNodeModel> PrimaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SecondaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SupportingConcepts { get; set; } = new();
}

public class ConceptGraphFullModel
{
    public Guid SessionId { get; set; }

    public string EngineVersion { get; set; } = "v3";

    public List<PatientMeaningNodeModel> Meanings { get; set; } = new();

    public List<MetaphorResolutionNodeModel> Metaphors { get; set; } = new();

    public List<ClinicalConceptNodeModel> ClinicalConcepts { get; set; } = new();

    public List<HomeopathicConceptNodeModel> HomeopathicConcepts { get; set; } = new();

    public List<RubricDiscoveryNodeModel> Discoveries { get; set; } = new();

    public List<SymptomBlockNodeModel> SymptomBlocks { get; set; } = new();

    public List<ConceptClusterNodeModel> Clusters { get; set; } = new();

    public List<ConceptGraphEdgeModel> ConceptGraphEdges { get; set; } = new();

    public List<TieredConceptNodeModel> PrimaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SecondaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SupportingConcepts { get; set; } = new();

    public List<RubricCandidateModel> RubricCandidates { get; set; } = new();

    public List<RubricEnterpriseEvidenceChainModel> EnterpriseEvidenceChains { get; set; } = new();

    public List<KgRubricPathModel> KnowledgeGraphPaths { get; set; } = new();

    public CaseCoverageMetricsModel? CoverageMetrics { get; set; }
}

public class SymptomBlockNodeModel
{
    public long? SymptomBlockId { get; set; }

    public int BlockOrder { get; set; }

    public string TranscriptSpan { get; set; } = string.Empty;

    public string CategoryHint { get; set; } = string.Empty;

    public string? BlockType { get; set; }

    public decimal Confidence { get; set; }

    public bool IsCovered { get; set; }

    public string ModelVersion { get; set; } = "v3.5-m0";
}

public class ConceptClusterNodeModel
{
    public long? ConceptClusterId { get; set; }

    public string ClusterLabel { get; set; } = string.Empty;

    public long? SymptomBlockId { get; set; }

    public List<long> HomeopathicConceptIds { get; set; } = new();
}

public class CaseCoverageMetricsModel
{
    public decimal TranscriptCoverage { get; set; }

    public decimal CaseCompleteness { get; set; }

    public int TotalBlocks { get; set; }

    public int CoveredBlocks { get; set; }

    public int Tier1Count { get; set; }

    public int Tier2Count { get; set; }

    public int Tier3Count { get; set; }

    public int MissingSymptomCount { get; set; }

    public List<string> UncoveredSpans { get; set; } = new();
}

public class MissingSymptomPassResult
{
    public int ResolvedBlockCount { get; set; }

    public List<RubricDiscoveryNodeModel> AdditionalDiscoveries { get; set; } = new();
}

public class ConceptGraphAnalysisResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public string EngineVersion { get; set; } = "v3";

    public List<string> StagesCompleted { get; set; } = new();

    public ConceptGraphFullModel Graph { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public PrimarySymptomModel? PrimarySymptom { get; set; }

    public int ValidationRejectedCount { get; set; }

    public decimal? TranscriptCoverageScore { get; set; }

    public decimal? CaseCompletenessScore { get; set; }

    public List<AudioCaseSuggestedRubricModel> Tier1Rubrics { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> Tier2Rubrics { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> Tier3Rubrics { get; set; } = new();

    public CaseCoverageMetricsModel? CoverageMetrics { get; set; }

    public List<TieredConceptNodeModel> PrimaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SecondaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SupportingConcepts { get; set; } = new();

    public List<ConceptGraphEdgeModel> ConceptGraphEdges { get; set; } = new();

    public List<RubricCandidateModel> RubricCandidates { get; set; } = new();

    public List<RubricCandidateModel> Tier1RubricCandidates { get; set; } = new();

    public List<RubricCandidateModel> Tier2RubricCandidates { get; set; } = new();

    public List<RubricCandidateModel> Tier3RubricCandidates { get; set; } = new();

    public List<RubricEnterpriseValidationReport> ValidationReports { get; set; } = new();

    public List<RubricEnterpriseEvidenceChainModel> EnterpriseEvidenceChains { get; set; } = new();
}

public class MetaphorExtractionGptModel
{
    public List<MetaphorExtractionItemGptModel> Metaphors { get; set; } = new();
}

public class MetaphorExtractionItemGptModel
{
    public int MeaningIndex { get; set; }

    public string? Expression { get; set; }

    public string? LiteralMeaning { get; set; }

    public string? ClinicalMeaning { get; set; }

    public decimal Confidence { get; set; }

    public bool IsMetaphor { get; set; }
}

public class ClinicalConceptExtractionGptModel
{
    public List<ClinicalConceptExtractionItemGptModel> Concepts { get; set; } = new();
}

public class ClinicalConceptExtractionItemGptModel
{
    public int MeaningIndex { get; set; }

    public string? ConceptName { get; set; }

    public string? Domain { get; set; }

    public decimal Confidence { get; set; }

    /// <summary>Task 2: Literal or Metaphor.</summary>
    public string? InterpretationSource { get; set; }
}

public class HomeopathicConceptExtractionGptModel
{
    public List<HomeopathicConceptExtractionItemGptModel> Concepts { get; set; } = new();
}

public class HomeopathicConceptExtractionItemGptModel
{
    public int ClinicalConceptIndex { get; set; }

    public string? ConceptName { get; set; }

    public string? Importance { get; set; }

    public string? SymptomClass { get; set; }

    public bool IsSRP { get; set; }

    public decimal Confidence { get; set; }
}

public class CaseDecompositionGptModel
{
    public List<CaseDecompositionBlockGptModel> Blocks { get; set; } = new();
}

public class CaseDecompositionBlockGptModel
{
    public int BlockOrder { get; set; }

    public string? TranscriptSpan { get; set; }

    public string? CategoryHint { get; set; }

    public string? BlockType { get; set; }

    public decimal Confidence { get; set; }
}

public class MultiSymptomExpansionGptModel
{
    public List<MultiSymptomExpansionItemGptModel> Expansions { get; set; } = new();
}

public class MultiSymptomExpansionItemGptModel
{
    public int ParentMeaningIndex { get; set; }

    public string? RawStatement { get; set; }

    public string? NormalizedMeaning { get; set; }

    public string? SymptomCategory { get; set; }

    public decimal Confidence { get; set; }
}

public class CategoryDiscoveryGptModel
{
    public List<CategoryDiscoveryItemGptModel> Categories { get; set; } = new();
}

public class CategoryDiscoveryItemGptModel
{
    public int MeaningIndex { get; set; }

    public string? SymptomCategory { get; set; }

    public decimal Confidence { get; set; }
}

public class RecallExpansionGptModel
{
    public List<RecallExpansionItemGptModel> Concepts { get; set; } = new();
}

public class RecallExpansionItemGptModel
{
    public int ClinicalConceptIndex { get; set; }

    public string? ConceptName { get; set; }

    public string? Importance { get; set; }

    public string? SymptomClass { get; set; }

    public bool IsSRP { get; set; }

    public decimal Confidence { get; set; }

    public string? EvidenceSpan { get; set; }
}

public class MultiConceptDiscoveryGptModel
{
    public List<MultiConceptDiscoveryItemGptModel> Concepts { get; set; } = new();
}

public class MultiConceptDiscoveryItemGptModel
{
    public int MeaningIndex { get; set; }

    public string? Category { get; set; }

    public string? ClinicalConceptName { get; set; }

    public string? HomeopathicConceptName { get; set; }

    public string? Importance { get; set; }

    public string? SymptomClass { get; set; }

    public bool IsSRP { get; set; }

    public decimal Confidence { get; set; }

    public string? EvidenceSpan { get; set; }

    /// <summary>Task 2: Literal or Metaphor path that produced this concept.</summary>
    public string? InterpretationSource { get; set; }
}

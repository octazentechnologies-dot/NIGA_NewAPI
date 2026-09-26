namespace Homeocentrum.Niga.NewAPI.Domain.Configuration;

public class RubricIntelligenceOptions
{
    public const string SectionName = "RubricIntelligence";

    /// <summary>Master switch for V2 rubric intelligence pipeline.</summary>
    public bool EnableV2 { get; set; }

    /// <summary>When true, V2 is available to all doctors (still gated by EnableV2).</summary>
    public bool EnableV2ForAllDoctors { get; set; } = true;

    /// <summary>Doctors must explicitly approve each suggested rubric before Repertorize.</summary>
    public bool RequireManualApprovalForAllAiRubrics { get; set; } = true;

    /// <summary>Future: auto-apply only after historical acceptance exceeds threshold.</summary>
    public bool AllowAutoApplyHighConfidence { get; set; }

    public decimal AutoApplyMinAcceptanceRateHistorical { get; set; } = 0.95m;

    public bool EnableEmbeddingSearch { get; set; } = true;

    public int EmbeddingTopK { get; set; } = 50;

    public int EmbeddingIndexerBatchSize { get; set; } = 100;

    public int EmbeddingIndexerMaxRubricsPerRun { get; set; } = 500;

    public int EmbeddingIndexerIntervalHours { get; set; } = 24;

    public HybridWeightOptions HybridWeights { get; set; } = new();

    public bool EnableClinicalInference { get; set; } = true;

    public decimal MinConceptConfidenceForInference { get; set; } = 0.85m;

    public decimal MinRetrievalScoreToSkipInference { get; set; } = 0.70m;

    public decimal MinEmbeddingNeighborForInference { get; set; } = 0.65m;

    public bool EnableDoctorFeedbackLearning { get; set; } = true;

    /// <summary>When true, Kent/Complete repertory mapping enriches rubric tiers.</summary>
    public bool EnableRepertoryMapping { get; set; } = true;

    /// <summary>Emergency rollback — skips V2 orchestrator entirely.</summary>
    public bool RollbackToV1Only { get; set; }

    /// <summary>V2.1: clinical validation gate (gender, domain, hallucination, quality score).</summary>
    public bool EnableClinicalValidationV21 { get; set; }

    public decimal MinRubricQualityScore { get; set; } = 70m;

    /// <summary>Lower quality floor for Tier-3 / supporting rubrics.</summary>
    public decimal MinRubricQualityScoreTier3 { get; set; } = 58m;

    /// <summary>When strict validation accepts zero rubrics, surface best rejected candidates at this floor.</summary>
    public decimal MinRubricQualityScoreReviewFallback { get; set; } = 50m;

    /// <summary>Return review-tier rubrics instead of an empty list when validation rejects all candidates.</summary>
    public bool EnableV3ReviewFallback { get; set; } = true;

    public decimal MinEmbeddingCosineForCandidate { get; set; } = 0.72m;

    public decimal MinEvidenceSimilarityForRubric { get; set; } = 0.38m;

    public decimal MinEvidenceSimilarityForInference { get; set; } = 0.50m;

    public decimal MinEvidenceStrength { get; set; } = 0.30m;

    /// <summary>V3: Concept Graph pipeline (Patient Meaning → Clinical → Homeopathic → Discovery).</summary>
    public bool EnableV3ConceptGraph { get; set; }

    /// <summary>V3 shadow mode: build graph but keep V2 rubric results for comparison.</summary>
    public bool EnableV3ShadowMode { get; set; } = true;

    /// <summary>
    /// When true with EnableV3ConceptGraph, skip V1 LIKE / unscoped embedding union if the concept graph
    /// produced usable homeopathic concepts above ConceptGraphMinConfidence. Flagged rollout — default off.
    /// </summary>
    public bool StrictConceptGatedDiscovery { get; set; }

    /// <summary>Minimum AIHomeopathicConcept.Confidence required for concept-gated discovery path.</summary>
    public decimal ConceptGraphMinConfidence { get; set; } = 0.55m;

    /// <summary>
    /// Task 1 follow-up: exclusive ConceptGraphOnly path requires at least this many
    /// authoritative repertory rubrics (SubSectionId &gt; 0). Below this count, fall back to V1+V2
    /// even if individual concept confidence is high (prevents high-confidence / low-recall lockout).
    /// </summary>
    public int ConceptGraphMinCandidatesForExclusivePath { get; set; } = 3;

    /// <summary>Task 3: minimum ontology match score to prefer repertory-grounded metaphor resolution.</summary>
    public decimal OntologyMatchMinConfidence { get; set; } = 0.6m;

    /// <summary>
    /// Task 4: when true, run one scoped original-language Whisper pass for sensation/emotion-bearing
    /// symptoms and pass dual-language text into PatientMeaningGraphEngine. Flagged rollout — default off.
    /// </summary>
    public bool DualLanguageForSensationSegments { get; set; }

    /// <summary>Task 6: minimum confidence to promote an exact (case/whitespace-insensitive) AI name to SubSectionMaster.</summary>
    public decimal AiReconciliationMinConfidence { get; set; } = 0.7m;

    /// <summary>Fuzzy/token-overlap promotions need a higher floor so they cannot look like clean DB hits.</summary>
    public decimal AiReconciliationFuzzyMinConfidence { get; set; } = 0.85m;

    /// <summary>Task 7: exclude incomplete evidence chains from validation/ranking (not merely flag them).</summary>
    public bool EnforceEvidenceChainCompleteGate { get; set; } = true;

    /// <summary>V3.5: Multi-symptom recall engine (decomposition, recall expansion, tiers, coverage).</summary>
    public bool EnableV35RecallEngine { get; set; } = true;

    public int MaxRubricsTier1 { get; set; } = 5;

    public int MaxRubricsTier2 { get; set; } = 10;

    public int MaxRubricsTier3 { get; set; } = 10;

    public decimal MinTranscriptCoverage { get; set; } = 0.90m;

    public decimal MinCaseCompleteness { get; set; } = 0.90m;

    public decimal MinEvidenceSimilarityTier3 { get; set; } = 0.45m;

    public int MaxDiscoveryCandidates { get; set; } = 60;

    public int MaxRubricsPerPattern { get; set; } = 8;

    /// <summary>V3.5: parallel stages, skip redundant GPT, defer heavy DB graph writes.</summary>
    public bool EnableV35FastPipeline { get; set; } = true;

    /// <summary>Save concept graph after rubrics are ready (user sees results faster).</summary>
    public bool DeferGraphPersistence { get; set; } = true;

    /// <summary>Max homeopathic concepts sent in one embedding batch (not one API call per concept).</summary>
    public int MaxEmbeddingConceptsPerPass { get; set; } = 12;

    /// <summary>Per-concept keyword discovery against SubSectionMaster (mental/particular/SRP included).</summary>
    public bool EnablePerConceptKeywordDiscovery { get; set; } = true;

    /// <summary>Bounded parallelism for per-concept discovery (latency Task 3).</summary>
    public int ConceptDiscoveryMaxConcurrency { get; set; } = 6;

    /// <summary>Hard timeout per concept during keyword discovery; skip-and-log on exceed.</summary>
    public int ConceptDiscoveryTimeoutSeconds { get; set; } = 25;

    /// <summary>Skip missing-symptom second pass when this many rubrics already accepted.</summary>
    public int MinRubricsToSkipSecondPass { get; set; } = 6;

    /// <summary>Skip M1b multi-symptom GPT when primary meaning graph already has this many nodes.</summary>
    public int MinPrimaryMeaningsToSkipMultiSymptom { get; set; } = 10;

    /// <summary>Phase 5: extract ALL supported concepts independently and build concept graph with tiers.</summary>
    public bool EnableMultiConceptDiscovery { get; set; } = true;

    /// <summary>Phase 6: rank rubric candidates via AIConceptEmbedding with clinical relevance and doctor acceptance.</summary>
    public bool EnableRubricCandidateEngine { get; set; } = true;

    /// <summary>Phase 7: mandatory 8-step enterprise clinical validation pipeline. No rubric bypasses validation.</summary>
    public bool EnableEnterpriseClinicalValidation { get; set; } = true;

    /// <summary>Phase 8: full rubric evidence chain from transcript through doctor feedback.</summary>
    public bool EnableEnterpriseRubricEvidenceChain { get; set; } = true;

    /// <summary>Phase 9: doctor learning engine — stores AICaseLearning signals; never modifies repertory tables.</summary>
    public bool EnableDoctorLearningEngine { get; set; } = true;

    public decimal DoctorLearningAcceptBoost { get; set; } = 0.15m;

    public decimal DoctorLearningRejectPenalty { get; set; } = -0.20m;

    public decimal DoctorLearningCorrectionBoost { get; set; } = 0.12m;

    public decimal DoctorLearningConceptRankingBoost { get; set; } = 0.08m;

    public decimal DoctorLearningClinicalRelevanceBoost { get; set; } = 0.10m;

    public decimal DoctorLearningMaxAccumulatedWeight { get; set; } = 1.0m;

    /// <summary>Phase 10: AI monitoring dashboard — KPI snapshots, trends, embedding health, audit log.</summary>
    public bool EnableAiMonitoringDashboard { get; set; } = true;

    /// <summary>Phase 11: enterprise homeopathic knowledge graph as primary reasoning layer.</summary>
    public bool EnableEnterpriseKnowledgeGraph { get; set; }

    /// <summary>V4.0: unified enterprise rubric discovery engine (multi-source merge).</summary>
    public bool EnableEnterpriseRubricDiscoveryEngine { get; set; } = true;

    /// <summary>V4.0: expand bootstrap patterns to related repertory rubrics per concept.</summary>
    public bool EnableEnterpriseRubricExpansion { get; set; } = true;

    /// <summary>V4.0: when enterprise validation rejects all, surface review-tier candidates.</summary>
    public bool EnableEnterpriseRubricReviewFallback { get; set; } = true;

    /// <summary>Max candidate rubrics before validation (discovery only).</summary>
    public int MaxEnterpriseDiscoveryCandidates { get; set; } = 100;

    /// <summary>Phase 11 shadow mode: build KG paths but keep embedding-primary ranking for comparison.</summary>
    public bool EnableKnowledgeGraphShadowMode { get; set; } = true;

    public decimal KnowledgeGraphMinPathConfidence { get; set; } = 0.55m;

    public decimal KnowledgeGraphEmbeddingBlendRatio { get; set; } = 0.35m;

    public bool KnowledgeGraphEnableMultilingual { get; set; } = true;

    public int KnowledgeGraphRemedyProjectionSyncHours { get; set; } = 24;

    /// <summary>Minimum enterprise confidence (0–100) required to display a rubric to doctors.</summary>
    public decimal MinEnterpriseRubricConfidenceScore { get; set; } = 62m;

    /// <summary>When true, unmatched concepts are returned as AI clinical concepts — never as invented rubrics.</summary>
    public bool EnableAiClinicalConceptSuggestions { get; set; } = true;

    /// <summary>When true, log every validation rejection with rule, threshold, and reason.</summary>
    public bool EnableRubricValidationAuditLogging { get; set; } = true;

    /// <summary>V5.2: per-concept hybrid completion — DB authoritative, AI concepts supplement unmapped only.</summary>
    public bool EnableHybridCompletionEngine { get; set; } = true;

    /// <summary>Max repertory rubrics allocated per extracted concept (prevents one concept dominating).</summary>
    public int MaxRubricsPerConceptSlots { get; set; } = 4;

    /// <summary>V6: enterprise clinical reasoning engine — SQL authoritative, AI reasoning only.</summary>
    public bool EnableV6ClinicalReasoningEngine { get; set; }

    /// <summary>V6: target minimum validated SQL rubrics per consultation when clinically justified.</summary>
    public int MinValidatedSqlRubrics { get; set; } = 15;

    /// <summary>V6: maximum AI clinical concepts displayed (never auto-repertorized).</summary>
    public int MaxAiClinicalConcepts { get; set; } = 5;

    /// <summary>V6: max SQL hits retained per symptom unit during authoritative search.</summary>
    public int MaxSqlHitsPerSymptom { get; set; } = 12;

    /// <summary>V6: run benchmark self-validation on startup (logs quality report).</summary>
    public bool EnableV6BenchmarkOnStartup { get; set; }

    /// <summary>V7: enterprise repertory intelligence — modular search, ranking, completion.</summary>
    public bool EnableV7RepertoryIntelligenceEngine { get; set; }

    /// <summary>V7: use GPT structured symptom extraction (language only, no rubrics).</summary>
    public bool EnableV7GptStructuredExtraction { get; set; } = true;

    /// <summary>V7: minimum database rubrics before completion retries stop.</summary>
    public int V7MinDatabaseRubrics { get; set; } = 15;

    /// <summary>V7: max vocabulary terms generated per symptom.</summary>
    public int V7MaxVocabularyTermsPerSymptom { get; set; } = 30;

    /// <summary>V7: embedding top-K per symptom search.</summary>
    public int V7EmbeddingTopK { get; set; } = 50;

    /// <summary>V7: target rubric retrieval latency budget in milliseconds.</summary>
    public int V7LatencyBudgetMs { get; set; } = 2000;

    /// <summary>
    /// Stage C: fast clinical retrieval production path.
    /// When true: skip V7/Enterprise/EnsureAll; use extraction symptoms + parallel V1/Keyword FTS.
    /// When false: legacy multi-engine path (rollback). Default false.
    /// </summary>
    public bool EnableFastClinicalRetrievalPipeline { get; set; }

    /// <summary>
    /// When fast path is enabled, max seconds to wait for semantic cache before proceeding degraded.
    /// 0 = do not block (recommended; baseline showed 120s Timeout waste).
    /// </summary>
    public int FastPipelineSemanticCacheMaxWaitSeconds { get; set; } = 0;

    /// <summary>Max final suggested rubrics on the fast path (never fabricate to fill).</summary>
    public int FastPipelineMaxFinalRubrics { get; set; } = 12;

    /// <summary>Engine version stamped on fast-path rubrics (must fit NVARCHAR(10) session fields).</summary>
    public string FastPipelineEngineVersion { get; set; } = "fast-f";

    /// <summary>Stage D: include embedding channel on fast path (bounded by timeout).</summary>
    public bool FastPipelineEnableEmbeddingSearch { get; set; } = true;

    /// <summary>Max seconds for embedding channel before continuing without it.</summary>
    public int FastPipelineEmbeddingTimeoutSeconds { get; set; } = 8;

    /// <summary>Stage F: MMR λ (relevance vs diversity). 0.75–0.85 recommended.</summary>
    public decimal FastPipelineMmrLambda { get; set; } = 0.80m;

    /// <summary>Stage F: minimum canonical score (0–1) to enter final MMR pool.</summary>
    public decimal FastPipelineMinCanonicalScore { get; set; } = 0.45m;

    /// <summary>Accuracy/Retrieval pack: expand symptom blocks into multi-query variants.</summary>
    public bool FastPipelineEnableMultiQueryBlocks { get; set; } = true;

    public int FastPipelineMaxExtraQueriesPerConcept { get; set; } = 4;

    /// <summary>Process-level token catalog lookup (from embedding cache snapshot).</summary>
    public bool FastPipelineEnableCatalogLookup { get; set; } = true;

    /// <summary>Cache query embedding vectors across sessions.</summary>
    public bool FastPipelineEnableQueryEmbeddingCache { get; set; } = true;

    /// <summary>Max candidates retained in the high-recall merge funnel before gates.</summary>
    public int FastPipelineCandidateFunnelSize { get; set; } = 80;

    /// <summary>Minimum evidence overlap for hallucination hard gate.</summary>
    public decimal FastPipelineMinEvidenceScore { get; set; } = 0.15m;

    /// <summary>Apply doctor learning boost on fast path (never overrides evidence hard gates).</summary>
    public bool FastPipelineEnableDoctorLearning { get; set; } = true;

    /// <summary>ECI v8.0: enterprise clinical intelligence engine (new pipeline). Off by default.</summary>
    public bool EnableEciV8Engine { get; set; }

    /// <summary>ECI v8.0: use the new EciDatabaseIntelligenceEngine for repertory retrieval (SQL authoritative).</summary>
    public bool EnableEciDatabaseIntelligenceEngine { get; set; }

    /// <summary>ECI v8.0: max end-to-end rubric retrieval time budget (seconds) after extraction.</summary>
    public int EciV8LatencyBudgetSeconds { get; set; } = 5;

    /// <summary>ECI v8.0: target minimum meaningful database rubrics when clinically justified.</summary>
    public int EciV8MinDatabaseRubrics { get; set; } = 10;

    /// <summary>ECI v8.0: maximum database rubrics to return (after ranking/dedup).</summary>
    public int EciV8MaxDatabaseRubrics { get; set; } = 20;

    /// <summary>ECI v8.0: maximum semantic variants generated per symptom.</summary>
    public int EciV8MaxSemanticVariantsPerSymptom { get; set; } = 100;

    /// <summary>ECI v8.0: ranking weights for the deterministic rubric ranker.</summary>
    public EciV8RankingWeights EciV8RankingWeights { get; set; } = new();

    public bool RequiresStrictValidation =>
        EnableEnterpriseClinicalValidation || EnableClinicalValidationV21;

    public bool IsV2Active =>
        EnableV2 && !RollbackToV1Only;

    public bool RequiresManualApproval =>
        IsV2Active && RequireManualApprovalForAllAiRubrics && !AllowAutoApplyHighConfidence;

    public bool IsV3Active =>
        EnableV3ConceptGraph && IsV2Active;
}

public sealed class EciV8RankingWeights
{
    /// <summary>0-100 contribution.</summary>
    public decimal ClinicalMatch { get; set; } = 30m;

    public decimal Evidence { get; set; } = 20m;

    public decimal Ontology { get; set; } = 15m;

    public decimal Embedding { get; set; } = 10m;

    public decimal Hierarchy { get; set; } = 10m;

    public decimal SqlExact { get; set; } = 5m;

    public decimal Historical { get; set; } = 5m;

    public decimal ExpertRules { get; set; } = 5m;

    /// <summary>Penalty range: -100 to 0.</summary>
    public decimal MaxPenalty { get; set; } = -100m;
}

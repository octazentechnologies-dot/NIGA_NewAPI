using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public class AudioCaseUploadResultModel
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class AudioCaseStatusModel
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ProgressStep { get; set; }

    /// <summary>Human-readable stage for UI (Transcribing / Extracting / Matching rubrics / …).</summary>
    public string? StageLabel { get; set; }

    public int Percent { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? ProcessingStartedAt { get; set; }

    public int? ElapsedSeconds { get; set; }

    public string? EngineVersion { get; set; }
}

public class AudioCaseMessageModel
{
    public string Role { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string? Timestamp { get; set; }
}

public class AudioCaseSummaryModel
{
    public string? ChiefComplaint { get; set; }

    public string? HistoryOfPresentIllness { get; set; }

    public List<string> Mentals { get; set; } = new();

    public List<string> Generals { get; set; } = new();

    public List<string> Modalities { get; set; } = new();

    public List<string> Particulars { get; set; } = new();

    public List<string> RedFlags { get; set; } = new();
}

public class AudioCaseSuggestedRubricModel
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public int? SectionId { get; set; }

    public decimal MatchScore { get; set; }

    public int SuggestedIntensityNo { get; set; }

    public string? MatchedFrom { get; set; }

    public int RemedyCountForSort { get; set; }

    public bool IsAiSuggested { get; set; }

    public string? MatchSource { get; set; }

    /// <summary>V2: calibrated confidence 0–1.</summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>V2: explainability text for doctor review.</summary>
    public string? WhySuggested { get; set; }

    /// <summary>v1 or v2 — set at match time.</summary>
    public string? EngineVersion { get; set; }

    /// <summary>When true, rubric must be explicitly approved before Repertorize.</summary>
    public bool RequiresManualApproval { get; set; }

    public decimal HomeopathicWeight { get; set; } = 1m;

    /// <summary>V2: Database | Alias | Metaphor | Embedding | Hybrid | Inference</summary>
    public string? MatchLayer { get; set; }

    /// <summary>Primary | Secondary | Confirmatory | Inference</summary>
    public string? RubricTier { get; set; }

    public bool RequiresDoctorReview { get; set; }

    public Guid? SourceConceptId { get; set; }

    public string? InferenceReason { get; set; }

    public RubricExplainabilityModel? Explainability { get; set; }

    /// <summary>V2 Phase 7: mapped repertory source codes (KENT, COMPLETE).</summary>
    public List<string> RepertorySources { get; set; } = new();

    /// <summary>V2 Phase 7: highest-priority mapped repertory source.</summary>
    public string? PrimaryRepertorySource { get; set; }

    /// <summary>V2.1: mandatory evidence chain for doctor review.</summary>
    public RubricEvidenceChainModel? EvidenceChain { get; set; }

    /// <summary>V2.1: composite quality score 0–100 after clinical validation.</summary>
    public decimal? QualityScore { get; set; }

    /// <summary>V2.1: Accepted | Rejected | Review</summary>
    public string? ValidationStatus { get; set; }

    public List<string> ValidationFlags { get; set; } = new();

    /// <summary>Human-readable rejection reason(s) from enterprise validation (Code: Message).</summary>
    public string? RejectionReason { get; set; }

    public bool? IsPrimarySymptomLinked { get; set; }

    public RubricEnterpriseValidationReport? EnterpriseValidation { get; set; }

    public RubricEnterpriseEvidenceChainModel? EnterpriseEvidenceChain { get; set; }

    /// <summary>RepertoryRubric | AiClinicalConcept</summary>
    public string? ResultKind { get; set; }

    public string? RepertoryPath { get; set; }

    public decimal? EnterpriseConfidenceScore { get; set; }

    public string? SelectionReason { get; set; }

    /// <summary>V6: full explainability chain for clinical reasoning engine.</summary>
    public V6RubricExplainabilityModel? V6Explainability { get; set; }

    /// <summary>V7: enterprise repertory intelligence explainability.</summary>
    public V7RubricExplainability? V7Explainability { get; set; }

    /// <summary>Task 6: 1-based unified rank across Database + AiSuggested.</summary>
    public int? Rank { get; set; }

    /// <summary>Task 6: Database | AiSuggested</summary>
    public string? Source { get; set; }

    /// <summary>Task 6: structured matched-from path (additive).</summary>
    public RubricMatchedFromModel? MatchedFromDetail { get; set; }

    /// <summary>Task 6: unified score breakdown (additive).</summary>
    public RubricUnifiedScoresModel? Scores { get; set; }

    /// <summary>Task 6/7: evidence chain completeness for doctor-facing contract.</summary>
    public bool? EvidenceChainComplete { get; set; }

    /// <summary>Task 6: light validation summary (additive).</summary>
    public RubricValidationSummaryModel? Validation { get; set; }

    /// <summary>Task 6: alias for RemedyCountForSort when serializing unified contract.</summary>
    public int? RemedyCount { get; set; }

    /// <summary>Task 3/6: metaphor/sensation grounded in AISensationOntology.</summary>
    public bool? GroundedInOntology { get; set; }

    /// <summary>Modality sub-variants grouped under this primary rubric (e.g. mutton-agg., mutton-amel.).</summary>
    public List<RubricModalityVariantModel>? ModalityVariants { get; set; }

    public int? ModalityVariantCount { get; set; }

    /// <summary>Fast path evidence contract: patient phrase supporting this rubric.</summary>
    public string? PatientEvidence { get; set; }

    /// <summary>Exact | Lexical | Alias | Semantic | Concept</summary>
    public string? EvidenceType { get; set; }

    /// <summary>Canonical 0–1 score after FastClinicalRanking.</summary>
    public decimal? CanonicalScore { get; set; }

    /// <summary>Discovery channel label (Keyword, Embedding, CatalogToken, …).</summary>
    public string? DiscoveryMethod { get; set; }

    /// <summary>True when SubSectionId maps to SubSectionMaster.</summary>
    public bool? IsDbBacked { get; set; }

    /// <summary>Lexical evidence overlap 0–1.</summary>
    public decimal? EvidenceScore { get; set; }

    /// <summary>Parsed hierarchy path (Section &gt; … &gt; Leaf).</summary>
    public string? HierarchyPath { get; set; }

    public int? HierarchyDepth { get; set; }
}

public class RubricModalityVariantModel
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public string? ModalityLabel { get; set; }

    public decimal? MatchScore { get; set; }

    public string? MatchSource { get; set; }
}

public class RubricMatchedFromModel
{
    public string? PatientStatement { get; set; }

    public string? NormalizedMeaning { get; set; }

    public string? MetaphorResolution { get; set; }

    public string? ClinicalConcept { get; set; }

    public string? HomeopathicConcept { get; set; }

    public string? SymptomClass { get; set; }

    public bool CausationLinked { get; set; }
}

public class RubricUnifiedScoresModel
{
    public decimal? ConceptMatchConfidence { get; set; }

    public decimal? EmbeddingCosine { get; set; }

    public decimal? AliasMatch { get; set; }

    public decimal? FinalHybridScore { get; set; }

    public decimal? CalibratedAcceptanceProbability { get; set; }
}

public class RubricValidationSummaryModel
{
    public string? Domain { get; set; }

    public string? Gender { get; set; }

    public bool? Hallucination { get; set; }
}

public class AudioCaseAiSuggestedRubricItemModel
{
    public string RubricName { get; set; } = string.Empty;

    public string? SectionHint { get; set; }

    public string? MatchedFrom { get; set; }

    public int SuggestedIntensityNo { get; set; } = 2;

    public string? Reason { get; set; }
}

public class AudioCaseAiSuggestedRubricsModel
{
    public List<AudioCaseAiSuggestedRubricItemModel> Rubrics { get; set; } = new();
}

public class AudioCaseResultModel
{
    public Guid SessionId { get; set; }

    public string? Transcript { get; set; }

    public List<AudioCaseMessageModel> Messages { get; set; } = new();

    public AudioCaseSummaryModel? Summary { get; set; }

    public List<AudioCaseSuggestedRubricModel> SuggestedRubrics { get; set; } = new();

    public RubricIntelligenceMetaModel? RubricIntelligence { get; set; }

    /// <summary>Stage A baseline metrics (optional; filled when PipelineBaselineSummary log exists).</summary>
    public RubricPipelineTelemetrySummary? ProcessingMetrics { get; set; }
}

public class AudioCaseSymptomModel
{
    public string Phrase { get; set; } = string.Empty;

    public List<string> SearchTerms { get; set; } = new();

    public string? Category { get; set; }

    public int IntensityHint { get; set; } = 2;

    /// <summary>Task 4: sensation or emotion-bearing — triggers scoped dual-language transcription when flagged.</summary>
    public bool IsSensationBearing { get; set; }

    /// <summary>Task 4: original-language fragment when DualLanguageForSensationSegments is on.</summary>
    public string? OriginalLanguageText { get; set; }

    public string? LanguageCode { get; set; }
}

public class AudioCaseExtractionModel
{
    /// <summary>Populated from Whisper translation/transcription — not returned by GPT extraction.</summary>
    public string? EnglishTranscript { get; set; }

    public List<AudioCaseMessageModel> Conversation { get; set; } = new();

    public List<AudioCaseSymptomModel> Symptoms { get; set; } = new();

    public AudioCaseSummaryModel Summary { get; set; } = new();

    public string? DetectedLanguage { get; set; }
}

public class AudioCaseTakingJob
{
    public Guid SessionId { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>ProcessAudio | ReAnalyze</summary>
    public string JobType { get; set; } = "ProcessAudio";

    public string? EditedTranscript { get; set; }
}

public class AudioCaseReAnalyzeRequestModel
{
    [Required]
    public string Transcript { get; set; } = string.Empty;
}

public class AudioCaseDoctorActionRequestModel
{
    [Required]
    public string ActionType { get; set; } = string.Empty;

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? Notes { get; set; }
}

public class AudioCaseLatestSessionModel
{
    public Guid? SessionId { get; set; }

    public string? Status { get; set; }

    public string? ProgressStep { get; set; }

    public string? AudioFileName { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>True when the session is too old to resume (orphaned upload or stuck processing).</summary>
    public bool IsStale { get; set; }

    /// <summary>True when the doctor can resume viewing or tracking this session.</summary>
    public bool CanResume { get; set; }

    public AudioCaseResultModel? Result { get; set; }
}

public class AudioCaseSessionListItemModel
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ProgressStep { get; set; }

    public string? StageLabel { get; set; }

    public string AudioSourceType { get; set; } = string.Empty;

    public string? AudioFileName { get; set; }

    public int? AudioDurationSeconds { get; set; }

    public DateTime EnteredDate { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public bool HasAudioFile { get; set; }

    public bool IsStale { get; set; }

    public bool CanOpen { get; set; }

    public string? ChiefComplaintSnippet { get; set; }

    public int MessageCount { get; set; }

    public int RubricCount { get; set; }
}

public class AudioCaseSessionListModel
{
    public List<AudioCaseSessionListItemModel> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int CompletedCount { get; set; }

    public int ProcessingCount { get; set; }

    public int FailedCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public bool HasMore { get; set; }
}

public class AudioCaseUploadRequestModel
{
    [Required]
    public IFormFile? AudioFile { get; set; }

    [Required]
    public long PatientId { get; set; }

    public long? CaseId { get; set; }

    public long? PatientAppId { get; set; }

    public long? DoctorUserId { get; set; }

    [Required]
    public string AudioSource { get; set; } = "LiveRecording";

    public string? OriginalFileName { get; set; }

    public string? Language { get; set; }

    public bool ConsentGiven { get; set; } = true;
}

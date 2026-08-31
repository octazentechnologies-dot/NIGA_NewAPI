using System;

namespace Niga_Domain.Master;

public partial class AudioCaseSession
{
    public Guid AudioCaseSessionId { get; set; }

    public long PatientId { get; set; }

    public long? CaseId { get; set; }

    public long DoctorUserId { get; set; }

    public long? PatientAppId { get; set; }

    public string AudioSourceType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? CurrentStep { get; set; }

    public string? AudioFilePath { get; set; }

    public string? AudioFileName { get; set; }

    public string? AudioMimeType { get; set; }

    public long? AudioFileSizeBytes { get; set; }

    public string? AudioSha256Hash { get; set; }

    public int? AudioDurationSeconds { get; set; }

    public string? TranscriptRaw { get; set; }

    public string? ConversationJson { get; set; }

    public string? SummaryJson { get; set; }

    public string? ExtractedSymptomsJson { get; set; }

    public string? SuggestedRubricsJson { get; set; }

    public string? ClinicalConceptsJson { get; set; }

    public string? CausationLinksJson { get; set; }

    public string? IntelligenceEngineVersion { get; set; }

    public string? ConceptGraphEngineVersion { get; set; }

    public decimal? TranscriptCoverageScore { get; set; }

    public decimal? CaseCompletenessScore { get; set; }

    public string? RecallEngineVersion { get; set; }

    public string? DetectedLanguage { get; set; }

    public string? LanguageOverride { get; set; }

    public string? CorrelationId { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public int ReAnalysisCount { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? AudioPurgedAtUtc { get; set; }

    public bool DeleteStatus { get; set; }
}

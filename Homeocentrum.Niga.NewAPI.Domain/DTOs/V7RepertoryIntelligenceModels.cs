namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public static class V7SearchStrategyNames
{
    public const string ExactMatch = "ExactMatch";
    public const string TokenMatch = "TokenMatch";
    public const string ParentSearch = "ParentSearch";
    public const string ChildSearch = "ChildSearch";
    public const string SynonymSearch = "SynonymSearch";
    public const string OntologySearch = "OntologySearch";
    public const string EmbeddingSearch = "EmbeddingSearch";
    public const string BootstrapMapping = "BootstrapMapping";
    public const string CrossReference = "CrossReference";
    public const string Hybrid = "Hybrid";
}

public class V7ExtractedSymptom
{
    public string Text { get; set; } = string.Empty;

    public string Normalized { get; set; } = string.Empty;

    public string Category { get; set; } = "Clinical";

    public string? Timing { get; set; }

    public string? Location { get; set; }

    public string? Modality { get; set; }

    public string? Etiology { get; set; }

    public decimal Confidence { get; set; }

    public long? SourceConceptId { get; set; }

    public string? TranscriptEvidence { get; set; }
}

public class V7GptSymptomExtractionResult
{
    public List<V7ExtractedSymptom> Symptoms { get; set; } = new();
}

public class V7NormalizedConcept
{
    public string OriginalText { get; set; } = string.Empty;

    public List<string> NormalizationChain { get; set; } = new();

    public string FinalConcept { get; set; } = string.Empty;
}

public class V7VocabularyTerm
{
    public string Term { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public decimal Weight { get; set; } = 1m;
}

public class V7OntologyNode
{
    public string Concept { get; set; } = string.Empty;

    public string RelationType { get; set; } = string.Empty;

    public string RelatedConcept { get; set; } = string.Empty;
}

public class V7RubricCandidate
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public string SearchStrategy { get; set; } = string.Empty;

    public string MatchPath { get; set; } = string.Empty;

    public decimal RawScore { get; set; }

    public decimal NormalizedScore { get; set; }

    public decimal EmbeddingSimilarity { get; set; }

    public string? MatchedTerm { get; set; }

    public long? SourceSymptomConceptId { get; set; }

    public string? SourceSymptomText { get; set; }
}

public class V7SymptomSearchAudit
{
    public string SymptomText { get; set; } = string.Empty;

    public List<string> NormalizationChain { get; set; } = new();

    public List<string> VocabularyTerms { get; set; } = new();

    public List<string> SynonymTerms { get; set; } = new();

    public List<string> OntologyTerms { get; set; } = new();

    public List<V7RubricCandidate> Candidates { get; set; } = new();

    public List<string> SearchStrategiesUsed { get; set; } = new();
}

public class V7RubricExplainability
{
    public string? OriginalTranscript { get; set; }

    public string? ExtractedSymptom { get; set; }

    public string? NormalizedSymptom { get; set; }

    public List<string> VocabularyExpansion { get; set; } = new();

    public List<string> SynonymExpansion { get; set; } = new();

    public List<string> OntologyPath { get; set; } = new();

    public string? SearchStrategy { get; set; }

    public string? MatchedRubric { get; set; }

    public decimal Confidence { get; set; }

    public string? FinalExplanation { get; set; }
}

public class V7RepertoryIntelligenceRequest
{
    public Guid SessionId { get; set; }

    public string Transcript { get; set; } = string.Empty;

    public ConceptGraphFullModel Graph { get; set; } = new();

    public IReadOnlyList<RubricDiscoveryNodeModel> GlobalDiscoveries { get; set; } = Array.Empty<RubricDiscoveryNodeModel>();

    public IReadOnlyList<RubricCandidateModel> EmbeddingCandidates { get; set; } = Array.Empty<RubricCandidateModel>();

    public IReadOnlyList<KgRubricPathModel> KnowledgeGraphPaths { get; set; } = Array.Empty<KgRubricPathModel>();
}

public class V7RepertoryIntelligenceResult
{
    public bool Success { get; set; }

    public string EngineVersion { get; set; } = "v7.0";

    public List<V7ExtractedSymptom> ExtractedSymptoms { get; set; } = new();

    public List<RubricDiscoveryNodeModel> Discoveries { get; set; } = new();

    public List<V7SymptomSearchAudit> AuditTrail { get; set; } = new();

    public List<string> StagesCompleted { get; set; } = new();

    public string? Error { get; set; }
}

public class V7BenchmarkAccuracyReport
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public int CasesEvaluated { get; set; }

    public decimal Precision { get; set; }

    public decimal Recall { get; set; }

    public decimal F1Score { get; set; }

    public decimal Top5Agreement { get; set; }

    public decimal Top10Agreement { get; set; }

    public decimal AverageLatencyMs { get; set; }

    public decimal WorstCaseLatencyMs { get; set; }

    public List<string> IdentifiedGaps { get; set; } = new();

    public List<V7BenchmarkCaseAccuracy> CaseResults { get; set; } = new();
}

public class V7BenchmarkCaseAccuracy
{
    public string CaseId { get; set; } = string.Empty;

    public decimal Recall { get; set; }

    public decimal Precision { get; set; }

    public decimal F1Score { get; set; }

    public decimal Top5Agreement { get; set; }

    public decimal Top10Agreement { get; set; }

    public List<string> MatchedPatterns { get; set; } = new();

    public List<string> MissedPatterns { get; set; } = new();

    public List<string> DiagnosticNotes { get; set; } = new();
}

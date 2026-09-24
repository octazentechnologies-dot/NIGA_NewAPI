namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

/// <summary>Authoritative source priority for rubric candidates (Phase 8).</summary>
public static class RubricDiscoverySources
{
    public const string RepertoryDb = "REPERTORY_DB";
    public const string Bootstrap = "BOOTSTRAP";
    public const string KnowledgeGraph = "KNOWLEDGE_GRAPH";
    public const string Embedding = "EMBEDDING";
    public const string EnterpriseEmbedding = "ENTERPRISE_EMBEDDING";
    public const string DoctorLearning = "DOCTOR_LEARNING";
    public const string RuleExpansion = "RULE_EXPANSION";
    public const string AiInference = "AI_INFERENCE";
}

public class EnterpriseRubricDiscoveryCandidate
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public long? HomeopathicConceptId { get; set; }

    public string SourceConceptName { get; set; } = string.Empty;

    public string DiscoverySource { get; set; } = RubricDiscoverySources.Bootstrap;

    public string DiscoveryMethod { get; set; } = string.Empty;

    public string? MatchReason { get; set; }

    public decimal SimilarityScore { get; set; }

    public decimal ClinicalRelevanceScore { get; set; }

    public decimal EvidenceScore { get; set; }

    public decimal DoctorAcceptanceScore { get; set; }

    public decimal CompositeScore { get; set; }

    public decimal QualityScore { get; set; }

    public int SourcePriority { get; set; }

    public RubricEvidenceChainV3Model? EvidenceChain { get; set; }

    public string? MatchedConceptKey { get; set; }

    public float? EmbeddingDistance { get; set; }
}

public class EnterpriseRubricDiscoveryResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<EnterpriseRubricDiscoveryCandidate> Candidates { get; set; } = new();

    public List<RubricDiscoveryNodeModel> Discoveries { get; set; } = new();

    public List<RubricCandidateModel> RubricCandidates { get; set; } = new();

    public RubricCandidateEngineResult? EmbeddingEngineResult { get; set; }

    public KgDiscoveryResult? KnowledgeGraphResult { get; set; }

    public PipelineDiagnosticReportModel Diagnostics { get; set; } = new();
}

public class EnterpriseRubricDiscoveryRequest
{
    public Guid SessionId { get; set; }

    public ConceptGraphFullModel Graph { get; set; } = new();

    public string Transcript { get; set; } = string.Empty;

    public int MaxCandidates { get; set; } = 100;

    public bool IncludeKnowledgeGraph { get; set; } = true;

    public bool IncludeEnterpriseEmbeddings { get; set; } = true;

    public bool IncludeLegacyDiscovery { get; set; } = true;

    public bool IncludeRuleExpansion { get; set; } = true;
}

public class PipelineDiagnosticReportModel
{
    public Guid SessionId { get; set; }

    public string EngineVersion { get; set; } = "v4.0";

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public List<PipelineStageDiagnosticModel> Stages { get; set; } = new();

    public int TotalCandidates { get; set; }

    public Dictionary<string, int> CandidatesBySource { get; set; } = new();

    public string? FailureStage { get; set; }

    public string? FailureReason { get; set; }
}

public class PipelineStageDiagnosticModel
{
    public string StageName { get; set; } = string.Empty;

    public string Status { get; set; } = "PASS";

    public int OutputCount { get; set; }

    public int DurationMs { get; set; }

    public string? Detail { get; set; }

    public string? Error { get; set; }
}

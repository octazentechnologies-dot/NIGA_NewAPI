namespace Niga_Domain.DTOs;

public static class HybridCompletionStageNames
{
    public const string ExactRepertory = "ExactRepertory";
    public const string SynonymSearch = "SynonymSearch";
    public const string BootstrapMapping = "BootstrapMapping";
    public const string KnowledgeGraph = "KnowledgeGraph";
    public const string EmbeddingSimilarity = "EmbeddingSimilarity";
    public const string ClinicalValidation = "ClinicalValidation";
}

public static class HybridCompletionOutputCategories
{
    public const string RepertoryDatabase = "RepertoryDatabase";
    public const string AiClinicalConcept = "AiClinicalConcept";
}

public class HybridCompletionRequest
{
    public Guid SessionId { get; set; }

    public ConceptGraphFullModel Graph { get; set; } = new();

    public string Transcript { get; set; } = string.Empty;

    public IReadOnlyList<RubricDiscoveryNodeModel> GlobalDiscoveries { get; set; } = Array.Empty<RubricDiscoveryNodeModel>();

    public IReadOnlyList<RubricCandidateModel> EmbeddingCandidates { get; set; } = Array.Empty<RubricCandidateModel>();

    public IReadOnlyList<KgRubricPathModel> KnowledgeGraphPaths { get; set; } = Array.Empty<KgRubricPathModel>();
}

public class HybridCompletionStageResult
{
    public string StageName { get; set; } = string.Empty;

    public int MatchCount { get; set; }

    public List<int> SubSectionIds { get; set; } = new();
}

public class HybridConceptAuditEntry
{
    public long? HomeopathicConceptId { get; set; }

    public string ConceptName { get; set; } = string.Empty;

    public decimal ConceptConfidence { get; set; }

    public List<HybridCompletionStageResult> Stages { get; set; } = new();

    public int DatabaseMatchCount { get; set; }

    public int KnowledgeGraphMatchCount { get; set; }

    public int EmbeddingMatchCount { get; set; }

    public int SlotsAllocated { get; set; }

    public string? ValidationSummary { get; set; }

    public string OutputCategory { get; set; } = HybridCompletionOutputCategories.RepertoryDatabase;

    /// <summary>Wall-clock ms for this concept's SearchAsync (Gap 2 instrumentation).</summary>
    public int LatencyMs { get; set; }
}

public class HybridCompletionDiscoveryResult
{
    public List<RubricDiscoveryNodeModel> Discoveries { get; set; } = new();

    public List<HybridConceptAuditEntry> AuditEntries { get; set; } = new();

    public int ConceptCount { get; set; }

    public int TotalSlotsAllocated { get; set; }
}

public class HybridCompletionOutput
{
    public List<AudioCaseSuggestedRubricModel> RepertoryRubrics { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> AiClinicalConcepts { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> AllRubrics { get; set; } = new();

    public List<HybridConceptAuditEntry> AuditEntries { get; set; } = new();
}

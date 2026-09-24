namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public class RepertoryRubricCatalogItem
{
    public int RubricId { get; set; }

    public string RubricName { get; set; } = string.Empty;

    public string? RubricAlias { get; set; }

    public string? Description { get; set; }

    public int? SectionId { get; set; }

    public string SectionName { get; set; } = string.Empty;

    public string? SectionAlias { get; set; }

    public string? SectionDescription { get; set; }

    public List<string> KnownSynonyms { get; set; } = new();

    public List<string> ClinicalConcepts { get; set; } = new();

    public List<string> HomeopathicConcepts { get; set; } = new();

    public List<string> Meanings { get; set; } = new();

    public List<string> SymptomExamples { get; set; } = new();
}

public class RubricSemanticDocument
{
    public int RubricId { get; set; }

    public string Section { get; set; } = string.Empty;

    public string Subsection { get; set; } = string.Empty;

    public string Rubric { get; set; } = string.Empty;

    public List<string> ClinicalConcepts { get; set; } = new();

    public List<string> HomeopathicConcepts { get; set; } = new();

    public List<string> KnownSynonyms { get; set; } = new();

    public List<string> Meanings { get; set; } = new();

    public List<string> SymptomExamples { get; set; } = new();

    public string SourceText { get; set; } = string.Empty;

    public string TextHash { get; set; } = string.Empty;

    public int SemanticFieldCount { get; set; }
}

public class BuildEnterpriseRubricEmbeddingsRequest
{
    public Guid? EmbeddingVersionId { get; set; }

    public int? MaxRubrics { get; set; }

    public bool FullReindex { get; set; }

    public int? ActorUserId { get; set; }

    public string? TriggerSource { get; set; }
}

public class EnterpriseRubricEmbeddingBuildResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public Guid? JobId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string VersionCode { get; set; } = string.Empty;

    public int TotalCatalogued { get; set; }

    public int Processed { get; set; }

    public int Created { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }
}

public enum AiRubricEmbeddingUpsertOutcome
{
    Skipped,
    Created,
    Updated,
}

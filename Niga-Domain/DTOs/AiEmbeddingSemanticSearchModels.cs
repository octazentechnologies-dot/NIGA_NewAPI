namespace Niga_Domain.DTOs;

public static class AiConceptEmbeddingTypes
{
    public const string Clinical = "Clinical";
    public const string Homeopathic = "Homeopathic";
}

public class AiConceptEmbeddingCacheEntry
{
    public long ConceptEmbeddingId { get; set; }

    public string ConceptKey { get; set; } = string.Empty;

    public string ConceptType { get; set; } = string.Empty;

    public string SourceText { get; set; } = string.Empty;

    public float[] Vector { get; set; } = Array.Empty<float>();

    public List<int> LinkedRubricIds { get; set; } = new();
}

public class AiEnterpriseRubricEmbeddingCacheEntry
{
    public int RubricId { get; set; }

    public string SourceText { get; set; } = string.Empty;

    public float[] Vector { get; set; } = Array.Empty<float>();
}

public class EnterpriseSemanticSearchRequest
{
    public string ClinicalConcept { get; set; } = string.Empty;

    public Guid? EmbeddingVersionId { get; set; }

    public int? TopConcepts { get; set; }

    public int? TopRubricsPerConcept { get; set; }

    public bool IncludeRubricMapping { get; set; } = true;

    public bool IncludeValidation { get; set; } = true;
}

public class EnterpriseSemanticSearchResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string VersionCode { get; set; } = string.Empty;

    public string QueryClinicalConcept { get; set; } = string.Empty;

    public string QueryText { get; set; } = string.Empty;

    public List<EnterpriseSemanticConceptMatchModel> Concepts { get; set; } = new();

    public List<EnterpriseSemanticRubricMatchModel> Rubrics { get; set; } = new();
}

public class EnterpriseSemanticConceptMatchModel
{
    public int Rank { get; set; }

    public string ConceptKey { get; set; } = string.Empty;

    public string ConceptType { get; set; } = string.Empty;

    public decimal SimilarityScore { get; set; }

    public float RawCosine { get; set; }

    public List<string> MatchedSynonyms { get; set; } = new();

    public string? MatchedClinicalMeaning { get; set; }

    public string? MatchedHomeopathicMeaning { get; set; }

    public List<int> LinkedRubricIds { get; set; } = new();
}

public class EnterpriseSemanticRubricMatchModel
{
    public int Rank { get; set; }

    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public string MappedFromConceptKey { get; set; } = string.Empty;

    public decimal ConceptSimilarityScore { get; set; }

    public decimal RubricSimilarityScore { get; set; }

    public decimal CombinedScore { get; set; }

    public string MatchMethod { get; set; } = string.Empty;

    public EnterpriseSemanticRubricValidationModel? Validation { get; set; }
}

public class EnterpriseSemanticRubricValidationModel
{
    public bool IsValid { get; set; }

    public decimal ValidationScore { get; set; }

    public List<string> Issues { get; set; } = new();

    public List<string> PassedChecks { get; set; } = new();
}

public class ParsedConceptSemanticDocument
{
    public string? ClinicalConcept { get; set; }

    public string? HomeopathicConcept { get; set; }

    public List<string> KnownSynonyms { get; set; } = new();

    public List<string> Meanings { get; set; } = new();

    public List<int> LinkedRubricIds { get; set; } = new();
}

public class BuildEnterpriseConceptEmbeddingsRequest
{
    public Guid? EmbeddingVersionId { get; set; }

    public int? MaxConcepts { get; set; }

    public int? ActorUserId { get; set; }

    public string? TriggerSource { get; set; }
}

public class EnterpriseConceptEmbeddingBuildResult
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

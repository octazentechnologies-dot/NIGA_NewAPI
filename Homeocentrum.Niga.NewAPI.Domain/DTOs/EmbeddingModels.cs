namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public class RubricEmbeddingCacheEntry
{
    public int RubricId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public float[] Vector { get; set; } = Array.Empty<float>();

    public string ModelName { get; set; } = string.Empty;
}

public class EmbeddingSearchCandidate
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public decimal CosineScore { get; set; }

    public string? MatchedConceptText { get; set; }
}

public class EmbeddingSearchResult
{
    public List<EmbeddingSearchCandidate> Candidates { get; set; } = new();

    public bool UsedFallback { get; set; }

    public string? Error { get; set; }
}

public class HybridRetrievalResult
{
    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public int EmbeddingCandidates { get; set; }

    public int AliasCandidates { get; set; }
}

public class EmbeddingReindexResult
{
    public int Processed { get; set; }

    public int Created { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    public bool CacheRefreshed { get; set; }

    public string? Error { get; set; }
}

public class EmbeddingIndexStatusModel
{
    public int IndexedRubrics { get; set; }

    public int CachedVectors { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public DateTime? LastIndexedUtc { get; set; }

    public bool EnableEmbeddingSearch { get; set; }
}

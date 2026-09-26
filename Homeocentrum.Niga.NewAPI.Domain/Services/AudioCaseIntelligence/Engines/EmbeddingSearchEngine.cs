using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

public class EmbeddingSearchEngine : IEmbeddingSearchEngine
{
    private readonly RubricIntelligenceOptions _options;
    private readonly OpenAiOptions _openAiOptions;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IRubricEmbeddingMemoryCache _cache;
    private readonly IFastClinicalQueryEmbeddingCache _queryEmbeddingCache;
    private readonly ILogger<EmbeddingSearchEngine> _logger;

    public EmbeddingSearchEngine(
        IOptions<RubricIntelligenceOptions> options,
        IOptions<OpenAiOptions> openAiOptions,
        IEmbeddingClient embeddingClient,
        IRubricEmbeddingMemoryCache cache,
        IFastClinicalQueryEmbeddingCache queryEmbeddingCache,
        ILogger<EmbeddingSearchEngine> logger)
    {
        _options = options.Value;
        _openAiOptions = openAiOptions.Value;
        _embeddingClient = embeddingClient;
        _cache = cache;
        _queryEmbeddingCache = queryEmbeddingCache;
        _logger = logger;
    }

    public async Task<EmbeddingSearchResult> SearchAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableEmbeddingSearch || concepts.Count == 0)
        {
            return new EmbeddingSearchResult();
        }

        if (_cache.Entries.Count == 0)
        {
            await _cache.RefreshAsync(cancellationToken);
        }

        var cacheEntries = _cache.Entries;
        if (cacheEntries.Count == 0)
        {
            return new EmbeddingSearchResult { Error = "No indexed rubric embeddings available." };
        }

        var maxQueries = Math.Clamp(_options.MaxEmbeddingConceptsPerPass, 1, 24);
        var queries = concepts
            .Select(BuildQueryText)
            .Where(x => x.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxQueries)
            .ToList();

        if (queries.Count == 0)
        {
            return new EmbeddingSearchResult();
        }

        var modelName = string.IsNullOrWhiteSpace(_openAiOptions.EmbeddingModel)
            ? "default"
            : _openAiOptions.EmbeddingModel;

        IReadOnlyList<float[]?> vectors;
        if (_options.FastPipelineEnableQueryEmbeddingCache)
        {
            vectors = await _queryEmbeddingCache.GetOrEmbedAsync(
                queries,
                modelName,
                async (misses, ct) => await _embeddingClient.EmbedTextsAsync(misses, modelName, ct),
                cancellationToken);
        }
        else
        {
            var embedResult = await _embeddingClient.EmbedTextsAsync(queries, modelName, cancellationToken);
            if (!embedResult.Success || embedResult.Vectors.Count == 0)
            {
                _logger.LogWarning("Embedding search falling back to Jaccard: {Error}", embedResult.Error);
                return FallbackJaccardSearch(queries, concepts, cacheEntries);
            }

            vectors = embedResult.Vectors.Cast<float[]?>().ToList();
        }

        if (vectors.All(v => v == null || v.Length == 0))
        {
            _logger.LogWarning("Embedding search falling back to Jaccard: no vectors.");
            return FallbackJaccardSearch(queries, concepts, cacheEntries);
        }

        var candidates = new Dictionary<int, EmbeddingSearchCandidate>();
        for (var i = 0; i < queries.Count && i < vectors.Count; i++)
        {
            var vector = vectors[i];
            if (vector == null || vector.Length == 0)
                continue;

            var query = queries[i];
            foreach (var entry in TopCosineMatches(vector, cacheEntries, _options.EmbeddingTopK))
            {
                if (entry.Score < _options.MinEmbeddingCosineForCandidate) continue;
                if (candidates.TryGetValue(entry.RubricId, out var existing))
                {
                    if (entry.Score > existing.CosineScore)
                    {
                        candidates[entry.RubricId] = new EmbeddingSearchCandidate
                        {
                            SubSectionId = entry.RubricId,
                            SubSectionName = entry.SubSectionName,
                            CosineScore = entry.Score,
                            MatchedConceptText = query,
                        };
                    }
                }
                else
                {
                    candidates[entry.RubricId] = new EmbeddingSearchCandidate
                    {
                        SubSectionId = entry.RubricId,
                        SubSectionName = entry.SubSectionName,
                        CosineScore = entry.Score,
                        MatchedConceptText = query,
                    };
                }
            }
        }

        return new EmbeddingSearchResult
        {
            Candidates = candidates.Values
                .OrderByDescending(x => x.CosineScore)
                .Take(_options.EmbeddingTopK)
                .ToList(),
        };
    }

    private static EmbeddingSearchResult FallbackJaccardSearch(
        IReadOnlyList<string> queries,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<RubricEmbeddingCacheEntry> cacheEntries)
    {
        var candidates = new Dictionary<int, EmbeddingSearchCandidate>();

        foreach (var concept in concepts)
        {
            var conceptText = BuildQueryText(concept);
            foreach (var entry in cacheEntries)
            {
                var score = Math.Max(
                    AudioCaseAiProcessor.ComputeTextSimilarity(conceptText, entry.SubSectionName),
                    queries.Max(q => AudioCaseAiProcessor.ComputeTextSimilarity(q, entry.SubSectionName)));

                if (score < 0.25m) continue;

                if (!candidates.TryGetValue(entry.RubricId, out var existing) || score > existing.CosineScore)
                {
                    candidates[entry.RubricId] = new EmbeddingSearchCandidate
                    {
                        SubSectionId = entry.RubricId,
                        SubSectionName = entry.SubSectionName,
                        CosineScore = score,
                        MatchedConceptText = conceptText,
                    };
                }
            }
        }

        return new EmbeddingSearchResult
        {
            Candidates = candidates.Values.OrderByDescending(x => x.CosineScore).Take(50).ToList(),
            UsedFallback = true,
            Error = "Embedding API unavailable; used Jaccard fallback.",
        };
    }

    private static IEnumerable<(int RubricId, string SubSectionName, decimal Score)> TopCosineMatches(
        IReadOnlyList<float> queryVector,
        IReadOnlyList<RubricEmbeddingCacheEntry> entries,
        int topK)
    {
        return entries
            .Select(entry => (
                entry.RubricId,
                entry.SubSectionName,
                EmbeddingVectorMath.ToScore(EmbeddingVectorMath.CosineSimilarity(queryVector, entry.Vector))))
            .OrderByDescending(x => x.Item3)
            .Take(topK);
    }

    private static string BuildQueryText(ClinicalConceptModel concept) =>
        (concept.HomeopathicMeaning ?? concept.ClinicalMeaning ?? concept.RawStatement).Trim();
}

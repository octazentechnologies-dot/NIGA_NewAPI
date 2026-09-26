using System.Collections.Concurrent;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;

/// <summary>
/// Phase 12: process-level cache for query embedding vectors (keyed by normalized text + model).
/// Avoids re-embedding identical symptom queries across sessions.
/// </summary>
public interface IFastClinicalQueryEmbeddingCache
{
    bool TryGet(string text, string modelName, out float[] vector);

    void Set(string text, string modelName, float[] vector);

    /// <summary>
    /// Resolve vectors for texts, calling embedMissing for cache misses only.
    /// Returns vectors aligned to input texts (null slots if embed failed for that item).
    /// </summary>
    Task<IReadOnlyList<float[]?>> GetOrEmbedAsync(
        IReadOnlyList<string> texts,
        string modelName,
        Func<IReadOnlyList<string>, CancellationToken, Task<EmbeddingClientResult>> embedMissing,
        CancellationToken cancellationToken = default);

    int Count { get; }
}

public sealed class FastClinicalQueryEmbeddingCache : IFastClinicalQueryEmbeddingCache
{
    private readonly ConcurrentDictionary<string, float[]> _cache = new(StringComparer.Ordinal);
    private const int MaxEntries = 4096;

    public int Count => _cache.Count;

    public bool TryGet(string text, string modelName, out float[] vector)
    {
        return _cache.TryGetValue(BuildKey(text, modelName), out vector!);
    }

    public void Set(string text, string modelName, float[] vector)
    {
        if (string.IsNullOrWhiteSpace(text) || vector.Length == 0)
            return;

        if (_cache.Count >= MaxEntries)
        {
            // Simple eviction: drop ~10% arbitrary keys
            foreach (var key in _cache.Keys.Take(MaxEntries / 10))
                _cache.TryRemove(key, out _);
        }

        _cache[BuildKey(text, modelName)] = vector;
    }

    public async Task<IReadOnlyList<float[]?>> GetOrEmbedAsync(
        IReadOnlyList<string> texts,
        string modelName,
        Func<IReadOnlyList<string>, CancellationToken, Task<EmbeddingClientResult>> embedMissing,
        CancellationToken cancellationToken = default)
    {
        var results = new float[]?[texts.Count];
        var missIndexes = new List<int>();
        var missTexts = new List<string>();

        for (var i = 0; i < texts.Count; i++)
        {
            if (TryGet(texts[i], modelName, out var cached))
            {
                results[i] = cached;
            }
            else
            {
                missIndexes.Add(i);
                missTexts.Add(texts[i]);
            }
        }

        if (missTexts.Count == 0)
            return results;

        var embedResult = await embedMissing(missTexts, cancellationToken);
        if (!embedResult.Success || embedResult.Vectors.Count == 0)
            return results;

        for (var j = 0; j < missIndexes.Count && j < embedResult.Vectors.Count; j++)
        {
            var idx = missIndexes[j];
            var vec = embedResult.Vectors[j];
            results[idx] = vec;
            Set(texts[idx], modelName, vec);
        }

        return results;
    }

    private static string BuildKey(string text, string modelName) =>
        $"{modelName ?? "default"}::{(text ?? string.Empty).Trim().ToLowerInvariant()}";
}

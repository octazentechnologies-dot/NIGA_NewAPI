using System.Collections.Concurrent;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public static class EnterpriseEmbeddingVectorSearch
{
    public static List<(AiConceptEmbeddingCacheEntry Entry, decimal Score, float RawCosine)> TopConceptMatches(
        IReadOnlyList<float> queryVector,
        IReadOnlyList<AiConceptEmbeddingCacheEntry> entries,
        int topK,
        decimal minScore) =>
        TopMatches(
            queryVector,
            entries,
            static entry => entry.Vector,
            topK,
            minScore);

    public static List<(AiEnterpriseRubricEmbeddingCacheEntry Entry, decimal Score, float RawCosine)> TopRubricMatches(
        IReadOnlyList<float> queryVector,
        IReadOnlyList<AiEnterpriseRubricEmbeddingCacheEntry> entries,
        int topK,
        decimal minScore) =>
        TopMatches(
            queryVector,
            entries,
            static entry => entry.Vector,
            topK,
            minScore);

    private static List<(TEntry Entry, decimal Score, float RawCosine)> TopMatches<TEntry>(
        IReadOnlyList<float> queryVector,
        IReadOnlyList<TEntry> entries,
        Func<TEntry, float[]> vectorSelector,
        int topK,
        decimal minScore)
    {
        if (entries.Count == 0 || topK <= 0)
        {
            return new List<(TEntry Entry, decimal Score, float RawCosine)>();
        }

        var threadBests = new ConcurrentBag<List<(TEntry Entry, decimal Score, float RawCosine)>>();
        var chunkSize = Math.Max(512, entries.Count / Math.Max(1, Environment.ProcessorCount * 4));

        Parallel.ForEach(
            Partitioner.Create(0, entries.Count, chunkSize),
            range =>
            {
                var local = new List<(TEntry Entry, decimal Score, float RawCosine)>(topK);
                for (var i = range.Item1; i < range.Item2; i++)
                {
                    var entry = entries[i];
                    var cosine = EmbeddingVectorMath.CosineSimilarity(queryVector, vectorSelector(entry));
                    var score = EmbeddingVectorMath.ToScore(cosine);
                    if (score < minScore)
                    {
                        continue;
                    }

                    InsertLocalTopK(local, (entry, score, cosine), topK);
                }

                if (local.Count > 0)
                {
                    threadBests.Add(local);
                }
            });

        return threadBests
            .SelectMany(x => x)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    private static void InsertLocalTopK<TEntry>(
        List<(TEntry Entry, decimal Score, float RawCosine)> local,
        (TEntry Entry, decimal Score, float RawCosine) candidate,
        int topK)
    {
        if (local.Count < topK)
        {
            local.Add(candidate);
            if (local.Count == topK)
            {
                local.Sort((a, b) => b.Score.CompareTo(a.Score));
            }

            return;
        }

        if (candidate.Score <= local[^1].Score)
        {
            return;
        }

        local[^1] = candidate;
        for (var i = local.Count - 2; i >= 0; i--)
        {
            if (local[i].Score >= local[i + 1].Score)
            {
                break;
            }

            (local[i], local[i + 1]) = (local[i + 1], local[i]);
        }
    }
}

using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Metrics;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Metrics;

public static class EciMetricsCalculator
{
    public static (decimal Precision, decimal Recall, decimal F1) ComputePrecisionRecallF1(
        int tp,
        int fp,
        int fn)
    {
        var precision = tp + fp > 0 ? (decimal)tp / (tp + fp) : 0m;
        var recall = tp + fn > 0 ? (decimal)tp / (tp + fn) : 0m;
        var f1 = precision + recall > 0 ? 2m * precision * recall / (precision + recall) : 0m;
        return (Round4(precision), Round4(recall), Round4(f1));
    }

    public static decimal ComputeMrr(IReadOnlyList<int> hitRanks)
    {
        // ranks are 1-based.
        var first = hitRanks.Where(r => r > 0).DefaultIfEmpty(0).Min();
        if (first <= 0) return 0m;
        return Round4(1m / first);
    }

    public static decimal ComputeNdcg(IReadOnlyList<int> expectedRanks, IReadOnlyDictionary<int, int> producedRankByRubricId)
    {
        // expectedRanks: rubric ids ordered by ideal relevance (first = most relevant).
        // Relevance is implicit: 1.0 for expected, discounted by produced position.
        if (expectedRanks.Count == 0) return 0m;

        decimal DcG()
        {
            decimal sum = 0m;
            for (var i = 0; i < expectedRanks.Count; i++)
            {
                var rubricId = expectedRanks[i];
                if (!producedRankByRubricId.TryGetValue(rubricId, out var rank) || rank <= 0)
                {
                    continue;
                }

                var rel = 1m; // binary relevance for now
                sum += rel / (decimal)Math.Log2(rank + 1);
            }
            return sum;
        }

        // Ideal DCG: expected rubrics ranked perfectly at 1..N
        decimal idcg = 0m;
        for (var i = 1; i <= expectedRanks.Count; i++)
        {
            idcg += 1m / (decimal)Math.Log2(i + 1);
        }

        var dcg = DcG();
        return idcg <= 0 ? 0m : Round4(dcg / idcg);
    }

    public static decimal ComputeTopKAccuracy(int topK, IReadOnlyDictionary<int, int> producedRankByRubricId, IReadOnlyList<int> expectedRubricIds)
    {
        if (expectedRubricIds.Count == 0) return 0m;
        var hit = expectedRubricIds.Any(id => producedRankByRubricId.TryGetValue(id, out var rank) && rank > 0 && rank <= topK);
        return hit ? 1m : 0m;
    }

    private static decimal Round4(decimal v) => Math.Round(v, 4);
}


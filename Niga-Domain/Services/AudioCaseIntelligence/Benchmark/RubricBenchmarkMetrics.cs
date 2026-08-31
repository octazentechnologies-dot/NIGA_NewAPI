using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Benchmark;

/// <summary>
/// Deterministic Precision@K / Recall@K helpers for golden-set evaluation.
/// Does not invent metrics — callers supply doctor-accepted SubSectionIds.
/// </summary>
public static class RubricBenchmarkMetrics
{
    public sealed class MetricsResult
    {
        public int SuggestedCount { get; init; }
        public int RelevantCount { get; init; }
        public int HitCount { get; init; }
        public decimal PrecisionAtK { get; init; }
        public decimal RecallAtK { get; init; }
        public decimal DbBackedRate { get; init; }
        public int K { get; init; }
    }

    public static MetricsResult ComputeAtK(
        IReadOnlyList<int> suggestedSubSectionIdsInRankOrder,
        IReadOnlySet<int> doctorAcceptedSubSectionIds,
        int k = 10)
    {
        k = Math.Clamp(k, 1, 50);
        var suggested = suggestedSubSectionIdsInRankOrder
            .Where(id => id > 0)
            .Distinct()
            .Take(k)
            .ToList();

        var relevant = doctorAcceptedSubSectionIds.Where(id => id > 0).ToHashSet();
        var hits = suggested.Count(id => relevant.Contains(id));

        return new MetricsResult
        {
            K = k,
            SuggestedCount = suggested.Count,
            RelevantCount = relevant.Count,
            HitCount = hits,
            PrecisionAtK = suggested.Count == 0 ? 0 : Math.Round(hits / (decimal)suggested.Count, 4),
            RecallAtK = relevant.Count == 0 ? 0 : Math.Round(hits / (decimal)relevant.Count, 4),
            DbBackedRate = suggestedSubSectionIdsInRankOrder.Count == 0
                ? 0
                : Math.Round(
                    suggestedSubSectionIdsInRankOrder.Count(id => id > 0)
                    / (decimal)suggestedSubSectionIdsInRankOrder.Count, 4),
        };
    }

    public static MetricsResult ComputeFromRubrics(
        IReadOnlyList<AudioCaseSuggestedRubricModel> suggested,
        IReadOnlySet<int> doctorAcceptedSubSectionIds,
        int k = 10)
    {
        var ordered = suggested
            .OrderByDescending(r => r.CanonicalScore ?? r.ConfidenceScore ?? r.MatchScore)
            .Select(r => r.SubSectionId)
            .ToList();
        return ComputeAtK(ordered, doctorAcceptedSubSectionIds, k);
    }
}

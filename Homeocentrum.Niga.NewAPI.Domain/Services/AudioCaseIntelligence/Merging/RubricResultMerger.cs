using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

public enum DiscoveryPathKind
{
    /// <summary>V3/ECI concept-driven candidates only — V1 LIKE and unscoped embedding union skipped.</summary>
    ConceptGraphOnly = 0,

    /// <summary>Existing V1 + V2/hybrid merge (fallback when concept graph is empty or below confidence).</summary>
    LegacyV1V2Merge = 1,
}

public sealed class DiscoveryPathSelectionResult
{
    public DiscoveryPathKind Path { get; init; }

    public int UsableConceptCount { get; init; }

    public decimal MaxConceptConfidence { get; init; }

    /// <summary>Authoritative repertory rubrics (SubSectionId &gt; 0) from the concept-graph path.</summary>
    public int RepertoryCandidateCount { get; init; }

    public List<AudioCaseSuggestedRubricModel> Rubrics { get; init; } = new();

    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Merges rubric candidate lists. Task 1 adds confidence-gated discovery so V1 LIKE / unscoped
/// embedding results cannot bypass the concept graph when StrictConceptGatedDiscovery is on.
/// Low-recall guard: exclusive concept path also requires a minimum repertory candidate count.
/// </summary>
public static class RubricResultMerger
{
    public static List<AudioCaseSuggestedRubricModel> Merge(
        IReadOnlyList<AudioCaseSuggestedRubricModel> v1Rubrics,
        IReadOnlyList<AudioCaseSuggestedRubricModel> v2Rubrics,
        int maxResults = 20)
    {
        var merged = new Dictionary<string, AudioCaseSuggestedRubricModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var rubric in v1Rubrics)
        {
            var key = GetMergeKey(rubric);
            merged[key] = rubric;
        }

        foreach (var rubric in v2Rubrics)
        {
            var key = GetMergeKey(rubric);
            if (merged.TryGetValue(key, out var existing))
            {
                if (GetEffectiveScore(rubric) > GetEffectiveScore(existing))
                {
                    merged[key] = rubric;
                }
            }
            else
            {
                merged[key] = rubric;
            }
        }

        return merged.Values
            .OrderByDescending(GetEffectiveScore)
            .ThenBy(x => x.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Confidence-gated discovery selection (Task 1 + low-recall fix).
    /// Exclusive ConceptGraphOnly requires:
    /// - usable homeopathic concepts ≥ ConceptGraphMinConfidence, AND
    /// - at least <paramref name="minRepertoryCandidatesForExclusivePath"/> authoritative
    ///   repertory rubrics (SubSectionId &gt; 0). A single high-confidence AI clinical concept
    ///   must NOT skip V1 LIKE / fallback search.
    /// </summary>
    public static DiscoveryPathSelectionResult SelectDiscoveryPath(
        bool enableV3ConceptGraph,
        bool strictConceptGatedDiscovery,
        decimal conceptGraphMinConfidence,
        IReadOnlyList<HomeopathicConceptNodeModel>? homeopathicConcepts,
        IReadOnlyList<AudioCaseSuggestedRubricModel>? conceptGraphRubrics,
        IReadOnlyList<AudioCaseSuggestedRubricModel> v1Rubrics,
        IReadOnlyList<AudioCaseSuggestedRubricModel> v2Rubrics,
        int maxResults = 20,
        int minRepertoryCandidatesForExclusivePath = 3)
    {
        var concepts = homeopathicConcepts ?? Array.Empty<HomeopathicConceptNodeModel>();
        var usable = concepts
            .Where(c => c != null && c.Confidence >= conceptGraphMinConfidence)
            .ToList();
        var maxConfidence = concepts.Count == 0
            ? 0m
            : concepts.Max(c => c.Confidence);

        var allConceptRubrics = conceptGraphRubrics ?? Array.Empty<AudioCaseSuggestedRubricModel>();
        var repertoryCandidates = allConceptRubrics
            .Where(IsAuthoritativeRepertoryCandidate)
            .ToList();
        var repertoryCount = repertoryCandidates.Count;
        var minRequired = Math.Max(1, minRepertoryCandidatesForExclusivePath);

        var canUseConceptPath = enableV3ConceptGraph
            && strictConceptGatedDiscovery
            && usable.Count >= 1
            && repertoryCount >= minRequired;

        if (canUseConceptPath)
        {
            var gated = repertoryCandidates
                .Concat(allConceptRubrics.Where(r => !IsAuthoritativeRepertoryCandidate(r)))
                .GroupBy(GetMergeKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(GetEffectiveScore).First())
                .OrderByDescending(GetEffectiveScore)
                .ThenBy(x => x.SubSectionName, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults)
                .ToList();

            return new DiscoveryPathSelectionResult
            {
                Path = DiscoveryPathKind.ConceptGraphOnly,
                UsableConceptCount = usable.Count,
                MaxConceptConfidence = maxConfidence,
                RepertoryCandidateCount = repertoryCount,
                Rubrics = gated,
                Reason = $"StrictConceptGatedDiscovery: {usable.Count} concept(s) ≥ {conceptGraphMinConfidence:0.##}, {repertoryCount} repertory candidate(s) ≥ {minRequired}; skipping V1/V2 union.",
            };
        }

        // Low-recall / empty path: merge any concept-graph repertory hits with legacy V1+V2.
        var legacyBase = Merge(v1Rubrics, v2Rubrics, maxResults: maxResults * 2);
        var supplemented = Merge(repertoryCandidates, legacyBase, maxResults);

        var reason = !enableV3ConceptGraph
            ? "V3 concept graph disabled; legacy V1+V2 merge."
            : !strictConceptGatedDiscovery
                ? "StrictConceptGatedDiscovery off; legacy V1+V2 merge (flagged rollout)."
                : usable.Count == 0
                    ? $"No homeopathic concepts ≥ {conceptGraphMinConfidence:0.##}; fallback legacy V1+V2 merge."
                    : repertoryCount == 0
                        ? "Concept graph produced no authoritative repertory rubrics; fallback legacy V1+V2 merge."
                        : $"Concept graph low recall ({repertoryCount} repertory < {minRequired} required); supplement with legacy V1+V2 merge.";

        return new DiscoveryPathSelectionResult
        {
            Path = DiscoveryPathKind.LegacyV1V2Merge,
            UsableConceptCount = usable.Count,
            MaxConceptConfidence = maxConfidence,
            RepertoryCandidateCount = repertoryCount,
            Rubrics = supplemented,
            Reason = reason,
        };
    }

    public static bool IsAuthoritativeRepertoryCandidate(AudioCaseSuggestedRubricModel rubric) =>
        rubric.SubSectionId > 0
        && !string.Equals(rubric.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(rubric.MatchSource, "AiClinicalConcept", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(rubric.MatchLayer, "AiClinicalConcept", StringComparison.OrdinalIgnoreCase);

    private static string GetMergeKey(AudioCaseSuggestedRubricModel rubric)
    {
        if (rubric.SubSectionId > 0)
        {
            return $"id:{rubric.SubSectionId}";
        }

        return $"name:{rubric.SubSectionName?.Trim().ToUpperInvariant()}";
    }

    private static decimal GetEffectiveScore(AudioCaseSuggestedRubricModel rubric)
    {
        return rubric.ConfidenceScore ?? rubric.MatchScore;
    }
}

using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

/// <summary>Phase 3: one rubric per SubSectionId; merge evidence from duplicate concept mappings.</summary>
public static class EnterpriseRubricDeduplicator
{
    public static List<RubricDiscoveryNodeModel> DeduplicateDiscoveries(IEnumerable<RubricDiscoveryNodeModel> discoveries)
    {
        var map = new Dictionary<int, RubricDiscoveryNodeModel>();
        foreach (var discovery in discoveries.Where(d => d.SubSectionId > 0))
        {
            if (!map.TryGetValue(discovery.SubSectionId, out var existing))
            {
                map[discovery.SubSectionId] = discovery;
                continue;
            }

            if (discovery.Confidence > existing.Confidence)
            {
                map[discovery.SubSectionId] = discovery;
            }
        }

        return map.Values
            .OrderByDescending(d => d.Confidence)
            .ToList();
    }

    public static List<AudioCaseSuggestedRubricModel> DeduplicateRubrics(IEnumerable<AudioCaseSuggestedRubricModel> rubrics)
    {
        var map = new Dictionary<int, AudioCaseSuggestedRubricModel>();
        foreach (var rubric in rubrics)
        {
            if (rubric.SubSectionId <= 0)
            {
                continue;
            }

            if (!map.TryGetValue(rubric.SubSectionId, out var existing)
                || (rubric.EnterpriseConfidenceScore ?? rubric.ConfidenceScore ?? rubric.MatchScore)
                > (existing.EnterpriseConfidenceScore ?? existing.ConfidenceScore ?? existing.MatchScore))
            {
                map[rubric.SubSectionId] = rubric;
            }
        }

        return map.Values.ToList();
    }

    public static List<RubricDiscoveryNodeModel> MergeRepertoryFirst(
        IReadOnlyList<RubricDiscoveryNodeModel> repertoryMatches,
        IReadOnlyList<RubricDiscoveryNodeModel> aiDiscoveries)
    {
        var map = new Dictionary<int, RubricDiscoveryNodeModel>();

        foreach (var discovery in aiDiscoveries.Where(d => d.SubSectionId > 0))
        {
            map[discovery.SubSectionId] = discovery;
        }

        foreach (var repertory in repertoryMatches.Where(d => d.SubSectionId > 0))
        {
            repertory.DiscoveryMethod = RubricDiscoverySources.RepertoryDb;
            repertory.Confidence = Math.Max(repertory.Confidence, 0.82m);
            map[repertory.SubSectionId] = repertory;
        }

        return map.Values
            .OrderByDescending(d => string.Equals(d.DiscoveryMethod, RubricDiscoverySources.RepertoryDb, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(d => d.Confidence)
            .ToList();
    }
}

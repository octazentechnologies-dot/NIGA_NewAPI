using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

/// <summary>
/// Phases 4 + 8: hierarchical repertory mapping — database is authoritative.
/// Search order: exact rubric → synonym/pattern → bootstrap → (embedding handled elsewhere).
/// </summary>
public interface IHierarchicalRepertorySearchEngine
{
    Task<List<RubricDiscoveryNodeModel>> DiscoverFromGraphAsync(
        ConceptGraphFullModel graph,
        string transcript,
        CancellationToken cancellationToken = default);

    Task<List<RubricDiscoveryNodeModel>> DiscoverForConceptAsync(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        PatientMeaningNodeModel? meaning,
        CancellationToken cancellationToken = default);
}

public class HierarchicalRepertorySearchEngine : IHierarchicalRepertorySearchEngine
{
    private readonly NIGACentrumContext _context;
    private readonly ISubSectionRepository _subSectionRepository;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<HierarchicalRepertorySearchEngine> _logger;

    public HierarchicalRepertorySearchEngine(
        NIGACentrumContext context,
        ISubSectionRepository subSectionRepository,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<HierarchicalRepertorySearchEngine> logger)
    {
        _context = context;
        _subSectionRepository = subSectionRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<RubricDiscoveryNodeModel>> DiscoverFromGraphAsync(
        ConceptGraphFullModel graph,
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var discoveries = new Dictionary<string, RubricDiscoveryNodeModel>(StringComparer.OrdinalIgnoreCase);
        var patternCache = new Dictionary<string, List<(int Id, string Name)>>(StringComparer.OrdinalIgnoreCase);

        var bootstrapMappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .ToListAsync(cancellationToken);

        foreach (var homeo in graph.HomeopathicConcepts.Where(c => !string.IsNullOrWhiteSpace(c.ConceptName)))
        {
            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            var meaning = clinical != null
                ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                    ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                : null;

            var searchPhrases = BuildSearchPhrases(homeo, clinical, meaning);
            foreach (var phrase in searchPhrases)
            {
                await AddExactMatchesAsync(discoveries, homeo, phrase, cancellationToken);
                await AddHotspotMatchesAsync(discoveries, homeo, phrase, cancellationToken);
            }

            foreach (var mapping in bootstrapMappings.Where(m =>
                ConceptGraphTierHelper.ConceptNamesMatch(homeo.ConceptName, m.HomeopathicConceptPattern)))
            {
                if (!patternCache.TryGetValue(mapping.SubSectionNamePattern, out var rubrics))
                {
                    rubrics = await FindRubricsByPatternAsync(mapping.SubSectionNamePattern, cancellationToken);
                    patternCache[mapping.SubSectionNamePattern] = rubrics;
                }

                foreach (var rubric in rubrics)
                {
                    AddDiscovery(discoveries, homeo, rubric.Id, rubric.Name,
                        $"Repertory bootstrap: {homeo.ConceptName} → {mapping.SubSectionNamePattern}",
                        0.86m);
                }
            }
        }

        var result = discoveries.Values
            .OrderByDescending(d => d.Confidence)
            .Take(_options.MaxDiscoveryCandidates)
            .ToList();

        _logger.LogInformation(
            "Hierarchical repertory search resolved {Count} authoritative rubric(s) from {ConceptCount} concept(s).",
            result.Count,
            graph.HomeopathicConcepts.Count);

        return result;
    }

    public async Task<List<RubricDiscoveryNodeModel>> DiscoverForConceptAsync(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        PatientMeaningNodeModel? meaning,
        CancellationToken cancellationToken = default)
    {
        var discoveries = new Dictionary<string, RubricDiscoveryNodeModel>(StringComparer.OrdinalIgnoreCase);
        var patternCache = new Dictionary<string, List<(int Id, string Name)>>(StringComparer.OrdinalIgnoreCase);

        var bootstrapMappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .ToListAsync(cancellationToken);

        var searchPhrases = BuildSearchPhrases(homeo, clinical, meaning).ToList();
        foreach (var phrase in searchPhrases)
        {
            await AddExactMatchesAsync(discoveries, homeo, phrase, cancellationToken);
            await AddHotspotMatchesAsync(discoveries, homeo, phrase, cancellationToken);
        }

        foreach (var mapping in bootstrapMappings.Where(m =>
            ConceptGraphTierHelper.ConceptNamesMatch(homeo.ConceptName, m.HomeopathicConceptPattern)))
        {
            if (!patternCache.TryGetValue(mapping.SubSectionNamePattern, out var rubrics))
            {
                rubrics = await FindRubricsByPatternAsync(mapping.SubSectionNamePattern, cancellationToken);
                patternCache[mapping.SubSectionNamePattern] = rubrics;
            }

            foreach (var rubric in rubrics)
            {
                AddDiscovery(discoveries, homeo, rubric.Id, rubric.Name,
                    $"Repertory bootstrap: {homeo.ConceptName} → {mapping.SubSectionNamePattern}",
                    0.86m);
            }
        }

        return discoveries.Values
            .OrderByDescending(d => d.Confidence)
            .ToList();
    }

    private static IEnumerable<string> BuildSearchPhrases(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        PatientMeaningNodeModel? meaning)
    {
        var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(homeo.ConceptName))
        {
            phrases.Add(homeo.ConceptName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(clinical?.ConceptName))
        {
            phrases.Add(clinical.ConceptName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(meaning?.NormalizedMeaning))
        {
            phrases.Add(meaning.NormalizedMeaning.Trim());
        }

        return phrases.Where(p => p.Length >= 3).Take(6);
    }

    private async Task AddExactMatchesAsync(
        Dictionary<string, RubricDiscoveryNodeModel> map,
        HomeopathicConceptNodeModel homeo,
        string phrase,
        CancellationToken cancellationToken)
    {
        var normalized = phrase.Trim();
        var exact = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null
                && (s.SubSectionName == normalized
                    || EF.Functions.Like(s.SubSectionName, $"%{normalized}%")))
            .OrderBy(s => s.SubSectionName!.Length)
            .Take(5)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        foreach (var row in exact)
        {
            if (!WordBoundaryMatcher.Matches(row.SubSectionName, normalized)
                && WordBoundaryMatcher.IsSubstringCollision(row.SubSectionName, normalized))
            {
                continue;
            }

            var confidence = string.Equals(row.SubSectionName, normalized, StringComparison.OrdinalIgnoreCase)
                ? 0.95m
                : WordBoundaryMatcher.Matches(row.SubSectionName, normalized) ? 0.82m : 0.55m;

            if (confidence < 0.70m)
                continue;

            AddDiscovery(map, homeo, row.SubSectionId, row.SubSectionName!,
                $"Exact repertory match for '{phrase}'", confidence);
        }
    }

    private async Task AddHotspotMatchesAsync(
        Dictionary<string, RubricDiscoveryNodeModel> map,
        HomeopathicConceptNodeModel homeo,
        string phrase,
        CancellationToken cancellationToken)
    {
        var search = await _subSectionRepository.SearchSubSectionsByHotspotAsync(new SearchSubSectionByHotspotRequest
        {
            HotspotName = phrase,
            PageNumber = 1,
            PageSize = 6,
        });

        foreach (var item in search.Items)
        {
            var name = item.SubSectionName ?? string.Empty;
            if (!WordBoundaryMatcher.Matches(name, phrase))
                continue;

            AddDiscovery(map, homeo, item.SubSectionId, name,
                $"Database hotspot search: '{phrase}'", 0.78m);
        }
    }

    private async Task<List<(int Id, string Name)>> FindRubricsByPatternAsync(
        string pattern,
        CancellationToken cancellationToken)
    {
        var likePattern = pattern.Replace('*', '%');
        if (!likePattern.Contains('%'))
        {
            likePattern = $"%{likePattern}%";
        }

        return await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null
                && EF.Functions.Like(s.SubSectionName, likePattern))
            .OrderBy(s => s.SubSectionName)
            .Take(_options.MaxRubricsPerPattern)
            .Select(s => new ValueTuple<int, string>(s.SubSectionId, s.SubSectionName!))
            .ToListAsync(cancellationToken);
    }

    private static void AddDiscovery(
        Dictionary<string, RubricDiscoveryNodeModel> map,
        HomeopathicConceptNodeModel homeo,
        int subSectionId,
        string subSectionName,
        string reason,
        decimal confidence)
    {
        if (subSectionId <= 0 || string.IsNullOrWhiteSpace(subSectionName))
        {
            return;
        }

        // Key by concept+rubric so one SubSectionId cannot steal another concept's citation.
        var key = $"{homeo.HomeopathicConceptId}:{subSectionId}";
        if (map.TryGetValue(key, out var existing) && existing.Confidence >= confidence)
        {
            return;
        }

        map[key] = new RubricDiscoveryNodeModel
        {
            HomeopathicConceptId = homeo.HomeopathicConceptId,
            SubSectionId = subSectionId,
            SubSectionName = subSectionName,
            MatchReason = reason,
            DiscoveryMethod = RubricDiscoverySources.RepertoryDb,
            Confidence = Math.Round(confidence, 4),
            RubricTier = ConceptGraphTierHelper.ResolveTier(confidence),
        };
    }
}

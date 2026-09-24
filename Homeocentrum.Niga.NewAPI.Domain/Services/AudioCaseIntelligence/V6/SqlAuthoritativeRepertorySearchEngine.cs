using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;
using System.Text.RegularExpressions;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V6;

/// <summary>V6: SQL-first authoritative repertory search. Database is the single source of repertory rubrics.</summary>
public interface ISqlAuthoritativeRepertorySearchEngine
{
    Task<List<V6SqlSearchHit>> SearchForSymptomAsync(
        V6ClinicalSymptomUnit symptom,
        CancellationToken cancellationToken = default);
}

public class SqlAuthoritativeRepertorySearchEngine : ISqlAuthoritativeRepertorySearchEngine
{
    private readonly NIGACentrumContext _context;
    private readonly ISubSectionRepository _subSectionRepository;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<SqlAuthoritativeRepertorySearchEngine> _logger;

    public SqlAuthoritativeRepertorySearchEngine(
        NIGACentrumContext context,
        ISubSectionRepository subSectionRepository,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<SqlAuthoritativeRepertorySearchEngine> logger)
    {
        _context = context;
        _subSectionRepository = subSectionRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<V6SqlSearchHit>> SearchForSymptomAsync(
        V6ClinicalSymptomUnit symptom,
        CancellationToken cancellationToken = default)
    {
        var hits = new Dictionary<int, V6SqlSearchHit>();
        var phrases = BuildSearchPhrases(symptom).ToList();

        foreach (var phrase in phrases)
        {
            await AddExactMatchesAsync(hits, phrase, cancellationToken);
            await AddLikeMatchesAsync(hits, phrase, cancellationToken);
            await AddNormalizedMatchesAsync(hits, phrase, cancellationToken);
            await AddAliasMatchesAsync(hits, phrase, cancellationToken);
            await AddHotspotMatchesAsync(hits, phrase);
        }

        await AddBootstrapMatchesAsync(hits, symptom, cancellationToken);

        var seedIds = hits.Keys.ToList();
        if (seedIds.Count > 0)
        {
            await ExpandHierarchyAsync(hits, seedIds, cancellationToken);
            await ExpandCrossReferencesAsync(hits, seedIds, cancellationToken);
        }

        return hits.Values
            .OrderByDescending(h => h.SqlConfidence)
            .ThenBy(h => h.HierarchyDepth)
            .Take(_options.MaxSqlHitsPerSymptom)
            .ToList();
    }

    private static IEnumerable<string> BuildSearchPhrases(V6ClinicalSymptomUnit symptom)
    {
        var phrases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in new[] { symptom.HomeopathicLabel, symptom.ClinicalLabel, symptom.Modality, symptom.Etiology })
        {
            if (!string.IsNullOrWhiteSpace(p) && p.Trim().Length >= 3)
            {
                phrases.Add(p.Trim());
            }
        }

        return phrases.Take(8);
    }

    private async Task AddExactMatchesAsync(
        Dictionary<int, V6SqlSearchHit> hits,
        string phrase,
        CancellationToken cancellationToken)
    {
        var rows = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null
                && s.SubSectionName == phrase)
            .Take(5)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            Upsert(hits, row.SubSectionId, row.SubSectionName!, V6SqlSearchStageNames.ExactMatch,
                $"Exact: '{phrase}'", 0.98m, 0);
        }
    }

    private async Task AddLikeMatchesAsync(
        Dictionary<int, V6SqlSearchHit> hits,
        string phrase,
        CancellationToken cancellationToken)
    {
        var rows = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null
                && EF.Functions.Like(s.SubSectionName, $"%{phrase}%"))
            .OrderBy(s => s.SubSectionName!.Length)
            .Take(8)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            Upsert(hits, row.SubSectionId, row.SubSectionName!, V6SqlSearchStageNames.LikeMatch,
                $"LIKE: '%{phrase}%'", 0.84m, 0);
        }
    }

    private async Task AddNormalizedMatchesAsync(
        Dictionary<int, V6SqlSearchHit> hits,
        string phrase,
        CancellationToken cancellationToken)
    {
        var tokens = phrase
            .Split(new[] { ' ', ';', ',', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4);

        foreach (var token in tokens)
        {
            var rows = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus && s.SubSectionName != null
                    && EF.Functions.Like(s.SubSectionName, $"%{token}%"))
                .OrderBy(s => s.SubSectionName!.Length)
                .Take(4)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                Upsert(hits, row.SubSectionId, row.SubSectionName!, V6SqlSearchStageNames.NormalizedMatch,
                    $"Token '{token}' in '{phrase}'", 0.76m, 0);
            }
        }
    }

    private async Task AddAliasMatchesAsync(
        Dictionary<int, V6SqlSearchHit> hits,
        string phrase,
        CancellationToken cancellationToken)
    {
        var aliasRows = await _context.RubricAliases.AsNoTracking()
            .Where(a => a.IsActive && a.AliasText != null
                && (a.AliasText == phrase || EF.Functions.Like(a.AliasText, $"%{phrase}%")))
            .Take(10)
            .Select(a => new { a.SubSectionId, a.AliasText })
            .ToListAsync(cancellationToken);

        foreach (var alias in aliasRows)
        {
            var rubric = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => s.SubSectionId == alias.SubSectionId && !s.DeleteStatus)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .FirstOrDefaultAsync(cancellationToken);

            if (rubric?.SubSectionName != null)
            {
                Upsert(hits, rubric.SubSectionId, rubric.SubSectionName, V6SqlSearchStageNames.SynonymAlias,
                    $"Alias '{alias.AliasText}' → rubric", 0.88m, 0);
            }
        }

        var aliasNameRows = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionNameAlias != null
                && EF.Functions.Like(s.SubSectionNameAlias, $"%{phrase}%"))
            .Take(6)
            .Select(s => new { s.SubSectionId, s.SubSectionName, s.SubSectionNameAlias })
            .ToListAsync(cancellationToken);

        foreach (var row in aliasNameRows)
        {
            Upsert(hits, row.SubSectionId, row.SubSectionName!, V6SqlSearchStageNames.SynonymAlias,
                $"SubSectionNameAlias: '{row.SubSectionNameAlias}'", 0.86m, 0);
        }
    }

    private async Task AddHotspotMatchesAsync(Dictionary<int, V6SqlSearchHit> hits, string phrase)
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

            Upsert(hits, item.SubSectionId, name,
                V6SqlSearchStageNames.HotspotSearch, $"Hotspot: '{phrase}'", 0.78m, 0);
        }
    }

    private async Task AddBootstrapMatchesAsync(
        Dictionary<int, V6SqlSearchHit> hits,
        V6ClinicalSymptomUnit symptom,
        CancellationToken cancellationToken)
    {
        var mappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings.Where(m =>
            ConceptGraphTierHelper.ConceptNamesMatch(symptom.HomeopathicLabel, m.HomeopathicConceptPattern)))
        {
            var likePattern = mapping.SubSectionNamePattern.Replace('*', '%');
            if (!likePattern.Contains('%'))
            {
                likePattern = $"%{likePattern}%";
            }

            var rubrics = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus && s.SubSectionName != null
                    && EF.Functions.Like(s.SubSectionName, likePattern))
                .Take(_options.MaxRubricsPerPattern)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var rubric in rubrics)
            {
                Upsert(hits, rubric.SubSectionId, rubric.SubSectionName!, V6SqlSearchStageNames.BootstrapMapping,
                    $"Bootstrap: {symptom.HomeopathicLabel} → {mapping.SubSectionNamePattern}", 0.90m, 0);
            }
        }
    }

    private async Task ExpandHierarchyAsync(
        Dictionary<int, V6SqlSearchHit> hits,
        List<int> seedIds,
        CancellationToken cancellationToken)
    {
        var seeds = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => seedIds.Contains(s.SubSectionId) && !s.DeleteStatus)
            .Select(s => new { s.SubSectionId, s.SubSectionName, s.ParentSubSectionId, s.SectionId })
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            if (seed.ParentSubSectionId.HasValue && seed.ParentSubSectionId > 0)
            {
                var parent = await _context.SubSectionMasters.AsNoTracking()
                    .Where(s => s.SubSectionId == seed.ParentSubSectionId && !s.DeleteStatus)
                    .Select(s => new { s.SubSectionId, s.SubSectionName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (parent?.SubSectionName != null)
                {
                    Upsert(hits, parent.SubSectionId, parent.SubSectionName, V6SqlSearchStageNames.ParentRubric,
                        $"Parent of '{seed.SubSectionName}'", 0.72m, 1);
                }
            }

            var children = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => s.ParentSubSectionId == seed.SubSectionId && !s.DeleteStatus)
                .Take(6)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                Upsert(hits, child.SubSectionId, child.SubSectionName!, V6SqlSearchStageNames.ChildRubric,
                    $"Child of '{seed.SubSectionName}'", 0.70m, 1);
            }

            if (seed.SectionId.HasValue)
            {
                var siblings = await _context.SubSectionMasters.AsNoTracking()
                    .Where(s => s.SectionId == seed.SectionId
                        && s.SubSectionId != seed.SubSectionId
                        && !s.DeleteStatus
                        && s.ParentSubSectionId == seed.ParentSubSectionId)
                    .Take(4)
                    .Select(s => new { s.SubSectionId, s.SubSectionName })
                    .ToListAsync(cancellationToken);

                foreach (var sibling in siblings)
                {
                    Upsert(hits, sibling.SubSectionId, sibling.SubSectionName!, V6SqlSearchStageNames.SiblingRubric,
                        $"Sibling of '{seed.SubSectionName}'", 0.65m, 2);
                }
            }
        }
    }

    private async Task ExpandCrossReferencesAsync(
        Dictionary<int, V6SqlSearchHit> hits,
        List<int> seedIds,
        CancellationToken cancellationToken)
    {
        var refs = await _context.ReferenceRubricDetails.AsNoTracking()
            .Where(r => r.DeleteStatus != true
                && (seedIds.Contains(r.SubSectionId ?? 0) || seedIds.Contains(r.RefSubSectionId ?? 0)))
            .Take(30)
            .ToListAsync(cancellationToken);

        var targetIds = refs
            .SelectMany(r => new[] { r.SubSectionId, r.RefSubSectionId })
            .Where(id => id.HasValue && id > 0)
            .Select(id => id!.Value)
            .Distinct()
            .Where(id => !hits.ContainsKey(id))
            .ToList();

        if (targetIds.Count == 0)
        {
            return;
        }

        var rubrics = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => targetIds.Contains(s.SubSectionId) && !s.DeleteStatus)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        foreach (var rubric in rubrics)
        {
            Upsert(hits, rubric.SubSectionId, rubric.SubSectionName!, V6SqlSearchStageNames.CrossReference,
                "Cross-reference expansion", 0.68m, 2);
        }
    }

    private static void Upsert(
        Dictionary<int, V6SqlSearchHit> hits,
        int subSectionId,
        string name,
        string stage,
        string path,
        decimal confidence,
        int hierarchyDepth)
    {
        if (subSectionId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!hits.TryGetValue(subSectionId, out var existing) || confidence > existing.SqlConfidence)
        {
            hits[subSectionId] = new V6SqlSearchHit
            {
                SubSectionId = subSectionId,
                SubSectionName = name,
                SearchStage = stage,
                MatchPath = path,
                SqlConfidence = confidence,
                HierarchyDepth = hierarchyDepth,
            };
        }
    }

    internal static string NormalizeRubricText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s;,\-]", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }
}

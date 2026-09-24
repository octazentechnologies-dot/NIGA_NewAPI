using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories.AiEmbeddingInfrastructure;

public class RepertoryRubricCatalogReader : IRepertoryRubricCatalogReader
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<RepertoryRubricCatalogReader> _logger;
    private RepertoryEnrichmentCache? _cache;

    public RepertoryRubricCatalogReader(
        NIGACentrumContext context,
        ILogger<RepertoryRubricCatalogReader> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<int> CountRubricsAsync(CancellationToken cancellationToken = default) =>
        _context.SubSectionMasters.AsNoTracking()
            .CountAsync(x => !x.DeleteStatus && x.SubSectionName != null && x.SubSectionName != "", cancellationToken);

    public async Task<List<RepertoryRubricCatalogItem>> ReadPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        _cache ??= await LoadEnrichmentCacheAsync(cancellationToken);

        var rows = await (
                from sub in _context.SubSectionMasters.AsNoTracking()
                join sec in _context.SectionMasters.AsNoTracking() on sub.SectionId equals sec.SectionId into secJoin
                from sec in secJoin.DefaultIfEmpty()
                where !sub.DeleteStatus && sub.SubSectionName != null && sub.SubSectionName != ""
                orderby sub.SubSectionId
                select new CatalogRow
                {
                    SubSectionId = sub.SubSectionId,
                    SubSectionName = sub.SubSectionName,
                    SubSectionNameAlias = sub.SubSectionNameAlias,
                    Description = sub.Description,
                    SectionId = sub.SectionId,
                    SectionName = sec != null ? sec.SectionName : string.Empty,
                    SectionAlias = sec != null ? sec.SectionAlias : null,
                    SectionDescription = sec != null ? sec.Description : null,
                })
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return MapRows(rows, _cache!);
    }

    public async Task<List<RepertoryRubricCatalogItem>> ReadByIdsAsync(
        IEnumerable<int> rubricIds,
        CancellationToken cancellationToken = default)
    {
        var ids = rubricIds.Distinct().ToList();
        if (ids.Count == 0)
            return new List<RepertoryRubricCatalogItem>();

        _cache ??= await LoadEnrichmentCacheAsync(cancellationToken);

        var rows = await (
                from sub in _context.SubSectionMasters.AsNoTracking()
                join sec in _context.SectionMasters.AsNoTracking() on sub.SectionId equals sec.SectionId into secJoin
                from sec in secJoin.DefaultIfEmpty()
                where ids.Contains(sub.SubSectionId)
                select new CatalogRow
                {
                    SubSectionId = sub.SubSectionId,
                    SubSectionName = sub.SubSectionName,
                    SubSectionNameAlias = sub.SubSectionNameAlias,
                    Description = sub.Description,
                    SectionId = sub.SectionId,
                    SectionName = sec != null ? sec.SectionName : string.Empty,
                    SectionAlias = sec != null ? sec.SectionAlias : null,
                    SectionDescription = sec != null ? sec.Description : null,
                })
            .ToListAsync(cancellationToken);

        return MapRows(rows, _cache!);
    }

    private List<RepertoryRubricCatalogItem> MapRows(IEnumerable<CatalogRow> rows, RepertoryEnrichmentCache cache) =>
        rows.Select(row =>
        {
            var item = new RepertoryRubricCatalogItem
            {
                RubricId = row.SubSectionId,
                RubricName = row.SubSectionName?.Trim() ?? string.Empty,
                RubricAlias = row.SubSectionNameAlias,
                Description = row.Description,
                SectionId = row.SectionId,
                SectionName = string.IsNullOrWhiteSpace(row.SectionName) ? "General" : row.SectionName.Trim(),
                SectionAlias = row.SectionAlias,
                SectionDescription = row.SectionDescription,
            };

            EnrichItem(item, cache);
            return item;
        }).ToList();

    private sealed class CatalogRow
    {
        public int SubSectionId { get; init; }
        public string? SubSectionName { get; init; }
        public string? SubSectionNameAlias { get; init; }
        public string? Description { get; init; }
        public int? SectionId { get; init; }
        public string SectionName { get; init; } = string.Empty;
        public string? SectionAlias { get; init; }
        public string? SectionDescription { get; init; }
    }

    private static void EnrichItem(RepertoryRubricCatalogItem item, RepertoryEnrichmentCache cache)
    {
        if (!string.IsNullOrWhiteSpace(item.RubricAlias))
            item.KnownSynonyms.Add(item.RubricAlias.Trim());

        if (!string.IsNullOrWhiteSpace(item.SectionAlias))
            item.KnownSynonyms.Add(item.SectionAlias.Trim());

        if (!string.IsNullOrWhiteSpace(item.Description))
            item.Meanings.Add(item.Description.Trim());

        if (!string.IsNullOrWhiteSpace(item.SectionDescription))
            item.Meanings.Add(item.SectionDescription.Trim());

        if (cache.AliasesByRubric.TryGetValue(item.RubricId, out var aliases))
            item.KnownSynonyms.AddRange(aliases);

        if (cache.MetaphorsByRubric.TryGetValue(item.RubricId, out var metaphors))
        {
            foreach (var metaphor in metaphors)
            {
                if (!string.IsNullOrWhiteSpace(metaphor.ClinicalMeaning))
                    item.ClinicalConcepts.Add(metaphor.ClinicalMeaning.Trim());
                if (!string.IsNullOrWhiteSpace(metaphor.RubricMeaning))
                {
                    item.HomeopathicConcepts.Add(metaphor.RubricMeaning.Trim());
                    item.Meanings.Add(metaphor.RubricMeaning.Trim());
                }
                if (!string.IsNullOrWhiteSpace(metaphor.PatientExpression))
                    item.SymptomExamples.Add(metaphor.PatientExpression.Trim());
            }
        }

        if (cache.ClinicalKeywordsByRubric.TryGetValue(item.RubricId, out var keywords))
            item.SymptomExamples.AddRange(keywords);

        foreach (var mapping in cache.BootstrapMappings)
        {
            if (!RubricPatternMatcher.Matches(item.RubricName, mapping.SubSectionNamePattern))
                continue;

            if (!string.IsNullOrWhiteSpace(mapping.HomeopathicConceptPattern))
                item.HomeopathicConcepts.Add(RubricPatternMatcher.HumanizePattern(mapping.HomeopathicConceptPattern));

            if (!string.IsNullOrWhiteSpace(mapping.Domain))
                item.ClinicalConcepts.Add(mapping.Domain.Trim());
        }

        item.KnownSynonyms = DistinctOrdered(item.KnownSynonyms);
        item.ClinicalConcepts = DistinctOrdered(item.ClinicalConcepts);
        item.HomeopathicConcepts = DistinctOrdered(item.HomeopathicConcepts);
        item.Meanings = DistinctOrdered(item.Meanings);
        item.SymptomExamples = DistinctOrdered(item.SymptomExamples);
    }

    private async Task<RepertoryEnrichmentCache> LoadEnrichmentCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading repertory enrichment cache for enterprise embedding builder.");

        var aliases = await _context.RubricAliases.AsNoTracking()
            .Where(x => x.IsActive && !string.IsNullOrWhiteSpace(x.AliasText))
            .Select(x => new { x.SubSectionId, x.AliasText })
            .ToListAsync(cancellationToken);

        var metaphors = await _context.RubricMetaphorDictionaries.AsNoTracking()
            .Where(x => x.IsActive
                && x.ApprovalStatus == "Approved"
                && x.SubSectionId != null)
            .Select(x => new
            {
                SubSectionId = x.SubSectionId!.Value,
                x.ClinicalMeaning,
                x.RubricMeaning,
                x.PatientExpression,
            })
            .ToListAsync(cancellationToken);

        var clinicalKeywords = await (
                from cqr in _context.ClinicalQueRubrics.AsNoTracking()
                join kw in _context.ClinicalQueKeywords.AsNoTracking()
                    on cqr.ClinicalQueKeywordId equals kw.ClinicalQueKeywordId
                where cqr.SubsectionId != null
                    && (cqr.IsDeleted != true)
                    && (kw.IsDeleted != true)
                    && kw.KeywordQuestion != null
                    && kw.KeywordQuestion != ""
                select new { SubSectionId = cqr.SubsectionId!.Value, kw.KeywordQuestion })
            .ToListAsync(cancellationToken);

        var bootstrapMappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .Select(x => new BootstrapMappingRow
            {
                HomeopathicConceptPattern = x.HomeopathicConceptPattern,
                SubSectionNamePattern = x.SubSectionNamePattern,
                Domain = x.Domain,
            })
            .ToListAsync(cancellationToken);

        return new RepertoryEnrichmentCache
        {
            AliasesByRubric = aliases
                .GroupBy(x => x.SubSectionId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.AliasText.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()),
            MetaphorsByRubric = metaphors
                .GroupBy(x => x.SubSectionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new MetaphorRow
                    {
                        ClinicalMeaning = x.ClinicalMeaning,
                        RubricMeaning = x.RubricMeaning,
                        PatientExpression = x.PatientExpression,
                    }).ToList()),
            ClinicalKeywordsByRubric = clinicalKeywords
                .GroupBy(x => x.SubSectionId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.KeywordQuestion!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()),
            BootstrapMappings = bootstrapMappings,
        };
    }

    private static List<string> DistinctOrdered(IEnumerable<string> values) =>
        values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

    private sealed class RepertoryEnrichmentCache
    {
        public Dictionary<int, List<string>> AliasesByRubric { get; init; } = new();
        public Dictionary<int, List<MetaphorRow>> MetaphorsByRubric { get; init; } = new();
        public Dictionary<int, List<string>> ClinicalKeywordsByRubric { get; init; } = new();
        public List<BootstrapMappingRow> BootstrapMappings { get; init; } = new();
    }

    private sealed class MetaphorRow
    {
        public string ClinicalMeaning { get; init; } = string.Empty;
        public string RubricMeaning { get; init; } = string.Empty;
        public string PatientExpression { get; init; } = string.Empty;
    }

    private sealed class BootstrapMappingRow
    {
        public string HomeopathicConceptPattern { get; init; } = string.Empty;
        public string SubSectionNamePattern { get; init; } = string.Empty;
        public string? Domain { get; init; }
    }
}

internal static class RubricPatternMatcher
{
    public static bool Matches(string rubricName, string pattern)
    {
        if (string.IsNullOrWhiteSpace(rubricName) || string.IsNullOrWhiteSpace(pattern))
            return false;

        var likePattern = pattern.Replace('*', '%');
        var tokens = likePattern
            .Split('%', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();

        if (tokens.Count == 0)
            return true;

        var upperName = rubricName.ToUpperInvariant();
        var index = 0;
        foreach (var token in tokens)
        {
            var pos = upperName.IndexOf(token.ToUpperInvariant(), index, StringComparison.Ordinal);
            if (pos < 0)
                return false;
            index = pos + token.Length;
        }

        return true;
    }

    public static string HumanizePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return string.Empty;

        return pattern
            .Replace('%', ' ')
            .Replace('*', ' ')
            .Replace('_', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Aggregate((a, b) => $"{a} {b}");
    }
}

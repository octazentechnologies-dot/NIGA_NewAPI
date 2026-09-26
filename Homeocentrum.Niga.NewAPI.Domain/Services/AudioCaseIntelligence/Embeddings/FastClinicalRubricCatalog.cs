using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;

/// <summary>
/// Phase 8 harden: immutable process-level catalog snapshot with atomic swap.
/// Built from embedding cache + SubSectionMaster metadata + active aliases.
/// </summary>
public interface IFastClinicalRubricCatalog
{
    int EntryCount { get; }

    DateTime? LastBuiltUtc { get; }

    string? SnapshotVersion { get; }

    Task EnsureBuiltAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<AudioCaseSuggestedRubricModel> LookupByTokens(
        IEnumerable<string> tokens,
        int maxResults = 40);
}

public sealed class FastClinicalRubricCatalog : IFastClinicalRubricCatalog
{
    private readonly IRubricEmbeddingMemoryCache _embeddingCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FastClinicalRubricCatalog> _logger;

    /// <summary>Immutable published snapshot — never mutate after publish.</summary>
    private volatile CatalogSnapshot _snapshot = CatalogSnapshot.Empty;

    public FastClinicalRubricCatalog(
        IRubricEmbeddingMemoryCache embeddingCache,
        IServiceScopeFactory scopeFactory,
        ILogger<FastClinicalRubricCatalog> logger)
    {
        _embeddingCache = embeddingCache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int EntryCount => _snapshot.ById.Count;

    public DateTime? LastBuiltUtc => _snapshot.BuiltUtc == default ? null : _snapshot.BuiltUtc;

    public string? SnapshotVersion => _snapshot.Version;

    public async Task EnsureBuiltAsync(CancellationToken cancellationToken = default)
    {
        if (_embeddingCache.Entries.Count == 0)
            await _embeddingCache.RefreshAsync(cancellationToken);

        var entries = _embeddingCache.Entries;
        if (entries.Count == 0)
            return;

        var current = _snapshot;
        if (current.ById.Count > 0
            && _embeddingCache.LastRefreshedUtc.HasValue
            && current.SourceCacheUtc.HasValue
            && current.SourceCacheUtc >= _embeddingCache.LastRefreshedUtc)
        {
            return;
        }

        var byId = new Dictionary<int, CatalogEntry>();
        var tokenIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (entry.RubricId <= 0 || string.IsNullOrWhiteSpace(entry.SubSectionName))
                continue;

            byId[entry.RubricId] = new CatalogEntry
            {
                SubSectionId = entry.RubricId,
                SubSectionName = entry.SubSectionName,
                NormalizedName = entry.SubSectionName.Trim().ToLowerInvariant(),
            };
            IndexNameTokens(tokenIndex, entry.RubricId, entry.SubSectionName);
        }

        // Enrich with DeleteStatus / Parent / aliases (scoped DbContext).
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
            var ids = byId.Keys.ToList();

            // Batch to avoid huge IN lists
            for (var offset = 0; offset < ids.Count; offset += 500)
            {
                var batch = ids.Skip(offset).Take(500).ToList();
                var meta = await db.SubSectionMasters.AsNoTracking()
                    .Where(s => batch.Contains(s.SubSectionId))
                    .Select(s => new
                    {
                        s.SubSectionId,
                        s.SubSectionName,
                        s.ParentSubSectionId,
                        s.SectionId,
                        s.DeleteStatus,
                        s.SubSectionNameAlias,
                    })
                    .ToListAsync(cancellationToken);

                foreach (var row in meta)
                {
                    if (!byId.TryGetValue(row.SubSectionId, out var existing))
                        continue;

                    if (row.DeleteStatus)
                    {
                        byId.Remove(row.SubSectionId);
                        continue;
                    }

                    existing.ParentSubSectionId = row.ParentSubSectionId;
                    existing.SectionId = row.SectionId;
                    existing.DeleteStatus = false;
                    if (!string.IsNullOrWhiteSpace(row.SubSectionName))
                        existing.SubSectionName = row.SubSectionName!;
                    if (!string.IsNullOrWhiteSpace(row.SubSectionNameAlias))
                    {
                        existing.Aliases.Add(row.SubSectionNameAlias!);
                        IndexNameTokens(tokenIndex, row.SubSectionId, row.SubSectionNameAlias!);
                    }
                }

                var aliases = await db.RubricAliases.AsNoTracking()
                    .Where(a => a.IsActive && batch.Contains(a.SubSectionId))
                    .Select(a => new { a.SubSectionId, a.NormalizedAlias, a.AliasText })
                    .ToListAsync(cancellationToken);

                foreach (var a in aliases)
                {
                    if (!byId.TryGetValue(a.SubSectionId, out var existing))
                        continue;
                    if (!string.IsNullOrWhiteSpace(a.NormalizedAlias))
                    {
                        existing.Aliases.Add(a.NormalizedAlias);
                        IndexNameTokens(tokenIndex, a.SubSectionId, a.NormalizedAlias);
                    }
                    else if (!string.IsNullOrWhiteSpace(a.AliasText))
                    {
                        existing.Aliases.Add(a.AliasText);
                        IndexNameTokens(tokenIndex, a.SubSectionId, a.AliasText);
                    }
                }
            }

            // Remove token postings for deleted ids
            var liveIds = byId.Keys.ToHashSet();
            foreach (var key in tokenIndex.Keys.ToList())
            {
                tokenIndex[key] = tokenIndex[key].Where(id => liveIds.Contains(id)).ToList();
                if (tokenIndex[key].Count == 0)
                    tokenIndex.Remove(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog metadata enrichment failed; using embedding-name snapshot only.");
        }

        // Freeze alias lists
        foreach (var e in byId.Values)
            e.Aliases = e.Aliases.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var next = new CatalogSnapshot
        {
            ById = byId,
            TokenIndex = tokenIndex,
            BuiltUtc = DateTime.UtcNow,
            SourceCacheUtc = _embeddingCache.LastRefreshedUtc,
            Version = $"cat-{DateTime.UtcNow:yyyyMMddHHmmss}-{byId.Count}",
        };

        // Atomic publish — readers always see complete snapshot
        Volatile.Write(ref _snapshot, next);
        _logger.LogInformation(
            "FastClinicalRubricCatalog published version={Version} entries={Count}",
            next.Version, next.ById.Count);
    }

    public IReadOnlyList<AudioCaseSuggestedRubricModel> LookupByTokens(
        IEnumerable<string> tokens,
        int maxResults = 40)
    {
        maxResults = Math.Clamp(maxResults, 5, 100);
        var snap = _snapshot;
        if (snap.ById.Count == 0)
            return Array.Empty<AudioCaseSuggestedRubricModel>();

        var scores = new Dictionary<int, int>();
        foreach (var token in tokens)
        {
            var t = (token ?? string.Empty).Trim().ToLowerInvariant();
            if (t.Length < 3) continue;
            if (!snap.TokenIndex.TryGetValue(t, out var ids)) continue;
            foreach (var id in ids)
                scores[id] = scores.TryGetValue(id, out var s) ? s + 1 : 1;
        }

        return scores
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(maxResults)
            .Where(kv => snap.ById.ContainsKey(kv.Key))
            .Select(kv =>
            {
                var e = snap.ById[kv.Key];
                var conf = Math.Min(0.85m, 0.45m + kv.Value * 0.08m);
                var path = FastClinicalHierarchyParser.Parse(e.SubSectionName);
                return new AudioCaseSuggestedRubricModel
                {
                    SubSectionId = e.SubSectionId,
                    SubSectionName = e.SubSectionName,
                    SectionId = e.SectionId,
                    MatchScore = Math.Round(conf * 100m, 1),
                    ConfidenceScore = conf,
                    SuggestedIntensityNo = 2,
                    MatchSource = "CatalogToken",
                    DiscoveryMethod = "CatalogToken",
                    IsDbBacked = true,
                    WhySuggested = $"Catalog token hits={kv.Value}; ver={snap.Version}",
                    RequiresManualApproval = true,
                    IsAiSuggested = false,
                    HierarchyPath = path.Joined,
                    HierarchyDepth = path.Depth,
                };
            })
            .ToList();
    }

    private static void IndexNameTokens(Dictionary<string, List<int>> tokenIndex, int rubricId, string text)
    {
        var path = FastClinicalHierarchyParser.Parse(text);
        var parts = path.Segments.Count > 0
            ? path.Segments
            : new[] { text };

        foreach (var segment in parts)
        {
            foreach (var token in segment.Split(
                         new[] { ' ', ',', '/', '(', ')', '-', ';' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                var t = token.Trim().ToLowerInvariant();
                if (t.Length < 3) continue;
                if (!tokenIndex.TryGetValue(t, out var list))
                {
                    list = new List<int>();
                    tokenIndex[t] = list;
                }

                if (!list.Contains(rubricId))
                    list.Add(rubricId);
            }
        }
    }

    private sealed class CatalogEntry
    {
        public int SubSectionId { get; set; }
        public string SubSectionName { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public int? ParentSubSectionId { get; set; }
        public int? SectionId { get; set; }
        public bool DeleteStatus { get; set; }
        public List<string> Aliases { get; set; } = new();
    }

    private sealed class CatalogSnapshot
    {
        public static CatalogSnapshot Empty { get; } = new();

        public IReadOnlyDictionary<int, CatalogEntry> ById { get; init; } =
            new Dictionary<int, CatalogEntry>();

        public IReadOnlyDictionary<string, List<int>> TokenIndex { get; init; } =
            new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        public DateTime BuiltUtc { get; init; }
        public DateTime? SourceCacheUtc { get; init; }
        public string Version { get; init; } = "empty";
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Search;

/// <summary>V7: multi-stage candidate rubric search — never blind full-table scan.</summary>
public interface ICandidateRubricSearchService
{
    Task<List<V7RubricCandidate>> SearchAsync(
        V7ExtractedSymptom symptom,
        V7NormalizedConcept normalized,
        IReadOnlyList<V7VocabularyTerm> vocabulary,
        IReadOnlyList<string> synonymTerms,
        IReadOnlyList<string> ontologyTerms,
        V7RepertoryIntelligenceRequest request,
        CancellationToken cancellationToken = default);
}

public class CandidateRubricSearchService : ICandidateRubricSearchService
{
    private readonly NIGACentrumContext _context;
    private readonly ISubSectionRepository _subSectionRepository;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<CandidateRubricSearchService> _logger;

    public CandidateRubricSearchService(
        NIGACentrumContext context,
        ISubSectionRepository subSectionRepository,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<CandidateRubricSearchService> logger)
    {
        _context = context;
        _subSectionRepository = subSectionRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<V7RubricCandidate>> SearchAsync(
        V7ExtractedSymptom symptom,
        V7NormalizedConcept normalized,
        IReadOnlyList<V7VocabularyTerm> vocabulary,
        IReadOnlyList<string> synonymTerms,
        IReadOnlyList<string> ontologyTerms,
        V7RepertoryIntelligenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var candidates = new Dictionary<int, V7RubricCandidate>();
        var searchTerms = BuildSearchTerms(vocabulary, synonymTerms, ontologyTerms, normalized);

        foreach (var term in searchTerms.Take(20))
        {
            await AddExactMatchesAsync(candidates, term, symptom, cancellationToken);
            await AddTokenMatchesAsync(candidates, term, symptom, cancellationToken);
            await AddSynonymAliasMatchesAsync(candidates, term, symptom, cancellationToken);
        }

        await AddBootstrapMatchesAsync(candidates, symptom, normalized, cancellationToken);
        AddEmbeddingMatches(candidates, symptom, request);
        AddKnowledgeGraphMatches(candidates, symptom, request);

        var seedIds = candidates.Keys.ToList();
        if (seedIds.Count > 0)
        {
            await ExpandParentChildAsync(candidates, seedIds, symptom, cancellationToken);
            await ExpandCrossReferencesAsync(candidates, seedIds, symptom, cancellationToken);
        }

        return candidates.Values
            .OrderByDescending(c => c.RawScore)
            .Take(_options.V7EmbeddingTopK)
            .ToList();
    }

    private static IEnumerable<string> BuildSearchTerms(
        IReadOnlyList<V7VocabularyTerm> vocabulary,
        IReadOnlyList<string> synonymTerms,
        IReadOnlyList<string> ontologyTerms,
        V7NormalizedConcept normalized)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in vocabulary.OrderByDescending(v => v.Weight).Take(15))
        {
            terms.Add(v.Term);
        }

        foreach (var s in synonymTerms.Take(10))
        {
            terms.Add(s);
        }

        foreach (var o in ontologyTerms.Take(8))
        {
            terms.Add(o);
        }

        terms.Add(normalized.FinalConcept);
        return terms.Where(t => t.Length >= 3);
    }

    private async Task AddExactMatchesAsync(
        Dictionary<int, V7RubricCandidate> candidates,
        string term,
        V7ExtractedSymptom symptom,
        CancellationToken cancellationToken)
    {
        var rows = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null && s.SubSectionName == term)
            .Take(5)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            Upsert(candidates, row.SubSectionId, row.SubSectionName!, V7SearchStrategyNames.ExactMatch,
                $"Exact: '{term}'", 100m, term, symptom);
        }
    }

    private async Task AddTokenMatchesAsync(
        Dictionary<int, V7RubricCandidate> candidates,
        string term,
        V7ExtractedSymptom symptom,
        CancellationToken cancellationToken)
    {
        var tokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            var rows = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus && s.SubSectionName != null
                    && EF.Functions.Like(s.SubSectionName, $"%{token}%"))
                .OrderBy(s => s.SubSectionName!.Length)
                .Take(6)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var overlap = ComputeTokenOverlap(term, row.SubSectionName!);
                if (overlap < 0.3m)
                {
                    continue;
                }

                Upsert(candidates, row.SubSectionId, row.SubSectionName!, V7SearchStrategyNames.TokenMatch,
                    $"Token '{token}' overlap={overlap:P0}", 25m + overlap * 30m, token, symptom);
            }
        }
    }

    private async Task AddSynonymAliasMatchesAsync(
        Dictionary<int, V7RubricCandidate> candidates,
        string term,
        V7ExtractedSymptom symptom,
        CancellationToken cancellationToken)
    {
        var aliases = await _context.RubricAliases.AsNoTracking()
            .Where(a => a.IsActive && a.AliasText != null
                && (a.AliasText == term || EF.Functions.Like(a.AliasText, $"%{term}%")))
            .Take(8)
            .Select(a => new { a.SubSectionId, a.AliasText })
            .ToListAsync(cancellationToken);

        foreach (var alias in aliases)
        {
            var rubric = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => s.SubSectionId == alias.SubSectionId && !s.DeleteStatus)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .FirstOrDefaultAsync(cancellationToken);

            if (rubric?.SubSectionName != null)
            {
                Upsert(candidates, rubric.SubSectionId, rubric.SubSectionName, V7SearchStrategyNames.SynonymSearch,
                    $"Alias '{alias.AliasText}'", 40m, term, symptom);
            }
        }
    }

    private async Task AddBootstrapMatchesAsync(
        Dictionary<int, V7RubricCandidate> candidates,
        V7ExtractedSymptom symptom,
        V7NormalizedConcept normalized,
        CancellationToken cancellationToken)
    {
        var mappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings.Where(m =>
            ConceptGraphTierHelper.ConceptNamesMatch(normalized.FinalConcept, m.HomeopathicConceptPattern)
            || ConceptGraphTierHelper.ConceptNamesMatch(symptom.Normalized, m.HomeopathicConceptPattern)))
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
                Upsert(candidates, rubric.SubSectionId, rubric.SubSectionName!, V7SearchStrategyNames.BootstrapMapping,
                    $"Bootstrap: {mapping.HomeopathicConceptPattern}", 45m, normalized.FinalConcept, symptom);
            }
        }
    }

    private void AddEmbeddingMatches(
        Dictionary<int, V7RubricCandidate> candidates,
        V7ExtractedSymptom symptom,
        V7RepertoryIntelligenceRequest request)
    {
        var embeddingHits = request.EmbeddingCandidates
            .Where(c => c.SubSectionId > 0
                && (c.HomeopathicConceptId == symptom.SourceConceptId
                    || string.Equals(c.SourceConceptName, symptom.Normalized, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.SourceConceptName, symptom.Text, StringComparison.OrdinalIgnoreCase)))
            .Take(_options.V7EmbeddingTopK);

        foreach (var hit in embeddingHits)
        {
            var score = 35m * hit.CompositeScore;
            Upsert(candidates, hit.SubSectionId, hit.SubSectionName, V7SearchStrategyNames.EmbeddingSearch,
                $"Embedding: {hit.SourceConceptName} ({hit.CompositeScore:P0})", score, hit.SourceConceptName, symptom,
                hit.CompositeScore);
        }
    }

    private void AddKnowledgeGraphMatches(
        Dictionary<int, V7RubricCandidate> candidates,
        V7ExtractedSymptom symptom,
        V7RepertoryIntelligenceRequest request)
    {
        foreach (var path in request.KnowledgeGraphPaths.Where(p => p.SubSectionId > 0))
        {
            var pathText = string.Join(' ', path.Steps.Select(s => s.DisplayText));
            if (!pathText.Contains(symptom.Normalized, StringComparison.OrdinalIgnoreCase)
                && !pathText.Contains(symptom.Text, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Upsert(candidates, path.SubSectionId, path.SubSectionName, V7SearchStrategyNames.OntologySearch,
                $"KG: {string.Join(" → ", path.Steps.Take(3).Select(s => s.DisplayText))}",
                30m * Math.Max(path.PathConfidence, path.CompositeScore), symptom.Normalized, symptom);
        }
    }

    private async Task ExpandParentChildAsync(
        Dictionary<int, V7RubricCandidate> candidates,
        List<int> seedIds,
        V7ExtractedSymptom symptom,
        CancellationToken cancellationToken)
    {
        var seeds = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => seedIds.Contains(s.SubSectionId) && !s.DeleteStatus)
            .Select(s => new { s.SubSectionId, s.SubSectionName, s.ParentSubSectionId })
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            if (seed.ParentSubSectionId is > 0)
            {
                var parent = await _context.SubSectionMasters.AsNoTracking()
                    .Where(s => s.SubSectionId == seed.ParentSubSectionId && !s.DeleteStatus)
                    .Select(s => new { s.SubSectionId, s.SubSectionName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (parent?.SubSectionName != null)
                {
                    Upsert(candidates, parent.SubSectionId, parent.SubSectionName, V7SearchStrategyNames.ParentSearch,
                        $"Parent of '{seed.SubSectionName}'", 20m, seed.SubSectionName, symptom);
                }
            }

            var children = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => s.ParentSubSectionId == seed.SubSectionId && !s.DeleteStatus)
                .Take(5)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                Upsert(candidates, child.SubSectionId, child.SubSectionName!, V7SearchStrategyNames.ChildSearch,
                    $"Child of '{seed.SubSectionName}'", 15m, seed.SubSectionName, symptom);
            }
        }
    }

    private async Task ExpandCrossReferencesAsync(
        Dictionary<int, V7RubricCandidate> candidates,
        List<int> seedIds,
        V7ExtractedSymptom symptom,
        CancellationToken cancellationToken)
    {
        var refs = await _context.ReferenceRubricDetails.AsNoTracking()
            .Where(r => r.DeleteStatus != true
                && (seedIds.Contains(r.SubSectionId ?? 0) || seedIds.Contains(r.RefSubSectionId ?? 0)))
            .Take(20)
            .ToListAsync(cancellationToken);

        var targetIds = refs
            .SelectMany(r => new[] { r.SubSectionId, r.RefSubSectionId })
            .Where(id => id.HasValue && id > 0)
            .Select(id => id!.Value)
            .Distinct()
            .Where(id => !candidates.ContainsKey(id))
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
            Upsert(candidates, rubric.SubSectionId, rubric.SubSectionName!, V7SearchStrategyNames.CrossReference,
                "Cross-reference", 12m, null, symptom);
        }
    }

    private static void Upsert(
        Dictionary<int, V7RubricCandidate> candidates,
        int subSectionId,
        string name,
        string strategy,
        string path,
        decimal rawScore,
        string? matchedTerm,
        V7ExtractedSymptom symptom,
        decimal embeddingSimilarity = 0m)
    {
        if (subSectionId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!candidates.TryGetValue(subSectionId, out var existing) || rawScore > existing.RawScore)
        {
            candidates[subSectionId] = new V7RubricCandidate
            {
                SubSectionId = subSectionId,
                SubSectionName = name,
                SearchStrategy = strategy,
                MatchPath = path,
                RawScore = rawScore,
                MatchedTerm = matchedTerm,
                SourceSymptomConceptId = symptom.SourceConceptId,
                SourceSymptomText = symptom.Text,
                EmbeddingSimilarity = embeddingSimilarity,
            };
        }
    }

    private static decimal ComputeTokenOverlap(string query, string rubricName)
    {
        var queryTokens = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var rubricTokens = rubricName.ToLowerInvariant().Split(new[] { ' ', '-', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (queryTokens.Count == 0 || rubricTokens.Length == 0)
        {
            return 0m;
        }

        var matches = rubricTokens.Count(t => queryTokens.Contains(t));
        return (decimal)matches / Math.Max(queryTokens.Count, rubricTokens.Length);
    }
}

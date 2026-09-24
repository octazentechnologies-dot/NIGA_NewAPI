using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

public class RubricDiscoveryEngineV3 : IRubricDiscoveryEngineV3
{
    public const string ModelId = "v3-m5";
    public const string StageName = "RubricDiscovery";

    private readonly NIGACentrumContext _context;
    private readonly RubricIntelligenceOptions _options;
    private readonly IEmbeddingSearchEngine _embeddingEngine;

    public RubricDiscoveryEngineV3(
        NIGACentrumContext context,
        IOptions<RubricIntelligenceOptions> options,
        IEmbeddingSearchEngine embeddingEngine)
    {
        _context = context;
        _options = options.Value;
        _embeddingEngine = embeddingEngine;
    }

    public async Task<List<RubricDiscoveryNodeModel>> DiscoverAsync(
        IReadOnlyList<HomeopathicConceptNodeModel> homeopathicConcepts,
        CancellationToken cancellationToken = default)
    {
        if (homeopathicConcepts.Count == 0) return new List<RubricDiscoveryNodeModel>();

        var uniqueConcepts = DeduplicateConcepts(homeopathicConcepts);

        var mappings = await _context.AiConceptMappingBootstraps
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .Select(x => new AiConceptMappingBootstrapModel
            {
                HomeopathicConceptPattern = x.HomeopathicConceptPattern,
                SubSectionNamePattern = x.SubSectionNamePattern,
                Domain = x.Domain,
                PriorityOrder = x.PriorityOrder,
            })
            .ToListAsync(cancellationToken);

        var learned = (await _context.AiCaseLearnings
            .AsNoTracking()
            .GroupBy(x => new { x.FromConcept, x.ToRubricSubSectionId })
            .Select(g => new { g.Key.FromConcept, g.Key.ToRubricSubSectionId, Weight = g.Sum(x => x.WeightDelta) })
            .ToListAsync(cancellationToken))
            .Select(x => (x.FromConcept, x.ToRubricSubSectionId, x.Weight))
            .ToList();

        var discoveries = new Dictionary<int, RubricDiscoveryNodeModel>();
        var maxPerPattern = _options.EnableV35RecallEngine ? _options.MaxRubricsPerPattern : 5;
        var maxTotal = _options.EnableV35RecallEngine ? _options.MaxDiscoveryCandidates : 30;

        var orderedConcepts = uniqueConcepts
            .OrderByDescending(c => c.Weight * c.Confidence)
            .ToList();

        var patternCache = new Dictionary<string, List<(int SubSectionId, string SubSectionName)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var concept in orderedConcepts)
        {
            var matchedMappings = mappings
                .Where(m => ConceptMatches(concept.ConceptName, m.HomeopathicConceptPattern))
                .ToList();

            foreach (var mapping in matchedMappings)
            {
                if (!patternCache.TryGetValue(mapping.SubSectionNamePattern, out var rubrics))
                {
                    rubrics = await FindRubricsByPatternAsync(mapping.SubSectionNamePattern, maxPerPattern, cancellationToken);
                    patternCache[mapping.SubSectionNamePattern] = rubrics;
                }

                foreach (var rubric in rubrics)
                    AddDiscovery(discoveries, concept, rubric.SubSectionId, rubric.SubSectionName, mapping, learned);
            }

            foreach (var item in learned.Where(l =>
                l.ToRubricSubSectionId.HasValue && ConceptMatches(concept.ConceptName, l.FromConcept)))
            {
                if (!item.ToRubricSubSectionId.HasValue) continue;
                if (discoveries.ContainsKey(item.ToRubricSubSectionId.Value)) continue;

                var rubric = await _context.SubSectionMasters
                    .AsNoTracking()
                    .Where(s => s.SubSectionId == item.ToRubricSubSectionId && s.DeleteStatus != true)
                    .Select(s => new { s.SubSectionId, s.SubSectionName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (rubric == null || string.IsNullOrWhiteSpace(rubric.SubSectionName)) continue;

                var confidence = Math.Round(Math.Min(0.99m, 0.70m + item.Weight * 0.03m), 4);
                discoveries[rubric.SubSectionId] = new RubricDiscoveryNodeModel
                {
                    HomeopathicConceptId = concept.HomeopathicConceptId,
                    SubSectionId = rubric.SubSectionId,
                    SubSectionName = rubric.SubSectionName,
                    MatchReason = $"Learned mapping: {concept.ConceptName} → {rubric.SubSectionName}",
                    DiscoveryMethod = "SelfLearning",
                    Confidence = confidence,
                    RubricTier = ConceptGraphTierHelper.ResolveTier(confidence),
                };
            }
        }

        if (_options.EnableEmbeddingSearch)
            await MergeBatchedEmbeddingDiscoveriesAsync(orderedConcepts, discoveries, cancellationToken);

        return discoveries.Values
            .OrderByDescending(d => d.Confidence)
            .Take(maxTotal)
            .ToList();
    }

    private async Task MergeBatchedEmbeddingDiscoveriesAsync(
        IReadOnlyList<HomeopathicConceptNodeModel> orderedConcepts,
        Dictionary<int, RubricDiscoveryNodeModel> discoveries,
        CancellationToken cancellationToken)
    {
        var limit = _options.StrictConceptGatedDiscovery
            ? orderedConcepts.Count
            : _options.EnableV35FastPipeline
                ? _options.MaxEmbeddingConceptsPerPass
                : Math.Min(8, orderedConcepts.Count);

        var topConcepts = orderedConcepts.Take(Math.Max(1, limit)).ToList();
        if (topConcepts.Count == 0) return;

        var queryConcepts = topConcepts.Select(c => new ClinicalConceptModel
        {
            ConceptId = Guid.NewGuid(),
            ClinicalMeaning = c.ConceptName,
            HomeopathicMeaning = c.ConceptName,
            Category = c.SymptomClass ?? "General",
            Confidence = c.Confidence,
            HomeopathicWeight = c.Weight,
        }).ToList();

        var search = await _embeddingEngine.SearchAsync(queryConcepts, cancellationToken);
        if (search.Candidates.Count == 0) return;

        foreach (var candidate in search.Candidates.OrderByDescending(c => c.CosineScore).Take(limit * 2))
        {
            var matchedConcept = topConcepts.FirstOrDefault(c =>
                candidate.MatchedConceptText != null
                && (candidate.MatchedConceptText.Contains(c.ConceptName, StringComparison.OrdinalIgnoreCase)
                    || c.ConceptName.Contains(candidate.MatchedConceptText, StringComparison.OrdinalIgnoreCase)))
                ?? topConcepts[0];

            var confidence = Math.Round(Math.Min(0.92m, 0.60m + candidate.CosineScore * 0.35m), 4);
            var discovery = new RubricDiscoveryNodeModel
            {
                HomeopathicConceptId = matchedConcept.HomeopathicConceptId,
                SubSectionId = candidate.SubSectionId,
                SubSectionName = candidate.SubSectionName,
                MatchReason = $"Batched embedding: {matchedConcept.ConceptName} → {candidate.SubSectionName} (cosine={candidate.CosineScore:0.00})",
                DiscoveryMethod = "ScopedEmbedding",
                Confidence = confidence,
                RubricTier = ConceptGraphTierHelper.ResolveTier(confidence),
            };

            if (discoveries.TryGetValue(candidate.SubSectionId, out var existing))
            {
                if (confidence > existing.Confidence)
                    discoveries[candidate.SubSectionId] = discovery;
            }
            else
            {
                discoveries[candidate.SubSectionId] = discovery;
            }
        }
    }

    private void AddDiscovery(
        Dictionary<int, RubricDiscoveryNodeModel> discoveries,
        HomeopathicConceptNodeModel concept,
        int subSectionId,
        string subSectionName,
        AiConceptMappingBootstrapModel mapping,
        IReadOnlyList<(string FromConcept, int? ToRubricSubSectionId, decimal Weight)> learned)
    {
        var confidence = Math.Round(
            Math.Min(0.98m, 0.55m + (concept.Confidence * 0.25m) + (concept.Weight * 0.05m)),
            4);

        var learnedBoost = learned
            .Where(l => l.ToRubricSubSectionId == subSectionId
                && ConceptMatches(concept.ConceptName, l.FromConcept))
            .Sum(l => l.Weight);
        if (learnedBoost > 0) confidence = Math.Min(0.99m, confidence + learnedBoost * 0.02m);

        if (discoveries.TryGetValue(subSectionId, out var existing))
        {
            if (confidence > existing.Confidence)
                discoveries[subSectionId] = BuildDiscovery(concept, subSectionId, subSectionName, mapping, confidence);
        }
        else
        {
            discoveries[subSectionId] = BuildDiscovery(concept, subSectionId, subSectionName, mapping, confidence);
        }
    }

    private static RubricDiscoveryNodeModel BuildDiscovery(
        HomeopathicConceptNodeModel concept,
        int subSectionId,
        string subSectionName,
        AiConceptMappingBootstrapModel mapping,
        decimal confidence) =>
        new()
        {
            HomeopathicConceptId = concept.HomeopathicConceptId,
            SubSectionId = subSectionId,
            SubSectionName = subSectionName,
            MatchReason = $"Concept '{concept.ConceptName}' → pattern '{mapping.SubSectionNamePattern}'",
            DiscoveryMethod = "ConceptMapping",
            Confidence = confidence,
            RubricTier = ConceptGraphTierHelper.ResolveTier(confidence),
        };

    private async Task<List<(int SubSectionId, string SubSectionName)>> FindRubricsByPatternAsync(
        string pattern,
        int take,
        CancellationToken cancellationToken)
    {
        var likePattern = pattern.Replace('*', '%');
        if (!likePattern.Contains('%')) likePattern = $"%{likePattern}%";

        return await _context.SubSectionMasters
            .AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null
                && EF.Functions.Like(s.SubSectionName, likePattern))
            .OrderBy(s => s.SubSectionName)
            .Take(take)
            .Select(s => new ValueTuple<int, string>(s.SubSectionId, s.SubSectionName!))
            .ToListAsync(cancellationToken);
    }

    private static List<HomeopathicConceptNodeModel> DeduplicateConcepts(
        IReadOnlyList<HomeopathicConceptNodeModel> concepts) =>
        concepts
            .Where(c => !string.IsNullOrWhiteSpace(c.ConceptName))
            .GroupBy(c => c.ConceptName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(c => c.Weight * c.Confidence).First())
            .ToList();

    private static bool ConceptMatches(string conceptName, string pattern) =>
        ConceptGraphTierHelper.ConceptNamesMatch(conceptName, pattern);
}

public class ConceptGraphEvidenceEngine : IConceptGraphEvidenceEngine
{
    public List<RubricDiscoveryNodeModel> BuildEvidenceChains(
        IReadOnlyList<RubricDiscoveryNodeModel> discoveries,
        ConceptGraphFullModel graph)
    {
        return discoveries.Select(d =>
        {
            var (homeo, clinical, meaning) = ResolveStrictChain(d, graph);

            var chain = new RubricEvidenceChainV3Model
            {
                TranscriptStatement = meaning?.RawStatement ?? homeo?.EvidenceSpan ?? d.MatchReason,
                PatientMeaning = meaning?.NormalizedMeaning ?? clinical?.ConceptName ?? homeo?.ConceptName,
                ClinicalConcept = clinical?.ConceptName ?? homeo?.ConceptName,
                HomeopathicConcept = homeo?.ConceptName ?? ExtractHomeopathicHint(d),
                RubricName = d.SubSectionName,
                Confidence = d.Confidence,
                IsComplete = meaning != null && clinical != null && homeo != null
                    && !string.IsNullOrWhiteSpace(d.SubSectionName),
            };

            // Keep transcript and normalized meaning aligned to the same source statement when possible.
            if (meaning != null && !string.IsNullOrWhiteSpace(meaning.RawStatement))
            {
                chain.TranscriptStatement = meaning.RawStatement;
                chain.PatientMeaning = meaning.NormalizedMeaning ?? meaning.RawStatement;
            }

            if (!chain.IsComplete && !string.IsNullOrWhiteSpace(homeo?.EvidenceSpan))
            {
                chain.TranscriptStatement = homeo.EvidenceSpan;
                chain.PatientMeaning ??= homeo.EvidenceSpan;
                chain.IsComplete = clinical != null && homeo != null && !string.IsNullOrWhiteSpace(d.SubSectionName);
            }

            // Literal repertory hits often arrive with SubSectionName + MatchReason but sparse graph links.
            // Complete the chain from discovery fields so valid DB matches are not killed by the gate.
            if (!chain.IsComplete
                && d.SubSectionId > 0
                && !string.IsNullOrWhiteSpace(d.SubSectionName)
                && (!string.IsNullOrWhiteSpace(chain.HomeopathicConcept)
                    || !string.IsNullOrWhiteSpace(d.MatchReason)
                    || !string.IsNullOrWhiteSpace(chain.TranscriptStatement)))
            {
                chain.TranscriptStatement ??= d.MatchReason;
                chain.PatientMeaning ??= chain.TranscriptStatement;
                chain.ClinicalConcept ??= chain.HomeopathicConcept ?? d.SubSectionName;
                chain.HomeopathicConcept ??= d.SubSectionName;
                chain.IsComplete = !string.IsNullOrWhiteSpace(chain.TranscriptStatement)
                    && !string.IsNullOrWhiteSpace(chain.HomeopathicConcept)
                    && !string.IsNullOrWhiteSpace(chain.RubricName);
            }

            d.EvidenceChain = chain;
            return d;
        }).ToList();
    }

    /// <summary>
    /// Strict homeopathicConceptId → clinical → meaning traversal. Fuzzy fallbacks only when ID is absent.
    /// </summary>
    public static (HomeopathicConceptNodeModel? Homeo, ClinicalConceptNodeModel? Clinical, PatientMeaningNodeModel? Meaning)
        ResolveStrictChain(RubricDiscoveryNodeModel discovery, ConceptGraphFullModel graph)
    {
        HomeopathicConceptNodeModel? homeo = null;

        if (discovery.HomeopathicConceptId is > 0)
        {
            homeo = graph.HomeopathicConcepts
                .FirstOrDefault(h => h.HomeopathicConceptId == discovery.HomeopathicConceptId);
        }

        if (homeo == null
            && !string.IsNullOrWhiteSpace(discovery.MatchReason)
            && discovery.HomeopathicConceptId is null or <= 0)
        {
            // Strict: only accept when MatchReason equals/starts with the concept name — never substring Contains
            // (prevents "fear" MatchReason latching onto an unrelated longer concept label).
            homeo = graph.HomeopathicConcepts.FirstOrDefault(h =>
                !string.IsNullOrWhiteSpace(h.ConceptName)
                && (string.Equals(discovery.MatchReason, h.ConceptName, StringComparison.OrdinalIgnoreCase)
                    || discovery.MatchReason!.StartsWith(h.ConceptName + " ", StringComparison.OrdinalIgnoreCase)
                    || discovery.MatchReason.EndsWith(": " + h.ConceptName, StringComparison.OrdinalIgnoreCase)));
        }

        ClinicalConceptNodeModel? clinical = null;
        PatientMeaningNodeModel? meaning = null;

        if (homeo != null)
        {
            clinical = homeo.ClinicalConceptIndex >= 0
                ? graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                : null;
            clinical ??= graph.ClinicalConcepts.FirstOrDefault(c =>
                c.ClinicalConceptId == homeo.ClinicalConceptId);

            if (clinical != null)
            {
                meaning = clinical.MeaningIndex >= 0
                    ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                    : null;
                meaning ??= clinical.PatientMeaningId > 0
                    ? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                    : null;
            }
        }

        return (homeo, clinical, meaning);
    }

    private static string? ExtractHomeopathicHint(RubricDiscoveryNodeModel d)
    {
        if (!string.IsNullOrWhiteSpace(d.MatchReason))
            return d.MatchReason;
        return null;
    }
}

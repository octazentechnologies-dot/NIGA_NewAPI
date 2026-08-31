using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AiEmbeddingInfrastructure;
using Niga_Domain.Services.AudioCaseIntelligence;
using Niga_Domain.Services.AudioCaseIntelligence.Learning;

namespace Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

public class RubricCandidateEngine : IRubricCandidateEngine
{
    public const string ModelId = "v6-rc";
    public const string StageName = "RubricCandidateEngine";

    private readonly NIGACentrumContext _context;
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly RubricIntelligenceOptions _intelligenceOptions;
    private readonly AiEmbeddingInfrastructureOptions _embeddingOptions;
    private readonly IAiEmbeddingVersionService _versionService;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IAiConceptEmbeddingMemoryCache _conceptCache;
    private readonly IAiEnterpriseRubricEmbeddingMemoryCache _rubricCache;
    private readonly IAiEmbeddingSemanticCacheReadiness _cacheReadiness;
    private readonly EnterpriseConceptToRubricMapper _rubricMapper;
    private readonly IDoctorLearningWeightProvider _learningWeights;
    private readonly ILogger<RubricCandidateEngine> _logger;

    public RubricCandidateEngine(
        NIGACentrumContext context,
        IAiEmbeddingUnitOfWork unitOfWork,
        IOptions<RubricIntelligenceOptions> intelligenceOptions,
        IOptions<AiEmbeddingInfrastructureOptions> embeddingOptions,
        IAiEmbeddingVersionService versionService,
        IEmbeddingClient embeddingClient,
        IAiConceptEmbeddingMemoryCache conceptCache,
        IAiEnterpriseRubricEmbeddingMemoryCache rubricCache,
        IAiEmbeddingSemanticCacheReadiness cacheReadiness,
        EnterpriseConceptToRubricMapper rubricMapper,
        IDoctorLearningWeightProvider learningWeights,
        ILogger<RubricCandidateEngine> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _intelligenceOptions = intelligenceOptions.Value;
        _embeddingOptions = embeddingOptions.Value;
        _versionService = versionService;
        _embeddingClient = embeddingClient;
        _conceptCache = conceptCache;
        _rubricCache = rubricCache;
        _cacheReadiness = cacheReadiness;
        _rubricMapper = rubricMapper;
        _learningWeights = learningWeights;
        _logger = logger;
    }

    public async Task<RubricCandidateEngineResult> DiscoverFromGraphAsync(
        ConceptGraphFullModel graph,
        RubricCandidateEngineRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        request ??= new RubricCandidateEngineRequest();
        var result = new RubricCandidateEngineResult();

        if (graph.HomeopathicConcepts.Count == 0)
        {
            result.Error = "Concept graph has no homeopathic concepts.";
            return result;
        }

        if (!_embeddingOptions.Enabled || !_embeddingOptions.EnableEnterpriseSemanticSearch)
        {
            result.Error = "Enterprise semantic search is disabled.";
            return result;
        }

        if (!_embeddingClient.IsConfigured)
        {
            result.Error = "Embedding API is not configured.";
            return result;
        }

        var version = await ResolveVersionAsync(request.EmbeddingVersionId, cancellationToken);
        if (version == null)
        {
            result.Error = "No embedding version is available.";
            return result;
        }

        result.EmbeddingVersionId = version.EmbeddingVersionId;
        result.VersionCode = version.VersionCode;

        await EnsureCachesAsync(version.EmbeddingVersionId, cancellationToken);
        if (_conceptCache.Entries.Count == 0)
        {
            result.Error = "No AIConceptEmbedding vectors are indexed for the current version.";
            return result;
        }

        var learnedSnapshot = await _learningWeights.LoadAsync(cancellationToken);
        var acceptanceRates = learnedSnapshot.RubricAcceptanceRates.Count > 0
            ? learnedSnapshot.RubricAcceptanceRates
            : await LoadDoctorAcceptanceRatesAsync(cancellationToken);
        var learnedBoosts = learnedSnapshot.ConceptRubricMapping.Count > 0
            ? learnedSnapshot.ConceptRubricMapping
            : await LoadLearnedBoostsAsync(cancellationToken);
        var maxPerConcept = request.MaxCandidatesPerConcept ?? _intelligenceOptions.MaxRubricsPerPattern;
        var maxTotal = request.MaxTotalCandidates ?? _intelligenceOptions.MaxDiscoveryCandidates;

        var orderedConcepts = graph.HomeopathicConcepts
            .Where(c => !string.IsNullOrWhiteSpace(c.ConceptName))
            .Select(c =>
            {
                var baseScore = TierBoost(c.ConceptTier) * c.Weight * c.Confidence;
                var learnedScore = DoctorLearningScoring.ApplyConceptRankingBoost(
                    baseScore,
                    c.ConceptName,
                    learnedSnapshot.ConceptRanking,
                    _intelligenceOptions);
                return (Concept: c, Score: learnedScore);
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Concept)
            .ToList();

        // StrictConceptGatedDiscovery needs every concept embedded; otherwise cap for latency.
        if (!_intelligenceOptions.StrictConceptGatedDiscovery
            && orderedConcepts.Count > _intelligenceOptions.MaxEmbeddingConceptsPerPass)
        {
            orderedConcepts = orderedConcepts
                .Take(_intelligenceOptions.MaxEmbeddingConceptsPerPass)
                .ToList();
        }

        var queryTexts = orderedConcepts
            .Select(homeo =>
            {
                var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                    ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
                var meaning = clinical != null
                    ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                        ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                    : null;
                return ClinicalConceptQueryBuilder.BuildFromGraphConcept(homeo, clinical, meaning);
            })
            .ToList();

        var embedResult = await _embeddingClient.EmbedTextsAsync(
            queryTexts,
            version.ModelName,
            cancellationToken);

        if (!embedResult.Success || embedResult.Vectors.Count != orderedConcepts.Count)
        {
            result.Error = embedResult.Error ?? "Failed to embed concept graph queries.";
            return result;
        }

        var candidateMap = new Dictionary<int, RubricCandidateModel>();

        for (var i = 0; i < orderedConcepts.Count; i++)
        {
            var homeo = orderedConcepts[i];
            var queryVector = embedResult.Vectors[i];
            var conceptMatches = EnterpriseEmbeddingVectorSearch.TopConceptMatches(
                queryVector,
                _conceptCache.Entries,
                _embeddingOptions.SemanticSearchTopConcepts,
                _embeddingOptions.MinConceptCosineScore);

            var mappedConcepts = conceptMatches
                .Select((match, index) => MapConceptMatch(match.Entry, match.Score, match.RawCosine, index + 1))
                .ToList();

            if (mappedConcepts.Count == 0)
                continue;

            var rubricMatches = _rubricCache.Entries.Count > 0
                ? await _rubricMapper.MapAsync(
                    queryVector,
                    mappedConcepts,
                    _rubricCache.Entries,
                    request.IncludeValidation,
                    cancellationToken)
                : MapLinkedRubricsOnly(mappedConcepts, acceptanceRates, learnedBoosts, homeo, graph, request.IncludeValidation);

            foreach (var rubric in rubricMatches.Take(maxPerConcept))
            {
                var candidate = BuildCandidate(
                    homeo,
                    graph,
                    rubric,
                    acceptanceRates,
                    learnedBoosts,
                    learnedSnapshot,
                    mappedConcepts.FirstOrDefault(c =>
                        string.Equals(c.ConceptKey, rubric.MappedFromConceptKey, StringComparison.OrdinalIgnoreCase)));

                if (candidateMap.TryGetValue(candidate.SubSectionId, out var existing))
                {
                    if (candidate.CompositeScore > existing.CompositeScore)
                        candidateMap[candidate.SubSectionId] = candidate;
                }
                else
                {
                    candidateMap[candidate.SubSectionId] = candidate;
                }
            }
        }

        var ranked = candidateMap.Values
            .OrderByDescending(c => c.CompositeScore)
            .Take(maxTotal)
            .Select((candidate, index) =>
            {
                candidate.Rank = index + 1;
                return candidate;
            })
            .ToList();

        RubricCandidateTierAssigner.Assign(ranked, _intelligenceOptions);

        result.Candidates = ranked;
        result.Tier1Rubrics = ranked.Where(c => c.RubricTier == "Tier1").ToList();
        result.Tier2Rubrics = ranked.Where(c => c.RubricTier == "Tier2").ToList();
        result.Tier3Rubrics = ranked.Where(c => c.RubricTier == "Tier3").ToList();
        result.Discoveries = ranked.Select(ToDiscoveryNode).ToList();
        result.Success = ranked.Count > 0;

        if (!result.Success)
            result.Error = "No rubric candidates met the minimum scoring threshold.";

        _logger.LogInformation(
            "Rubric candidate engine session graph: concepts={ConceptCount} candidates={CandidateCount} T1={T1} T2={T2} T3={T3}",
            orderedConcepts.Count,
            ranked.Count,
            result.Tier1Rubrics.Count,
            result.Tier2Rubrics.Count,
            result.Tier3Rubrics.Count);

        return result;
    }

    private RubricCandidateModel BuildCandidate(
        HomeopathicConceptNodeModel homeo,
        ConceptGraphFullModel graph,
        EnterpriseSemanticRubricMatchModel rubric,
        IReadOnlyDictionary<int, decimal> acceptanceRates,
        IReadOnlyDictionary<(string Concept, int RubricId), decimal> learnedBoosts,
        DoctorLearningWeightsSnapshot learnedSnapshot,
        EnterpriseSemanticConceptMatchModel? conceptMatch)
    {
        var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
            ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
        var meaning = clinical != null && clinical.MeaningIndex >= 0
            ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
            : graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical?.PatientMeaningId);

        var evidenceChain = new RubricEvidenceChainV3Model
        {
            TranscriptStatement = meaning?.RawStatement ?? homeo.EvidenceSpan,
            PatientMeaning = meaning?.NormalizedMeaning,
            ClinicalConcept = clinical?.ConceptName,
            HomeopathicConcept = homeo.ConceptName,
            RubricName = rubric.SubSectionName,
            Confidence = rubric.CombinedScore,
            IsComplete = meaning != null && clinical != null && !string.IsNullOrWhiteSpace(rubric.SubSectionName),
        };

        var similarity = rubric.CombinedScore;
        var clinicalRelevance = RubricCandidateScoring.ComputeClinicalRelevance(
            homeo,
            clinical,
            rubric.Validation,
            learnedSnapshot,
            _intelligenceOptions);
        var evidence = RubricCandidateScoring.ComputeEvidenceScore(evidenceChain, conceptMatch);
        var doctorAcceptance = DoctorLearningScoring.ApplyDoctorAcceptanceBoost(
            acceptanceRates.GetValueOrDefault(rubric.SubSectionId, 0.50m),
            homeo.ConceptName,
            rubric.SubSectionId,
            learnedSnapshot,
            _intelligenceOptions);

        if (learnedBoosts.TryGetValue((homeo.ConceptName, rubric.SubSectionId), out var learned))
            doctorAcceptance = Math.Min(1m, doctorAcceptance + learned * 0.05m);

        var composite = RubricCandidateScoring.ComputeCompositeScore(
            similarity,
            clinicalRelevance,
            evidence,
            doctorAcceptance);

        return new RubricCandidateModel
        {
            SubSectionId = rubric.SubSectionId,
            SubSectionName = rubric.SubSectionName,
            HomeopathicConceptId = homeo.HomeopathicConceptId,
            ClinicalConceptIndex = homeo.ClinicalConceptIndex,
            SourceConceptName = homeo.ConceptName,
            SourceCategory = homeo.Category ?? clinical?.SymptomCategory,
            SimilarityScore = Math.Round(similarity, 4),
            ClinicalRelevanceScore = Math.Round(clinicalRelevance, 4),
            EvidenceScore = Math.Round(evidence, 4),
            DoctorAcceptanceScore = Math.Round(doctorAcceptance, 4),
            CompositeScore = Math.Round(composite, 4),
            MatchMethod = rubric.MatchMethod,
            MatchReason = $"AIConceptEmbedding: {homeo.ConceptName} → {rubric.SubSectionName} (sim={similarity:0.00}, rel={clinicalRelevance:0.00}, ev={evidence:0.00}, doc={doctorAcceptance:0.00})",
            MappedFromConceptKey = rubric.MappedFromConceptKey,
            EvidenceChain = evidenceChain,
            Validation = rubric.Validation,
        };
    }

    private static List<EnterpriseSemanticRubricMatchModel> MapLinkedRubricsOnly(
        IReadOnlyList<EnterpriseSemanticConceptMatchModel> concepts,
        IReadOnlyDictionary<int, decimal> acceptanceRates,
        IReadOnlyDictionary<(string Concept, int RubricId), decimal> learnedBoosts,
        HomeopathicConceptNodeModel homeo,
        ConceptGraphFullModel graph,
        bool includeValidation)
    {
        var results = new List<EnterpriseSemanticRubricMatchModel>();
        foreach (var concept in concepts)
        {
            foreach (var rubricId in concept.LinkedRubricIds.Distinct())
            {
                var score = Math.Round(concept.SimilarityScore * 0.85m, 4);
                results.Add(new EnterpriseSemanticRubricMatchModel
                {
                    SubSectionId = rubricId,
                    SubSectionName = $"Rubric {rubricId}",
                    MappedFromConceptKey = concept.ConceptKey,
                    ConceptSimilarityScore = concept.SimilarityScore,
                    RubricSimilarityScore = score,
                    CombinedScore = score,
                    MatchMethod = "LinkedConceptRubric",
                });
            }
        }

        return results
            .OrderByDescending(x => x.CombinedScore)
            .ToList();
    }

    private static RubricDiscoveryNodeModel ToDiscoveryNode(RubricCandidateModel candidate) =>
        new()
        {
            HomeopathicConceptId = candidate.HomeopathicConceptId,
            SubSectionId = candidate.SubSectionId,
            SubSectionName = candidate.SubSectionName,
            MatchReason = candidate.MatchReason,
            DiscoveryMethod = candidate.MatchMethod,
            Confidence = candidate.CompositeScore,
            RubricTier = candidate.RubricTier,
            EvidenceChain = candidate.EvidenceChain,
            QualityScore = Math.Round(candidate.CompositeScore * 100m, 2),
        };

    private async Task EnsureCachesAsync(Guid embeddingVersionId, CancellationToken cancellationToken)
    {
        if (!_cacheReadiness.IsReady)
        {
            // Gap 1 follow-on: never block another 20 minutes here — short wait then proceed degraded.
            _logger.LogInformation("Rubric candidate engine: cache not ready; waiting up to 30s then proceeding.");
            try
            {
                await _cacheReadiness.WaitUntilReadyAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Rubric candidate engine: cache still not ready after 30s — skipping embedding refresh.");
                return;
            }
        }

        var conceptExpired = _conceptCache.LastRefreshedUtc == null
            || _conceptCache.EmbeddingVersionId != embeddingVersionId
            || DateTime.UtcNow - _conceptCache.LastRefreshedUtc.Value > TimeSpan.FromMinutes(_embeddingOptions.ConceptEmbeddingCacheMinutes);

        if (conceptExpired || _conceptCache.Entries.Count == 0)
            await _conceptCache.RefreshAsync(embeddingVersionId, cancellationToken);

        var rubricExpired = _rubricCache.LastRefreshedUtc == null
            || _rubricCache.EmbeddingVersionId != embeddingVersionId
            || DateTime.UtcNow - _rubricCache.LastRefreshedUtc.Value > TimeSpan.FromMinutes(_embeddingOptions.RubricEmbeddingCacheMinutes);

        if (rubricExpired || _rubricCache.Entries.Count == 0)
            await _rubricCache.RefreshAsync(embeddingVersionId, cancellationToken);
    }

    private async Task<Dictionary<int, decimal>> LoadDoctorAcceptanceRatesAsync(CancellationToken cancellationToken)
    {
        var stats = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .Where(x => x.SubSectionId.HasValue)
            .GroupBy(x => x.SubSectionId!.Value)
            .Select(g => new
            {
                SubSectionId = g.Key,
                Accepted = g.Count(x => x.FeedbackType == "Accepted"),
                Total = g.Count(),
            })
            .ToListAsync(cancellationToken);

        return stats.ToDictionary(
            x => x.SubSectionId,
            x => x.Total == 0 ? 0.50m : Math.Round((decimal)x.Accepted / x.Total, 4));
    }

    private async Task<Dictionary<(string Concept, int RubricId), decimal>> LoadLearnedBoostsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _context.AiCaseLearnings.AsNoTracking()
            .Where(x => x.ToRubricSubSectionId.HasValue)
            .GroupBy(x => new { x.FromConcept, x.ToRubricSubSectionId })
            .Select(g => new
            {
                g.Key.FromConcept,
                RubricId = g.Key.ToRubricSubSectionId!.Value,
                Weight = g.Sum(x => x.WeightDelta),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => (x.FromConcept, x.RubricId),
            x => x.Weight);
    }

    private async Task<AiEmbeddingVersionModel?> ResolveVersionAsync(
        Guid? requestedVersionId,
        CancellationToken cancellationToken)
    {
        if (requestedVersionId.HasValue)
        {
            var entity = await _unitOfWork.Versions.GetByIdAsync(requestedVersionId.Value, cancellationToken);
            if (entity == null) return null;
            return new AiEmbeddingVersionModel
            {
                EmbeddingVersionId = entity.EmbeddingVersionId,
                VersionCode = entity.VersionCode,
                ModelName = entity.ModelName,
            };
        }

        return await _versionService.GetCurrentVersionAsync(cancellationToken);
    }

    private static EnterpriseSemanticConceptMatchModel MapConceptMatch(
        AiConceptEmbeddingCacheEntry entry,
        decimal score,
        float rawCosine,
        int rank)
    {
        var parsed = ConceptSemanticDocumentParser.Parse(entry.SourceText);
        return new EnterpriseSemanticConceptMatchModel
        {
            Rank = rank,
            ConceptKey = entry.ConceptKey,
            ConceptType = entry.ConceptType,
            SimilarityScore = score,
            RawCosine = rawCosine,
            MatchedSynonyms = parsed.KnownSynonyms,
            MatchedClinicalMeaning = parsed.ClinicalConcept ?? entry.ConceptKey,
            MatchedHomeopathicMeaning = parsed.HomeopathicConcept,
            LinkedRubricIds = parsed.LinkedRubricIds.Count > 0 ? parsed.LinkedRubricIds : entry.LinkedRubricIds,
        };
    }

    private static decimal TierBoost(string? conceptTier) =>
        conceptTier switch
        {
            ConceptTierLabels.Primary => 1.0m,
            ConceptTierLabels.Secondary => 0.85m,
            _ => 0.70m,
        };
}

public static class RubricCandidateScoring
{
    public static decimal ComputeClinicalRelevance(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        EnterpriseSemanticRubricValidationModel? validation,
        DoctorLearningWeightsSnapshot? learnedSnapshot = null,
        RubricIntelligenceOptions? options = null)
    {
        var baseScore = Math.Clamp(homeo.Weight * homeo.Confidence / 3m, 0m, 1m);
        if (homeo.IsSRP) baseScore = Math.Min(1m, baseScore + 0.10m);
        if (string.Equals(homeo.ConceptTier, ConceptTierLabels.Primary, StringComparison.OrdinalIgnoreCase))
            baseScore = Math.Min(1m, baseScore + 0.08m);
        else if (string.Equals(homeo.ConceptTier, ConceptTierLabels.Secondary, StringComparison.OrdinalIgnoreCase))
            baseScore = Math.Min(1m, baseScore + 0.04m);

        if (validation?.ValidationScore > 0)
            baseScore = Math.Min(1m, (baseScore * 0.7m) + (validation.ValidationScore * 0.3m));

        if (learnedSnapshot != null && options != null)
        {
            baseScore = DoctorLearningScoring.ApplyClinicalRelevanceBoost(
                baseScore,
                clinical?.ConceptName,
                homeo.ConceptName,
                learnedSnapshot.ClinicalRelevance,
                options);
        }

        return Math.Clamp(baseScore, 0m, 1m);
    }

    public static decimal ComputeEvidenceScore(
        RubricEvidenceChainV3Model chain,
        EnterpriseSemanticConceptMatchModel? conceptMatch)
    {
        var score = 0.35m;
        if (!string.IsNullOrWhiteSpace(chain.TranscriptStatement)) score += 0.20m;
        if (!string.IsNullOrWhiteSpace(chain.PatientMeaning)) score += 0.15m;
        if (!string.IsNullOrWhiteSpace(chain.ClinicalConcept)) score += 0.15m;
        if (!string.IsNullOrWhiteSpace(chain.HomeopathicConcept)) score += 0.10m;
        if (chain.IsComplete) score += 0.10m;
        if (conceptMatch?.MatchedSynonyms.Count > 0) score += 0.05m;
        return Math.Clamp(score, 0m, 1m);
    }

    public static decimal ComputeCompositeScore(
        decimal similarity,
        decimal clinicalRelevance,
        decimal evidence,
        decimal doctorAcceptance) =>
        Math.Clamp(
            (similarity * 0.35m)
            + (clinicalRelevance * 0.30m)
            + (evidence * 0.20m)
            + (doctorAcceptance * 0.15m),
            0m,
            1m);
}

public static class RubricCandidateTierAssigner
{
    public static void Assign(IList<RubricCandidateModel> candidates, RubricIntelligenceOptions options)
    {
        foreach (var candidate in candidates)
        {
            candidate.RubricTier = candidate.CompositeScore >= 0.90m ? "Tier1"
                : candidate.CompositeScore >= 0.75m ? "Tier2"
                : candidate.CompositeScore >= 0.60m ? "Tier3"
                : "BelowThreshold";
        }

        var tier1 = candidates.Where(c => c.RubricTier == "Tier1").Take(options.MaxRubricsTier1).Select(c => c.SubSectionId).ToHashSet();
        var tier2 = candidates.Where(c => c.RubricTier == "Tier2" && !tier1.Contains(c.SubSectionId)).Take(options.MaxRubricsTier2).Select(c => c.SubSectionId).ToHashSet();

        foreach (var candidate in candidates)
        {
            if (tier1.Contains(candidate.SubSectionId))
                candidate.RubricTier = "Tier1";
            else if (tier2.Contains(candidate.SubSectionId))
                candidate.RubricTier = "Tier2";
            else if (candidate.CompositeScore >= 0.60m)
                candidate.RubricTier = "Tier3";
            else
                candidate.RubricTier = "BelowThreshold";
        }
    }
}

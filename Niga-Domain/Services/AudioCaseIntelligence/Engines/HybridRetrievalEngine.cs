using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services;
using Niga_Domain.Services.AudioCaseIntelligence;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class HybridRetrievalEngine : IHybridRetrievalEngine
{
    private readonly RubricIntelligenceOptions _options;
    private readonly IEmbeddingSearchEngine _embeddingSearchEngine;
    private readonly IConfidenceScoringEngine _confidenceScoringEngine;
    private readonly ILogger<HybridRetrievalEngine> _logger;

    public HybridRetrievalEngine(
        IOptions<RubricIntelligenceOptions> options,
        IEmbeddingSearchEngine embeddingSearchEngine,
        IConfidenceScoringEngine confidenceScoringEngine,
        ILogger<HybridRetrievalEngine> logger)
    {
        _options = options.Value;
        _embeddingSearchEngine = embeddingSearchEngine;
        _confidenceScoringEngine = confidenceScoringEngine;
        _logger = logger;
    }

    public async Task<HybridRetrievalResult> RetrieveAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        IReadOnlyList<AudioCaseSuggestedRubricModel> aliasRubrics,
        CancellationToken cancellationToken = default)
    {
        var weights = _options.HybridWeights;
        var embeddingResult = await _embeddingSearchEngine.SearchAsync(concepts, cancellationToken);

        if (embeddingResult.UsedFallback)
        {
            _logger.LogInformation("Hybrid retrieval used Jaccard fallback for embeddings.");
        }

        var aliasById = aliasRubrics
            .Where(r => r.SubSectionId > 0)
            .GroupBy(r => r.SubSectionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.ConfidenceScore ?? x.MatchScore).First());

        var embeddingById = embeddingResult.Candidates
            .GroupBy(c => c.SubSectionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CosineScore).First());

        var rubricIds = aliasById.Keys.Union(embeddingById.Keys).ToHashSet();
        var merged = new List<AudioCaseSuggestedRubricModel>();

        foreach (var rubricId in rubricIds)
        {
            aliasById.TryGetValue(rubricId, out var aliasRubric);
            embeddingById.TryGetValue(rubricId, out var embeddingCandidate);

            var subSectionName = aliasRubric?.SubSectionName
                ?? embeddingCandidate?.SubSectionName
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(subSectionName)) continue;

            var embeddingScore = embeddingCandidate?.CosineScore ?? 0m;
            if (embeddingCandidate != null && embeddingScore < _options.MinEmbeddingCosineForCandidate)
            {
                embeddingCandidate = null;
                embeddingScore = 0m;
            }

            var aliasScore = aliasRubric?.ConfidenceScore ?? aliasRubric?.MatchScore ?? 0m;
            var clinicalScore = ComputeClinicalScore(concepts, subSectionName);
            var keywordScore = ComputeKeywordScore(concepts, symptoms, subSectionName);
            var domainScore = ComputeDomainAwareScore(concepts, subSectionName);

            // Reject clinically wrong embedding neighbors (e.g. MIND-DESIRES for thirst).
            if (domainScore < 0.25m && embeddingScore > 0 && aliasScore < 0.4m)
                continue;

            var hybridScore = Math.Round(
                (weights.Embedding * embeddingScore)
                + (weights.Alias * aliasScore)
                + (weights.ClinicalMeaning * clinicalScore)
                + (weights.KeywordLike * keywordScore)
                + (0.15m * domainScore),
                4);

            var confidence = _confidenceScoringEngine.Calibrate(hybridScore);
            var matchLayer = ResolveMatchLayer(aliasRubric, embeddingCandidate, hybridScore);

            merged.Add(new AudioCaseSuggestedRubricModel
            {
                SubSectionId = rubricId,
                SubSectionName = subSectionName,
                MatchScore = hybridScore,
                ConfidenceScore = confidence,
                SuggestedIntensityNo = aliasRubric?.SuggestedIntensityNo ?? 2,
                MatchedFrom = aliasRubric?.MatchedFrom ?? embeddingCandidate?.MatchedConceptText,
                MatchSource = matchLayer,
                MatchLayer = matchLayer,
                WhySuggested = BuildWhySuggested(
                    subSectionName, embeddingScore, aliasScore, clinicalScore, keywordScore, hybridScore, embeddingResult.UsedFallback),
                EngineVersion = "v2",
                RequiresManualApproval = true,
                IsAiSuggested = aliasRubric?.IsAiSuggested ?? embeddingScore >= 0.7m,
            });
        }

        return new HybridRetrievalResult
        {
            Rubrics = merged
                .OrderByDescending(r => r.ConfidenceScore ?? r.MatchScore)
                .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList(),
            EmbeddingCandidates = embeddingResult.Candidates.Count,
            AliasCandidates = aliasRubrics.Count,
        };
    }

    private static decimal ComputeClinicalScore(IReadOnlyList<ClinicalConceptModel> concepts, string subSectionName)
    {
        if (string.IsNullOrWhiteSpace(subSectionName)) return 0m;

        return concepts
            .Select(c => AudioCaseAiProcessor.ComputeTextSimilarity(
                c.ClinicalMeaning ?? c.HomeopathicMeaning ?? c.RawStatement,
                subSectionName))
            .DefaultIfEmpty(0m)
            .Max();
    }

    private static decimal ComputeKeywordScore(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        string subSectionName)
    {
        if (string.IsNullOrWhiteSpace(subSectionName)) return 0m;

        var terms = concepts
            .SelectMany(c => c.SearchTerms.Concat(new[] { c.RawStatement, c.ClinicalMeaning ?? string.Empty }))
            .Concat(symptoms.SelectMany(s => s.SearchTerms.Concat(new[] { s.Phrase })))
            .Where(x => !string.IsNullOrWhiteSpace(x)
                        && !string.Equals(x, "desire", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(x, "desires", StringComparison.OrdinalIgnoreCase));

        var scores = terms
            .Select(term => AudioCaseAiProcessor.ComputeTextSimilarity(term, subSectionName))
            .ToList();

        if (subSectionName.Contains('-'))
        {
            var tail = subSectionName.Split('-', 2)[1];
            scores.Add(AudioCaseAiProcessor.ComputeTextSimilarity(tail, string.Join(' ', terms.Take(5))));
        }

        return scores.DefaultIfEmpty(0m).Max();
    }

    private static decimal ComputeDomainAwareScore(
        IReadOnlyList<ClinicalConceptModel> concepts,
        string subSectionName)
    {
        if (concepts.Count == 0 || string.IsNullOrWhiteSpace(subSectionName))
            return 0.5m;

        return concepts
            .Select(c =>
            {
                var domain = ConceptSearchTermBuilder.ResolveDomain(c);
                return ConceptSearchTermBuilder.ScoreCandidate(c, domain, c.ClinicalMeaning ?? c.RawStatement, subSectionName);
            })
            .DefaultIfEmpty(0.5m)
            .Max();
    }

    private static string ResolveMatchLayer(
        AudioCaseSuggestedRubricModel? aliasRubric,
        EmbeddingSearchCandidate? embeddingCandidate,
        decimal hybridScore)
    {
        if (aliasRubric != null && embeddingCandidate != null) return "Hybrid";
        if (embeddingCandidate != null && hybridScore >= 0.65m) return "Embedding";
        if (aliasRubric != null) return aliasRubric.MatchSource ?? "Alias";
        return "Embedding";
    }

    private static string BuildWhySuggested(
        string subSectionName,
        decimal embeddingScore,
        decimal aliasScore,
        decimal clinicalScore,
        decimal keywordScore,
        decimal hybridScore,
        bool usedFallback)
    {
        var embeddingLabel = usedFallback ? "Jaccard fallback" : "embedding";
        return $"Hybrid match for '{subSectionName}': {embeddingLabel}={embeddingScore:0.00}, alias={aliasScore:0.00}, clinical={clinicalScore:0.00}, keyword={keywordScore:0.00}, total={hybridScore:0.00}";
    }
}

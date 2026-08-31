using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Normalization;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ontology;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ranking;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Search;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Synonyms;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Vocabulary;

namespace Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Completion;

/// <summary>V7: retries with expanded synonyms/ontology until 15+ clinically relevant rubrics.</summary>
public interface IRubricCompletionEngine
{
    Task<(List<V7RubricCandidate> Candidates, List<V7SymptomSearchAudit> Audits)> CompleteAsync(
        IReadOnlyList<V7ExtractedSymptom> symptoms,
        V7RepertoryIntelligenceRequest request,
        CancellationToken cancellationToken = default);
}

public class RubricCompletionEngine : IRubricCompletionEngine
{
    private readonly IClinicalNormalizer _normalizer;
    private readonly IHomeopathicVocabularyEngine _vocabularyEngine;
    private readonly ISynonymEngine _synonymEngine;
    private readonly IOntologyEngine _ontologyEngine;
    private readonly ICandidateRubricSearchService _searchService;
    private readonly IRubricRankingService _rankingService;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<RubricCompletionEngine> _logger;

    public RubricCompletionEngine(
        IClinicalNormalizer normalizer,
        IHomeopathicVocabularyEngine vocabularyEngine,
        ISynonymEngine synonymEngine,
        IOntologyEngine ontologyEngine,
        ICandidateRubricSearchService searchService,
        IRubricRankingService rankingService,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<RubricCompletionEngine> logger)
    {
        _normalizer = normalizer;
        _vocabularyEngine = vocabularyEngine;
        _synonymEngine = synonymEngine;
        _ontologyEngine = ontologyEngine;
        _searchService = searchService;
        _rankingService = rankingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(List<V7RubricCandidate> Candidates, List<V7SymptomSearchAudit> Audits)> CompleteAsync(
        IReadOnlyList<V7ExtractedSymptom> symptoms,
        V7RepertoryIntelligenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var globalCandidates = new Dictionary<int, V7RubricCandidate>();
        var audits = new List<V7SymptomSearchAudit>();
        var minRubrics = _options.V7MinDatabaseRubrics;

        foreach (var symptom in symptoms)
        {
            var audit = await SearchSymptomWithRetriesAsync(symptom, request, cancellationToken);
            audits.Add(audit);

            foreach (var candidate in audit.Candidates)
            {
                if (!globalCandidates.TryGetValue(candidate.SubSectionId, out var existing)
                    || candidate.RawScore > existing.RawScore)
                {
                    globalCandidates[candidate.SubSectionId] = candidate;
                }
            }
        }

        if (globalCandidates.Count < minRubrics)
        {
            _logger.LogInformation(
                "V7 completion retry: {Count}/{Min} rubrics — expanding ontology for all symptoms",
                globalCandidates.Count,
                minRubrics);

            foreach (var symptom in symptoms)
            {
                var expandedOntology = _ontologyEngine.ExpandSearchTerms(symptom.Normalized, 30);
                var expandedSynonyms = _synonymEngine.ExpandAll(
                    expandedOntology.Concat(new[] { symptom.Text, symptom.Normalized }), 40);

                var normalized = _normalizer.Normalize(symptom);
                var vocabulary = _vocabularyEngine.ExpandVocabulary(normalized, symptom);

                var extraCandidates = await _searchService.SearchAsync(
                    symptom, normalized, vocabulary, expandedSynonyms, expandedOntology, request, cancellationToken);

                foreach (var candidate in _rankingService.RankCandidates(extraCandidates, symptom))
                {
                    if (!globalCandidates.TryGetValue(candidate.SubSectionId, out var existing)
                        || candidate.RawScore > existing.RawScore)
                    {
                        globalCandidates[candidate.SubSectionId] = candidate;
                    }
                }

                if (globalCandidates.Count >= minRubrics)
                {
                    break;
                }
            }
        }

        var ranked = globalCandidates.Values
            .OrderByDescending(c => c.RawScore)
            .ThenByDescending(c => c.NormalizedScore)
            .ToList();

        return (ranked, audits);
    }

    private async Task<V7SymptomSearchAudit> SearchSymptomWithRetriesAsync(
        V7ExtractedSymptom symptom,
        V7RepertoryIntelligenceRequest request,
        CancellationToken cancellationToken)
    {
        var audit = new V7SymptomSearchAudit { SymptomText = symptom.Text };
        var normalized = _normalizer.Normalize(symptom);
        audit.NormalizationChain = normalized.NormalizationChain;

        var vocabulary = _vocabularyEngine.ExpandVocabulary(normalized, symptom);
        audit.VocabularyTerms = vocabulary.Select(v => v.Term).ToList();

        var synonymTerms = _synonymEngine.ExpandAll(
            normalized.NormalizationChain.Concat(new[] { symptom.Text }), 20);
        audit.SynonymTerms = synonymTerms.ToList();

        var ontologyTerms = _ontologyEngine.ExpandSearchTerms(normalized.FinalConcept, 15);
        audit.OntologyTerms = ontologyTerms.ToList();

        var candidates = await _searchService.SearchAsync(
            symptom, normalized, vocabulary, synonymTerms, ontologyTerms, request, cancellationToken);

        audit.Candidates = _rankingService.RankCandidates(candidates, symptom);
        audit.SearchStrategiesUsed = audit.Candidates
            .Select(c => c.SearchStrategy)
            .Distinct()
            .ToList();

        if (audit.Candidates.Count < 2)
        {
            var expandedSynonyms = _synonymEngine.ExpandAll(
                audit.VocabularyTerms.Concat(audit.OntologyTerms), 35);
            var expandedOntology = _ontologyEngine.ExpandSearchTerms(normalized.FinalConcept, 25);

            var retryCandidates = await _searchService.SearchAsync(
                symptom, normalized, vocabulary, expandedSynonyms, expandedOntology, request, cancellationToken);

            audit.Candidates = _rankingService.RankCandidates(
                MergeCandidates(audit.Candidates, retryCandidates), symptom);
            audit.SearchStrategiesUsed.Add("CompletionRetry");
        }

        return audit;
    }

    private static List<V7RubricCandidate> MergeCandidates(
        IReadOnlyList<V7RubricCandidate> first,
        IReadOnlyList<V7RubricCandidate> second)
    {
        var map = first.ToDictionary(c => c.SubSectionId);
        foreach (var candidate in second)
        {
            if (!map.TryGetValue(candidate.SubSectionId, out var existing)
                || candidate.RawScore > existing.RawScore)
            {
                map[candidate.SubSectionId] = candidate;
            }
        }

        return map.Values.ToList();
    }
}

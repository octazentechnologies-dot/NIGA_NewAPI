using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ontology;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Synonyms;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database;

public interface IEciDatabaseIntelligenceEngine
{
    Task<IReadOnlyList<AudioCaseSuggestedRubricModel>> RetrieveRubricsAsync(
        IReadOnlyList<EciValidatedSymptom> validatedSymptoms,
        string cleanTranscript,
        ConceptGraphFullModel? enrichmentGraph,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// ECI v8: Authoritative repertory retrieval engine.
/// This is a new service and must not modify the legacy V6 SQL engine.
///
/// Phase A/B implementation: scaffold only (modules will be implemented incrementally).
/// </summary>
public sealed class EciDatabaseIntelligenceEngine : IEciDatabaseIntelligenceEngine
{
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<EciDatabaseIntelligenceEngine> _logger;
    private readonly IEciExactSqlSearchModule _exact;
    private readonly ISynonymEngine _synonyms;
    private readonly IOntologyEngine _ontology;
    private readonly IEciSynonymSearchModule _synonymSearch;
    private readonly IEciOntologySearchModule _ontologySearch;
    private readonly IEciFullTextSearchModule _fullText;
    private readonly IEciEmbeddingSearchModule _embedding;
    private readonly IEciBootstrapMappingSearchModule _bootstrap;
    private readonly IEciHistoricalMappingSearchModule _historical;
    private readonly IEciCandidateMerger _merger;
    private readonly IEciHierarchySearchModule _hierarchy;
    private readonly IEciRubricRanker _ranker;
    private readonly IEciEvidenceVerifier _evidence;
    private readonly IEciExplainabilityGenerator _explainability;

    public EciDatabaseIntelligenceEngine(
        IOptions<RubricIntelligenceOptions> options,
        ILogger<EciDatabaseIntelligenceEngine> logger,
        IEciExactSqlSearchModule exact,
        ISynonymEngine synonyms,
        IOntologyEngine ontology,
        IEciSynonymSearchModule synonymSearch,
        IEciOntologySearchModule ontologySearch,
        IEciFullTextSearchModule fullText,
        IEciEmbeddingSearchModule embedding,
        IEciHierarchySearchModule hierarchy,
        IEciBootstrapMappingSearchModule bootstrap,
        IEciHistoricalMappingSearchModule historical,
        IEciCandidateMerger merger,
        IEciRubricRanker ranker,
        IEciEvidenceVerifier evidence,
        IEciExplainabilityGenerator explainability)
    {
        _options = options.Value;
        _logger = logger;
        _exact = exact;
        _synonyms = synonyms;
        _ontology = ontology;
        _synonymSearch = synonymSearch;
        _ontologySearch = ontologySearch;
        _fullText = fullText;
        _embedding = embedding;
        _hierarchy = hierarchy;
        _bootstrap = bootstrap;
        _historical = historical;
        _merger = merger;
        _ranker = ranker;
        _evidence = evidence;
        _explainability = explainability;
    }

    public async Task<IReadOnlyList<AudioCaseSuggestedRubricModel>> RetrieveRubricsAsync(
        IReadOnlyList<EciValidatedSymptom> validatedSymptoms,
        string cleanTranscript,
        ConceptGraphFullModel? enrichmentGraph,
        CancellationToken cancellationToken = default)
    {
        // Phase C: complete deterministic retrieval + ranking. No GPT usage.
        if (validatedSymptoms.Count == 0)
        {
            return Array.Empty<AudioCaseSuggestedRubricModel>();
        }

        var allRubrics = new List<AudioCaseSuggestedRubricModel>();

        foreach (var accepted in validatedSymptoms.Where(v => v.Accepted))
        {
            var symptomText = accepted.Symptom.Symptom ?? string.Empty;
            var curatedSynonyms = _synonyms.ExpandSynonyms(symptomText);
            var ontologyTerms = _ontology.ExpandSearchTerms(symptomText, _options.EciV8MaxSemanticVariantsPerSymptom);

            var exact = await _exact.SearchAsync(accepted, cancellationToken);
            var syn = await _synonymSearch.SearchAsync(accepted, curatedSynonyms, cancellationToken);
            var ont = await _ontologySearch.SearchAsync(accepted, ontologyTerms, cancellationToken);
            var fts = await _fullText.SearchAsync(accepted, ontologyTerms, cancellationToken);
            var emb = await _embedding.SearchAsync(accepted, ontologyTerms, cancellationToken);
            var boot = await _bootstrap.SearchAsync(accepted, cancellationToken);
            var hist = await _historical.SearchAsync(accepted, cancellationToken);

            var merged = _merger.Merge(exact, syn, ont, fts, emb, boot, hist);
            await _merger.ResolveNamesAsync(merged, cancellationToken);

            // Hierarchy expansion is only allowed for known seed rubrics.
            var hierarchy = await _hierarchy.ExpandAsync(accepted, merged.Take(25).ToList(), cancellationToken);
            merged = _merger.Merge(merged, hierarchy);

            // Required output: top 50 candidates before ranking.
            var top50 = merged
                .Where(c => !string.IsNullOrWhiteSpace(c.SubSectionName))
                .Take(50)
                .ToList();

            var ranked = _ranker.Rank(accepted, top50, cleanTranscript);
            ranked = _evidence.VerifyAndReject(accepted, ranked);

            var final = ranked
                .Where(c => !c.Rejected)
                .Take(Math.Clamp(_options.EciV8MaxDatabaseRubrics, 10, 20))
                .ToList();

            foreach (var candidate in final)
            {
                var rubric = new AudioCaseSuggestedRubricModel
                {
                    SubSectionId = candidate.SubSectionId,
                    SubSectionName = candidate.SubSectionName,
                    MatchSource = "EciV8DatabaseIntelligence",
                    MatchLayer = "ECI-V8",
                    EngineVersion = "v8.0",
                    RequiresManualApproval = true,
                    ResultKind = "RepertoryRubric",
                    ConfidenceScore = Math.Clamp(candidate.FinalScore / 100m, 0m, 1m),
                    MatchScore = Math.Clamp(candidate.FinalScore / 100m, 0m, 1m),
                    QualityScore = candidate.FinalScore,
                };

                _explainability.ApplyExplainability(rubric, accepted, candidate);
                allRubrics.Add(rubric);
            }

            _logger.LogInformation(
                "ECI v8 symptom='{Symptom}' candidates={Candidates} final={Final}",
                symptomText,
                top50.Count,
                final.Count);
        }

        // Global dedup by SubSectionId, keep highest score.
        return allRubrics
            .GroupBy(r => r.SubSectionId)
            .Select(g => g.OrderByDescending(x => x.QualityScore ?? 0m).First())
            .OrderByDescending(r => r.QualityScore ?? 0m)
            .Take(_options.EciV8MaxDatabaseRubrics)
            .ToList();
    }
}


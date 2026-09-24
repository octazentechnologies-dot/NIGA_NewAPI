using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Completion;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Concepts;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Confidence;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Extraction;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Explanation;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ranking;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence;

public interface IRepertoryIntelligenceOrchestrator
{
    Task<V7RepertoryIntelligenceResult> DiscoverAsync(
        V7RepertoryIntelligenceRequest request,
        CancellationToken cancellationToken = default);

    HybridCompletionOutput FinalizeOutput(
        IReadOnlyList<AudioCaseSuggestedRubricModel> validatedRepertoryRubrics,
        V7RepertoryIntelligenceRequest request,
        IReadOnlyList<V7SymptomSearchAudit> auditTrail,
        IReadOnlyList<V7ExtractedSymptom> extractedSymptoms,
        ConceptGraphFullModel graph);
}

/// <summary>V7: enterprise repertory intelligence — modular pipeline, database authoritative.</summary>
public class RepertoryIntelligenceOrchestrator : IRepertoryIntelligenceOrchestrator
{
    private readonly IClinicalExtractionService _extractionService;
    private readonly IRubricCompletionEngine _completionEngine;
    private readonly IRubricRankingService _rankingService;
    private readonly IExplanationService _explanationService;
    private readonly IConfidenceCalculator _confidenceCalculator;
    private readonly IClinicalConceptService _clinicalConceptService;
    private readonly IHybridCompletionAuditLogger _auditLogger;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<RepertoryIntelligenceOrchestrator> _logger;

    public RepertoryIntelligenceOrchestrator(
        IClinicalExtractionService extractionService,
        IRubricCompletionEngine completionEngine,
        IRubricRankingService rankingService,
        IExplanationService explanationService,
        IConfidenceCalculator confidenceCalculator,
        IClinicalConceptService clinicalConceptService,
        IHybridCompletionAuditLogger auditLogger,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<RepertoryIntelligenceOrchestrator> logger)
    {
        _extractionService = extractionService;
        _completionEngine = completionEngine;
        _rankingService = rankingService;
        _explanationService = explanationService;
        _confidenceCalculator = confidenceCalculator;
        _clinicalConceptService = clinicalConceptService;
        _auditLogger = auditLogger;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<V7RepertoryIntelligenceResult> DiscoverAsync(
        V7RepertoryIntelligenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new V7RepertoryIntelligenceResult();

        try
        {
            result.StagesCompleted.Add("TranscriptCleanup");

            var graphSymptoms = await _extractionService.ExtractFromGraphAsync(
                request.Graph, request.Transcript, cancellationToken);
            result.StagesCompleted.Add("GraphSymptomExtraction");

            var symptoms = graphSymptoms.ToList();

            if (_options.EnableV7GptStructuredExtraction
                && symptoms.Count < _options.MinValidatedSqlRubrics / 2)
            {
                var gptSymptoms = await _extractionService.ExtractFromGptAsync(
                    request.Transcript, cancellationToken);
                foreach (var gpt in gptSymptoms)
                {
                    if (!symptoms.Any(s => s.Text.Equals(gpt.Text, StringComparison.OrdinalIgnoreCase)))
                    {
                        symptoms.Add(gpt);
                    }
                }

                result.StagesCompleted.Add("GptStructuredExtraction");
            }

            result.ExtractedSymptoms = symptoms;
            result.StagesCompleted.Add("ClinicalNormalization");
            result.StagesCompleted.Add("VocabularyExpansion");
            result.StagesCompleted.Add("SynonymExpansion");
            result.StagesCompleted.Add("OntologyExpansion");

            var (candidates, audits) = await _completionEngine.CompleteAsync(
                symptoms, request, cancellationToken);

            result.AuditTrail = audits;
            result.StagesCompleted.Add("MultiStageDatabaseSearch");
            result.StagesCompleted.Add("HybridRanking");

            var globalDiscoveries = new Dictionary<int, RubricDiscoveryNodeModel>();
            foreach (var symptom in symptoms)
            {
                var symptomCandidates = candidates
                    .Where(c => c.SourceSymptomConceptId == symptom.SourceConceptId
                        || string.Equals(c.SourceSymptomText, symptom.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (symptomCandidates.Count == 0)
                {
                    symptomCandidates = candidates.Take(_options.MaxRubricsPerConceptSlots).ToList();
                }

                var ranked = _rankingService.RankCandidates(symptomCandidates.ToList(), symptom);
                var discoveries = _rankingService.ToDiscoveries(
                    ranked.Take(_options.MaxRubricsPerConceptSlots).ToList(), symptom);

                foreach (var discovery in discoveries)
                {
                    if (!globalDiscoveries.TryGetValue(discovery.SubSectionId, out var existing)
                        || discovery.Confidence > existing.Confidence)
                    {
                        globalDiscoveries[discovery.SubSectionId] = discovery;
                    }
                }
            }

            result.Discoveries = globalDiscoveries.Values
                .OrderByDescending(d => d.Confidence)
                .Take(_options.MaxEnterpriseDiscoveryCandidates)
                .ToList();

            result.Success = true;
            result.StagesCompleted.Add("RubricCompletion");

            sw.Stop();
            _logger.LogInformation(
                "V7 repertory intelligence session {SessionId}: symptoms={Symptoms} discoveries={Discoveries} latency={Ms}ms",
                request.SessionId,
                symptoms.Count,
                result.Discoveries.Count,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "V7 repertory intelligence failed for session {SessionId}", request.SessionId);
        }

        return result;
    }

    public HybridCompletionOutput FinalizeOutput(
        IReadOnlyList<AudioCaseSuggestedRubricModel> validatedRepertoryRubrics,
        V7RepertoryIntelligenceRequest request,
        IReadOnlyList<V7SymptomSearchAudit> auditTrail,
        IReadOnlyList<V7ExtractedSymptom> extractedSymptoms,
        ConceptGraphFullModel graph)
    {
        var symptomMap = extractedSymptoms
            .Where(s => s.SourceConceptId.HasValue)
            .ToDictionary(s => s.SourceConceptId!.Value, s => s);

        var repertory = validatedRepertoryRubrics
            .Where(EnterpriseRubricPresentationHelper.IsAuthoritativeRepertory)
            .Select(r =>
            {
                EnterpriseRubricPresentationHelper.MarkRepertoryRubric(
                    r, r.RepertoryPath, r.SelectionReason ?? r.WhySuggested);
                r.EngineVersion = "v7.0";

                var symptom = FindSymptom(r, extractedSymptoms, auditTrail);
                var audit = FindAudit(r, auditTrail);
                var explainability = _explanationService.BuildExplanation(r, symptom, audit);
                _explanationService.ApplyToRubric(r, explainability);
                r.EnterpriseConfidenceScore = _confidenceCalculator.ComputeForRubric(r, explainability);
                return r;
            })
            .Where(r => EnterpriseRubricConfidenceEngine.MeetsDisplayThreshold(
                r.EnterpriseConfidenceScore ?? 0, _options))
            .Where(r => !EnterpriseGenericRubricFilter.ShouldSuppress(r))
            .OrderByDescending(r => r.EnterpriseConfidenceScore ?? 0)
            .ToList();

        repertory = EnterpriseRubricDeduplicator.DeduplicateRubrics(repertory);

        var maxRubrics = Math.Max(_options.V7MinDatabaseRubrics, 25);
        if (repertory.Count > maxRubrics)
        {
            repertory = repertory.Take(maxRubrics).ToList();
        }

        var mappedConceptIds = new HashSet<long?>();
        foreach (var rubric in repertory)
        {
            var discovery = request.GlobalDiscoveries.FirstOrDefault(d => d.SubSectionId == rubric.SubSectionId);
            if (discovery?.HomeopathicConceptId != null)
            {
                mappedConceptIds.Add(discovery.HomeopathicConceptId);
            }

            var symptom = FindSymptom(rubric, extractedSymptoms, auditTrail);
            if (symptom?.SourceConceptId != null)
            {
                mappedConceptIds.Add(symptom.SourceConceptId);
            }
        }

        var aiConcepts = _options.EnableAiClinicalConceptSuggestions
            ? _clinicalConceptService.CreateFallbackConcepts(
                extractedSymptoms, mappedConceptIds, graph, _options.MaxAiClinicalConcepts)
            : new List<AudioCaseSuggestedRubricModel>();

        var output = new HybridCompletionOutput
        {
            RepertoryRubrics = repertory,
            AiClinicalConcepts = aiConcepts,
            AllRubrics = EnterpriseRubricPresentationHelper.PartitionResults(repertory, aiConcepts),
            AuditEntries = auditTrail.Select(a => new HybridConceptAuditEntry
            {
                ConceptName = a.SymptomText,
                OutputCategory = a.Candidates.Count > 0
                    ? HybridCompletionOutputCategories.RepertoryDatabase
                    : HybridCompletionOutputCategories.AiClinicalConcept,
                ValidationSummary = string.Join(", ", a.SearchStrategiesUsed),
                DatabaseMatchCount = a.Candidates.Count,
            }).ToList(),
        };

        _auditLogger.LogFinalizeSummary(request.SessionId, output);

        _logger.LogInformation(
            "V7 finalize session {SessionId}: repertory={Repertory} aiConcepts={Ai}",
            request.SessionId,
            repertory.Count,
            aiConcepts.Count);

        return output;
    }

    private static V7ExtractedSymptom? FindSymptom(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<V7ExtractedSymptom> symptoms,
        IReadOnlyList<V7SymptomSearchAudit> auditTrail)
    {
        var audit = auditTrail.FirstOrDefault(a =>
            a.Candidates.Any(c => c.SubSectionId == rubric.SubSectionId));

        if (audit != null)
        {
            return symptoms.FirstOrDefault(s =>
                string.Equals(s.Text, audit.SymptomText, StringComparison.OrdinalIgnoreCase));
        }

        return symptoms.FirstOrDefault(s =>
            rubric.MatchedFrom?.Contains(s.Text, StringComparison.OrdinalIgnoreCase) == true
            || rubric.WhySuggested?.Contains(s.Normalized, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static V7SymptomSearchAudit? FindAudit(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<V7SymptomSearchAudit> auditTrail) =>
        auditTrail.FirstOrDefault(a =>
            a.Candidates.Any(c => c.SubSectionId == rubric.SubSectionId));
}

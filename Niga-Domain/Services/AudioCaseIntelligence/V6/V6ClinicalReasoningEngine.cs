using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.V6;

public interface IV6ClinicalReasoningEngine
{
    Task<V6ClinicalReasoningResult> DiscoverAsync(
        V6ClinicalReasoningRequest request,
        CancellationToken cancellationToken = default);

    HybridCompletionOutput FinalizeOutput(
        IReadOnlyList<AudioCaseSuggestedRubricModel> validatedRepertoryRubrics,
        V6ClinicalReasoningRequest request,
        IReadOnlyList<V6SymptomDiscoveryAudit> auditTrail,
        ConceptGraphFullModel graph);
}

/// <summary>V6 enterprise clinical reasoning — SQL authoritative, AI reasoning only.</summary>
public class V6ClinicalReasoningEngine : IV6ClinicalReasoningEngine
{
    private readonly V6PerSymptomDiscoveryPipeline _pipeline;
    private readonly IHybridCompletionAuditLogger _auditLogger;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<V6ClinicalReasoningEngine> _logger;

    public V6ClinicalReasoningEngine(
        V6PerSymptomDiscoveryPipeline pipeline,
        IHybridCompletionAuditLogger auditLogger,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<V6ClinicalReasoningEngine> logger)
    {
        _pipeline = pipeline;
        _auditLogger = auditLogger;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<V6ClinicalReasoningResult> DiscoverAsync(
        V6ClinicalReasoningRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new V6ClinicalReasoningResult();
        var globalMap = new Dictionary<int, RubricDiscoveryNodeModel>();

        try
        {
            var cleanedTranscript = V6SymptomOntologyBuilder.CleanTranscript(request.Transcript);
            result.StagesCompleted.Add(V6PipelineStageNames.TranscriptCleanup);

            var symptomUnits = V6SymptomOntologyBuilder.BuildSymptomUnits(request.Graph, cleanedTranscript);
            result.SymptomUnits = symptomUnits;
            result.StagesCompleted.Add(V6PipelineStageNames.SymptomExtraction);
            result.StagesCompleted.Add(V6PipelineStageNames.ClinicalOntologyMapping);
            result.StagesCompleted.Add(V6PipelineStageNames.HomeopathicOntologyMapping);

            foreach (var symptom in symptomUnits)
            {
                var (discoveries, audit) = await _pipeline.SearchAsync(
                    symptom, request, _options, cancellationToken);

                result.AuditTrail.Add(audit);
                LogSymptomAudit(request.SessionId, audit);

                foreach (var discovery in discoveries)
                {
                    if (!globalMap.TryGetValue(discovery.SubSectionId, out var existing)
                        || discovery.Confidence > existing.Confidence)
                    {
                        globalMap[discovery.SubSectionId] = discovery;
                    }
                }
            }

            result.StagesCompleted.Add(V6PipelineStageNames.CandidateMerge);
            result.StagesCompleted.Add(V6PipelineStageNames.EvidenceAggregation);

            result.Discoveries = globalMap.Values
                .OrderByDescending(d => d.Confidence)
                .Take(_options.MaxEnterpriseDiscoveryCandidates)
                .ToList();

            result.Success = true;
            result.StagesCompleted.Add(V6PipelineStageNames.RubricRanking);

            _logger.LogInformation(
                "V6 clinical reasoning session {SessionId}: symptoms={SymptomCount} sqlDiscoveries={DiscoveryCount} audits={AuditCount}",
                request.SessionId,
                symptomUnits.Count,
                result.Discoveries.Count,
                result.AuditTrail.Count);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "V6 clinical reasoning failed for session {SessionId}", request.SessionId);
        }

        return result;
    }

    public HybridCompletionOutput FinalizeOutput(
        IReadOnlyList<AudioCaseSuggestedRubricModel> validatedRepertoryRubrics,
        V6ClinicalReasoningRequest request,
        IReadOnlyList<V6SymptomDiscoveryAudit> auditTrail,
        ConceptGraphFullModel graph)
    {
        var symptomMap = graph.HomeopathicConcepts
            .Where(h => h.HomeopathicConceptId.HasValue)
            .ToDictionary(h => h.HomeopathicConceptId!.Value, h => h);

        var repertory = validatedRepertoryRubrics
            .Where(EnterpriseRubricPresentationHelper.IsAuthoritativeRepertory)
            .Select(r =>
            {
                EnterpriseRubricPresentationHelper.MarkRepertoryRubric(
                    r, r.RepertoryPath, r.SelectionReason ?? r.WhySuggested);
                r.EngineVersion = "v6.0";

                var symptom = FindSymptomForRubric(r, request, auditTrail);
                var audit = FindAuditForRubric(r, auditTrail);
                var explainability = V6EvidenceRanker.BuildExplainability(r, symptom, audit);
                r.V6Explainability = explainability;
                r.SelectionReason = explainability.FinalExplanation ?? r.SelectionReason;
                r.EnterpriseConfidenceScore = V6EvidenceRanker.ComputeRankScore(r, symptom, explainability, _options);
                return r;
            })
            .Where(r => EnterpriseRubricConfidenceEngine.MeetsDisplayThreshold(
                r.EnterpriseConfidenceScore ?? 0, _options))
            .Where(r => !EnterpriseGenericRubricFilter.ShouldSuppress(r))
            .ToList();

        repertory = repertory
            .OrderByDescending(r => r.EnterpriseConfidenceScore ?? 0)
            .ThenByDescending(r => r.QualityScore ?? 0)
            .ToList();

        repertory = EnterpriseRubricDeduplicator.DeduplicateRubrics(repertory);

        var maxSqlRubrics = Math.Max(_options.MinValidatedSqlRubrics, 25);
        if (repertory.Count > maxSqlRubrics)
        {
            repertory = repertory.Take(maxSqlRubrics).ToList();
        }

        var mappedConceptIds = new HashSet<long?>();
        foreach (var rubric in repertory)
        {
            var discovery = request.GlobalDiscoveries.FirstOrDefault(d => d.SubSectionId == rubric.SubSectionId);
            if (discovery?.HomeopathicConceptId != null)
            {
                mappedConceptIds.Add(discovery.HomeopathicConceptId);
            }

            var matchedSymptom = FindSymptomForRubric(rubric, request, auditTrail);
            if (matchedSymptom?.HomeopathicConceptId != null)
            {
                mappedConceptIds.Add(matchedSymptom.HomeopathicConceptId);
            }
        }

        var aiConcepts = new List<AudioCaseSuggestedRubricModel>();
        if (_options.EnableAiClinicalConceptSuggestions)
        {
            foreach (var homeo in graph.HomeopathicConcepts.Where(c => !string.IsNullOrWhiteSpace(c.ConceptName)))
            {
                if (mappedConceptIds.Contains(homeo.HomeopathicConceptId))
                {
                    continue;
                }

                var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                    ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
                var meaning = clinical != null
                    ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                        ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                    : null;

                aiConcepts.Add(EnterpriseRubricPresentationHelper.CreateAiClinicalConcept(
                    homeo, clinical, meaning, "v6.0"));

                if (aiConcepts.Count >= _options.MaxAiClinicalConcepts)
                {
                    break;
                }
            }
        }

        var output = new HybridCompletionOutput
        {
            RepertoryRubrics = repertory,
            AiClinicalConcepts = aiConcepts,
            AllRubrics = EnterpriseRubricPresentationHelper.PartitionResults(repertory, aiConcepts),
            AuditEntries = auditTrail.Select(a => new HybridConceptAuditEntry
            {
                HomeopathicConceptId = a.HomeopathicConceptId,
                ConceptName = a.SymptomLabel,
                ConceptConfidence = a.HomeopathicConceptId.HasValue
                    && symptomMap.TryGetValue(a.HomeopathicConceptId.Value, out var homeo)
                    ? homeo.Confidence
                    : 0,
                OutputCategory = a.OutputCategory,
                ValidationSummary = a.ValidationResult,
                DatabaseMatchCount = a.SqlHits.Count,
                KnowledgeGraphMatchCount = a.KnowledgeGraphSubSectionIds.Count,
                EmbeddingMatchCount = a.EmbeddingSubSectionIds.Count,
            }).ToList(),
        };

        _auditLogger.LogFinalizeSummary(request.SessionId, output);

        _logger.LogInformation(
            "V6 finalize session {SessionId}: repertory={RepertoryCount} aiConcepts={AiCount} (maxAi={MaxAi})",
            request.SessionId,
            repertory.Count,
            aiConcepts.Count,
            _options.MaxAiClinicalConcepts);

        return output;
    }

    private void LogSymptomAudit(Guid sessionId, V6SymptomDiscoveryAudit audit)
    {
        _logger.LogDebug(
            "V6 symptom audit Session={SessionId} Symptom={Symptom} SqlHits={SqlHits} KG={Kg} Embed={Embed} Category={Category}",
            sessionId,
            audit.SymptomLabel,
            audit.SqlHits.Count,
            audit.KnowledgeGraphSubSectionIds.Count,
            audit.EmbeddingSubSectionIds.Count,
            audit.OutputCategory);
    }

    private static V6ClinicalSymptomUnit? FindSymptomForRubric(
        AudioCaseSuggestedRubricModel rubric,
        V6ClinicalReasoningRequest request,
        IReadOnlyList<V6SymptomDiscoveryAudit> auditTrail)
    {
        var audit = auditTrail.FirstOrDefault(a =>
            a.SqlHits.Any(h => h.SubSectionId == rubric.SubSectionId)
            || a.KnowledgeGraphSubSectionIds.Contains(rubric.SubSectionId)
            || a.EmbeddingSubSectionIds.Contains(rubric.SubSectionId));

        if (audit?.HomeopathicConceptId == null)
        {
            return null;
        }

        return request.Graph.HomeopathicConcepts
            .Where(h => h.HomeopathicConceptId == audit.HomeopathicConceptId)
            .Select(h => new V6ClinicalSymptomUnit
            {
                HomeopathicConceptId = h.HomeopathicConceptId,
                ClinicalLabel = h.ConceptName,
                HomeopathicLabel = h.ConceptName,
                TranscriptEvidence = audit.SymptomLabel,
                Confidence = h.Confidence,
            })
            .FirstOrDefault();
    }

    private static V6SymptomDiscoveryAudit? FindAuditForRubric(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<V6SymptomDiscoveryAudit> auditTrail) =>
        auditTrail.FirstOrDefault(a =>
            a.SqlHits.Any(h => h.SubSectionId == rubric.SubSectionId)
            || a.KnowledgeGraphSubSectionIds.Contains(rubric.SubSectionId)
            || a.EmbeddingSubSectionIds.Contains(rubric.SubSectionId));
}

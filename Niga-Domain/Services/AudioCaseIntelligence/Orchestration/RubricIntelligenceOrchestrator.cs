using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;
using Niga_Domain.Services.AudioCaseIntelligence.Validation;

namespace Niga_Domain.Services.AudioCaseIntelligence.Orchestration;

public class RubricIntelligenceOrchestrator : IRubricIntelligenceOrchestrator
{
    private readonly RubricIntelligenceOptions _options;
    private readonly ICaseUnderstandingEngine _caseUnderstandingEngine;
    private readonly IModalityDetectionEngine _modalityEngine;
    private readonly IConcomitantDetectionEngine _concomitantEngine;
    private readonly IClinicalReasoningEngine _clinicalReasoningEngine;
    private readonly IHomeopathicReasoningEngine _homeopathicReasoningEngine;
    private readonly ICausationDetectionEngine _causationEngine;
    private readonly IHomeopathicWeightEngine _weightEngine;
    private readonly IMetaphorInterpretationEngine _metaphorEngine;
    private readonly IRubricAliasEngine _aliasEngine;
    private readonly IHybridRetrievalEngine _hybridRetrievalEngine;
    private readonly IConceptKeywordDiscoveryEngine _conceptKeywordDiscoveryEngine;
    private readonly IClinicalInferenceEngine _clinicalInferenceEngine;
    private readonly IExplainabilityEngine _explainabilityEngine;
    private readonly IRepertoryTierEngine _repertoryTierEngine;
    private readonly IRubricIntelligenceSettingsService _settings;
    private readonly ISymptomExtractionEngine _symptomExtractionEngine;
    private readonly IClinicalValidationEngine _clinicalValidationEngine;
    private readonly IAudioCaseIntelligenceRepository _intelligenceRepository;
    private readonly IConceptGraphRepository _conceptGraphRepository;
    private readonly ILogger<RubricIntelligenceOrchestrator> _logger;

    public RubricIntelligenceOrchestrator(
        IOptions<RubricIntelligenceOptions> options,
        ICaseUnderstandingEngine caseUnderstandingEngine,
        IModalityDetectionEngine modalityEngine,
        IConcomitantDetectionEngine concomitantEngine,
        IClinicalReasoningEngine clinicalReasoningEngine,
        IHomeopathicReasoningEngine homeopathicReasoningEngine,
        ICausationDetectionEngine causationEngine,
        IHomeopathicWeightEngine weightEngine,
        IMetaphorInterpretationEngine metaphorEngine,
        IRubricAliasEngine aliasEngine,
        IHybridRetrievalEngine hybridRetrievalEngine,
        IConceptKeywordDiscoveryEngine conceptKeywordDiscoveryEngine,
        IClinicalInferenceEngine clinicalInferenceEngine,
        IExplainabilityEngine explainabilityEngine,
        IRepertoryTierEngine repertoryTierEngine,
        IRubricIntelligenceSettingsService settings,
        ISymptomExtractionEngine symptomExtractionEngine,
        IClinicalValidationEngine clinicalValidationEngine,
        IAudioCaseIntelligenceRepository intelligenceRepository,
        IConceptGraphRepository conceptGraphRepository,
        ILogger<RubricIntelligenceOrchestrator> logger)
    {
        _options = options.Value;
        _caseUnderstandingEngine = caseUnderstandingEngine;
        _modalityEngine = modalityEngine;
        _concomitantEngine = concomitantEngine;
        _clinicalReasoningEngine = clinicalReasoningEngine;
        _homeopathicReasoningEngine = homeopathicReasoningEngine;
        _causationEngine = causationEngine;
        _weightEngine = weightEngine;
        _metaphorEngine = metaphorEngine;
        _aliasEngine = aliasEngine;
        _hybridRetrievalEngine = hybridRetrievalEngine;
        _conceptKeywordDiscoveryEngine = conceptKeywordDiscoveryEngine;
        _clinicalInferenceEngine = clinicalInferenceEngine;
        _explainabilityEngine = explainabilityEngine;
        _repertoryTierEngine = repertoryTierEngine;
        _settings = settings;
        _symptomExtractionEngine = symptomExtractionEngine;
        _clinicalValidationEngine = clinicalValidationEngine;
        _intelligenceRepository = intelligenceRepository;
        _conceptGraphRepository = conceptGraphRepository;
        _logger = logger;
    }

    public async Task<RubricIntelligenceAnalysisResult> AnalyzeAsync(
        AudioCaseSession session,
        string transcript,
        List<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary,
        string correlationId,
        PatientClinicalContext? patientContext = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.IsV2Active)
        {
            return new RubricIntelligenceAnalysisResult
            {
                EngineVersion = "v1",
                RequiresManualApproval = false,
            };
        }

        var sessionId = session.AudioCaseSessionId;
        var stages = new List<string>();

        await LogStageAsync(sessionId, correlationId, "CaseUnderstanding", "Started", null, null, null, cancellationToken);

        var understanding = await _caseUnderstandingEngine.AnalyzeAsync(
            transcript, symptoms, summary, session.DetectedLanguage, cancellationToken);

        if (!understanding.Success)
        {
            await LogStageAsync(sessionId, correlationId, "CaseUnderstanding", "Failed", understanding.Error, null, understanding.LatencyMs, cancellationToken);
            return new RubricIntelligenceAnalysisResult
            {
                EngineVersion = "v2",
                RequiresManualApproval = _settings.RequiresManualApproval,
                StagesCompleted = stages,
            };
        }

        stages.Add("CaseUnderstanding");
        await LogStageAsync(sessionId, correlationId, "CaseUnderstanding", "Success",
            $"Extracted {understanding.Concepts.Count} concept(s).", null, understanding.LatencyMs, cancellationToken);

        var concepts = understanding.Concepts;
        ApplyAmbiguityAndSeedSearchTerms(concepts, transcript);
        concepts = _modalityEngine.Enrich(concepts);
        stages.Add("ModalityDetection");

        concepts = _concomitantEngine.Enrich(concepts);
        stages.Add("ConcomitantDetection");

        var causationResult = _causationEngine.Detect(concepts, transcript);
        concepts = causationResult.Concepts;
        var causationLinks = causationResult.Links;
        stages.Add("CausationDetection");
        await LogStageAsync(sessionId, correlationId, "CausationDetection", "Success",
            $"Detected {causationLinks.Count} causation link(s).", null, null, cancellationToken);

        concepts = _clinicalReasoningEngine.Enrich(concepts);
        stages.Add("ClinicalReasoning");

        concepts = _homeopathicReasoningEngine.Enrich(concepts);
        stages.Add("HomeopathicReasoning");

        var weightResult = await _weightEngine.ApplyAsync(concepts, causationLinks, cancellationToken);
        concepts = weightResult.Concepts;
        stages.Add("HomeopathicWeight");
        await LogStageAsync(sessionId, correlationId, "HomeopathicWeight", "Success",
            $"Applied {weightResult.RuleWeights.Count} weight rule(s).", null, null, cancellationToken);

        for (var i = 0; i < concepts.Count; i++)
        {
            concepts[i].SequenceOrder = i + 1;
        }

        var metaphorResult = await _metaphorEngine.EnrichAsync(concepts, session.DetectedLanguage, cancellationToken);
        concepts = metaphorResult.Concepts;
        stages.Add("MetaphorInterpretation");
        await LogStageAsync(sessionId, correlationId, "MetaphorInterpretation", "Success",
            $"Matched {metaphorResult.MetaphorsMatched} metaphor(s).", null, null, cancellationToken);

        var aliasResult = await _aliasEngine.SearchRubricsAsync(concepts, symptoms, session.DetectedLanguage, cancellationToken);
        stages.Add("RubricAliasSearch");
        await LogStageAsync(sessionId, correlationId, "RubricAliasSearch", "Success",
            $"Matched {aliasResult.AliasesMatched} alias(es), {aliasResult.Rubrics.Count} rubric candidate(s).", null, null, cancellationToken);

        List<AudioCaseSuggestedRubricModel> rubrics;
        if (_options.EnableEmbeddingSearch)
        {
            var hybridResult = await _hybridRetrievalEngine.RetrieveAsync(
                concepts, symptoms, aliasResult.Rubrics, cancellationToken);
            rubrics = hybridResult.Rubrics;
            stages.Add("HybridEmbeddingSearch");
            await LogStageAsync(sessionId, correlationId, "HybridEmbeddingSearch", "Success",
                $"Hybrid merged {hybridResult.AliasCandidates} alias + {hybridResult.EmbeddingCandidates} embedding candidate(s) into {rubrics.Count} rubric(s).",
                null, null, cancellationToken);
        }
        else
        {
            rubrics = aliasResult.Rubrics;
        }

        // Per-concept keyword discovery: every concept (including SRP/mental/particular) gets a search attempt.
        if (_options.EnablePerConceptKeywordDiscovery)
        {
            var keywordBatch = await _conceptKeywordDiscoveryEngine.DiscoverAsync(
                sessionId, correlationId, concepts, rubrics, cancellationToken);
            rubrics = MergeKeywordRubrics(rubrics, keywordBatch.Rubrics);
            stages.Add("PerConceptKeywordDiscovery");
            await LogStageAsync(sessionId, correlationId, "PerConceptKeywordDiscovery", "Success",
                $"Attempted={keywordBatch.Traces.Count}, withHits={keywordBatch.Traces.Count(t => t.KeptCandidateCount > 0)}, " +
                $"noAttempt={keywordBatch.Traces.Count(t => !t.SearchAttempted)}, timeout={keywordBatch.Traces.Count(t => t.Outcome == "Timeout")}",
                null, keywordBatch.Traces.Sum(t => t.LatencyMs), cancellationToken);
        }

        if (_options.EnableClinicalInference)
        {
            var inferenceResult = await _clinicalInferenceEngine.InferAsync(
                concepts, causationLinks, rubrics, cancellationToken);

            if (inferenceResult.Rubrics.Count > 0)
            {
                rubrics = MergeInferenceRubrics(rubrics, inferenceResult.Rubrics);
            }

            await _intelligenceRepository.SaveInferenceLogsAsync(sessionId, inferenceResult.Logs, cancellationToken);
            stages.Add("ClinicalInference");
            await LogStageAsync(sessionId, correlationId, "ClinicalInference", "Success",
                $"Added {inferenceResult.Rubrics.Count} inferred rubric(s).", null, null, cancellationToken);
        }

        PrimarySymptomModel? primarySymptom = null;
        var validationRejected = 0;

        if (_options.RequiresStrictValidation)
        {
            var validationResult = _clinicalValidationEngine.ValidateAndFilter(
                rubrics,
                new ClinicalValidationContext
                {
                    SessionId = sessionId,
                    Patient = patientContext,
                    Transcript = transcript,
                    Concepts = concepts,
                    CausationLinks = causationLinks,
                    Symptoms = symptoms,
                    Summary = summary,
                });

            rubrics = validationResult.AcceptedRubrics;
            primarySymptom = validationResult.PrimarySymptom;
            validationRejected = validationResult.RejectedCount;
            stages.Add(_options.EnableEnterpriseClinicalValidation
                ? "EnterpriseClinicalValidation"
                : "ClinicalValidationV21");

            var rejectionBreakdown = validationResult.RejectedRubrics
                .Select(r => new
                {
                    r.SubSectionId,
                    r.SubSectionName,
                    r.MatchSource,
                    Flags = r.ValidationFlags,
                    Reason = r.RejectionReason,
                })
                .ToList();

            await LogStageAsync(sessionId, correlationId,
                _options.EnableEnterpriseClinicalValidation ? "EnterpriseClinicalValidation" : "ClinicalValidationV21",
                "Success",
                $"Accepted {validationResult.AcceptedRubrics.Count}, rejected {validationResult.RejectedCount} rubric(s). Primary: {validationResult.PrimarySymptom?.Text}",
                JsonSerializer.Serialize(rejectionBreakdown),
                null, cancellationToken);

            // Per-candidate rejection reasons into AIReasoningAudit (Bug 1). Non-fatal if audit write fails.
            try
            {
                var errorPreview = validationResult.RejectedCount > 0
                    ? string.Join(" | ", validationResult.RejectedRubrics
                        .Take(5)
                        .Select(r => $"{Truncate(r.SubSectionName, 80)}: {Truncate(r.RejectionReason ?? string.Join(',', r.ValidationFlags), 120)}"))
                    : null;

                await _conceptGraphRepository.SaveReasoningAuditAsync(sessionId, new AiReasoningAuditModel
                {
                    PipelineStage = "EnterpriseClinicalValidation",
                    ModelId = "v2-validation",
                    Success = validationResult.AcceptedRubrics.Count > 0,
                    RequestJson = JsonSerializer.Serialize(new
                    {
                        candidateCount = validationResult.AcceptedRubrics.Count + validationResult.RejectedCount,
                        accepted = validationResult.AcceptedRubrics.Count,
                        rejected = validationResult.RejectedCount,
                    }),
                    ResponseJson = JsonSerializer.Serialize(rejectionBreakdown),
                    ErrorMessage = errorPreview,
                }, cancellationToken);
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(auditEx,
                    "Failed to persist EnterpriseClinicalValidation AIReasoningAudit for session {SessionId}",
                    sessionId);
            }
        }

        rubrics = _explainabilityEngine.Enrich(rubrics, concepts, causationLinks);
        stages.Add("Explainability");

        if (_settings.EnableRepertoryMapping)
        {
            var repertoryResult = await _repertoryTierEngine.EnrichAsync(rubrics, cancellationToken);
            rubrics = repertoryResult.Rubrics;
            stages.Add("RepertoryMapping");
            await LogStageAsync(sessionId, correlationId, "RepertoryMapping", "Success",
                $"Mapped {repertoryResult.MappedRubricCount} rubric(s) to Kent/Complete repertory sources.",
                null, null, cancellationToken);
        }

        var enhancedSymptoms = _symptomExtractionEngine.Merge(symptoms, concepts, understanding.EnhancedSymptoms);
        stages.Add("SymptomExtraction");

        await _intelligenceRepository.SaveConceptsAsync(sessionId, concepts, cancellationToken);
        await _intelligenceRepository.SaveCausationLinksAsync(sessionId, causationLinks, cancellationToken);
        stages.Add("ConceptsPersisted");

        return new RubricIntelligenceAnalysisResult
        {
            Concepts = concepts,
            EnhancedSymptoms = enhancedSymptoms,
            Rubrics = rubrics,
            EngineVersion = "v2",
            RequiresManualApproval = _settings.RequiresManualApproval,
            StagesCompleted = stages,
            CausationLinks = causationLinks,
            PrimarySymptom = primarySymptom,
            ValidationRejectedCount = validationRejected,
        };
    }

    /// <summary>
    /// Lightweight contradiction flag: when supporting text clearly conflicts (e.g. thirst more vs no thirst),
    /// mark ambiguous and keep both search interpretations rather than asserting one confident reading.
    /// </summary>
    private static void ApplyAmbiguityAndSeedSearchTerms(List<ClinicalConceptModel> concepts, string transcript)
    {
        foreach (var concept in concepts)
        {
            ConceptSearchTermBuilder.SeedSearchTermsIfEmpty(concept);

            var hay = $"{concept.RawStatement} {concept.ClinicalMeaning} {concept.HomeopathicMeaning} {transcript}";
            if (!ConceptSearchTermBuilder.DetectContradiction(hay))
                continue;

            concept.IsAmbiguous = true;
            concept.Confidence = Math.Min(concept.Confidence, 0.55m);
            if (!concept.Modalities.Contains("ambiguous", StringComparer.OrdinalIgnoreCase))
                concept.Modalities.Add("ambiguous");

            // Dual thirst interpretations when contradictory source material.
            var terms = new HashSet<string>(concept.SearchTerms, StringComparer.OrdinalIgnoreCase)
            {
                "thirst large quantities",
                "THIRST - large quantities",
                "thirstless",
                "THIRST - wanting",
            };
            concept.SearchTerms = terms.ToList();
            concept.ClinicalMeaning = string.IsNullOrWhiteSpace(concept.ClinicalMeaning)
                ? "Thirst status ambiguous (contradictory transcript)"
                : $"{concept.ClinicalMeaning} [ambiguous:contradictory thirst]";
        }
    }

    private static List<AudioCaseSuggestedRubricModel> MergeKeywordRubrics(
        IReadOnlyList<AudioCaseSuggestedRubricModel> existing,
        IReadOnlyList<AudioCaseSuggestedRubricModel> keywordHits)
    {
        var merged = new Dictionary<int, AudioCaseSuggestedRubricModel>();
        foreach (var rubric in existing.Where(r => r.SubSectionId > 0))
            merged[rubric.SubSectionId] = rubric;

        foreach (var rubric in keywordHits.Where(r => r.SubSectionId > 0))
        {
            if (!merged.TryGetValue(rubric.SubSectionId, out var existingRubric))
            {
                merged[rubric.SubSectionId] = rubric;
                continue;
            }

            // Prefer domain-aware keyword hit when existing is a known bad thirst→MIND-DESIRES pattern.
            var existingName = existingRubric.SubSectionName ?? string.Empty;
            var keywordName = rubric.SubSectionName ?? string.Empty;
            var existingIsPollutedDesire = existingName.Contains("MIND", StringComparison.OrdinalIgnoreCase)
                && existingName.Contains("DESIRE", StringComparison.OrdinalIgnoreCase)
                && !existingName.Contains("THIRST", StringComparison.OrdinalIgnoreCase);
            var keywordIsThirst = keywordName.Contains("THIRST", StringComparison.OrdinalIgnoreCase);

            if (existingIsPollutedDesire && keywordIsThirst)
            {
                merged[rubric.SubSectionId] = rubric;
            }
            else if ((rubric.ConfidenceScore ?? rubric.MatchScore) > (existingRubric.ConfidenceScore ?? existingRubric.MatchScore))
            {
                merged[rubric.SubSectionId] = rubric;
            }
        }

        // Drop polluted MIND-DESIRES when a thirst rubric is present for the same session.
        var hasThirst = merged.Values.Any(r =>
            (r.SubSectionName ?? string.Empty).Contains("THIRST", StringComparison.OrdinalIgnoreCase));
        if (hasThirst)
        {
            foreach (var key in merged.Keys.ToList())
            {
                var name = merged[key].SubSectionName ?? string.Empty;
                if (name.Contains("MIND", StringComparison.OrdinalIgnoreCase)
                    && name.Contains("DESIRE", StringComparison.OrdinalIgnoreCase)
                    && name.Contains("amount of the same", StringComparison.OrdinalIgnoreCase))
                {
                    merged.Remove(key);
                }
            }
        }

        return merged.Values
            .OrderByDescending(r => r.ConfidenceScore ?? r.MatchScore)
            .ToList();
    }

    private static List<AudioCaseSuggestedRubricModel> MergeInferenceRubrics(
        IReadOnlyList<AudioCaseSuggestedRubricModel> existing,
        IReadOnlyList<AudioCaseSuggestedRubricModel> inferred)
    {
        var merged = new Dictionary<int, AudioCaseSuggestedRubricModel>();
        foreach (var rubric in existing.Where(r => r.SubSectionId > 0))
        {
            merged[rubric.SubSectionId] = rubric;
        }

        foreach (var rubric in inferred.Where(r => r.SubSectionId > 0 && !merged.ContainsKey(r.SubSectionId)))
        {
            merged[rubric.SubSectionId] = rubric;
        }

        return merged.Values
            .OrderByDescending(r => r.ConfidenceScore ?? r.MatchScore)
            .ToList();
    }

    private Task LogStageAsync(
        Guid sessionId, string correlationId, string stage, string status,
        string? message, string? detailsJson, int? latencyMs, CancellationToken cancellationToken) =>
        _intelligenceRepository.SaveIntelligenceLogAsync(
            sessionId, correlationId, stage, status, message, detailsJson, latencyMs, cancellationToken);

    private static string? Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLen ? value : value[..(maxLen - 3)] + "...";
    }
}

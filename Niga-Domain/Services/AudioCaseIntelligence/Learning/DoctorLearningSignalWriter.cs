using System.Text.Json;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Services.AudioCaseIntelligence.Learning;

public class DoctorLearningSignalWriter
{
    private readonly NIGACentrumContext _context;
    private readonly RubricIntelligenceOptions _options;

    public DoctorLearningSignalWriter(
        NIGACentrumContext context,
        IOptions<RubricIntelligenceOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public Task<(int SignalsWritten, DoctorLearningAppliedSummaryModel Summary)> WriteAsync(
        Guid sessionId,
        AudioCaseRubricFeedback feedback,
        DoctorLearningFeedbackContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableDoctorLearningEngine)
        {
            return Task.FromResult((0, new DoctorLearningAppliedSummaryModel
            {
                FeedbackType = feedback.FeedbackType,
                SubSectionId = feedback.SubSectionId,
                CorrectedSubSectionId = feedback.CorrectedSubSectionId,
            }));
        }

        var concept = context.ResolvedConceptName?.Trim();
        if (string.IsNullOrWhiteSpace(concept))
        {
            return Task.FromResult((0, new DoctorLearningAppliedSummaryModel
            {
                FeedbackType = feedback.FeedbackType,
                SubSectionId = feedback.SubSectionId,
                CorrectedSubSectionId = feedback.CorrectedSubSectionId,
            }));
        }

        var clinical = context.ClinicalConceptName?.Trim() ?? concept;
        var signals = new List<AiCaseLearning>();

        switch (feedback.FeedbackType)
        {
            case "Accepted":
                AddConceptRubric(signals, sessionId, concept, feedback.SubSectionId, _options.DoctorLearningAcceptBoost, feedback);
                AddConceptRanking(signals, sessionId, concept, _options.DoctorLearningConceptRankingBoost, feedback);
                AddClinicalRelevance(signals, sessionId, clinical, _options.DoctorLearningClinicalRelevanceBoost, feedback);
                AddConfidenceCalibration(signals, sessionId, concept, _options.DoctorLearningAcceptBoost * 0.5m, feedback);
                break;

            case "Rejected":
                AddRejectedSignals(signals, sessionId, concept, clinical, feedback);
                break;

            case "Corrected":
                AddConceptRubric(signals, sessionId, concept, feedback.SubSectionId, _options.DoctorLearningRejectPenalty, feedback);
                if (feedback.CorrectedSubSectionId.HasValue)
                {
                    AddConceptRubric(
                        signals,
                        sessionId,
                        concept,
                        feedback.CorrectedSubSectionId,
                        _options.DoctorLearningCorrectionBoost,
                        feedback,
                        corrected: true);
                    AddConceptRanking(signals, sessionId, concept, _options.DoctorLearningConceptRankingBoost * 0.75m, feedback);
                    AddClinicalRelevance(signals, sessionId, clinical, _options.DoctorLearningClinicalRelevanceBoost * 0.75m, feedback);
                }
                break;
        }

        foreach (var signal in signals)
            _context.AiCaseLearnings.Add(signal);

        return Task.FromResult((signals.Count, new DoctorLearningAppliedSummaryModel
        {
            ConceptName = concept,
            SignalsWritten = signals.Count,
            FeedbackType = feedback.FeedbackType,
            SubSectionId = feedback.SubSectionId,
            CorrectedSubSectionId = feedback.CorrectedSubSectionId,
        }));
    }

    private void AddRejectedSignals(
        ICollection<AiCaseLearning> signals,
        Guid sessionId,
        string concept,
        string clinical,
        AudioCaseRubricFeedback feedback)
    {
        switch (feedback.RejectReasonStage)
        {
            case RejectReasonStages.Meaning:
                AddConfidenceCalibration(signals, sessionId, concept, _options.DoctorLearningRejectPenalty * 0.4m, feedback);
                AddConceptRanking(signals, sessionId, concept, _options.DoctorLearningRejectPenalty * 0.15m, feedback);
                break;
            case RejectReasonStages.Metaphor:
                AddClinicalRelevance(
                    signals, sessionId, clinical, _options.DoctorLearningRejectPenalty * 0.3m, feedback, "Metaphor");
                break;
            case RejectReasonStages.ClinicalConcept:
                AddClinicalRelevance(signals, sessionId, clinical, _options.DoctorLearningRejectPenalty * 0.3m, feedback);
                break;
            case RejectReasonStages.HomeopathicConcept:
                AddConceptRanking(signals, sessionId, concept, _options.DoctorLearningRejectPenalty * 0.5m, feedback);
                break;
            case RejectReasonStages.RubricMapping:
                AddConceptRubric(signals, sessionId, concept, feedback.SubSectionId, _options.DoctorLearningRejectPenalty, feedback);
                break;
            default:
                AddConceptRubric(signals, sessionId, concept, feedback.SubSectionId, _options.DoctorLearningRejectPenalty, feedback);
                AddConceptRanking(signals, sessionId, concept, _options.DoctorLearningRejectPenalty * 0.5m, feedback);
                AddClinicalRelevance(signals, sessionId, clinical, _options.DoctorLearningRejectPenalty * 0.3m, feedback);
                AddConfidenceCalibration(signals, sessionId, concept, _options.DoctorLearningRejectPenalty * 0.4m, feedback);
                break;
        }
    }

    private static void AddConceptRubric(
        ICollection<AiCaseLearning> signals,
        Guid sessionId,
        string concept,
        int? subSectionId,
        decimal weightDelta,
        AudioCaseRubricFeedback feedback,
        bool corrected = false)
    {
        if (!subSectionId.HasValue || weightDelta == 0m)
            return;

        signals.Add(new AiCaseLearning
        {
            SourceSessionId = sessionId,
            LearningType = DoctorLearningTypes.ConceptRubricMapping,
            FromConcept = concept,
            ToRubricSubSectionId = subSectionId,
            WeightDelta = Math.Round(weightDelta, 3),
            ContextJson = BuildContextJson(feedback, corrected ? "CorrectedTarget" : "OriginalTarget"),
            EnteredDate = DateTime.UtcNow,
        });
    }

    private static void AddConceptRanking(
        ICollection<AiCaseLearning> signals,
        Guid sessionId,
        string concept,
        decimal weightDelta,
        AudioCaseRubricFeedback feedback)
    {
        if (weightDelta == 0m) return;

        signals.Add(new AiCaseLearning
        {
            SourceSessionId = sessionId,
            LearningType = DoctorLearningTypes.ConceptRanking,
            FromConcept = concept,
            WeightDelta = Math.Round(weightDelta, 3),
            ContextJson = BuildContextJson(feedback, "ConceptRanking"),
            EnteredDate = DateTime.UtcNow,
        });
    }

    private static void AddClinicalRelevance(
        ICollection<AiCaseLearning> signals,
        Guid sessionId,
        string clinicalConcept,
        decimal weightDelta,
        AudioCaseRubricFeedback feedback,
        string signalKind = "ClinicalRelevance")
    {
        if (weightDelta == 0m || string.IsNullOrWhiteSpace(clinicalConcept)) return;

        signals.Add(new AiCaseLearning
        {
            SourceSessionId = sessionId,
            LearningType = DoctorLearningTypes.ClinicalRelevance,
            FromConcept = clinicalConcept,
            WeightDelta = Math.Round(weightDelta, 3),
            ContextJson = BuildContextJson(feedback, signalKind),
            EnteredDate = DateTime.UtcNow,
        });
    }

    private static void AddConfidenceCalibration(
        ICollection<AiCaseLearning> signals,
        Guid sessionId,
        string concept,
        decimal weightDelta,
        AudioCaseRubricFeedback feedback)
    {
        if (weightDelta == 0m) return;

        signals.Add(new AiCaseLearning
        {
            SourceSessionId = sessionId,
            LearningType = DoctorLearningTypes.ConfidenceCalibration,
            FromConcept = concept,
            WeightDelta = Math.Round(weightDelta, 3),
            ContextJson = BuildContextJson(feedback, "ConfidenceCalibration"),
            EnteredDate = DateTime.UtcNow,
        });
    }

    private static string BuildContextJson(AudioCaseRubricFeedback feedback, string signalKind) =>
        JsonSerializer.Serialize(new
        {
            signalKind,
            feedback.FeedbackType,
            feedback.SubSectionId,
            feedback.CorrectedSubSectionId,
            feedback.RubricName,
            feedback.OriginalMatchLayer,
            feedback.EngineVersion,
            feedback.RejectReasonStage,
            feedback.RejectReasonNote,
        });
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Services.AudioCaseIntelligence.Learning;

public class DoctorFeedbackLearningEngine : IDoctorFeedbackLearningEngine
{
    private static readonly HashSet<string> ValidFeedbackTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accepted", "Approved",
        "Rejected",
        "Corrected", "Edited",
    };

    private readonly NIGACentrumContext _context;
    private readonly IRubricBenchmarkService _benchmarkService;
    private readonly DoctorLearningContextResolver _contextResolver;
    private readonly DoctorLearningSignalWriter _signalWriter;
    private readonly IKgFeedbackGraphWriter _kgFeedbackWriter;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<DoctorFeedbackLearningEngine> _logger;

    public DoctorFeedbackLearningEngine(
        NIGACentrumContext context,
        IRubricBenchmarkService benchmarkService,
        DoctorLearningContextResolver contextResolver,
        DoctorLearningSignalWriter signalWriter,
        IKgFeedbackGraphWriter kgFeedbackWriter,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<DoctorFeedbackLearningEngine> logger)
    {
        _context = context;
        _benchmarkService = benchmarkService;
        _contextResolver = contextResolver;
        _signalWriter = signalWriter;
        _kgFeedbackWriter = kgFeedbackWriter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AudioCaseRubricFeedbackResultModel? Result)> ProcessFeedbackAsync(
        int doctorUserId,
        Guid sessionId,
        AudioCaseRubricFeedbackRequestModel request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!ValidFeedbackTypes.Contains(request.FeedbackType))
            return (false, "FeedbackType must be Approved, Rejected, or Edited (Accepted/Corrected also supported).", null);

        if (string.IsNullOrWhiteSpace(request.RubricName))
            return (false, "RubricName is required.", null);

        var normalizedType = NormalizeFeedbackType(request.FeedbackType);

        if (normalizedType == "Corrected" && !request.CorrectedSubSectionId.HasValue)
            return (false, "CorrectedSubSectionId is required for Edited/Corrected feedback.", null);

        if (!string.IsNullOrWhiteSpace(request.RejectReasonStage)
            && !RejectReasonStages.IsValid(request.RejectReasonStage))
            return (false, "RejectReasonStage is invalid.", null);

        var session = await _context.AudioCaseSessions
            .FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId && x.DoctorUserId == doctorUserId && !x.DeleteStatus, cancellationToken);
        if (session == null)
            return (false, "Session not found.", null);

        var entity = new AudioCaseRubricFeedback
        {
            AudioCaseSessionId = sessionId,
            SubSectionId = request.SubSectionId,
            RubricName = request.RubricName.Trim(),
            FeedbackType = normalizedType,
            OriginalMatchLayer = request.OriginalMatchLayer,
            CorrectedSubSectionId = request.CorrectedSubSectionId,
            Reason = request.Reason,
            RejectReasonStage = normalizedType == "Rejected" ? request.RejectReasonStage?.Trim() : null,
            RejectReasonNote = normalizedType == "Rejected" ? request.RejectReasonNote?.Trim() : null,
            ConfidenceAtFeedback = request.ConfidenceAtFeedback,
            EngineVersion = string.IsNullOrWhiteSpace(request.EngineVersion)
                ? session.IntelligenceEngineVersion ?? (_options.IsV2Active ? "v2" : "v1")
                : request.EngineVersion.Trim(),
            DoctorUserId = doctorUserId,
            EnteredDate = DateTime.UtcNow,
        };

        _context.AudioCaseRubricFeedbacks.Add(entity);
        _context.AiDoctorFeedbacks.Add(new AiDoctorFeedback
        {
            AudioCaseSessionId = sessionId,
            Action = normalizedType,
            Reason = normalizedType == "Rejected"
                ? request.RejectReasonNote?.Trim() ?? request.Reason
                : request.Reason,
            RejectReasonStage = normalizedType == "Rejected" ? request.RejectReasonStage?.Trim() : null,
            RejectReasonNote = normalizedType == "Rejected" ? request.RejectReasonNote?.Trim() : null,
            DoctorUserId = doctorUserId,
            EnteredDate = DateTime.UtcNow,
        });

        _context.AudioCaseDoctorActionLogs.Add(new AudioCaseDoctorActionLog
        {
            AudioCaseSessionId = sessionId,
            DoctorUserId = doctorUserId,
            ActionType = normalizedType switch
            {
                "Accepted" => "RubricAccepted",
                "Rejected" => "RubricRejected",
                _ => "RubricCorrected",
            },
            TargetType = "SubSection",
            TargetId = request.SubSectionId?.ToString() ?? request.RubricName,
            AfterJson = JsonSerializer.Serialize(new
            {
                request.RubricName,
                request.OriginalMatchLayer,
                request.CorrectedSubSectionId,
                request.Reason,
                request.RejectReasonStage,
                request.RejectReasonNote,
                request.ConfidenceAtFeedback,
                request.SourceConceptName,
                request.ClinicalConceptName,
                NormalizedFeedbackType = normalizedType,
            }),
            Notes = request.Reason,
            IpAddress = ipAddress,
            EnteredDate = DateTime.UtcNow,
        });

        DoctorLearningAppliedSummaryModel? learningSummary = null;
        var learningApplied = false;
        var signalsWritten = 0;

        if (_options.EnableDoctorFeedbackLearning || _options.EnableDoctorLearningEngine)
        {
            try
            {
                var learningContext = await _contextResolver.ResolveAsync(sessionId, request, cancellationToken);
                var (written, summary) = await _signalWriter.WriteAsync(sessionId, entity, learningContext, cancellationToken);
                signalsWritten = written;
                learningSummary = summary;
                learningApplied = written > 0 || _options.EnableDoctorLearningEngine;

                await _kgFeedbackWriter.ApplyFeedbackAsync(
                    sessionId, request, entity.FeedbackId, doctorUserId, learningContext, cancellationToken);

                _logger.LogInformation(
                    "Doctor learning session={SessionId} feedback={FeedbackType} concept={Concept} signals={Signals}",
                    sessionId,
                    normalizedType,
                    learningContext.ResolvedConceptName ?? "unknown",
                    written);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Doctor learning signal write failed for session {SessionId}", sessionId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        AudioCaseRubricBenchmarkSnapshotModel? benchmark = null;
        try
        {
            benchmark = await _benchmarkService.RecalculateSessionBenchmarkAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Benchmark recalculation failed for session {SessionId}", sessionId);
        }

        return (true, "Feedback recorded.", new AudioCaseRubricFeedbackResultModel
        {
            FeedbackId = entity.FeedbackId,
            SessionId = sessionId,
            FeedbackType = normalizedType,
            LearningApplied = learningApplied,
            LearningSignalsApplied = signalsWritten,
            LearningSummary = learningSummary,
            SessionBenchmark = benchmark,
        });
    }

    private static string NormalizeFeedbackType(string feedbackType) =>
        feedbackType switch
        {
            var t when string.Equals(t, "Approved", StringComparison.OrdinalIgnoreCase) => "Accepted",
            var t when string.Equals(t, "Accepted", StringComparison.OrdinalIgnoreCase) => "Accepted",
            var t when string.Equals(t, "Rejected", StringComparison.OrdinalIgnoreCase) => "Rejected",
            var t when string.Equals(t, "Edited", StringComparison.OrdinalIgnoreCase) => "Corrected",
            _ => "Corrected",
        };
}

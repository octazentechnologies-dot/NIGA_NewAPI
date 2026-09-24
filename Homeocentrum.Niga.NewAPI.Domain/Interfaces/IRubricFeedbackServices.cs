using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IDoctorFeedbackLearningEngine
{
    Task<(bool Success, string Message, AudioCaseRubricFeedbackResultModel? Result)> ProcessFeedbackAsync(
        int doctorUserId,
        Guid sessionId,
        AudioCaseRubricFeedbackRequestModel request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}

public interface IRubricBenchmarkService
{
    Task<AudioCaseRubricBenchmarkSnapshotModel?> RecalculateSessionBenchmarkAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<RubricBenchmarkSummaryModel> GetSummaryAsync(
        int days = 30,
        CancellationToken cancellationToken = default);

    Task<RubricBenchmarkTrendsModel> GetTrendsAsync(
        int weeks = 12,
        CancellationToken cancellationToken = default);

    Task<RubricFeedbackQueueModel> GetFeedbackQueueAsync(
        int topN = 20,
        CancellationToken cancellationToken = default);
}

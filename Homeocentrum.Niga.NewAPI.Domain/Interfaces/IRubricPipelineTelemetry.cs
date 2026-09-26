using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

/// <summary>
/// Scoped per background job / request. Stage A: additive telemetry only.
/// Does not log transcript or patient-identifying content.
/// </summary>
public interface IRubricPipelineTelemetry
{
    bool IsActive { get; }

    Guid? CaseSessionId { get; }

    void BeginSession(Guid sessionId, string? correlationId);

    /// <summary>Stamp telemetry rows with the same engine version as the session (e.g. fast-f).</summary>
    void SetEngineVersion(string? engineVersion);

    void IncrementLlmCalls(int count = 1);

    void IncrementEmbeddingCalls(int count = 1);

    void IncrementSqlQueries(int count = 1);

    void IncrementCacheHit(int count = 1);

    void IncrementCacheMiss(int count = 1);

    void SetCandidateCount(int count);

    void SetFinalRubricCounts(int finalCount, int dbBackedCount, int aiOnlyCount);

    /// <summary>Records a completed stage and persists it asynchronously when a repository is available.</summary>
    Task RecordStageAsync(
        string stageName,
        long elapsedMilliseconds,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        string status = "Success",
        string? message = null,
        int? candidateCount = null,
        int? finalRubricCount = null,
        CancellationToken cancellationToken = default);

    /// <summary>Convenience: time an async block and record the stage.</summary>
    Task<T> TimeAsync<T>(
        string stageName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        Func<T, int?>? candidateCountSelector = null,
        Func<T, int?>? finalRubricCountSelector = null,
        string? message = null);

    Task TimeAsync(
        string stageName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        string? message = null);

    RubricPipelineTelemetrySummary BuildSummary();

    Task FlushSummaryAsync(CancellationToken cancellationToken = default);
}

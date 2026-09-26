using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Monitoring;

public class RubricPipelineTelemetry : IRubricPipelineTelemetry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly IAudioCaseIntelligenceRepository _intelligenceRepository;
    private readonly ILogger<RubricPipelineTelemetry> _logger;
    private readonly object _gate = new();
    private readonly List<RubricPipelineStageMetric> _stages = new();

    private Guid? _sessionId;
    private string? _correlationId;
    private DateTime _startUtc;
    private int _llmCalls;
    private int _embeddingCalls;
    private int _sqlQueries;
    private int _cacheHits;
    private int _cacheMisses;
    private int _candidateCount;
    private int _finalRubricCount;
    private int _dbBackedRubricCount;
    private int _aiOnlyRubricCount;
    private bool _summaryFlushed;
    private string _engineVersion = RubricEngineStamp.FastFallback;

    public RubricPipelineTelemetry(
        IAudioCaseIntelligenceRepository intelligenceRepository,
        ILogger<RubricPipelineTelemetry> logger)
    {
        _intelligenceRepository = intelligenceRepository;
        _logger = logger;
    }

    public bool IsActive => _sessionId.HasValue;

    public Guid? CaseSessionId => _sessionId;

    public void BeginSession(Guid sessionId, string? correlationId)
    {
        lock (_gate)
        {
            _sessionId = sessionId;
            _correlationId = correlationId ?? sessionId.ToString("N")[..12];
            _startUtc = DateTime.UtcNow;
            _stages.Clear();
            _llmCalls = 0;
            _embeddingCalls = 0;
            _sqlQueries = 0;
            _cacheHits = 0;
            _cacheMisses = 0;
            _candidateCount = 0;
            _finalRubricCount = 0;
            _dbBackedRubricCount = 0;
            _aiOnlyRubricCount = 0;
            _summaryFlushed = false;
            _engineVersion = RubricEngineStamp.FastFallback;
        }
    }

    public void SetEngineVersion(string? engineVersion)
    {
        var stamp = RubricEngineStamp.Normalize(engineVersion, RubricEngineStamp.FastFallback);
        lock (_gate)
        {
            _engineVersion = stamp;
        }
    }

    private string CurrentEngineVersion
    {
        get
        {
            lock (_gate)
            {
                return _engineVersion;
            }
        }
    }

    public void IncrementLlmCalls(int count = 1)
    {
        if (count <= 0 || !IsActive) return;
        Interlocked.Add(ref _llmCalls, count);
    }

    public void IncrementEmbeddingCalls(int count = 1)
    {
        if (count <= 0 || !IsActive) return;
        Interlocked.Add(ref _embeddingCalls, count);
    }

    public void IncrementSqlQueries(int count = 1)
    {
        if (count <= 0 || !IsActive) return;
        Interlocked.Add(ref _sqlQueries, count);
    }

    public void IncrementCacheHit(int count = 1)
    {
        if (count <= 0 || !IsActive) return;
        Interlocked.Add(ref _cacheHits, count);
    }

    public void IncrementCacheMiss(int count = 1)
    {
        if (count <= 0 || !IsActive) return;
        Interlocked.Add(ref _cacheMisses, count);
    }

    public void SetCandidateCount(int count)
    {
        if (!IsActive) return;
        Interlocked.Exchange(ref _candidateCount, Math.Max(0, count));
    }

    public void SetFinalRubricCounts(int finalCount, int dbBackedCount, int aiOnlyCount)
    {
        if (!IsActive) return;
        Interlocked.Exchange(ref _finalRubricCount, Math.Max(0, finalCount));
        Interlocked.Exchange(ref _dbBackedRubricCount, Math.Max(0, dbBackedCount));
        Interlocked.Exchange(ref _aiOnlyRubricCount, Math.Max(0, aiOnlyCount));
    }

    public async Task RecordStageAsync(
        string stageName,
        long elapsedMilliseconds,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        string status = "Success",
        string? message = null,
        int? candidateCount = null,
        int? finalRubricCount = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsActive || string.IsNullOrWhiteSpace(stageName))
            return;

        var metric = new RubricPipelineStageMetric
        {
            StageName = stageName.Trim(),
            StartTimeUtc = startTimeUtc,
            EndTimeUtc = endTimeUtc,
            ElapsedMilliseconds = Math.Max(0, elapsedMilliseconds),
            EngineVersion = CurrentEngineVersion,
            CaseSessionId = _sessionId!.Value,
            CandidateCount = candidateCount,
            FinalRubricCount = finalRubricCount,
            Status = string.IsNullOrWhiteSpace(status) ? "Success" : status,
            Message = Truncate(message, 500),
        };

        lock (_gate)
        {
            _stages.Add(metric);
        }

        _logger.LogInformation(
            "AI_Rubric_Stage Session={SessionId} Stage={Stage} ElapsedMs={Elapsed} Status={Status} Candidates={Candidates} Final={Final} Llm={Llm} Emb={Emb} Sql={Sql}",
            _sessionId,
            metric.StageName,
            metric.ElapsedMilliseconds,
            metric.Status,
            metric.CandidateCount,
            metric.FinalRubricCount,
            Volatile.Read(ref _llmCalls),
            Volatile.Read(ref _embeddingCalls),
            Volatile.Read(ref _sqlQueries));

        try
        {
            await _intelligenceRepository.SaveIntelligenceLogAsync(
                _sessionId!.Value,
                _correlationId,
                metric.StageName,
                metric.Status,
                metric.Message ?? $"elapsedMs={metric.ElapsedMilliseconds}",
                detailsJson: JsonSerializer.Serialize(new
                {
                    metric.StartTimeUtc,
                    metric.EndTimeUtc,
                    metric.ElapsedMilliseconds,
                    metric.EngineVersion,
                    metric.CaseSessionId,
                    metric.CandidateCount,
                    metric.FinalRubricCount,
                    llmCallCount = Volatile.Read(ref _llmCalls),
                    embeddingCallCount = Volatile.Read(ref _embeddingCalls),
                    sqlQueryCount = Volatile.Read(ref _sqlQueries),
                    cacheHitCount = Volatile.Read(ref _cacheHits),
                    cacheMissCount = Volatile.Read(ref _cacheMisses),
                }, JsonOptions),
                (int)Math.Min(int.MaxValue, metric.ElapsedMilliseconds),
                cancellationToken,
                CurrentEngineVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist telemetry stage {Stage} for session {SessionId}", stageName, _sessionId);
        }
    }

    public async Task<T> TimeAsync<T>(
        string stageName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        Func<T, int?>? candidateCountSelector = null,
        Func<T, int?>? finalRubricCountSelector = null,
        string? message = null)
    {
        var start = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action(cancellationToken);
            sw.Stop();
            await RecordStageAsync(
                stageName,
                sw.ElapsedMilliseconds,
                start,
                DateTime.UtcNow,
                "Success",
                message,
                candidateCountSelector?.Invoke(result),
                finalRubricCountSelector?.Invoke(result),
                cancellationToken);
            return result;
        }
        catch (Exception)
        {
            sw.Stop();
            await RecordStageAsync(
                stageName,
                sw.ElapsedMilliseconds,
                start,
                DateTime.UtcNow,
                "Failure",
                message,
                cancellationToken: cancellationToken);
            throw;
        }
    }

    public async Task TimeAsync(
        string stageName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        string? message = null)
    {
        await TimeAsync<object?>(
            stageName,
            async ct =>
            {
                await action(ct);
                return null;
            },
            cancellationToken,
            message: message);
    }

    public RubricPipelineTelemetrySummary BuildSummary()
    {
        List<RubricPipelineStageMetric> stages;
        lock (_gate)
        {
            stages = _stages.Select(CloneStage).ToList();
        }

        var end = DateTime.UtcNow;
        var start = _startUtc == default ? end : _startUtc;
        var summary = new RubricPipelineTelemetrySummary
        {
            CaseSessionId = _sessionId ?? Guid.Empty,
            CorrelationId = _correlationId,
            EngineVersion = CurrentEngineVersion,
            StartTimeUtc = start,
            EndTimeUtc = end,
            TotalElapsedMilliseconds = (long)Math.Max(0, (end - start).TotalMilliseconds),
            LlmCallCount = Volatile.Read(ref _llmCalls),
            EmbeddingCallCount = Volatile.Read(ref _embeddingCalls),
            SqlQueryCount = Volatile.Read(ref _sqlQueries),
            CacheHitCount = Volatile.Read(ref _cacheHits),
            CacheMissCount = Volatile.Read(ref _cacheMisses),
            CandidateCount = Volatile.Read(ref _candidateCount),
            FinalRubricCount = Volatile.Read(ref _finalRubricCount),
            DbBackedRubricCount = Volatile.Read(ref _dbBackedRubricCount),
            AiOnlyRubricCount = Volatile.Read(ref _aiOnlyRubricCount),
            Stages = stages,
            StageElapsedMs = stages
                .GroupBy(s => s.StageName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.ElapsedMilliseconds), StringComparer.OrdinalIgnoreCase),
        };

        return summary;
    }

    public async Task FlushSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (!IsActive || _summaryFlushed)
            return;

        _summaryFlushed = true;
        var summary = BuildSummary();

        _logger.LogInformation(
            "AI_Rubric_TotalLatencyMs={Total} Session={SessionId} Llm={Llm} Emb={Emb} Sql={Sql} Candidates={Cand} Final={Final} DbBacked={Db} AiOnly={Ai} CacheHit={Hit} CacheMiss={Miss}",
            summary.TotalElapsedMilliseconds,
            summary.CaseSessionId,
            summary.LlmCallCount,
            summary.EmbeddingCallCount,
            summary.SqlQueryCount,
            summary.CandidateCount,
            summary.FinalRubricCount,
            summary.DbBackedRubricCount,
            summary.AiOnlyRubricCount,
            summary.CacheHitCount,
            summary.CacheMissCount);

        foreach (var (stage, ms) in summary.StageElapsedMs.OrderByDescending(kv => kv.Value).Take(12))
        {
            _logger.LogInformation(
                "AI_Rubric_StageRank Session={SessionId} Stage={Stage} ElapsedMs={Ms}",
                summary.CaseSessionId,
                stage,
                ms);
        }

        try
        {
            // Persist summary without embedding full stage list twice if huge — keep compact.
            var compact = new
            {
                summary.CaseSessionId,
                summary.CorrelationId,
                summary.EngineVersion,
                summary.StartTimeUtc,
                summary.EndTimeUtc,
                summary.TotalElapsedMilliseconds,
                summary.LlmCallCount,
                summary.EmbeddingCallCount,
                summary.SqlQueryCount,
                summary.CacheHitCount,
                summary.CacheMissCount,
                summary.CandidateCount,
                summary.FinalRubricCount,
                summary.DbBackedRubricCount,
                summary.AiOnlyRubricCount,
                stageElapsedMs = summary.StageElapsedMs,
                stages = summary.Stages.Select(s => new
                {
                    s.StageName,
                    s.ElapsedMilliseconds,
                    s.StartTimeUtc,
                    s.EndTimeUtc,
                    s.CandidateCount,
                    s.FinalRubricCount,
                    s.Status,
                }),
            };

            await _intelligenceRepository.SaveIntelligenceLogAsync(
                summary.CaseSessionId,
                summary.CorrelationId,
                RubricPipelineTelemetryConstants.SummaryStageName,
                "Success",
                $"totalMs={summary.TotalElapsedMilliseconds}; llm={summary.LlmCallCount}; emb={summary.EmbeddingCallCount}; final={summary.FinalRubricCount}",
                JsonSerializer.Serialize(compact, JsonOptions),
                (int)Math.Min(int.MaxValue, summary.TotalElapsedMilliseconds),
                cancellationToken,
                CurrentEngineVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flush pipeline telemetry summary for session {SessionId}", _sessionId);
        }
    }

    private static RubricPipelineStageMetric CloneStage(RubricPipelineStageMetric s) => new()
    {
        StageName = s.StageName,
        StartTimeUtc = s.StartTimeUtc,
        EndTimeUtc = s.EndTimeUtc,
        ElapsedMilliseconds = s.ElapsedMilliseconds,
        EngineVersion = s.EngineVersion,
        CaseSessionId = s.CaseSessionId,
        CandidateCount = s.CandidateCount,
        FinalRubricCount = s.FinalRubricCount,
        LlmCallCountDelta = s.LlmCallCountDelta,
        EmbeddingCallCountDelta = s.EmbeddingCallCountDelta,
        SqlQueryCountDelta = s.SqlQueryCountDelta,
        CacheHitCountDelta = s.CacheHitCountDelta,
        CacheMissCountDelta = s.CacheMissCountDelta,
        Status = s.Status,
        Message = s.Message,
    };

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..(max - 3)] + "...";
}

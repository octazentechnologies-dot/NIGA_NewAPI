using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services.AudioCaseIntelligence.Learning;
using Niga_Domain.Services.AudioCaseIntelligence.Benchmark;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;

namespace Niga_Domain.Repositories;

public class RubricBenchmarkService : IRubricBenchmarkService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const decimal LowAcceptanceThreshold = 0.70m;
    private const int MinUsageForQueue = 3;

    private readonly NIGACentrumContext _context;
    private readonly ILogger<RubricBenchmarkService> _logger;

    public RubricBenchmarkService(NIGACentrumContext context, ILogger<RubricBenchmarkService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AudioCaseRubricBenchmarkSnapshotModel?> RecalculateSessionBenchmarkAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.AudioCaseSessions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId && !x.DeleteStatus, cancellationToken);
            if (session == null)
                return null;

            var suggested = DeserializeSuggestedRubrics(session.SuggestedRubricsJson);
            var feedbacks = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
                .Where(x => x.AudioCaseSessionId == sessionId)
                .ToListAsync(cancellationToken);

            var metrics = BenchmarkMetricsCalculator.Compute(suggested, feedbacks, session.IntelligenceEngineVersion ?? "v1");

            var existing = await _context.AudioCaseRubricBenchmarks
                .FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId, cancellationToken);

            if (existing == null)
            {
                existing = new AudioCaseRubricBenchmark { AudioCaseSessionId = sessionId };
                _context.AudioCaseRubricBenchmarks.Add(existing);
            }

            existing.EngineVersion = metrics.EngineVersion;
            existing.AiSuggestedCount = metrics.AiSuggestedCount;
            existing.DoctorAcceptedCount = metrics.DoctorAcceptedCount;
            existing.DoctorRejectedCount = metrics.DoctorRejectedCount;
            existing.DoctorCorrectedCount = metrics.DoctorCorrectedCount;
            existing.PrimaryInTop5 = metrics.PrimaryInTop5;
            existing.PrecisionScore = metrics.PrecisionScore;
            existing.RecallScore = metrics.RecallScore;
            existing.F1Score = metrics.F1Score;
            existing.AcceptanceRate = metrics.AcceptanceRate;
            existing.FalsePositiveRate = metrics.FalsePositiveRate;
            existing.ConfidenceCalibration = metrics.ConfidenceCalibration;
            existing.CalculatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return MapSnapshot(existing);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session benchmark recalculation failed for {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<RubricBenchmarkSummaryModel> GetSummaryAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 365);
        var cutoff7 = DateTime.UtcNow.AddDays(-7);
        var cutoff30 = DateTime.UtcNow.AddDays(-days);

        var benchmarks30 = await _context.AudioCaseRubricBenchmarks.AsNoTracking()
            .Where(x => x.CalculatedDate >= cutoff30)
            .ToListAsync(cancellationToken);

        var benchmarks7 = benchmarks30.Where(x => x.CalculatedDate >= cutoff7).ToList();

        var totalFeedback = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .CountAsync(x => x.EnteredDate >= cutoff30, cancellationToken);

        var goldCount = await _context.GoldCaseLibraries.AsNoTracking()
            .CountAsync(x => x.IsActive, cancellationToken);

        var topRejected = await GetTopRejectedRubricsAsync(10, cutoff30, cancellationToken);
        var lastGate = await _context.AiRolloutGates.AsNoTracking()
            .OrderByDescending(x => x.BenchmarkRunUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var acceptanceRate30Day = AverageRate(benchmarks30, x => x.AcceptanceRate);
        var top5Rate30Day = AverageBoolRate(benchmarks30, x => x.PrimaryInTop5);
        var fastF = await ComputeFastFAccuracyAsync(cutoff30, benchmarks30, cancellationToken);

        return new RubricBenchmarkSummaryModel
        {
            TotalSessionsBenchmarked = benchmarks30.Count,
            TotalFeedbackCount = totalFeedback,
            GoldCaseCount = goldCount,
            AcceptanceRate7Day = AverageRate(benchmarks7, x => x.AcceptanceRate),
            AcceptanceRate30Day = acceptanceRate30Day,
            PrimaryInTop5Rate7Day = AverageBoolRate(benchmarks7, x => x.PrimaryInTop5),
            PrimaryInTop5Rate30Day = top5Rate30Day,
            FalsePositiveRate30Day = AverageRate(benchmarks30, x => x.FalsePositiveRate),
            F1Score30Day = AverageRate(benchmarks30, x => x.F1Score),
            V1AcceptanceRate30Day = AverageRate(
                benchmarks30.Where(x => string.Equals(x.EngineVersion, "v1", StringComparison.OrdinalIgnoreCase)),
                x => x.AcceptanceRate),
            V2AcceptanceRate30Day = AverageRate(
                benchmarks30.Where(x => string.Equals(x.EngineVersion, "v2", StringComparison.OrdinalIgnoreCase)),
                x => x.AcceptanceRate),
            LastGateTimestamp = lastGate?.BenchmarkRunUtc,
            LastGateTop5Accuracy = lastGate?.Top5Accuracy,
            DeltaTop5VsLastGate = top5Rate30Day.HasValue && lastGate?.Top5Accuracy is decimal gateTop5
                ? Math.Round(top5Rate30Day.Value - gateTop5, 4)
                : null,
            DeltaAcceptanceVsLastGate = acceptanceRate30Day.HasValue && lastGate?.DoctorAcceptanceRate is decimal gateAcceptance
                ? Math.Round(acceptanceRate30Day.Value - gateAcceptance, 4)
                : null,
            TopRejectedRubrics = topRejected,
            GeneratedAtUtc = DateTime.UtcNow,
            FastFSessionCount = fastF.SessionCount,
            FastFFeedbackCount = fastF.FeedbackCount,
            FastFAcceptanceRate = fastF.AcceptanceRate,
            FastFPrimaryInTop5Rate = fastF.PrimaryInTop5Rate,
            FastFFalsePositiveRate = fastF.FalsePositiveRate,
            FastFPrecisionAt1 = fastF.PrecisionAt1,
            FastFPrecisionAt3 = fastF.PrecisionAt3,
            FastFPrecisionAt5 = fastF.PrecisionAt5,
            FastFRecallAt5 = fastF.RecallAt5,
            FastFPrecisionAt10 = fastF.PrecisionAt10,
        };
    }

    public async Task<RubricBenchmarkTrendsModel> GetTrendsAsync(int weeks = 12, CancellationToken cancellationToken = default)
    {
        weeks = Math.Clamp(weeks, 1, 52);
        var start = DateTime.UtcNow.Date.AddDays(-7 * weeks);

        var benchmarks = await _context.AudioCaseRubricBenchmarks.AsNoTracking()
            .Where(x => x.CalculatedDate >= start)
            .ToListAsync(cancellationToken);

        var points = new List<RubricBenchmarkTrendPointModel>();
        for (var i = 0; i < weeks; i++)
        {
            var weekStart = DateTime.UtcNow.Date.AddDays(-7 * (weeks - i));
            var weekEnd = weekStart.AddDays(7);
            var weekRows = benchmarks.Where(x => x.CalculatedDate >= weekStart && x.CalculatedDate < weekEnd).ToList();
            if (weekRows.Count == 0)
                continue;

            points.Add(new RubricBenchmarkTrendPointModel
            {
                WeekStartUtc = weekStart,
                EngineVersion = "all",
                SessionCount = weekRows.Count,
                AcceptanceRate = AverageRate(weekRows, x => x.AcceptanceRate),
                PrimaryInTop5Rate = AverageBoolRate(weekRows, x => x.PrimaryInTop5),
                FalsePositiveRate = AverageRate(weekRows, x => x.FalsePositiveRate),
            });
        }

        return new RubricBenchmarkTrendsModel
        {
            WeeksRequested = weeks,
            Weeks = points,
            FastFWeeks = BuildWeeklyPoints(benchmarks, weeks, engine => RubricEngineStamp.IsFastPipeline(engine)),
        };
    }

    public async Task<RubricFeedbackQueueModel> GetFeedbackQueueAsync(int topN = 20, CancellationToken cancellationToken = default)
    {
        topN = Math.Clamp(topN, 1, 100);
        var cutoff = DateTime.UtcNow.AddDays(-90);

        var lowAliases = await _context.RubricAliases.AsNoTracking()
            .Where(x => x.IsActive && x.UsageCount >= MinUsageForQueue && x.AcceptanceRate != null && x.AcceptanceRate < LowAcceptanceThreshold)
            .OrderBy(x => x.AcceptanceRate)
            .Take(topN)
            .Select(x => new RubricFeedbackQueueItemModel
            {
                ItemType = "Alias",
                EntityId = x.RubricAliasId,
                DisplayText = x.AliasText,
                SubSectionId = x.SubSectionId,
                UsageCount = x.UsageCount,
                AcceptanceRate = x.AcceptanceRate,
                Language = x.Language,
            })
            .ToListAsync(cancellationToken);

        var lowMetaphors = await _context.RubricMetaphorDictionaries.AsNoTracking()
            .Where(x => x.IsActive && x.UsageCount >= MinUsageForQueue && x.AcceptanceRate != null && x.AcceptanceRate < LowAcceptanceThreshold)
            .OrderBy(x => x.AcceptanceRate)
            .Take(topN)
            .Select(x => new RubricFeedbackQueueItemModel
            {
                ItemType = "Metaphor",
                EntityId = x.MetaphorId,
                DisplayText = x.PatientExpression,
                SubSectionId = x.SubSectionId,
                UsageCount = x.UsageCount,
                AcceptanceRate = x.AcceptanceRate,
                Language = x.Language,
            })
            .ToListAsync(cancellationToken);

        var topRejected = await GetTopRejectedRubricsAsync(topN, cutoff, cancellationToken);

        return new RubricFeedbackQueueModel
        {
            LowAcceptanceAliases = lowAliases,
            LowAcceptanceMetaphors = lowMetaphors,
            TopRejectedRubrics = topRejected,
        };
    }

    private async Task<List<RubricBenchmarkTopRejectedModel>> GetTopRejectedRubricsAsync(
        int topN,
        DateTime since,
        CancellationToken cancellationToken)
    {
        return await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .Where(x => x.EnteredDate >= since && x.FeedbackType == "Rejected")
            .GroupBy(x => new { x.RubricName, x.SubSectionId, x.OriginalMatchLayer })
            .Select(g => new RubricBenchmarkTopRejectedModel
            {
                RubricName = g.Key.RubricName,
                SubSectionId = g.Key.SubSectionId,
                RejectionCount = g.Count(),
                MostCommonMatchLayer = g.Key.OriginalMatchLayer,
            })
            .OrderByDescending(x => x.RejectionCount)
            .Take(topN)
            .ToListAsync(cancellationToken);
    }

    private async Task<FastFAccuracySnapshot> ComputeFastFAccuracyAsync(
        DateTime cutoffUtc,
        IReadOnlyList<AudioCaseRubricBenchmark> benchmarks30,
        CancellationToken cancellationToken)
    {
        var fastBenchmarks = benchmarks30.Where(x => RubricEngineStamp.IsFastPipeline(x.EngineVersion)).ToList();

        var fastSessions = await _context.AudioCaseSessions.AsNoTracking()
            .Where(s => !s.DeleteStatus
                && s.Status == "Completed"
                && s.IntelligenceEngineVersion != null
                && s.IntelligenceEngineVersion.StartsWith("fast"))
            .OrderByDescending(s => s.CompletedAtUtc)
            .Take(200)
            .Select(s => new { s.AudioCaseSessionId, s.SuggestedRubricsJson, s.CompletedAtUtc })
            .ToListAsync(cancellationToken);

        var recentFastIds = fastSessions
            .Where(s => s.CompletedAtUtc == null || s.CompletedAtUtc >= cutoffUtc)
            .Select(s => s.AudioCaseSessionId)
            .ToList();

        var feedbacks = recentFastIds.Count == 0
            ? new List<AudioCaseRubricFeedback>()
            : await _context.AudioCaseRubricFeedbacks.AsNoTracking()
                .Where(f => recentFastIds.Contains(f.AudioCaseSessionId))
                .ToListAsync(cancellationToken);

        var feedbackBySession = feedbacks
            .GroupBy(f => f.AudioCaseSessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var precisionAt1 = new List<decimal>();
        var precisionAt3 = new List<decimal>();
        var precisionAt5 = new List<decimal>();
        var recallAt5 = new List<decimal>();
        var precisionAt10 = new List<decimal>();
        var sessionsWithAccepted = 0;
        var primaryInTop5Hits = 0;

        foreach (var session in fastSessions.Where(s => recentFastIds.Contains(s.AudioCaseSessionId)))
        {
            if (!feedbackBySession.TryGetValue(session.AudioCaseSessionId, out var sessionFeedback)
                || sessionFeedback.Count == 0)
            {
                continue;
            }

            var accepted = sessionFeedback
                .Where(f => string.Equals(f.FeedbackType, "Accepted", StringComparison.OrdinalIgnoreCase)
                    && f.SubSectionId.GetValueOrDefault() > 0)
                .Select(f => f.SubSectionId!.Value)
                .ToHashSet();
            if (accepted.Count == 0)
                continue;

            sessionsWithAccepted++;
            var suggested = DeserializeSuggestedRubrics(session.SuggestedRubricsJson);
            var at1 = RubricBenchmarkMetrics.ComputeFromRubrics(suggested, accepted, 1);
            var at3 = RubricBenchmarkMetrics.ComputeFromRubrics(suggested, accepted, 3);
            var at5 = RubricBenchmarkMetrics.ComputeFromRubrics(suggested, accepted, 5);
            var at10 = RubricBenchmarkMetrics.ComputeFromRubrics(suggested, accepted, 10);
            precisionAt1.Add(at1.PrecisionAtK);
            precisionAt3.Add(at3.PrecisionAtK);
            precisionAt5.Add(at5.PrecisionAtK);
            recallAt5.Add(at5.RecallAtK);
            precisionAt10.Add(at10.PrecisionAtK);
            if (at5.HitCount > 0)
                primaryInTop5Hits++;
        }

        var acceptedCount = feedbacks.Count(f => string.Equals(f.FeedbackType, "Accepted", StringComparison.OrdinalIgnoreCase));
        var rejectedCount = feedbacks.Count(f => string.Equals(f.FeedbackType, "Rejected", StringComparison.OrdinalIgnoreCase));

        return new FastFAccuracySnapshot
        {
            SessionCount = fastBenchmarks.Count > 0 ? fastBenchmarks.Count : recentFastIds.Count,
            FeedbackCount = feedbacks.Count,
            AcceptanceRate = AverageRate(fastBenchmarks, x => x.AcceptanceRate)
                ?? (feedbacks.Count == 0 ? null : Math.Round(acceptedCount / (decimal)feedbacks.Count, 4)),
            PrimaryInTop5Rate = AverageBoolRate(fastBenchmarks, x => x.PrimaryInTop5)
                ?? (sessionsWithAccepted == 0
                    ? null
                    : Math.Round(primaryInTop5Hits / (decimal)sessionsWithAccepted, 4)),
            FalsePositiveRate = AverageRate(fastBenchmarks, x => x.FalsePositiveRate)
                ?? (feedbacks.Count == 0 ? null : Math.Round(rejectedCount / (decimal)feedbacks.Count, 4)),
            PrecisionAt1 = AverageOrNull(precisionAt1),
            PrecisionAt3 = AverageOrNull(precisionAt3),
            PrecisionAt5 = AverageOrNull(precisionAt5),
            RecallAt5 = AverageOrNull(recallAt5),
            PrecisionAt10 = AverageOrNull(precisionAt10),
        };
    }

    private static List<RubricBenchmarkTrendPointModel> BuildWeeklyPoints(
        IReadOnlyList<AudioCaseRubricBenchmark> benchmarks,
        int weeks,
        Func<string?, bool> enginePredicate)
    {
        var points = new List<RubricBenchmarkTrendPointModel>();
        for (var i = 0; i < weeks; i++)
        {
            var weekStart = DateTime.UtcNow.Date.AddDays(-7 * (weeks - i));
            var weekEnd = weekStart.AddDays(7);
            var weekRows = benchmarks
                .Where(x => x.CalculatedDate >= weekStart
                    && x.CalculatedDate < weekEnd
                    && enginePredicate(x.EngineVersion))
                .ToList();
            if (weekRows.Count == 0)
                continue;

            points.Add(new RubricBenchmarkTrendPointModel
            {
                WeekStartUtc = weekStart,
                EngineVersion = "fast-f",
                SessionCount = weekRows.Count,
                AcceptanceRate = AverageRate(weekRows, x => x.AcceptanceRate),
                PrimaryInTop5Rate = AverageBoolRate(weekRows, x => x.PrimaryInTop5),
                FalsePositiveRate = AverageRate(weekRows, x => x.FalsePositiveRate),
            });
        }

        return points;
    }

    private static decimal? AverageOrNull(IReadOnlyList<decimal> values) =>
        values.Count == 0 ? null : Math.Round(values.Average(), 4);

    private sealed class FastFAccuracySnapshot
    {
        public int SessionCount { get; init; }
        public int FeedbackCount { get; init; }
        public decimal? AcceptanceRate { get; init; }
        public decimal? PrimaryInTop5Rate { get; init; }
        public decimal? FalsePositiveRate { get; init; }
        public decimal? PrecisionAt1 { get; init; }
        public decimal? PrecisionAt3 { get; init; }
        public decimal? PrecisionAt5 { get; init; }
        public decimal? RecallAt5 { get; init; }
        public decimal? PrecisionAt10 { get; init; }
    }

    private static List<AudioCaseSuggestedRubricModel> DeserializeSuggestedRubrics(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<AudioCaseSuggestedRubricModel>();

        try
        {
            return JsonSerializer.Deserialize<List<AudioCaseSuggestedRubricModel>>(json, JsonOptions)
                ?? new List<AudioCaseSuggestedRubricModel>();
        }
        catch
        {
            return new List<AudioCaseSuggestedRubricModel>();
        }
    }

    private static decimal? AverageRate(IEnumerable<AudioCaseRubricBenchmark> rows, Func<AudioCaseRubricBenchmark, decimal?> selector)
    {
        var values = rows.Select(selector).Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return values.Count == 0 ? null : Math.Round(values.Average(), 4);
    }

    private static decimal? AverageBoolRate(IEnumerable<AudioCaseRubricBenchmark> rows, Func<AudioCaseRubricBenchmark, bool?> selector)
    {
        var values = rows.Select(selector).Where(x => x.HasValue).Select(x => x!.Value).ToList();
        if (values.Count == 0)
            return null;

        return Math.Round((decimal)values.Count(x => x) / values.Count, 4);
    }

    private static AudioCaseRubricBenchmarkSnapshotModel MapSnapshot(AudioCaseRubricBenchmark row) => new()
    {
        AiSuggestedCount = row.AiSuggestedCount,
        DoctorAcceptedCount = row.DoctorAcceptedCount,
        DoctorRejectedCount = row.DoctorRejectedCount,
        DoctorCorrectedCount = row.DoctorCorrectedCount,
        PrimaryInTop5 = row.PrimaryInTop5,
        PrecisionScore = row.PrecisionScore,
        RecallScore = row.RecallScore,
        F1Score = row.F1Score,
        AcceptanceRate = row.AcceptanceRate,
        FalsePositiveRate = row.FalsePositiveRate,
        ConfidenceCalibration = row.ConfidenceCalibration,
        EngineVersion = row.EngineVersion,
        CalculatedDateUtc = row.CalculatedDate,
    };
}

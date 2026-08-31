using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services.AudioCaseIntelligence.Monitoring;

namespace Niga_Domain.Repositories;

public class AiMonitoringDashboardService : IAiMonitoringDashboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly NIGACentrumContext _context;
    private readonly AiMonitoringAuditLogger _auditLogger;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<AiMonitoringDashboardService> _logger;

    public AiMonitoringDashboardService(
        NIGACentrumContext context,
        AiMonitoringAuditLogger auditLogger,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<AiMonitoringDashboardService> logger)
    {
        _context = context;
        _auditLogger = auditLogger;
        _options = options.Value;
        _logger = logger;
    }

    public Task<AiMonitoringDashboardOverviewModel> GetOverviewAsync(
        int days = 30,
        int? actorUserId = null,
        CancellationToken cancellationToken = default) =>
        _auditLogger.TrackAsync(
            nameof(GetOverviewAsync),
            actorUserId,
            () => BuildOverviewAsync(days, cancellationToken),
            new { days },
            cancellationToken);

    public Task<AiMonitoringTrendsModel> GetTrendsAsync(
        int days = 30,
        string granularity = "daily",
        int? actorUserId = null,
        CancellationToken cancellationToken = default) =>
        _auditLogger.TrackAsync(
            nameof(GetTrendsAsync),
            actorUserId,
            () => BuildTrendsAsync(days, granularity, cancellationToken),
            new { days, granularity },
            cancellationToken);

    public Task<AiMonitoringChartSeriesModel> GetChartAsync(
        string metricKey,
        int days = 30,
        int? actorUserId = null,
        CancellationToken cancellationToken = default) =>
        _auditLogger.TrackAsync(
            nameof(GetChartAsync),
            actorUserId,
            () => BuildChartAsync(metricKey, days, cancellationToken),
            new { metricKey, days },
            cancellationToken);

    public Task<AiMonitoringEmbeddingHealthModel> GetEmbeddingHealthAsync(
        int? actorUserId = null,
        CancellationToken cancellationToken = default) =>
        _auditLogger.TrackAsync(
            nameof(GetEmbeddingHealthAsync),
            actorUserId,
            async () => AiMonitoringMetricsCalculator.BuildEmbeddingHealth(await LoadEmbeddingSnapshotAsync(cancellationToken)),
            null,
            cancellationToken);

    public Task<AiMonitoringHallucinationSummaryModel> GetHallucinationSummaryAsync(
        int days = 30,
        int? actorUserId = null,
        CancellationToken cancellationToken = default) =>
        _auditLogger.TrackAsync(
            nameof(GetHallucinationSummaryAsync),
            actorUserId,
            () => BuildHallucinationSummaryAsync(days, cancellationToken),
            new { days },
            cancellationToken);

    public async Task<AiMonitoringSnapshotRefreshResultModel> RefreshDailySnapshotAsync(
        DateTime? snapshotDate = null,
        int? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableAiMonitoringDashboard)
        {
            return new AiMonitoringSnapshotRefreshResultModel
            {
                SnapshotDate = (snapshotDate ?? DateTime.UtcNow.Date),
                EngineVersion = "all",
            };
        }

        var date = DateOnly.FromDateTime((snapshotDate ?? DateTime.UtcNow).Date);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await UpsertDailySnapshotAsync(date, cancellationToken);
            stopwatch.Stop();
            await _auditLogger.LogSnapshotRefreshAsync(
                actorUserId,
                date.ToDateTime(TimeOnly.MinValue),
                true,
                null,
                (int)stopwatch.ElapsedMilliseconds,
                cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await _auditLogger.LogSnapshotRefreshAsync(
                actorUserId,
                date.ToDateTime(TimeOnly.MinValue),
                false,
                ex.Message,
                (int)stopwatch.ElapsedMilliseconds,
                cancellationToken);
            _logger.LogError(ex, "Daily monitoring snapshot refresh failed for {SnapshotDate}", date);
            throw;
        }
    }

    public async Task<AiMonitoringAuditLogModel> GetAuditLogAsync(
        int topN = 50,
        CancellationToken cancellationToken = default)
    {
        topN = Math.Clamp(topN, 1, 500);
        var rows = await _context.AiMonitoringAuditLogs.AsNoTracking()
            .OrderByDescending(x => x.EnteredDate)
            .Take(topN)
            .ToListAsync(cancellationToken);

        var total = await _context.AiMonitoringAuditLogs.AsNoTracking().CountAsync(cancellationToken);

        return new AiMonitoringAuditLogModel
        {
            TotalCount = total,
            Entries = rows.Select(MapAudit).ToList(),
        };
    }

    private async Task<AiMonitoringDashboardOverviewModel> BuildOverviewAsync(
        int days,
        CancellationToken cancellationToken)
    {
        days = Math.Clamp(days, 1, 365);
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var previousCutoff = DateTime.UtcNow.AddDays(-days * 2);

        var benchmarks = await _context.AudioCaseRubricBenchmarks.AsNoTracking()
            .Where(x => x.CalculatedDate >= cutoff)
            .ToListAsync(cancellationToken);

        var previousBenchmarks = await _context.AudioCaseRubricBenchmarks.AsNoTracking()
            .Where(x => x.CalculatedDate >= previousCutoff && x.CalculatedDate < cutoff)
            .ToListAsync(cancellationToken);

        var coverage = await _context.AiCaseCoverageMetrics.AsNoTracking()
            .Where(x => x.EnteredDate >= cutoff)
            .ToListAsync(cancellationToken);

        var confidence = await _context.AiRubricConfidences.AsNoTracking()
            .Where(x => x.EnteredDate >= cutoff)
            .ToListAsync(cancellationToken);

        var validations = await _context.AiRubricValidationsV3.AsNoTracking()
            .Where(x => x.ValidatedAt >= cutoff)
            .ToListAsync(cancellationToken);

        var feedbackCount = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .CountAsync(x => x.EnteredDate >= cutoff, cancellationToken);

        var embedding = await LoadEmbeddingSnapshotAsync(cancellationToken);
        var current = AiMonitoringMetricsCalculator.ComputeDaily(
            benchmarks,
            coverage,
            confidence,
            validations,
            feedbackCount,
            embedding);

        var previousCoverage = await _context.AiCaseCoverageMetrics.AsNoTracking()
            .Where(x => x.EnteredDate >= previousCutoff && x.EnteredDate < cutoff)
            .ToListAsync(cancellationToken);
        var previousConfidence = await _context.AiRubricConfidences.AsNoTracking()
            .Where(x => x.EnteredDate >= previousCutoff && x.EnteredDate < cutoff)
            .ToListAsync(cancellationToken);
        var previousValidations = await _context.AiRubricValidationsV3.AsNoTracking()
            .Where(x => x.ValidatedAt >= previousCutoff && x.ValidatedAt < cutoff)
            .ToListAsync(cancellationToken);
        var previousFeedbackCount = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .CountAsync(x => x.EnteredDate >= previousCutoff && x.EnteredDate < cutoff, cancellationToken);

        var previous = AiMonitoringMetricsCalculator.ComputeDaily(
            previousBenchmarks,
            previousCoverage,
            previousConfidence,
            previousValidations,
            previousFeedbackCount,
            embedding);

        return new AiMonitoringDashboardOverviewModel
        {
            EngineVersion = "v10",
            DaysAnalyzed = days,
            SessionsAnalyzed = current.SessionsAnalyzed,
            FeedbackCount = current.FeedbackCount,
            Kpis = AiMonitoringMetricsCalculator.BuildKpiCards(current, previous),
            EmbeddingHealth = AiMonitoringMetricsCalculator.BuildEmbeddingHealth(embedding),
            GeneratedAtUtc = DateTime.UtcNow,
        };
    }

    private async Task<AiMonitoringTrendsModel> BuildTrendsAsync(
        int days,
        string granularity,
        CancellationToken cancellationToken)
    {
        days = Math.Clamp(days, 1, 365);
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);

        var snapshots = await _context.AiMonitoringDailySnapshots.AsNoTracking()
            .Where(x => x.SnapshotDate >= cutoff && x.EngineVersion == "all")
            .OrderBy(x => x.SnapshotDate)
            .ToListAsync(cancellationToken);

        if (snapshots.Count == 0)
            snapshots = await BuildFallbackSnapshotsAsync(cutoff, cancellationToken);

        return new AiMonitoringTrendsModel
        {
            DaysRequested = days,
            Granularity = granularity,
            Series =
            [
                AiMonitoringMetricsCalculator.BuildSeries(AiMonitoringMetricKeys.Precision, "Precision", snapshots, x => x.PrecisionScore),
                AiMonitoringMetricsCalculator.BuildSeries(AiMonitoringMetricKeys.Recall, "Recall", snapshots, x => x.RecallScore),
                AiMonitoringMetricsCalculator.BuildSeries(AiMonitoringMetricKeys.DoctorAcceptance, "Doctor Acceptance", snapshots, x => x.DoctorAcceptanceRate),
                AiMonitoringMetricsCalculator.BuildSeries(AiMonitoringMetricKeys.HallucinationRate, "Hallucination Rate", snapshots, x => x.HallucinationRate, "bar"),
                AiMonitoringMetricsCalculator.BuildSeries(AiMonitoringMetricKeys.TranscriptCoverage, "Transcript Coverage", snapshots, x => x.TranscriptCoverage),
                AiMonitoringMetricsCalculator.BuildSeries(AiMonitoringMetricKeys.AverageConfidence, "Average Confidence", snapshots, x => x.AverageConfidence),
                AiMonitoringMetricsCalculator.BuildSeries(AiMonitoringMetricKeys.PrimaryRubricAccuracy, "Primary Rubric Accuracy", snapshots, x => x.PrimaryRubricAccuracy),
                AiMonitoringMetricsCalculator.BuildSeries("RubricEmbeddingCoverage", "Rubric Embedding Coverage", snapshots, x => x.RubricEmbeddingCoverage),
                AiMonitoringMetricsCalculator.BuildSeries("ConceptEmbeddingCoverage", "Concept Embedding Coverage", snapshots, x => x.ConceptEmbeddingCoverage),
            ],
            GeneratedAtUtc = DateTime.UtcNow,
        };
    }

    private async Task<AiMonitoringChartSeriesModel> BuildChartAsync(
        string metricKey,
        int days,
        CancellationToken cancellationToken)
    {
        var trends = await BuildTrendsAsync(days, "daily", cancellationToken);
        var series = trends.Series.FirstOrDefault(s =>
            string.Equals(s.MetricKey, metricKey, StringComparison.OrdinalIgnoreCase));

        if (series != null)
            return series;

        throw new ArgumentException($"Unknown monitoring metric key: {metricKey}", nameof(metricKey));
    }

    private async Task<AiMonitoringHallucinationSummaryModel> BuildHallucinationSummaryAsync(
        int days,
        CancellationToken cancellationToken)
    {
        days = Math.Clamp(days, 1, 365);
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var validations = await _context.AiRubricValidationsV3.AsNoTracking()
            .Where(x => x.ValidatedAt >= cutoff)
            .ToListAsync(cancellationToken);

        var rejected = validations.Count(x =>
            string.Equals(x.ValidationStatus, "Rejected", StringComparison.OrdinalIgnoreCase));

        var hallucinations = validations.Count(x =>
            string.Equals(x.ValidationStatus, "Rejected", StringComparison.OrdinalIgnoreCase)
            && x.ValidationFlagsJson != null
            && x.ValidationFlagsJson.Contains("Hallucination", StringComparison.OrdinalIgnoreCase));

        var dailyTrend = validations
            .GroupBy(x => x.ValidatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AiMonitoringChartPointModel
            {
                TimestampUtc = g.Key,
                Label = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(x =>
                    x.ValidationFlagsJson != null
                    && x.ValidationFlagsJson.Contains("Hallucination", StringComparison.OrdinalIgnoreCase)),
                Value = g.Count() == 0
                    ? null
                    : Math.Round((decimal)g.Count(x =>
                        x.ValidationFlagsJson != null
                        && x.ValidationFlagsJson.Contains("Hallucination", StringComparison.OrdinalIgnoreCase)) / g.Count(), 4),
            })
            .ToList();

        return new AiMonitoringHallucinationSummaryModel
        {
            DaysAnalyzed = days,
            TotalValidationEvents = validations.Count,
            HallucinationCount = hallucinations,
            HallucinationRate = validations.Count == 0 ? null : Math.Round((decimal)hallucinations / validations.Count, 4),
            RejectedRubricCount = rejected,
            DailyTrend = dailyTrend,
            GeneratedAtUtc = DateTime.UtcNow,
        };
    }

    private async Task<AiMonitoringSnapshotRefreshResultModel> UpsertDailySnapshotAsync(
        DateOnly snapshotDate,
        CancellationToken cancellationToken)
    {
        var dayStart = snapshotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var benchmarks = await _context.AudioCaseRubricBenchmarks.AsNoTracking()
            .Where(x => x.CalculatedDate >= dayStart && x.CalculatedDate < dayEnd)
            .ToListAsync(cancellationToken);

        var coverage = await _context.AiCaseCoverageMetrics.AsNoTracking()
            .Where(x => x.EnteredDate >= dayStart && x.EnteredDate < dayEnd)
            .ToListAsync(cancellationToken);

        var confidence = await _context.AiRubricConfidences.AsNoTracking()
            .Where(x => x.EnteredDate >= dayStart && x.EnteredDate < dayEnd)
            .ToListAsync(cancellationToken);

        var validations = await _context.AiRubricValidationsV3.AsNoTracking()
            .Where(x => x.ValidatedAt >= dayStart && x.ValidatedAt < dayEnd)
            .ToListAsync(cancellationToken);

        var feedbackCount = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .CountAsync(x => x.EnteredDate >= dayStart && x.EnteredDate < dayEnd, cancellationToken);

        var embedding = await LoadEmbeddingSnapshotAsync(cancellationToken);
        var computed = AiMonitoringMetricsCalculator.ComputeDaily(
            benchmarks,
            coverage,
            confidence,
            validations,
            feedbackCount,
            embedding);

        var existing = await _context.AiMonitoringDailySnapshots
            .FirstOrDefaultAsync(x => x.SnapshotDate == dayStart && x.EngineVersion == "all", cancellationToken);

        var created = existing == null;
        existing ??= new AiMonitoringDailySnapshot
        {
            SnapshotDate = dayStart,
            EngineVersion = "all",
        };

        existing.SessionsAnalyzed = computed.SessionsAnalyzed;
        existing.FeedbackCount = computed.FeedbackCount;
        existing.PrecisionScore = computed.PrecisionScore;
        existing.RecallScore = computed.RecallScore;
        existing.DoctorAcceptanceRate = computed.DoctorAcceptanceRate;
        existing.HallucinationCount = computed.HallucinationCount;
        existing.HallucinationRate = computed.HallucinationRate;
        existing.EmbeddingFreshnessHours = computed.EmbeddingFreshnessHours;
        existing.RubricEmbeddingCoverage = computed.RubricEmbeddingCoverage;
        existing.ConceptEmbeddingCoverage = computed.ConceptEmbeddingCoverage;
        existing.TranscriptCoverage = computed.TranscriptCoverage;
        existing.AverageConfidence = computed.AverageConfidence;
        existing.PrimaryRubricAccuracy = computed.PrimaryRubricAccuracy;
        existing.F1Score = computed.F1Score;
        existing.MetricsJson = JsonSerializer.Serialize(computed, JsonOptions);
        existing.CalculatedDate = DateTime.UtcNow;

        if (created)
            _context.AiMonitoringDailySnapshots.Add(existing);

        await _context.SaveChangesAsync(cancellationToken);

        return new AiMonitoringSnapshotRefreshResultModel
        {
            SnapshotDate = dayStart,
            EngineVersion = "all",
            Created = created,
            Updated = !created,
            Snapshot = MapSnapshot(existing),
        };
    }

    private async Task<List<AiMonitoringDailySnapshot>> BuildFallbackSnapshotsAsync(
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        var benchmarks = await _context.AudioCaseRubricBenchmarks.AsNoTracking()
            .Where(x => x.CalculatedDate >= cutoff)
            .ToListAsync(cancellationToken);

        return benchmarks
            .GroupBy(x => x.CalculatedDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var rows = g.ToList();
                return new AiMonitoringDailySnapshot
                {
                    SnapshotDate = g.Key,
                    EngineVersion = "all",
                    SessionsAnalyzed = rows.Count,
                    PrecisionScore = Average(rows.Select(x => x.PrecisionScore)),
                    RecallScore = Average(rows.Select(x => x.RecallScore)),
                    DoctorAcceptanceRate = Average(rows.Select(x => x.AcceptanceRate)),
                    PrimaryRubricAccuracy = AverageBool(rows.Select(x => x.PrimaryInTop5)),
                    F1Score = Average(rows.Select(x => x.F1Score)),
                    CalculatedDate = g.Max(x => x.CalculatedDate),
                };
            })
            .ToList();
    }

    private async Task<AiMonitoringEmbeddingSnapshot> LoadEmbeddingSnapshotAsync(CancellationToken cancellationToken)
    {
        var version = await _context.AiEmbeddingVersions.AsNoTracking()
            .Where(x => x.IsCurrent && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            return new AiMonitoringEmbeddingSnapshot();
        }

        var totalRubrics = await _context.SubSectionMasters.AsNoTracking()
            .CountAsync(x => x.DeleteStatus != true, cancellationToken);

        var embeddedRubrics = await _context.AiRubricEmbeddings.AsNoTracking()
            .Where(x => x.EmbeddingVersionId == version.EmbeddingVersionId
                && !x.IsDeleted
                && x.Status == "Ready"
                && x.EmbeddingPayloadJson != null)
            .Select(x => x.RubricId)
            .Distinct()
            .CountAsync(cancellationToken);

        var embeddedConcepts = await _context.AiConceptEmbeddings.AsNoTracking()
            .Where(x => x.EmbeddingVersionId == version.EmbeddingVersionId
                && !x.IsDeleted
                && x.Status == "Ready"
                && x.EmbeddingPayloadJson != null)
            .CountAsync(cancellationToken);

        var lastRubricUpdate = await _context.AiRubricEmbeddings.AsNoTracking()
            .Where(x => x.EmbeddingVersionId == version.EmbeddingVersionId && !x.IsDeleted)
            .MaxAsync(x => (DateTime?)x.UpdatedDate ?? x.CreatedDate, cancellationToken);

        var lastConceptUpdate = await _context.AiConceptEmbeddings.AsNoTracking()
            .Where(x => x.EmbeddingVersionId == version.EmbeddingVersionId && !x.IsDeleted)
            .MaxAsync(x => (DateTime?)x.UpdatedDate ?? x.CreatedDate, cancellationToken);

        var latestUpdate = new[] { lastRubricUpdate, lastConceptUpdate, version.UpdatedDate, version.CreatedDate }
            .Where(x => x.HasValue)
            .Max();

        decimal? freshnessHours = latestUpdate.HasValue
            ? Math.Round((decimal)(DateTime.UtcNow - latestUpdate.Value).TotalHours, 2)
            : null;

        decimal? rubricCoverage = totalRubrics == 0
            ? null
            : Math.Round((decimal)embeddedRubrics / totalRubrics, 4);

        return new AiMonitoringEmbeddingSnapshot
        {
            VersionId = version.EmbeddingVersionId,
            VersionCode = version.VersionCode,
            ModelName = version.ModelName,
            LastRubricUpdateUtc = lastRubricUpdate,
            LastConceptUpdateUtc = lastConceptUpdate,
            FreshnessHours = freshnessHours,
            TotalActiveRubrics = totalRubrics,
            EmbeddedRubrics = embeddedRubrics,
            RubricCoverage = rubricCoverage,
            EmbeddedConcepts = embeddedConcepts,
            ConceptCoverage = embeddedConcepts > 0 ? 1m : 0m,
        };
    }

    private static AiMonitoringDailySnapshotModel MapSnapshot(AiMonitoringDailySnapshot row) => new()
    {
        SnapshotId = row.SnapshotId,
        SnapshotDate = row.SnapshotDate,
        EngineVersion = row.EngineVersion,
        SessionsAnalyzed = row.SessionsAnalyzed,
        FeedbackCount = row.FeedbackCount,
        PrecisionScore = row.PrecisionScore,
        RecallScore = row.RecallScore,
        DoctorAcceptanceRate = row.DoctorAcceptanceRate,
        HallucinationCount = row.HallucinationCount,
        HallucinationRate = row.HallucinationRate,
        EmbeddingFreshnessHours = row.EmbeddingFreshnessHours,
        RubricEmbeddingCoverage = row.RubricEmbeddingCoverage,
        ConceptEmbeddingCoverage = row.ConceptEmbeddingCoverage,
        TranscriptCoverage = row.TranscriptCoverage,
        AverageConfidence = row.AverageConfidence,
        PrimaryRubricAccuracy = row.PrimaryRubricAccuracy,
        F1Score = row.F1Score,
        CalculatedDateUtc = row.CalculatedDate,
    };

    private static AiMonitoringAuditLogEntryModel MapAudit(AiMonitoringAuditLog row) => new()
    {
        AuditLogId = row.AuditLogId,
        EventType = row.EventType,
        Operation = row.Operation,
        ActorUserId = row.ActorUserId,
        CorrelationId = row.CorrelationId,
        DurationMs = row.DurationMs,
        Success = row.Success,
        ErrorMessage = row.ErrorMessage,
        EnteredDateUtc = row.EnteredDate,
    };

    private static decimal? Average(IEnumerable<decimal?> values)
    {
        var list = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 4);
    }

    private static decimal? AverageBool(IEnumerable<bool?> values)
    {
        var list = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return list.Count == 0 ? null : Math.Round((decimal)list.Count(x => x) / list.Count, 4);
    }
}

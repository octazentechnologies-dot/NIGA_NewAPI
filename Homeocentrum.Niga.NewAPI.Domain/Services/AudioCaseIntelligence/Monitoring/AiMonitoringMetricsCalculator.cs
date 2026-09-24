using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Monitoring;

public static class AiMonitoringMetricsCalculator
{
    public static AiMonitoringComputedMetrics ComputeDaily(
        IReadOnlyList<AudioCaseRubricBenchmark> benchmarks,
        IReadOnlyList<AiCaseCoverageMetrics> coverageRows,
        IReadOnlyList<AiRubricConfidence> confidenceRows,
        IReadOnlyList<AiRubricValidationV3> validationRows,
        int feedbackCount,
        AiMonitoringEmbeddingSnapshot embedding)
    {
        var sessions = benchmarks.Count;
        var precision = Average(benchmarks.Select(x => x.PrecisionScore));
        var recall = Average(benchmarks.Select(x => x.RecallScore));
        var acceptance = Average(benchmarks.Select(x => x.AcceptanceRate));
        var f1 = Average(benchmarks.Select(x => x.F1Score));
        var primaryAccuracy = AverageBool(benchmarks.Select(x => x.PrimaryInTop5));
        var transcriptCoverage = Average(coverageRows.Select(x => (decimal?)x.TranscriptCoverage));
        var averageConfidence = Average(confidenceRows.Select(x => (decimal?)x.FinalScore));

        var hallucinationCount = validationRows.Count(IsHallucinationValidation);
        var validationEvents = validationRows.Count;
        decimal? hallucinationRate = validationEvents > 0
            ? Math.Round((decimal)hallucinationCount / validationEvents, 4)
            : null;

        return new AiMonitoringComputedMetrics
        {
            SessionsAnalyzed = sessions,
            FeedbackCount = feedbackCount,
            PrecisionScore = precision,
            RecallScore = recall,
            DoctorAcceptanceRate = acceptance,
            F1Score = f1,
            PrimaryRubricAccuracy = primaryAccuracy,
            TranscriptCoverage = transcriptCoverage,
            AverageConfidence = averageConfidence,
            HallucinationCount = hallucinationCount,
            HallucinationRate = hallucinationRate,
            EmbeddingFreshnessHours = embedding.FreshnessHours,
            RubricEmbeddingCoverage = embedding.RubricCoverage,
            ConceptEmbeddingCoverage = embedding.ConceptCoverage,
        };
    }

    public static AiMonitoringEmbeddingHealthModel BuildEmbeddingHealth(AiMonitoringEmbeddingSnapshot embedding) =>
        new()
        {
            CurrentEmbeddingVersionId = embedding.VersionId,
            CurrentVersionCode = embedding.VersionCode,
            ModelName = embedding.ModelName,
            LastRubricEmbeddingUpdateUtc = embedding.LastRubricUpdateUtc,
            LastConceptEmbeddingUpdateUtc = embedding.LastConceptUpdateUtc,
            EmbeddingFreshnessHours = embedding.FreshnessHours,
            TotalActiveRubrics = embedding.TotalActiveRubrics,
            EmbeddedRubrics = embedding.EmbeddedRubrics,
            RubricEmbeddingCoverage = embedding.RubricCoverage,
            EmbeddedConcepts = embedding.EmbeddedConcepts,
            ConceptEmbeddingCoverage = embedding.ConceptCoverage,
            FreshnessStatus = ResolveFreshnessStatus(embedding.FreshnessHours),
            CoverageStatus = ResolveCoverageStatus(embedding.RubricCoverage),
        };

    public static List<AiMonitoringKpiCardModel> BuildKpiCards(
        AiMonitoringComputedMetrics current,
        AiMonitoringComputedMetrics? previous)
    {
        return
        [
            BuildKpi(AiMonitoringMetricKeys.Precision, "Precision", current.PrecisionScore, previous?.PrecisionScore, 0.70m),
            BuildKpi(AiMonitoringMetricKeys.Recall, "Recall", current.RecallScore, previous?.RecallScore, 0.65m),
            BuildKpi(AiMonitoringMetricKeys.DoctorAcceptance, "Doctor Acceptance", current.DoctorAcceptanceRate, previous?.DoctorAcceptanceRate, 0.75m),
            BuildKpi(AiMonitoringMetricKeys.HallucinationRate, "Hallucination Rate", current.HallucinationRate, previous?.HallucinationRate, 0.10m, invertThreshold: true),
            BuildKpi(AiMonitoringMetricKeys.TranscriptCoverage, "Transcript Coverage", current.TranscriptCoverage, previous?.TranscriptCoverage, 0.85m),
            BuildKpi(AiMonitoringMetricKeys.AverageConfidence, "Average Confidence", current.AverageConfidence, previous?.AverageConfidence, 0.70m),
            BuildKpi(AiMonitoringMetricKeys.PrimaryRubricAccuracy, "Primary Rubric Accuracy", current.PrimaryRubricAccuracy, previous?.PrimaryRubricAccuracy, 0.80m),
            BuildKpi(AiMonitoringMetricKeys.F1Score, "F1 Score", current.F1Score, previous?.F1Score, 0.70m),
        ];
    }

    public static AiMonitoringChartSeriesModel BuildSeries(
        string metricKey,
        string label,
        IEnumerable<AiMonitoringDailySnapshot> snapshots,
        Func<AiMonitoringDailySnapshot, decimal?> selector,
        string chartType = "line")
    {
        var points = snapshots
            .OrderBy(x => x.SnapshotDate)
            .Select(x => new AiMonitoringChartPointModel
            {
                TimestampUtc = DateTime.SpecifyKind(x.SnapshotDate.Date, DateTimeKind.Utc),
                Label = x.SnapshotDate.ToString("yyyy-MM-dd"),
                Value = selector(x) is { } v ? Math.Round(v, 4) : null,
                Count = x.SessionsAnalyzed,
            })
            .ToList();

        return new AiMonitoringChartSeriesModel
        {
            MetricKey = metricKey,
            Label = label,
            ChartType = chartType,
            Unit = "ratio",
            Points = points,
        };
    }

    private static AiMonitoringKpiCardModel BuildKpi(
        string key,
        string label,
        decimal? value,
        decimal? previousValue,
        decimal healthyThreshold,
        bool invertThreshold = false)
    {
        var trendDelta = value.HasValue && previousValue.HasValue
            ? Math.Round(value.Value - previousValue.Value, 4)
            : (decimal?)null;

        var trendDirection = trendDelta switch
        {
            > 0.005m => "Up",
            < -0.005m => "Down",
            _ => "Flat",
        };

        var status = "Normal";
        if (value.HasValue)
        {
            var healthy = invertThreshold
                ? value.Value <= healthyThreshold
                : value.Value >= healthyThreshold;
            status = healthy ? "Healthy" : "Attention";
        }

        return new AiMonitoringKpiCardModel
        {
            MetricKey = key,
            Label = label,
            Value = value.HasValue ? Math.Round(value.Value, 4) : null,
            Unit = "ratio",
            TrendDirection = trendDirection,
            TrendDelta = trendDelta,
            Status = status,
        };
    }

    private static bool IsHallucinationValidation(AiRubricValidationV3 row) =>
        row.ValidationFlagsJson != null
        && row.ValidationFlagsJson.Contains("Hallucination", StringComparison.OrdinalIgnoreCase);

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

    private static string ResolveFreshnessStatus(decimal? hours) =>
        hours switch
        {
            null => "Unknown",
            <= 48 => "Fresh",
            <= 168 => "Stale",
            _ => "Critical",
        };

    private static string ResolveCoverageStatus(decimal? coverage) =>
        coverage switch
        {
            null => "Unknown",
            >= 0.90m => "Healthy",
            >= 0.70m => "Partial",
            _ => "Low",
        };
}

public sealed class AiMonitoringComputedMetrics
{
    public int SessionsAnalyzed { get; init; }

    public int FeedbackCount { get; init; }

    public decimal? PrecisionScore { get; init; }

    public decimal? RecallScore { get; init; }

    public decimal? DoctorAcceptanceRate { get; init; }

    public decimal? F1Score { get; init; }

    public decimal? PrimaryRubricAccuracy { get; init; }

    public decimal? TranscriptCoverage { get; init; }

    public decimal? AverageConfidence { get; init; }

    public int HallucinationCount { get; init; }

    public decimal? HallucinationRate { get; init; }

    public decimal? EmbeddingFreshnessHours { get; init; }

    public decimal? RubricEmbeddingCoverage { get; init; }

    public decimal? ConceptEmbeddingCoverage { get; init; }
}

public sealed class AiMonitoringEmbeddingSnapshot
{
    public Guid? VersionId { get; init; }

    public string? VersionCode { get; init; }

    public string? ModelName { get; init; }

    public DateTime? LastRubricUpdateUtc { get; init; }

    public DateTime? LastConceptUpdateUtc { get; init; }

    public decimal? FreshnessHours { get; init; }

    public int TotalActiveRubrics { get; init; }

    public int EmbeddedRubrics { get; init; }

    public decimal? RubricCoverage { get; init; }

    public int EmbeddedConcepts { get; init; }

    public decimal? ConceptCoverage { get; init; }
}

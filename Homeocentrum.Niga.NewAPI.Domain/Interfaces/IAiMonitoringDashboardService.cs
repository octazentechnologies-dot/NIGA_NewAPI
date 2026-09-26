using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IAiMonitoringDashboardService
{
    Task<AiMonitoringDashboardOverviewModel> GetOverviewAsync(
        int days = 30,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<AiMonitoringTrendsModel> GetTrendsAsync(
        int days = 30,
        string granularity = "daily",
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<AiMonitoringChartSeriesModel> GetChartAsync(
        string metricKey,
        int days = 30,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<AiMonitoringEmbeddingHealthModel> GetEmbeddingHealthAsync(
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<AiMonitoringHallucinationSummaryModel> GetHallucinationSummaryAsync(
        int days = 30,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<AiMonitoringSnapshotRefreshResultModel> RefreshDailySnapshotAsync(
        DateTime? snapshotDate = null,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<AiMonitoringAuditLogModel> GetAuditLogAsync(
        int topN = 50,
        CancellationToken cancellationToken = default);
}

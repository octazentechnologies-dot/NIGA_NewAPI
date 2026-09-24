using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiMonitoringDashboardController : ControllerBase
{
    private readonly IAiMonitoringDashboardService _dashboardService;

    public AiMonitoringDashboardController(IAiMonitoringDashboardService dashboardService) =>
        _dashboardService = dashboardService;

    /// <summary>Phase 10: KPI overview cards for the AI monitoring dashboard.</summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(AiMonitoringDashboardOverviewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiMonitoringDashboardOverviewModel>> GetOverview(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        return Ok(await _dashboardService.GetOverviewAsync(days, userId, cancellationToken));
    }

    /// <summary>Multi-series chart data for dashboard trends.</summary>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(AiMonitoringTrendsModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiMonitoringTrendsModel>> GetTrends(
        [FromQuery] int days = 30,
        [FromQuery] string granularity = "daily",
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        return Ok(await _dashboardService.GetTrendsAsync(days, granularity, userId, cancellationToken));
    }

    /// <summary>Single metric chart series (Precision, Recall, DoctorAcceptance, etc.).</summary>
    [HttpGet("charts/{metricKey}")]
    [ProducesResponseType(typeof(AiMonitoringChartSeriesModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiMonitoringChartSeriesModel>> GetChart(
        string metricKey,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        return Ok(await _dashboardService.GetChartAsync(metricKey, days, userId, cancellationToken));
    }

    /// <summary>Embedding freshness and coverage health panel.</summary>
    [HttpGet("embedding-health")]
    [ProducesResponseType(typeof(AiMonitoringEmbeddingHealthModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiMonitoringEmbeddingHealthModel>> GetEmbeddingHealth(
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        return Ok(await _dashboardService.GetEmbeddingHealthAsync(userId, cancellationToken));
    }

    /// <summary>Hallucination detection summary and daily trend.</summary>
    [HttpGet("hallucinations")]
    [ProducesResponseType(typeof(AiMonitoringHallucinationSummaryModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiMonitoringHallucinationSummaryModel>> GetHallucinations(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        return Ok(await _dashboardService.GetHallucinationSummaryAsync(days, userId, cancellationToken));
    }

    /// <summary>Refresh persisted daily KPI snapshot (admin/scheduler).</summary>
    [HttpPost("snapshots/refresh")]
    [ProducesResponseType(typeof(AiMonitoringSnapshotRefreshResultModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiMonitoringSnapshotRefreshResultModel>> RefreshSnapshot(
        [FromQuery] DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        return Ok(await _dashboardService.RefreshDailySnapshotAsync(date, userId, cancellationToken));
    }

    /// <summary>Enterprise audit log for dashboard queries and snapshot refreshes.</summary>
    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(AiMonitoringAuditLogModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<AiMonitoringAuditLogModel>> GetAuditLog(
        [FromQuery] int topN = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _dashboardService.GetAuditLogAsync(topN, cancellationToken));
}

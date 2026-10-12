using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Clinic and doctor reports: follow-ups, clinic performance, practice, earnings, medicine orders, and finance exports.
/// </summary>
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IS5Week5Service s5, NIGACentrumContext context, ILogger<ReportsController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Reports/FollowUpDue")]
    public Task<IActionResult> FollowDue() => Done(_s5.FollowUpDueAsync(Caller()));

    [HttpGet("/api/Reports/FollowUpSummary")]
    public Task<IActionResult> FollowSummary() => Done(_s5.FollowUpSummaryAsync(Caller()));

    [HttpGet("/api/Reports/ClinicPerformance")]
    public Task<IActionResult> Performance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.ClinicPerformanceAsync(from, to, Caller()));

    [SecurityAudit(SecurityAuditEvents.FinanceExport)]
    [HttpGet("/api/Reports/Reconciliation/Export")]
    public Task<IActionResult> ReconExport() => Done(_s5.ExportReconciliationAsync(Caller()));

    [SecurityAudit(SecurityAuditEvents.FinanceExport)]
    [HttpGet("/api/Reports/Settlements/Export")]
    public Task<IActionResult> SettlementExport() => Done(_s5.ExportSettlementsAsync(Caller()));

    [SecurityAudit(SecurityAuditEvents.FinanceExport)]
    [HttpGet("/api/Reports/Payouts/Export")]
    public Task<IActionResult> PayoutExport() => Done(_s5.ExportPayoutsAsync(Caller()));

    [HttpGet("/api/Reports/MedicineOrders")]
    public Task<IActionResult> Medicine() => Done(_s5.MedicineReportAsync(Caller()));

    [HttpGet("/api/Reports/Doctor/Practice")]
    public Task<IActionResult> PracticeReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.PracticeReportAsync(from, to, Caller()));

    [HttpGet("/api/Reports/Doctor/FollowUps")]
    public Task<IActionResult> FollowUpReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.FollowUpReportAsync(from, to, Caller()));

    [HttpPut("/api/Reports/Doctor/FollowUps/{taskId:int}")]
    public Task<IActionResult> UpdateFollowUpTask(int taskId, [FromBody] FollowUpTaskWrite request)
        => Done(_s5.UpdateFollowUpTaskAsync(taskId, request, Caller()));

    [HttpGet("/api/Reports/Doctor/Earnings")]
    public Task<IActionResult> EarningsReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.EarningsReportAsync(from, to, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

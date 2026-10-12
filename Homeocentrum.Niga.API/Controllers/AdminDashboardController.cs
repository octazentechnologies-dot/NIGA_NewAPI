using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Admin dashboard overview and summary figures.
/// </summary>
[ApiController]
[Authorize]
public class AdminDashboardController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(IS5Week5Service s5, NIGACentrumContext context, ILogger<AdminDashboardController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/AdminDashboard/Overview")]
    public Task<IActionResult> Overview() => Done(_s5.OverviewAsync(Caller()));

    [HttpGet("/api/AdminDashboard/Summary")]
    public Task<IActionResult> AdminSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? doctorId)
        => Done(_s5.AdminSummaryAsync(from, to, doctorId, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

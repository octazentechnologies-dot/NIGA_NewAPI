using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Doctor earnings: summary, earnings buckets, and the accounts view of a doctor's earnings.
/// </summary>
[ApiController]
[Authorize]
public class EarningsController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<EarningsController> _logger;

    public EarningsController(IS4Week4Service s4, IS5Week5Service s5, NIGACentrumContext context, ILogger<EarningsController> logger)
    {
        _s4 = s4;
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    /// <summary>DMO-10.02 — doctor mobile earnings rollup (own clinic).</summary>
    [HttpGet("/api/Earnings/Summary")]
    public Task<IActionResult> EarningsSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s4.EarningsSummaryAsync(from, to, Caller()));

    [HttpGet("/api/Account/DoctorEarnings")]
    public Task<IActionResult> AccountDoctorEarnings([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? doctorId)
        => Done(_s4.EarningsSummaryAsync(from, to, Caller(), doctorId));

    [HttpGet("/api/Earnings/Buckets")]
    public Task<IActionResult> Buckets([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.EarningsBucketsAsync(from, to, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

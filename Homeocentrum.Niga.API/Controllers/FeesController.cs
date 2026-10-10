using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Doctor consultation fees: public fee for checkout, fee changes, and fee history.
/// </summary>
[ApiController]
[Authorize]
public class FeesController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<FeesController> _logger;

    public FeesController(IS4Week4Service s4, NIGACentrumContext context, ILogger<FeesController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    /// <summary>PAT-18.02 — public consult fee for patient checkout (anonymous).</summary>
    [AllowAnonymous]
    [HttpGet("/api/Fees/Public/{doctorId:int}")]
    public Task<IActionResult> PublicFee(int doctorId) => Done(_s4.GetPublicFeeAsync(doctorId));

    [HttpPut("/api/Fees")]
    public Task<IActionResult> SaveFee([FromBody] FeeUpsertRequest request) => Done(_s4.UpsertFeeAsync(request, Caller()));

    [HttpGet("/api/Fees/History")]
    public Task<IActionResult> FeeHistory([FromQuery] int doctorId) => Done(_s4.FeeHistoryAsync(doctorId, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

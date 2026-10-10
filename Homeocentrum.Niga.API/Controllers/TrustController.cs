using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Doctor verification (trust) status, the verification queue and decisions, and the ranking explanation.
/// </summary>
[ApiController]
[Authorize]
public class TrustController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<TrustController> _logger;

    public TrustController(IS4Week4Service s4, NIGACentrumContext context, ILogger<TrustController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Trust/MyStatus")]
    public Task<IActionResult> MyStatus() => Done(_s4.MyVerificationAsync(Caller()));

    [HttpGet("/api/Trust/Queue")]
    public Task<IActionResult> Queue([FromQuery] string? status) => Done(_s4.VerificationQueueAsync(status, Caller()));

    [HttpGet("/api/Trust/{doctorId:int}")]
    public Task<IActionResult> Trust(int doctorId) => Done(_s4.VerificationDetailAsync(doctorId, Caller()));

    [HttpPost("/api/Trust/{doctorId:int}/Decide")]
    public Task<IActionResult> Decide(int doctorId, [FromBody] VerificationDecisionRequest request)
        => Done(_s4.DecideVerificationAsync(doctorId, request, Caller()));

    [AllowAnonymous]
    [HttpGet("/api/Doctors/RankingExplain/{doctorId:int}")]
    public Task<IActionResult> Ranking(int doctorId) => Done(_s4.RankingExplainAsync(doctorId));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Prescription refill requests and the doctor's approve or reject decision.
/// </summary>
[ApiController]
[Authorize]
public class RefillsController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<RefillsController> _logger;

    public RefillsController(IS4Week4Service s4, NIGACentrumContext context, ILogger<RefillsController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpPost("/api/Erx/Refills")]
    public Task<IActionResult> RequestRefill([FromBody] RefillCreateRequest request) => Done(_s4.RequestRefillAsync(request, Caller()));

    [HttpGet("/api/Erx/Refills")]
    [HttpGet("/api/Refill")]
    public Task<IActionResult> Refills([FromQuery] string? status) => Done(_s4.ListRefillsAsync(Caller(), status));

    [HttpGet("/api/Erx/Refills/{id:int}")]
    [HttpGet("/api/Refill/{id:int}")]
    public Task<IActionResult> Refill(int id) => Done(_s4.RefillDetailAsync(id, Caller()));

    [HttpPost("/api/Erx/Refills/{id:int}/Approve")]
    [HttpPost("/api/Refill/{id:int}/Approve")]
    public Task<IActionResult> ApproveRefill(int id) => Done(_s4.DecideRefillAsync(id, true, null, Caller()));

    [HttpPost("/api/Erx/Refills/{id:int}/Reject")]
    [HttpPost("/api/Refill/{id:int}/Reject")]
    public Task<IActionResult> RejectRefill(int id, [FromBody] RefillDecisionRequest? request)
        => Done(_s4.DecideRefillAsync(id, false, request?.Reason, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

public class RejectRefillBody
{
    public string? Reason { get; set; }
}

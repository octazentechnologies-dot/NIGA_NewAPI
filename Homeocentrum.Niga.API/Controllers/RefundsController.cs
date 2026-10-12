using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Refunds: policy preview, refund requests, and the refund list.
/// </summary>
[ApiController]
[Authorize]
public class RefundsController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<RefundsController> _logger;

    public RefundsController(IS4Week4Service s4, NIGACentrumContext context, ILogger<RefundsController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Refunds/Policy/{paymentOrderId:long}")]
    public Task<IActionResult> Policy(long paymentOrderId) => Done(_s4.PreviewRefundPolicyAsync(paymentOrderId, Caller()));

    [HttpPost("/api/Refunds")]
    public Task<IActionResult> Refund([FromBody] CreateRefundRequest request) => Done(_s4.CreateRefundAsync(request, Caller()));

    [HttpGet("/api/Refunds")]
    [HttpGet("/api/Account/Refunds")]
    public Task<IActionResult> Refunds() => Done(_s4.ListRefundsAsync(Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

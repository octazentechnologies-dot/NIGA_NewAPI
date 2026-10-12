using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Payment receipt emails and email history.
/// </summary>
[ApiController]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IS5Week5Service s5, NIGACentrumContext context, ILogger<EmailController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpPost("/api/Email/Receipts/{paymentOrderId:long}")]
    public Task<IActionResult> ReceiptEmail(long paymentOrderId) => Done(_s5.SendReceiptEmailAsync(paymentOrderId, Caller()));

    [HttpGet("/api/Email/History")]
    public Task<IActionResult> EmailHistory() => Done(_s5.EmailHistoryAsync(Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

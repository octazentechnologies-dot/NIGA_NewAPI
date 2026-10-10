using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Invoices for a payment order.
/// </summary>
[ApiController]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IS4Week4Service s4, NIGACentrumContext context, ILogger<InvoicesController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Invoices/ByPayment/{paymentOrderId:long}")]
    public Task<IActionResult> Invoice(long paymentOrderId) => Done(_s4.GetOrCreateInvoiceAsync(paymentOrderId, Caller(), false));

    [HttpPost("/api/Invoices/ByPayment/{paymentOrderId:long}")]
    public Task<IActionResult> CreateInvoice(long paymentOrderId) => Done(_s4.GetOrCreateInvoiceAsync(paymentOrderId, Caller(), true));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

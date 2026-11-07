using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Pharmacy partners: onboarding, activation, licence sweep, sellers, order queue, routing rules, and pharmacy configuration.
/// </summary>
[ApiController]
[Authorize]
public class PharmacyController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<PharmacyController> _logger;

    public PharmacyController(IS4Week4Service s4, IS5Week5Service s5, NIGACentrumContext context, ILogger<PharmacyController> logger)
    {
        _s4 = s4;
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpPost("/api/Pharmacy/Onboard")]
    public Task<IActionResult> Onboard([FromBody] PharmacyOnboardRequest request) => Done(_s4.OnboardPharmacyAsync(request, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/Activate")]
    public Task<IActionResult> Activate(int id) => Done(_s4.ActivatePharmacyAsync(id, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/Reject")]
    public Task<IActionResult> Reject(int id, [FromBody] PharmacyDecisionRequest? body)
        => Done(_s4.RejectPharmacyAsync(id, body?.Reason, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/Suspend")]
    public Task<IActionResult> Suspend(int id) => Done(_s4.SuspendPharmacyAsync(id, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/Close")]
    public Task<IActionResult> RequestClose(int id, [FromBody] PharmacyDecisionRequest? body)
        => Done(_s4.RequestPharmacyCloseAsync(id, body?.Reason, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/ApproveClose")]
    public Task<IActionResult> ApproveClose(int id, [FromBody] PharmacyDecisionRequest? body)
        => Done(_s4.ApprovePharmacyCloseAsync(id, body?.Reason, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/Withdraw")]
    public Task<IActionResult> Withdraw(int id) => Done(_s4.WithdrawPharmacyAsync(id, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/Documents")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public Task<IActionResult> UploadDocument(int id, IFormFile file, [FromForm] string? documentType = null, [FromForm] string? description = null)
        => Done(_s4.SavePharmacyDocumentAsync(id, file, Caller(), documentType, description));

    [HttpGet("/api/Pharmacy/{id:int}/Documents")]
    public Task<IActionResult> ListDocuments(int id) => Done(_s4.ListPharmacyDocumentsAsync(id, Caller()));

    [HttpPost("/api/Pharmacy/Licences/Sweep")]
    public Task<IActionResult> Sweep() => Done(_s4.SweepLicencesAsync(Caller()));

    [AllowAnonymous]
    [HttpGet("/api/Pharmacy/Sellers")]
    public Task<IActionResult> Sellers([FromQuery] string? area) => Done(_s4.ListSellersAsync(area));

    [HttpGet("/api/Pharmacy/Partners")]
    public Task<IActionResult> Partners() => Done(_s4.ListPharmacyPartnersAsync(Caller()));

    [HttpGet("/api/Pharmacy/Orders")]
    public Task<IActionResult> PharmacyOrders() => Done(_s4.PharmacyQueueAsync(Caller()));

    [HttpPut("/api/Pharmacy/Routing")]
    public Task<IActionResult> Routing([FromBody] RoutingRuleRequest request) => Done(_s4.SaveRoutingAsync(request, Caller()));

    [HttpGet("/api/Pharmacy/{pharmacyPartnerId:int}/Config")]
    public Task<IActionResult> PharmacyConfig(int pharmacyPartnerId)
        => Done(_s5.GetPharmacyConfigAsync(pharmacyPartnerId, Caller()));

    [HttpPut("/api/Pharmacy/{pharmacyPartnerId:int}/Config")]
    public Task<IActionResult> SavePharmacyConfig(int pharmacyPartnerId, [FromBody] PharmacyConfigWrite request)
        => Done(_s5.SavePharmacyConfigAsync(pharmacyPartnerId, request, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

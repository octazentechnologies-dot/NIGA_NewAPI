using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Patient consents (grant and withdraw) and patient data requests.
/// </summary>
[ApiController]
[Authorize]
public class PatientConsentsController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<PatientConsentsController> _logger;

    public PatientConsentsController(IS4Week4Service s4, NIGACentrumContext context, ILogger<PatientConsentsController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Patient/Consents")]
    public Task<IActionResult> Consents() => Done(_s4.ListConsentsAsync(Caller()));

    [HttpPost("/api/Patient/Consents/Types/{consentTypeId:int}/Grant")]
    public Task<IActionResult> GrantConsent(int consentTypeId) => Done(_s4.GrantConsentAsync(consentTypeId, Caller()));

    [HttpPost("/api/Patient/Consents/{id:long}/Withdraw")]
    public Task<IActionResult> Withdraw(long id) => Done(_s4.WithdrawConsentAsync(id, Caller()));

    [HttpPost("/api/Patient/DataRequests")]
    public Task<IActionResult> DataRequest([FromBody] DataRequestCreate request) => Done(_s4.CreateDataRequestAsync(request, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

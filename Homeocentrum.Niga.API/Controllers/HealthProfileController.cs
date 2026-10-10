using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Patient health profile.
/// </summary>
[ApiController]
[Authorize]
public class HealthProfileController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<HealthProfileController> _logger;

    public HealthProfileController(IS4Week4Service s4, NIGACentrumContext context, ILogger<HealthProfileController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Patient/Profile")]
    public Task<IActionResult> Profile() => Done(_s4.GetHealthProfileAsync(Caller()));

    [HttpPut("/api/Patient/Profile")]
    public Task<IActionResult> UpdateProfile([FromBody] HealthProfileUpdate request) => Done(_s4.UpdateHealthProfileAsync(request, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Security posture summary for admin.
/// </summary>
[ApiController]
[Authorize]
public class SecurityPostureController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<SecurityPostureController> _logger;

    public SecurityPostureController(IS5Week5Service s5, NIGACentrumContext context, ILogger<SecurityPostureController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Security/Posture")]
    public Task<IActionResult> Posture() => Done(_s5.SecurityPostureAsync(Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

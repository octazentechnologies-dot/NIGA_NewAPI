using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Services;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Opens the clinician web portal from the Doctor app without putting a JWT in the URL.
/// The phone calls Create. The browser calls Exchange once. Renew keeps a long case signed in.
/// </summary>
[Route("api/Account/WebHandoff")]
[ApiController]
[Authorize]
public class WebHandoffController : ControllerBase
{
    private readonly IWebHandoffService _handoff;

    public WebHandoffController(IWebHandoffService handoff)
    {
        _handoff = handoff;
    }

    /// <summary>Doctor app only. Returns a 60-second code. The code is the only value that may go in a URL.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WebHandoffCreateRequest? request)
    {
        var result = await _handoff.CreateAsync(
            User,
            request?.Purpose,
            request?.PatientAppId,
            request?.ReturnUrl,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.Status, result.Body);
    }

    /// <summary>Browser only. Redeems the code once and returns the same login shape the web already stores.</summary>
    [AllowAnonymous]
    [HttpPost("Exchange")]
    public async Task<IActionResult> Exchange([FromBody] WebHandoffExchangeRequest? request)
    {
        var result = await _handoff.ExchangeAsync(
            request?.Code,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(result.Status, result.Body);
    }

    /// <summary>Extends a handoff session by 20 minutes while the doctor is working. Cannot pass the 5-hour cap and cannot become a 7-day login.</summary>
    [HttpPost("Renew")]
    public async Task<IActionResult> Renew()
    {
        var result = await _handoff.RenewAsync(User);
        return StatusCode(result.Status, result.Body);
    }

    /// <summary>Ends the portal session. A normal doctor login ends every portal session opened for that doctor.</summary>
    [HttpPost("Revoke")]
    public async Task<IActionResult> Revoke()
    {
        var result = await _handoff.RevokeAsync(User);
        return StatusCode(result.Status, result.Body);
    }
}

public sealed class WebHandoffCreateRequest
{
    public string? Purpose { get; set; }
    public int? PatientAppId { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class WebHandoffExchangeRequest
{
    public string? Code { get; set; }
}

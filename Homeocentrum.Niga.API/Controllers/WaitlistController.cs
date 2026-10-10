using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Waitlist: patients join a doctor's waitlist; the doctor reads it. Joining does not reserve a slot.
/// </summary>
[ApiController]
[Authorize]
public class WaitlistController : ControllerBase
{
    private readonly IS3Week3Service _s3;

    public WaitlistController(IS3Week3Service s3)
    {
        _s3 = s3;
    }

    /// <summary>
    /// PAT-23.02 — join waitlist (anonymous). Does not reserve a slot; offer comes later as OFFERED.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/api/Waitlist/Join")]
    public Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
        => Done(_s3.JoinWaitlistAsync(request));

    [HttpGet("/api/Waitlist")]
    public Task<IActionResult> Waitlist([FromQuery] int doctorId)
    {
        var deny = RequireDoctor(doctorId);
        return deny != null ? Task.FromResult(deny) : Done(_s3.GetWaitlistAsync(doctorId));
    }

    private Task<IActionResult> Done(Task<S3ActionResult> work) => this.Respond(work);
    private IActionResult? RequireDoctor(int doctorId) => DoctorOwnership.ForbidIfNotOwner(User, doctorId);
}

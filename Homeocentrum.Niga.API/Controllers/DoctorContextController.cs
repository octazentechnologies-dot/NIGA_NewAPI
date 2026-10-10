using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Patient context card the doctor sees during a consultation.
/// </summary>
[ApiController]
[Authorize]
public class DoctorContextController : ControllerBase
{
    private readonly IS3Week3Service _s3;

    public DoctorContextController(IS3Week3Service s3)
    {
        _s3 = s3;
    }

    [HttpGet("/api/DoctorMobile/Context/{patientAppId:int}")]
    public async Task<IActionResult> Context(int patientAppId)
    {
        var result = await _s3.GetDoctorContextAsync(patientAppId, Caller());
        if (result.StatusCode != 200)
            return StatusCode(result.StatusCode, result.Body);
        return StatusCode(result.StatusCode, result.Body);
    }

    private Task<IActionResult> Done(Task<S3ActionResult> work) => this.Respond(work);
    private S3Caller Caller() => this.S3Caller();
}

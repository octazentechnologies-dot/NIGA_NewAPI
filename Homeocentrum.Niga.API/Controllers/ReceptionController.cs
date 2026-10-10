using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Reception desk: reception staff profile, the pre-consult case paper, and opening a patient row.
/// </summary>
[ApiController]
[Authorize]
public class ReceptionController : ControllerBase
{
    private readonly IS3Week3Service _s3;

    public ReceptionController(IS3Week3Service s3)
    {
        _s3 = s3;
    }

    [HttpGet("/api/Reception/Profile")]
    public Task<IActionResult> ReceptionProfile()
    {
        if (!IsReception())
            return Task.FromResult<IActionResult>(ForbidRole("Reception profile is for reception staff."));
        return Done(_s3.GetReceptionProfileAsync(User.GetUserId()));
    }

    [HttpPut("/api/Reception/Profile")]
    public Task<IActionResult> UpdateReceptionProfile([FromBody] ReceptionProfileUpdate request)
    {
        if (!IsReception())
            return Task.FromResult<IActionResult>(ForbidRole("Reception profile is for reception staff."));
        return Done(_s3.UpdateReceptionProfileAsync(User.GetUserId(), request));
    }

    /// <summary>
    /// REC-12.02 — POST case-paper subset of SaveComplaints (CaseEntryChiefComplaint + CreatedByRole=Reception).
    /// </summary>
    [HttpPost("/api/Reception/CasePaper")]
    public Task<IActionResult> SaveCasePaper([FromBody] CasePaperRequest request)
    {
        if (!IsReception())
            return Task.FromResult<IActionResult>(ForbidRole("Only reception can log the pre-consult case paper."));
        var doctorId = DoctorOwnership.GetDoctorId(User);
        if (!doctorId.HasValue)
            return Task.FromResult<IActionResult>(ForbidRole("Doctor context is required."));
        return Done(_s3.SaveCasePaperAsync(request, doctorId.Value, User.GetUserId()));
    }

    /// <summary>
    /// REC-12.02 — GET chief complaints for board / reception (same complaints table as SaveComplaints).
    /// </summary>
    [HttpGet("/api/Reception/CasePaper")]
    public Task<IActionResult> CasePapers([FromQuery] int patientId)
    {
        var doctorId = DoctorOwnership.GetDoctorId(User);
        var deny = DoctorOwnership.ForbidIfNotOwner(User, doctorId);
        if (doctorId == null || deny != null)
            return Task.FromResult<IActionResult>(deny ?? ForbidRole("Doctor context is required."));
        return Done(_s3.GetCasePapersAsync(doctorId.Value, patientId));
    }

    /// <summary>REC-05.02 — reception row open. Destination is case paper or appointment, never repertory.</summary>
    [HttpGet("/api/Reception/PatientOpen")]
    public Task<IActionResult> PatientOpen([FromQuery] int patientId)
    {
        if (!IsReception())
            return Task.FromResult<IActionResult>(ForbidRole("Only reception can open a patient row this way."));
        var doctorId = DoctorOwnership.GetDoctorId(User);
        if (!doctorId.HasValue)
            return Task.FromResult<IActionResult>(ForbidRole("Doctor context is required."));
        return Done(_s3.OpenPatientRowAsync(doctorId.Value, patientId));
    }

    private Task<IActionResult> Done(Task<S3ActionResult> work) => this.Respond(work);
    private bool IsReception() => ServiceResultSupport.IsReception(this);
    private IActionResult ForbidRole(string message) => ServiceResultSupport.ForbidRole(this, message);
}

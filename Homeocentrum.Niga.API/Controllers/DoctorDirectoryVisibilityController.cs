using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Doctor discovery (Find a Doctor / public book) visibility toggle.
/// Separate from Online (<c>/api/Tele/Availability</c> / <c>/api/Availability/Me</c>).
/// Same endpoints for web and doctor mobile so both stay in sync on <c>Doctor.DirectoryVisible</c>.
/// </summary>
[ApiController]
[Authorize]
public class DoctorDirectoryVisibilityController : ControllerBase
{
    private readonly NIGACentrumContext _context;

    public DoctorDirectoryVisibilityController(NIGACentrumContext context)
    {
        _context = context;
    }

    /// <summary>GET current discovery visibility for the signed-in doctor.</summary>
    [HttpGet("/api/Doctor/DirectoryVisibility")]
    public async Task<IActionResult> Get()
    {
        var doctor = await ResolveDoctorAsync();
        if (doctor == null)
            return NotFound(new { success = false, message = "Doctor not found." });
        return Ok(Payload(doctor));
    }

    /// <summary>PUT toggle discovery visibility. Body: { "directoryVisible": true|false }.</summary>
    [HttpPut("/api/Doctor/DirectoryVisibility")]
    public Task<IActionResult> Put([FromBody] DirectoryVisibilityUpdateRequest? request)
        => SetAsync(request?.DirectoryVisible ?? false);

    /// <summary>POST same as PUT (mobile-friendly, mirrors Tele/Availability).</summary>
    [HttpPost("/api/Doctor/DirectoryVisibility")]
    public Task<IActionResult> Post([FromBody] DirectoryVisibilityUpdateRequest? request)
        => SetAsync(request?.DirectoryVisible ?? false);

    private async Task<IActionResult> SetAsync(bool directoryVisible)
    {
        var deny = DoctorOwnership.ForbidIfReception(User);
        if (deny != null)
            return deny;

        var doctor = await ResolveDoctorAsync();
        if (doctor == null)
            return NotFound(new { success = false, message = "Doctor not found." });

        if (directoryVisible && !IsVerified(doctor.VerificationStatus))
        {
            return Conflict(new
            {
                success = false,
                code = "NOT_VERIFIED",
                message = "Complete credential verification before publishing your profile in Find a Doctor."
            });
        }

        doctor.DirectoryVisible = directoryVisible;
        doctor.ChangedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(Payload(doctor));
    }

    private object Payload(Doctor doctor)
    {
        var verified = IsVerified(doctor.VerificationStatus);
        return new
        {
            success = true,
            data = new
            {
                doctorId = doctor.DoctorId,
                directoryVisible = doctor.DirectoryVisible,
                verificationStatus = doctor.VerificationStatus,
                canPublish = verified,
                // Patients see you only when Verified + DirectoryVisible.
                visibleToPatients = verified && doctor.DirectoryVisible
            }
        };
    }

    private static bool IsVerified(string? status)
        => string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);

    private async Task<Doctor?> ResolveDoctorAsync()
    {
        var jwtDoctor = DoctorOwnership.GetDoctorId(User);
        if (jwtDoctor.HasValue)
            return await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == jwtDoctor.Value && !d.DeleteStatus);
        var userId = User.GetUserId();
        return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && !d.DeleteStatus);
    }
}

public sealed class DirectoryVisibilityUpdateRequest
{
    public bool DirectoryVisible { get; set; }
}

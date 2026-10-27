using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Services;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Doctor discovery (Find a Doctor / public book) visibility toggle.
/// Separate from Online (tele). Active clinic leave hides the doctor from public
/// discovery without clearing their saved DirectoryVisible preference; Online remains allowed.
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
        var onLeave = await DoctorTimeOffLookup.IsOnLeaveAsync(_context, doctor.DoctorId);
        return Ok(Payload(doctor, onLeave));
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

        var onLeave = await DoctorTimeOffLookup.IsOnLeaveAsync(_context, doctor.DoctorId);

        // Only verification blocks publishing — clinic leave defaults Hidden, but a verified
        // doctor may turn Public Visibility back on for tele consultation during leave.
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
        return Ok(Payload(doctor, onLeave));
    }

    private object Payload(Doctor doctor, bool onLeave)
    {
        var verified = IsVerified(doctor.VerificationStatus);
        // Patients see Verified + DirectoryVisible (leave only hides by default until doctor opts back in).
        var visibleToPatients = verified && doctor.DirectoryVisible;
        return new
        {
            success = true,
            data = new
            {
                doctorId = doctor.DoctorId,
                directoryVisible = doctor.DirectoryVisible,
                isOnTimeOff = onLeave,
                effectiveDirectoryVisible = visibleToPatients,
                verificationStatus = doctor.VerificationStatus,
                // Ability to publish preference — not blocked by leave (leave is a separate default).
                canPublish = verified,
                visibleToPatients
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

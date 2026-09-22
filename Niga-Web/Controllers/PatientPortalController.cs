using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>PAT-08 / PAT-09 — patient home dashboard + care categories (mobile-reusable).</summary>
    [Route("api/PatientPortal")]
    [ApiController]
    [Authorize]
    public class PatientPortalController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public PatientPortalController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("Home")]
        public async Task<IActionResult> Home()
        {
            var userId = (long)User.GetUserId();
            if (userId <= 0)
                return Unauthorized(new { success = false, message = "Not signed in." });

            var owner = await PatientPortalOwnerResolver.ResolveAsync(
                _context, userId, preferActingFor: true, createIfMissing: false);

            var dto = new PatientHomeDashboardDto();
            if (owner == null)
                return Ok(new { success = true, data = dto });

            dto.PatientId = owner.PatientId;
            dto.PatientName = owner.PatientName;
            dto.FamilyCount = await _context.PatientFamilyMembers.CountAsync(f =>
                f.OwnerPatientId == owner.PatientId && !f.DeleteStatus);
            dto.CaregiverCount = await _context.CaregiverAuthorizations.CountAsync(c =>
                c.PatientId == owner.PatientId && !c.DeleteStatus && c.RevokedAt == null);

            var upcoming = await _context.PatientAppointments.AsNoTracking()
                .Where(a => a.PatientId == owner.PatientId && a.DeleteStatus != true && a.AppointmentDate >= DateTime.Today)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(5)
                .Select(a => new PatientAppointmentModel
                {
                    PatientAppId = a.PatientAppId,
                    PatientId = a.PatientId,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentTime = a.AppointmentTime,
                    Status = a.Status ?? string.Empty,
                    DoctorId = a.DoctorId,
                    UserId = a.UserId
                })
                .ToListAsync();
            dto.UpcomingAppointments = upcoming.Count;
            dto.NextAppointments = upcoming;
            return Ok(new { success = true, data = dto });
        }

        [HttpGet("CareCategories")]
        [AllowAnonymous]
        public async Task<IActionResult> CareCategories()
        {
            var rows = await _context.HumanSystemMasters.AsNoTracking()
                .OrderBy(h => h.HumanSystemName)
                .Select(h => new CareCategoryDto
                {
                    Id = h.HumanSystemId,
                    Name = h.HumanSystemName ?? string.Empty,
                    Description = h.Description
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }
    }
}

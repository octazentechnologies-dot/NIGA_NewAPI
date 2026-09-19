using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>DMO-05 — doctor online toggle + working-hours note. Hours CRUD stays on PatientAppointment daily schedule APIs.</summary>
    [Route("api/Availability")]
    [ApiController]
    [Authorize]
    public class AvailabilityController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public AvailabilityController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found." });

            var today = DateTime.Today;
            var schedule = await _context.DoctorDailySchedules.AsNoTracking()
                .FirstOrDefaultAsync(s => s.DoctorId == doctor.DoctorId && s.ScheduleDate == today);

            return Ok(new
            {
                success = true,
                data = new AvailabilityMeDto
                {
                    DoctorId = doctor.DoctorId,
                    IsOnline = doctor.IsOnline,
                    WorkingHoursNote = doctor.WorkingHoursNote,
                    TodaySchedule = schedule == null ? null : new DoctorDailyScheduleModel
                    {
                        DoctorDailyScheduleId = schedule.DoctorDailyScheduleId,
                        DoctorId = schedule.DoctorId,
                        ScheduleDate = schedule.ScheduleDate,
                        SlotIntervalMinutes = schedule.SlotIntervalMinutes,
                        WorkStartTime = schedule.WorkStartTime,
                        WorkEndTime = schedule.WorkEndTime
                    }
                }
            });
        }

        [HttpPut("Me")]
        public async Task<IActionResult> Update([FromBody] AvailabilityUpdateRequest request)
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;

            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found." });

            doctor.IsOnline = request?.IsOnline ?? false;
            if (request?.WorkingHoursNote != null)
                doctor.WorkingHoursNote = request.WorkingHoursNote.Trim();
            doctor.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await Me();
        }

        private async Task<Master.Doctor?> ResolveDoctorAsync()
        {
            var jwtDoctor = DoctorOwnership.GetDoctorId(User);
            if (jwtDoctor.HasValue)
                return await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == jwtDoctor.Value && !d.DeleteStatus);
            var userId = User.GetUserId();
            return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && !d.DeleteStatus);
        }
    }
}

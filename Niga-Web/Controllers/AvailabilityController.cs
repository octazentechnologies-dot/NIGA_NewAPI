using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Helpers;
using Niga_Domain.Master;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>DMO-05 — doctor online toggle, hours note, and today's slot hours (upsert DoctorDailySchedule).</summary>
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

            var hoursError = await UpsertTodayHoursAsync(doctor, request);
            if (hoursError != null)
                return hoursError;

            await _context.SaveChangesAsync();
            return await Me();
        }

        private async Task<IActionResult?> UpsertTodayHoursAsync(Master.Doctor doctor, AvailabilityUpdateRequest? request)
        {
            var hasAnyHour =
                request?.WorkStartTime != null
                || request?.WorkEndTime != null
                || request?.SlotIntervalMinutes != null;

            if (!hasAnyHour)
                return null;

            if (request!.WorkStartTime == null || request.WorkEndTime == null || request.SlotIntervalMinutes == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "WorkStartTime, WorkEndTime and SlotIntervalMinutes are required together."
                });
            }

            var interval = request.SlotIntervalMinutes.Value;
            if (!AppointmentSlotHelper.IsAllowedInterval(interval))
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Invalid slot interval. Enter a whole number between {AppointmentSlotHelper.MinIntervalMinutes} and {AppointmentSlotHelper.MaxIntervalMinutes} minutes."
                });
            }

            if (request.WorkEndTime.Value <= request.WorkStartTime.Value)
            {
                return BadRequest(new { success = false, message = "Work end time must be after work start time." });
            }

            var scheduleDate = (request.ScheduleDate ?? DateTime.Today).Date;
            if (scheduleDate < DateTime.Today)
            {
                return BadRequest(new { success = false, message = "Schedule can only be created for today or future dates." });
            }

            long createdBy = doctor.UserId ?? 0;
            try
            {
                createdBy = User.GetUserId();
            }
            catch
            {
                // keep doctor.UserId
            }

            var existing = await _context.DoctorDailySchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctor.DoctorId && s.ScheduleDate == scheduleDate);

            if (existing == null)
            {
                _context.DoctorDailySchedules.Add(new DoctorDailySchedule
                {
                    DoctorId = doctor.DoctorId,
                    ScheduleDate = scheduleDate,
                    SlotIntervalMinutes = interval,
                    WorkStartTime = request.WorkStartTime.Value,
                    WorkEndTime = request.WorkEndTime.Value,
                    CreatedByUserId = createdBy,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.SlotIntervalMinutes = interval;
                existing.WorkStartTime = request.WorkStartTime.Value;
                existing.WorkEndTime = request.WorkEndTime.Value;
            }

            return null;
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

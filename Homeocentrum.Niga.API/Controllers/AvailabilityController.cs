using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers
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

            // Leave is reported only — Online may stay on if the doctor opted into tele during clinic leave.
            var onLeave = await HasActiveTimeOffAsync(doctor.DoctorId);

            var today = DateTime.Today;
            var until = today.AddDays(13);
            var week = await _context.DoctorDailySchedules.AsNoTracking()
                .Where(s => s.DoctorId == doctor.DoctorId && s.ScheduleDate >= today && s.ScheduleDate <= until)
                .OrderBy(s => s.ScheduleDate)
                .ToListAsync();
            var todayRow = week.FirstOrDefault(s => s.ScheduleDate == today);
            var rule = await ReadBookingRuleAsync(doctor.DoctorId);

            DoctorDailyScheduleModel MapRow(DoctorDailySchedule s) => new()
            {
                DoctorDailyScheduleId = s.DoctorDailyScheduleId,
                DoctorId = s.DoctorId,
                ScheduleDate = s.ScheduleDate,
                SlotIntervalMinutes = s.SlotIntervalMinutes,
                WorkStartTime = s.WorkStartTime,
                WorkEndTime = s.WorkEndTime,
                BreakStartTime = s.BreakStartTime,
                BreakEndTime = s.BreakEndTime
            };

            return Ok(new
            {
                success = true,
                data = new AvailabilityMeDto
                {
                    DoctorId = doctor.DoctorId,
                    IsOnline = doctor.IsOnline,
                    IsOnTimeOff = onLeave,
                    WorkingHoursNote = doctor.WorkingHoursNote,
                    TodaySchedule = todayRow == null ? null : MapRow(todayRow),
                    WeekSchedules = week.Select(MapRow).ToList(),
                    GapMinutes = rule.GapMinutes,
                    BookingHorizonDays = rule.HorizonDays
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

            var onLeave = await HasActiveTimeOffAsync(doctor.DoctorId);
            var isHoursFormSave = request?.Days != null || request?.UpdateBookingRules == true;
            // Hours Update sends isOnline from consultation-mode checkboxes — do not overwrite a
            // manual tele Online choice while the doctor is on clinic leave.
            if (!(isHoursFormSave && onLeave))
                doctor.IsOnline = request?.IsOnline ?? false;
            if (request?.WorkingHoursNote != null)
                doctor.WorkingHoursNote = request.WorkingHoursNote.Trim();
            doctor.ChangedDate = DateTime.UtcNow;

            var hoursError = await UpsertTodayHoursAsync(doctor, request);
            if (hoursError != null)
                return hoursError;

            var daysError = await UpsertWeekHoursAsync(doctor, request);
            if (daysError != null)
                return daysError;

            await ClearScheduleDatesAsync(doctor.DoctorId, request?.ClearScheduleDates);

            await _context.SaveChangesAsync();
            // Hours form always sends Days (even []). Write exact gap/horizon including clears (null).
            // Online-only toggles omit Days and must not wipe booking rules.
            if (request?.Days != null || request?.UpdateBookingRules == true)
                await SaveBookingRuleAsync(doctor.DoctorId, request.GapMinutes, request.BookingHorizonDays, replaceExact: true);
            else if (request?.GapMinutes != null || request?.BookingHorizonDays != null)
                await SaveBookingRuleAsync(doctor.DoctorId, request.GapMinutes, request.BookingHorizonDays, replaceExact: false);
            return await Me();
        }

        private async Task<bool> HasActiveTimeOffAsync(int doctorId)
        {
            await EnsureDoctorTimeOffTableAsync();
            var now = DateTime.Now;
            var hit = await _context.Database.SqlQuery<TimeOffHitRow>($@"
                SELECT TOP 1 DoctorTimeOffId AS Id
                FROM dbo.DoctorTimeOff
                WHERE DoctorId = {doctorId}
                  AND StartsAt <= {now}
                  AND EndsAt >= {now}").FirstOrDefaultAsync();
            return hit != null;
        }

        private static int _doctorTimeOffReady;

        private async Task EnsureDoctorTimeOffTableAsync()
        {
            if (System.Threading.Volatile.Read(ref _doctorTimeOffReady) == 1) return;
            await _context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NULL
                CREATE TABLE dbo.DoctorTimeOff (
                    DoctorTimeOffId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    DoctorId int NOT NULL,
                    StartsAt datetime2(0) NOT NULL,
                    EndsAt datetime2(0) NOT NULL,
                    Reason nvarchar(500) NULL
                );
                """);
            System.Threading.Volatile.Write(ref _doctorTimeOffReady, 1);
        }

        private sealed class TimeOffHitRow
        {
            public int Id { get; set; }
        }

        private async Task<IActionResult?> UpsertTodayHoursAsync(Doctor doctor, AvailabilityUpdateRequest? request)
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

        private async Task<IActionResult?> UpsertWeekHoursAsync(Doctor doctor, AvailabilityUpdateRequest? request)
        {
            if (request?.Days == null || request.Days.Count == 0)
                return null;

            long createdBy = doctor.UserId ?? 0;
            try { createdBy = User.GetUserId(); } catch { }

            foreach (var day in request.Days)
            {
                var scheduleDate = day.ScheduleDate.Date;
                if (scheduleDate < DateTime.Today)
                    continue;
                var interval = day.SlotIntervalMinutes <= 0 ? 15 : day.SlotIntervalMinutes;
                if (!AppointmentSlotHelper.IsAllowedInterval(interval))
                {
                    return BadRequest(new { success = false, message = $"Invalid slot interval on {scheduleDate:yyyy-MM-dd}." });
                }
                if (day.WorkEndTime <= day.WorkStartTime)
                {
                    return BadRequest(new { success = false, message = $"Work end time must be after start on {scheduleDate:yyyy-MM-dd}." });
                }
                var breakError = BreakError(day, scheduleDate);
                if (breakError != null)
                    return breakError;

                var existing = await _context.DoctorDailySchedules
                    .FirstOrDefaultAsync(s => s.DoctorId == doctor.DoctorId && s.ScheduleDate == scheduleDate);
                if (existing == null)
                {
                    _context.DoctorDailySchedules.Add(new DoctorDailySchedule
                    {
                        DoctorId = doctor.DoctorId,
                        ScheduleDate = scheduleDate,
                        SlotIntervalMinutes = interval,
                        WorkStartTime = day.WorkStartTime,
                        WorkEndTime = day.WorkEndTime,
                        BreakStartTime = day.IncludeBreak == true ? day.BreakStartTime : null,
                        BreakEndTime = day.IncludeBreak == true ? day.BreakEndTime : null,
                        CreatedByUserId = createdBy,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.SlotIntervalMinutes = interval;
                    existing.WorkStartTime = day.WorkStartTime;
                    existing.WorkEndTime = day.WorkEndTime;
                    if (day.IncludeBreak == true)
                    {
                        existing.BreakStartTime = day.BreakStartTime;
                        existing.BreakEndTime = day.BreakEndTime;
                    }
                    else if (day.IncludeBreak == false)
                    {
                        existing.BreakStartTime = null;
                        existing.BreakEndTime = null;
                    }
                }
            }

            return null;
        }

        private async Task ClearScheduleDatesAsync(int doctorId, List<DateTime>? dates)
        {
            if (dates == null || dates.Count == 0)
                return;
            var clear = dates.Select(d => d.Date).Distinct().ToList();
            var from = clear.Min();
            var to = clear.Max();
            var candidates = await _context.DoctorDailySchedules
                .Where(s => s.DoctorId == doctorId && s.ScheduleDate >= from && s.ScheduleDate <= to)
                .ToListAsync();
            var rows = candidates.Where(s => clear.Contains(s.ScheduleDate.Date)).ToList();
            if (rows.Count > 0)
                _context.DoctorDailySchedules.RemoveRange(rows);
        }

        private IActionResult? BreakError(AvailabilityDayHours day, DateTime scheduleDate)
        {
            if (day.IncludeBreak != true)
                return null;
            var hasStart = day.BreakStartTime.HasValue;
            var hasEnd = day.BreakEndTime.HasValue;
            if (!hasStart && !hasEnd)
                return null;
            if (!hasStart || !hasEnd || day.BreakEndTime <= day.BreakStartTime)
            {
                return BadRequest(new { success = false, message = $"Break end must be after break start on {scheduleDate:yyyy-MM-dd}." });
            }
            if (day.BreakStartTime < day.WorkStartTime || day.BreakEndTime > day.WorkEndTime)
            {
                return BadRequest(new { success = false, message = $"Break must fall inside working hours on {scheduleDate:yyyy-MM-dd}." });
            }
            return null;
        }

        private async Task<Doctor?> ResolveDoctorAsync()
        {
            var jwtDoctor = DoctorOwnership.GetDoctorId(User);
            if (jwtDoctor.HasValue)
                return await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == jwtDoctor.Value && !d.DeleteStatus);
            var userId = User.GetUserId();
            return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && !d.DeleteStatus);
        }

        private async Task<(int? GapMinutes, int? HorizonDays)> ReadBookingRuleAsync(int doctorId)
        {
            await EnsureBookingRuleTableAsync();
            var row = await _context.Database.SqlQuery<BookingRuleRow>($@"
                SELECT GapMinutes, BookingHorizonDays
                FROM dbo.DoctorBookingRule
                WHERE DoctorId = {doctorId}").FirstOrDefaultAsync();
            return (row?.GapMinutes, row?.BookingHorizonDays);
        }

        /// <param name="replaceExact">
        /// When true (Hours Update), null clears the column. When false, null keeps the existing value.
        /// </param>
        private async Task SaveBookingRuleAsync(int doctorId, int? gapMinutes, int? horizonDays, bool replaceExact)
        {
            if (gapMinutes is < 0 or > 240)
                gapMinutes = null;
            if (horizonDays is < 1 or > 90)
                horizonDays = null;

            await EnsureBookingRuleTableAsync();
            var existing = await _context.Database.SqlQuery<BookingRuleRow>($@"
                SELECT GapMinutes, BookingHorizonDays
                FROM dbo.DoctorBookingRule
                WHERE DoctorId = {doctorId}").FirstOrDefaultAsync();

            int? nextGap;
            int? nextHorizon;
            if (replaceExact)
            {
                nextGap = gapMinutes;
                nextHorizon = horizonDays;
            }
            else
            {
                if (gapMinutes == null && horizonDays == null)
                    return;
                nextGap = gapMinutes ?? existing?.GapMinutes;
                nextHorizon = horizonDays ?? existing?.BookingHorizonDays;
            }

            if (existing == null)
            {
                // No row yet and both cleared — nothing to store.
                if (nextGap == null && nextHorizon == null)
                    return;
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.DoctorBookingRule (DoctorId, GapMinutes, BookingHorizonDays)
                    VALUES ({doctorId}, {nextGap}, {nextHorizon})");
            }
            else
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE dbo.DoctorBookingRule
                    SET GapMinutes = {nextGap}, BookingHorizonDays = {nextHorizon}
                    WHERE DoctorId = {doctorId}");
            }
        }

        private async Task EnsureBookingRuleTableAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("""
                IF OBJECT_ID(N'dbo.DoctorBookingRule', N'U') IS NULL
                CREATE TABLE dbo.DoctorBookingRule (
                    DoctorId int NOT NULL PRIMARY KEY,
                    GapMinutes int NULL,
                    BookingHorizonDays int NULL
                );
                """);
        }

        private sealed class BookingRuleRow
        {
            public int? GapMinutes { get; set; }
            public int? BookingHorizonDays { get; set; }
        }
    }
}

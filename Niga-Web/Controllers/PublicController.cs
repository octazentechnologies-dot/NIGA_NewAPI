using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Enums;
using Niga_Domain.Master;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// WEB-03 / WEB-04 / WEB-01 / PAT-09–14 — public doctor directory and self-service booking.
    /// Anonymous. Unverified doctors are excluded. Same JSON is reused by the patient app.
    /// </summary>
    [Route("api/Public")]
    [ApiController]
    [AllowAnonymous]
    public class PublicController : ControllerBase
    {
        private readonly NIGACentrumContext _context;
        private readonly IWebHostEnvironment _env;

        public PublicController(NIGACentrumContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("Doctors")]
        public async Task<IActionResult> ListDoctors([FromQuery] PublicDoctorListRequest request)
        {
            request ??= new PublicDoctorListRequest();
            var page = request.PageNumber < 1 ? 1 : request.PageNumber;
            var size = request.PageSize is < 1 or > 50 ? 20 : request.PageSize;

            var q = from d in _context.Doctors.AsNoTracking()
                    join qual in _context.QualificationMasters.AsNoTracking()
                        on d.QualificationId equals qual.QualificationId into qj
                    from qual in qj.DefaultIfEmpty()
                    where !d.DeleteStatus
                          && d.DirectoryVisible
                          && d.VerificationStatus == "Verified"
                    select new { d, Qualification = qual != null ? qual.QualificationName : null };

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim();
                q = q.Where(x => x.d.City != null && x.d.City.Contains(city));
            }

            if (!string.IsNullOrWhiteSpace(request.Q))
            {
                var term = request.Q.Trim();
                q = q.Where(x =>
                    (x.d.FirstName + " " + x.d.LastName).Contains(term)
                    || (x.d.ClinicName != null && x.d.ClinicName.Contains(term))
                    || (x.Qualification != null && x.Qualification.Contains(term)));
            }

            if (request.IsOnline == true)
                q = q.Where(x => x.d.IsOnline);
            if (request.TeleOnly == true)
                q = q.Where(x => x.d.ConsultFeeTele != null);
            if (request.MinFee.HasValue)
                q = q.Where(x => (x.d.ConsultFeeInClinic ?? x.d.ConsultFeeTele ?? 0) >= request.MinFee.Value);
            if (request.MaxFee.HasValue)
                q = q.Where(x => (x.d.ConsultFeeInClinic ?? x.d.ConsultFeeTele ?? 0) <= request.MaxFee.Value);

            var total = await q.CountAsync();
            var rows = await q
                .OrderByDescending(x => x.d.IsOnline)
                .ThenBy(x => x.d.LastName)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var data = rows.Select(x => MapCard(x.d, x.Qualification)).ToList();
            return Ok(new { success = true, pageNumber = page, pageSize = size, totalRecords = total, data });
        }

        [HttpGet("Doctors/{id:int}")]
        public async Task<IActionResult> GetDoctor(int id)
        {
            var row = await (
                from d in _context.Doctors.AsNoTracking()
                join qual in _context.QualificationMasters.AsNoTracking()
                    on d.QualificationId equals qual.QualificationId into qj
                from qual in qj.DefaultIfEmpty()
                where d.DoctorId == id && !d.DeleteStatus && d.DirectoryVisible && d.VerificationStatus == "Verified"
                select new { d, Qualification = qual != null ? qual.QualificationName : null }
            ).FirstOrDefaultAsync();

            if (row == null)
                return NotFound(new { success = false, message = "Doctor not found or not verified for directory." });

            var card = MapCard(row.d, row.Qualification);
            var profile = new PublicDoctorProfileDto
            {
                DoctorId = card.DoctorId,
                DisplayName = card.DisplayName,
                Qualification = card.Qualification,
                City = card.City,
                ClinicName = card.ClinicName,
                ConsultFeeInClinic = card.ConsultFeeInClinic,
                ConsultFeeTele = card.ConsultFeeTele,
                IsOnline = card.IsOnline,
                Verified = card.Verified,
                PhotoPath = card.PhotoPath,
                RankingSummary = card.RankingSummary,
                WorkingHoursNote = row.d.WorkingHoursNote,
                VerificationStatus = row.d.VerificationStatus,
                RankingReasons = RankingReasons(row.d, row.Qualification)
            };
            return Ok(new { success = true, data = profile });
        }

        [HttpGet("Doctors/{id:int}/Ranking")]
        public async Task<IActionResult> Ranking(int id)
        {
            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == id && !d.DeleteStatus && d.DirectoryVisible && d.VerificationStatus == "Verified");
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found." });

            var qual = doctor.QualificationId == null
                ? null
                : await _context.QualificationMasters.AsNoTracking()
                    .Where(x => x.QualificationId == doctor.QualificationId)
                    .Select(x => x.QualificationName)
                    .FirstOrDefaultAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    doctorId = doctor.DoctorId,
                    summary = RankingSummary(doctor, qual),
                    reasons = RankingReasons(doctor, qual)
                }
            });
        }

        [HttpGet("Doctors/{id:int}/Slots")]
        public async Task<IActionResult> Slots(int id, [FromQuery] DateTime date)
        {
            if (date == default)
                date = DateTime.Today;

            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == id && !d.DeleteStatus && d.DirectoryVisible && d.VerificationStatus == "Verified");
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found." });

            var day = date.Date;
            var schedule = await _context.DoctorDailySchedules.AsNoTracking()
                .FirstOrDefaultAsync(s => s.DoctorId == id && s.ScheduleDate == day);

            var booked = await _context.PatientAppointments.AsNoTracking()
                .Where(a => a.DoctorId == id
                            && a.AppointmentDate.HasValue
                            && a.AppointmentDate.Value.Date == day
                            && a.DeleteStatus != true
                            && a.AppointmentTime != null)
                .Select(a => a.AppointmentTime!.Value)
                .ToListAsync();
            var bookedSet = booked.Select(t => t.ToString("HH:mm")).ToHashSet(StringComparer.Ordinal);

            var slots = new List<object>();
            if (schedule != null)
            {
                var cursor = schedule.WorkStartTime;
                var interval = schedule.SlotIntervalMinutes <= 0 ? 15 : schedule.SlotIntervalMinutes;
                while (cursor < schedule.WorkEndTime)
                {
                    var key = cursor.ToString("HH:mm");
                    slots.Add(new
                    {
                        time = key,
                        label = key,
                        status = bookedSet.Contains(key) ? "booked" : "available"
                    });
                    cursor = cursor.AddMinutes(interval);
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    doctorId = id,
                    appointmentDate = day,
                    hasSchedule = schedule != null,
                    intervalMinutes = schedule?.SlotIntervalMinutes ?? 0,
                    slots
                }
            });
        }

        [HttpPost("Doctors/{id:int}/Bookings")]
        public async Task<IActionResult> CreateBooking(int id, [FromBody] PublicBookingCreateRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid booking request." });

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorId == id && !d.DeleteStatus && d.DirectoryVisible && d.VerificationStatus == "Verified");
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found or not verified." });

            var mobile = PhoneNormalizer.Digits(request.Mobile);
            if (mobile.Length < 8)
                return BadRequest(new { success = false, message = "Valid mobile is required." });

            var session = await _context.OtpChallenges.FirstOrDefaultAsync(c =>
                c.OtpChallengeId == request.BookingSessionId
                && c.Action == "PatientAuth"
                && c.VerifiedAt != null
                && c.EntityId == mobile);
            if (session == null)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Verify PatientAuth OTP before booking." });

            if (!TimeOnly.TryParse(request.AppointmentTime, out var time))
                return BadRequest(new { success = false, message = "AppointmentTime must be HH:mm." });

            var day = request.AppointmentDate.Date;
            var clash = await _context.PatientAppointments.AnyAsync(a =>
                a.DoctorId == id
                && a.DeleteStatus != true
                && a.AppointmentDate.HasValue
                && a.AppointmentDate.Value.Date == day
                && a.AppointmentTime == time);
            if (clash)
                return Conflict(new { success = false, message = "That slot is already booked." });

            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.MobileNo == mobile && p.DeleteStatus != true);
            if (patient == null)
            {
                patient = new Patient
                {
                    PatientName = request.PatientName.Trim(),
                    MobileNo = mobile,
                    Email = request.Email,
                    DeleteStatus = false,
                    EnteredDate = DateTime.UtcNow,
                    IsWhatsAppOptIn = false
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }

            var policy = await _context.PolicyVersions.AsNoTracking()
                .Where(p => p.PolicyType == "Terms" && p.IsCurrent)
                .Select(p => p.Version)
                .FirstOrDefaultAsync();

            var token = Guid.NewGuid().ToString("N");
            var appt = new PatientAppointment
            {
                PatientId = patient.PatientId,
                DoctorId = id,
                UserId = doctor.UserId ?? 0,
                AppointmentDate = day,
                AppointmentTime = time,
                Status = PatientsStatus.NotArrived.GetDisplayName(),
                DeleteStatus = false,
                BookingToken = token,
                VisitType = string.IsNullOrWhiteSpace(request.VisitType) ? "InClinic" : request.VisitType.Trim(),
                ConsultMode = string.IsNullOrWhiteSpace(request.ConsultMode) ? "InClinic" : request.ConsultMode.Trim(),
                IsTele = request.IsTele,
                PaymentStatus = "PENDING",
                HoldExpiresAt = DateTime.UtcNow.AddMinutes(15),
                ConsentPolicyVersion = request.ConsentPolicyVersion ?? policy
            };
            _context.PatientAppointments.Add(appt);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    patientAppId = appt.PatientAppId,
                    bookingToken = appt.BookingToken,
                    paymentStatus = appt.PaymentStatus,
                    holdExpiresAt = appt.HoldExpiresAt,
                    consentPolicyVersion = appt.ConsentPolicyVersion
                }
            });
        }

        [HttpGet("Bookings/{bookingToken}")]
        public async Task<IActionResult> GetBooking(string bookingToken)
        {
            if (string.IsNullOrWhiteSpace(bookingToken))
                return BadRequest(new { success = false, message = "bookingToken is required." });

            var appt = await _context.PatientAppointments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.BookingToken == bookingToken && a.DeleteStatus != true);
            if (appt == null)
                return NotFound(new { success = false, message = "Booking not found." });

            return Ok(new
            {
                success = true,
                data = new
                {
                    appt.PatientAppId,
                    appt.DoctorId,
                    appt.PatientId,
                    appt.AppointmentDate,
                    appointmentTime = appt.AppointmentTime?.ToString("HH:mm"),
                    appt.Status,
                    appt.VisitType,
                    appt.ConsultMode,
                    appt.PaymentStatus,
                    appt.IsTele,
                    appt.BookingToken,
                    appt.HoldExpiresAt,
                    appt.ConsentPolicyVersion
                }
            });
        }

        [HttpGet("Policies/{policyType}")]
        public async Task<IActionResult> Policy(string policyType)
        {
            var row = await _context.PolicyVersions.AsNoTracking()
                .Where(p => p.PolicyType == policyType && p.IsCurrent)
                .OrderByDescending(p => p.EffectiveAt)
                .FirstOrDefaultAsync();
            if (row == null)
                return NotFound(new { success = false, message = "Policy not found." });

            return Ok(new
            {
                success = true,
                data = new
                {
                    row.PolicyType,
                    row.Version,
                    row.Title,
                    row.BodyHtml,
                    row.EffectiveAt
                }
            });
        }

        [HttpGet("Articles")]
        public async Task<IActionResult> Articles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize is < 1 or > 50 ? 20 : pageSize;
            var q = _context.BlogDetails.AsNoTracking().Where(b => b.IsActive == true);
            var total = await q.CountAsync();
            var rows = await q.OrderByDescending(b => b.BlogDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.BlogId,
                    b.BlogHead,
                    b.BlogSubHead,
                    b.BlogDate,
                    b.BlogImage1,
                    BodyUrl = "/api/Public/Articles/" + b.BlogId
                })
                .ToListAsync();
            return Ok(new { success = true, pageNumber, pageSize, totalRecords = total, data = rows });
        }

        [HttpGet("Articles/{id:int}")]
        public async Task<IActionResult> ArticleById(int id)
        {
            var row = await _context.BlogDetails.AsNoTracking()
                .FirstOrDefaultAsync(b => b.BlogId == id && b.IsActive == true);
            if (row == null)
                return NotFound(new { success = false, message = "Article not found." });

            return Ok(new
            {
                success = true,
                data = new
                {
                    row.BlogId,
                    row.BlogHead,
                    row.BlogSubHead,
                    row.BlogDate,
                    row.BlogImage1,
                    row.BlogImage2,
                    body = row.BlogDetails
                }
            });
        }

        private PublicDoctorCardDto MapCard(Doctor d, string? qualification)
        {
            var name = string.Join(" ", new[] { d.FirstName, d.MiddleName, d.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return new PublicDoctorCardDto
            {
                DoctorId = d.DoctorId,
                DisplayName = name,
                Qualification = qualification,
                City = d.City,
                ClinicName = d.ClinicName,
                ConsultFeeInClinic = d.ConsultFeeInClinic,
                ConsultFeeTele = d.ConsultFeeTele,
                IsOnline = d.IsOnline,
                Verified = string.Equals(d.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase),
                PhotoPath = d.PhotoPath,
                RankingSummary = RankingSummary(d, qualification)
            };
        }

        private static string RankingSummary(Doctor d, string? qualification)
        {
            var bits = new List<string>();
            if (string.Equals(d.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase))
                bits.Add("Verified credentials");
            if (!string.IsNullOrWhiteSpace(qualification))
                bits.Add(qualification);
            if (d.IsOnline)
                bits.Add("Currently online");
            return bits.Count == 0 ? "Listed doctor" : string.Join(" · ", bits);
        }

        private static List<string> RankingReasons(Doctor d, string? qualification)
        {
            var reasons = new List<string>();
            if (string.Equals(d.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase))
                reasons.Add("Platform verification completed (directory eligible).");
            if (!string.IsNullOrWhiteSpace(qualification))
                reasons.Add("Qualification: " + qualification);
            if (d.ConsultFeeInClinic.HasValue || d.ConsultFeeTele.HasValue)
                reasons.Add("Published consultation fees.");
            if (d.IsOnline)
                reasons.Add("Doctor is online now.");
            if (reasons.Count == 0)
                reasons.Add("No ranking signals yet.");
            return reasons;
        }
    }
}

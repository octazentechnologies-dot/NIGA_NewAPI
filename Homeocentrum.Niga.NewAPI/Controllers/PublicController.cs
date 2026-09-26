using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Enums;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers
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
        private readonly IPatientAppointmentService _appointments;
        private readonly IS4Week4Service _fees;

        public PublicController(
            NIGACentrumContext context,
            IWebHostEnvironment env,
            IPatientAppointmentService appointments,
            IS4Week4Service fees)
        {
            _context = context;
            _env = env;
            _appointments = appointments;
            _fees = fees;
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

            var stats = await LoadReviewStatsAsync(rows.Select(x => x.d.DoctorId).ToList());
            var data = rows.Select(x => MapCard(x.d, x.Qualification, stats.GetValueOrDefault(x.d.DoctorId))).ToList();
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

            var stats = await LoadReviewStatsAsync(new List<int> { row.d.DoctorId });
            var card = MapCard(row.d, row.Qualification, stats.GetValueOrDefault(row.d.DoctorId));
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
                AverageRating = card.AverageRating,
                ReviewCount = card.ReviewCount,
                WorkingHoursNote = row.d.WorkingHoursNote,
                VerificationStatus = row.d.VerificationStatus,
                RankingReasons = card.RankingReasons
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

        /// <summary>
        /// APT-08.03 / PAT-16.02 — public + patient-app slot picker.
        /// Same GetAppointmentSlotsAsync engine as clinic GetAppointmentSlots. No second calculator.
        /// Anonymous. Does not return patient names/ids (mobile-safe). Slot status: available|booked|break|past.
        /// </summary>
        [HttpGet("Doctors/{id:int}/Slots")]
        public async Task<IActionResult> GetDoctorSlots(int id, [FromQuery] DateTime date)
        {
            if (date == default)
                date = DateTime.Today;

            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == id && !d.DeleteStatus && d.DirectoryVisible && d.VerificationStatus == "Verified");
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found or not verified." });

            var day = date.Date;
            var engine = await _appointments.GetAppointmentSlotsAsync(new GetAppointmentSlotsRequest
            {
                DoctorId = id,
                AppointmentDate = day
            });

            return Ok(new
            {
                success = true,
                data = new
                {
                    doctorId = engine.DoctorId,
                    appointmentDate = engine.AppointmentDate,
                    hasSchedule = engine.HasSchedule,
                    intervalMinutes = engine.IntervalMinutes,
                    slots = engine.Slots.Select(s => new { time = s.Time, label = s.Label, status = s.Status })
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
            // Same slot engine as the staff calendar. A cancelled visit does not keep the slot.
            var engine = await _appointments.GetAppointmentSlotsAsync(new GetAppointmentSlotsRequest
            {
                DoctorId = id,
                AppointmentDate = day
            });
            if (!engine.HasSchedule)
                return BadRequest(new { success = false, message = "Daily appointment schedule is not configured for this date." });
            var match = engine.Slots.FirstOrDefault(s => TimeOnly.TryParse(s.Time, out var slotTime) && slotTime == time);
            if (match == null)
                return BadRequest(new { success = false, message = "Selected time is not a valid appointment slot." });
            if (!string.Equals(match.Status, "available", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(match.Status, "booked", StringComparison.OrdinalIgnoreCase))
                    return Conflict(new { success = false, message = "That slot is already booked." });
                return BadRequest(new { success = false, message = "Selected time is " + match.Status + "." });
            }

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

            var payAtClinicEnabled = await _fees.PayAtClinicEnabledAsync(id);
            if (request.PayAtClinic == true && !payAtClinicEnabled)
                return BadRequest(new { success = false, code = "PAY_AT_CLINIC_DISABLED", message = "This doctor does not accept pay at clinic." });

            var token = Guid.NewGuid().ToString("N");
            var mode = S3AppointmentRules.NormalizeMode(request.ConsultMode ?? request.VisitType);
            var isTele = mode == S3AppointmentRules.Tele;
            var fees = await _fees.ResolveConsultFeesAsync(id);
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
                VisitType = mode,
                ConsultMode = mode,
                IsTele = isTele,
                PaymentStatus = "PENDING",
                PayAtClinicAllowed = request.PayAtClinic ?? payAtClinicEnabled,
                HoldExpiresAt = DateTime.UtcNow.AddMinutes(15),
                ConsentPolicyVersion = request.ConsentPolicyVersion ?? policy
            };
            _context.PatientAppointments.Add(appt);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                // PAT-18.02 — checkout inputs for patient app (PENDING hold; fee; pay-at-clinic flags)
                data = new
                {
                    patientAppId = appt.PatientAppId,
                    bookingToken = appt.BookingToken,
                    paymentStatus = appt.PaymentStatus,
                    holdExpiresAt = appt.HoldExpiresAt,
                    consentPolicyVersion = appt.ConsentPolicyVersion,
                    consultMode = appt.ConsultMode,
                    consultFee = isTele ? fees.TeleFee : fees.InClinicFee,
                    payAtClinicAllowed = appt.PayAtClinicAllowed,
                    payAtClinicEnabled
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

            var isTele = appt.IsTele == true
                || S3AppointmentRules.NormalizeMode(appt.ConsultMode) == S3AppointmentRules.Tele;
            var fees = await _fees.ResolveConsultFeesAsync(appt.DoctorId);
            var payAtClinicEnabled = await _fees.PayAtClinicEnabledAsync(appt.DoctorId);

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
                    appt.ConsentPolicyVersion,
                    consultFee = isTele ? fees.TeleFee : fees.InClinicFee,
                    payAtClinicEnabled
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
            // Project only article columns. BlogDetail.EnteredBy is string on the entity
            // but int in SQL, so materializing the full row throws InvalidCastException.
            var row = await _context.BlogDetails.AsNoTracking()
                .Where(b => b.BlogId == id && b.IsActive == true)
                .Select(b => new
                {
                    b.BlogId,
                    b.BlogHead,
                    b.BlogSubHead,
                    b.BlogDate,
                    b.BlogImage1,
                    b.BlogImage2,
                    Body = b.BlogDetails
                })
                .FirstOrDefaultAsync();
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
                    body = row.Body
                }
            });
        }

        private PublicDoctorCardDto MapCard(Doctor d, string? qualification, ReviewStat? stats = null)
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
                RankingSummary = RankingSummary(d, qualification),
                RankingReasons = RankingReasons(d, qualification),
                AverageRating = stats is { ReviewCount: > 0 } ? stats.AverageRating : null,
                ReviewCount = stats?.ReviewCount ?? 0
            };
        }

        private async Task<Dictionary<int, ReviewStat>> LoadReviewStatsAsync(List<int> doctorIds)
        {
            var map = new Dictionary<int, ReviewStat>();
            if (doctorIds == null || doctorIds.Count == 0)
                return map;
            var wanted = doctorIds.ToHashSet();
            var rows = await _context.Database.SqlQuery<ReviewStat>($@"
                SELECT DoctorId, CAST(AVG(CAST(Rating AS float)) AS decimal(9,1)) AS AverageRating, COUNT(1) AS ReviewCount
                FROM dbo.Review
                WHERE Status = N'APPROVED'
                GROUP BY DoctorId").ToListAsync();
            foreach (var row in rows)
            {
                if (wanted.Contains(row.DoctorId))
                    map[row.DoctorId] = row;
            }
            return map;
        }

        private sealed class ReviewStat
        {
            public int DoctorId { get; set; }
            public decimal AverageRating { get; set; }
            public int ReviewCount { get; set; }
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

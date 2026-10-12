using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Services;

namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>WEB-04.02 — anonymous PatientAuth OTP for public booking (reuses OtpChallenge).</summary>
    [Route("api/PatientAuth")]
    [ApiController]
    [AllowAnonymous]
    public class PatientAuthController : ControllerBase
    {
        private const int OtpTtlMinutes = 10;
        private readonly NIGACentrumContext _context;
        private readonly ITokenService _tokenService;

        public PatientAuthController(NIGACentrumContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [SecurityAudit(SecurityAuditEvents.OtpRequested)]
        [HttpPost("RequestOtp")]
        public async Task<IActionResult> RequestOtp([FromBody] PatientAuthRequestOtpModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mobile))
                return BadRequest(new { success = false, message = "Mobile is required." });

            var digits = PhoneNormalizer.Digits(request.Mobile);
            if (digits.Length < 8)
                return BadRequest(new { success = false, message = "Destination must be a valid mobile number." });

            // SMS vendor is not live yet. devCode is always returned with the challenge.

            var since = DateTime.UtcNow.AddMinutes(-1);
            var recent = await _context.OtpChallenges.CountAsync(c =>
                c.Action == "PatientAuth" && c.EntityId == digits && c.CreatedAt >= since);
            if (recent >= 3)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = "Too many OTP requests." });

            // Option A: registered = active Patient UserMaster for this mobile (Ignore IsUserActivated).
            var patientRoleId = await _context.RoleMasters.AsNoTracking()
                .Where(r => r.RoleName == "Patient")
                .Select(r => (int?)r.RoleId)
                .FirstOrDefaultAsync();
            var isUserRegistered = false;
            if (patientRoleId.HasValue)
            {
                var candidateMobiles = await _context.UserMasters.AsNoTracking()
                    .Where(u => !u.DeleteStatus && u.RoleId == patientRoleId && u.MobileNo != null)
                    .Select(u => u.MobileNo!)
                    .ToListAsync();
                isUserRegistered = candidateMobiles.Any(m => PhoneNormalizer.EqualsNormalized(m, digits));
            }

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var challenge = new OtpChallenge
            {
                Action = "PatientAuth",
                EntityType = "Mobile",
                EntityId = digits,
                DestinationMasked = digits.Length > 4 ? "******" + digits[^4..] : "****",
                OtpHash = SecurityTokenHash.Sha256Hex(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.OtpChallenges.Add(challenge);
            await _context.SaveChangesAsync();

            var payload = new Dictionary<string, object?>
            {
                ["success"] = true,
                ["otpChallengeId"] = challenge.OtpChallengeId,
                ["expiresAt"] = challenge.ExpiresAt,
                ["destinationMasked"] = challenge.DestinationMasked,
                ["devCode"] = code,
                ["isUserRegistered"] = isUserRegistered
            };

            return Ok(payload);
        }

        [SecurityAudit(SecurityAuditEvents.Login)]
        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] PatientAuthVerifyOtpModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { success = false, message = "Mobile and Code are required." });

            var digits = PhoneNormalizer.Digits(request.Mobile);
            var row = await _context.OtpChallenges
                .Where(c => c.Action == "PatientAuth" && c.EntityId == digits && c.VerifiedAt == null)
                .OrderByDescending(c => c.OtpChallengeId)
                .FirstOrDefaultAsync();
            if (row == null)
                return BadRequest(new { success = false, message = "No pending PatientAuth OTP." });
            if (row.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { success = false, message = "OTP expired." });
            if (row.LockedUntil.HasValue && row.LockedUntil > DateTime.UtcNow)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = "OTP locked. Try later." });

            row.AttemptCount += 1;
            if (!string.Equals(row.OtpHash, SecurityTokenHash.Sha256Hex(request.Code.Trim()), StringComparison.OrdinalIgnoreCase))
            {
                if (row.AttemptCount >= 5)
                    row.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                return BadRequest(new { success = false, message = "Invalid OTP." });
            }

            row.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var payload = new Dictionary<string, object?>
            {
                ["success"] = true,
                ["bookingSessionId"] = row.OtpChallengeId,
                ["mobile"] = digits
            };
            var user = await ExistingPatientUserAsync(digits);
            payload["isUserAlreadyRegistered"] = user != null;
            if (user != null)
            {
                payload["token"] = user.Token;
                payload["user"] = new
                {
                    user.UserId,
                    user.UserName,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.MobileNo,
                    user.Role,
                    user.RoleId,
                    user.PatientId,
                    user.PatientName
                };
            }
            return Ok(payload);
        }

        /// <summary>
        /// User details and token only when this mobile is already on a Patient row and that login exists.
        /// </summary>
        private async Task<PatientAuthUserDetails?> ExistingPatientUserAsync(string digits)
        {
            var patients = await _context.Patients.AsNoTracking()
                .Where(p => p.DeleteStatus != true && p.MobileNo != null)
                .Select(p => new { p.PatientId, p.PatientName, p.MobileNo, p.Email })
                .ToListAsync();
            var patient = patients.FirstOrDefault(p => PhoneNormalizer.EqualsNormalized(p.MobileNo, digits));
            if (patient == null)
                return null;

            var mappedUserId = await _context.PatientUserMaps.AsNoTracking()
                .Where(m => m.PatientId == patient.PatientId && !m.DeleteStatus)
                .OrderByDescending(m => m.IsPrimary)
                .Select(m => (long?)m.UserId)
                .FirstOrDefaultAsync();
            var user = mappedUserId.HasValue
                ? await _context.UserMasters.FirstOrDefaultAsync(u => u.UserId == mappedUserId.Value && !u.DeleteStatus && u.IsUserActivated == true)
                : null;
            if (user == null)
            {
                var users = await _context.UserMasters.AsNoTracking()
                    .Where(u => !u.DeleteStatus && u.MobileNo != null && u.IsUserActivated == true)
                    .Select(u => new { u.UserId, u.MobileNo, u.RoleId })
                    .ToListAsync();
                var patientRoleId = await _context.RoleMasters.AsNoTracking()
                    .Where(r => r.RoleName == "Patient" && !r.DeleteStatus)
                    .Select(r => (int?)r.RoleId)
                    .FirstOrDefaultAsync();
                var chosen = users
                    .Where(u => PhoneNormalizer.EqualsNormalized(u.MobileNo, digits))
                    .OrderByDescending(u => patientRoleId.HasValue && u.RoleId == patientRoleId.Value)
                    .FirstOrDefault();
                if (chosen == null)
                    return null;
                user = await _context.UserMasters
                    .FirstOrDefaultAsync(u => u.UserId == chosen.UserId && !u.DeleteStatus);
            }
            if (user == null)
                return null;

            var roleName = await _context.RoleMasters.AsNoTracking()
                .Where(r => r.RoleId == user.RoleId && !r.DeleteStatus)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(roleName))
                return null;

            int? doctorId = await _context.Doctors.AsNoTracking()
                .Where(d => d.UserId == user.UserId && !d.DeleteStatus)
                .Select(d => (int?)d.DoctorId)
                .FirstOrDefaultAsync();

            var token = await _tokenService.CreateToken(user, 7 * 24 * 60, roleName, doctorId);
            return new PatientAuthUserDetails
            {
                UserId = user.UserId,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.EmailId ?? patient.Email,
                MobileNo = digits,
                Role = roleName,
                RoleId = user.RoleId,
                PatientId = patient.PatientId,
                PatientName = patient.PatientName,
                Token = token
            };
        }

        private sealed class PatientAuthUserDetails
        {
            public long UserId { get; set; }
            public string? UserName { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? MobileNo { get; set; }
            public string? Role { get; set; }
            public int? RoleId { get; set; }
            public int PatientId { get; set; }
            public string? PatientName { get; set; }
            public string Token { get; set; } = string.Empty;
        }
    }
}

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>WEB-04.02 — anonymous PatientAuth OTP for public booking (reuses OtpChallenge).</summary>
    [Route("api/PatientAuth")]
    [ApiController]
    [AllowAnonymous]
    public class PatientAuthController : ControllerBase
    {
        private const int OtpTtlMinutes = 10;
        private readonly NIGACentrumContext _context;

        public PatientAuthController(NIGACentrumContext context)
        {
            _context = context;
        }

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
            return Ok(new { success = true, bookingSessionId = row.OtpChallengeId, mobile = digits });
        }
    }
}

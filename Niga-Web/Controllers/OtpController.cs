using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Authorization;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Master;
using Niga_Domain.Services;

namespace Niga_Domain.API.Controllers
{
    /// <summary>SEC-07.02 / SEC-07.03 — Generic OTP request/verify + audit (SMS stub).</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OtpController : ControllerBase
    {
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 15;
        private const int OtpTtlMinutes = 10;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
        private const int MaxPerWindow = 3;

        private readonly NIGACentrumContext _context;

        public OtpController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpPost("RequestOtp")]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpModel request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Action)
                || string.IsNullOrWhiteSpace(request.EntityType)
                || string.IsNullOrWhiteSpace(request.EntityId)
                || string.IsNullOrWhiteSpace(request.Destination))
            {
                return BadRequest(new { success = false, message = "Action, EntityType, EntityId, and Destination are required." });
            }

            var since = DateTime.UtcNow.Subtract(RateLimitWindow);
            var recent = await _context.OtpChallenges.CountAsync(c =>
                c.EntityType == request.EntityType
                && c.EntityId == request.EntityId
                && c.Action == request.Action
                && c.CreatedAt >= since);

            if (recent >= MaxPerWindow)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = "Too many OTP requests. Try again later." });

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var masked = MaskDestination(request.Destination);

            var challenge = new OtpChallenge
            {
                Action = request.Action.Trim(),
                EntityType = request.EntityType.Trim(),
                EntityId = request.EntityId.Trim(),
                DestinationMasked = masked,
                OtpHash = SecurityTokenHash.Sha256Hex(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.OtpChallenges.Add(challenge);
            _context.OtpAuditLogs.Add(new OtpAuditLog
            {
                Action = "RequestOtp",
                EntityType = challenge.EntityType,
                EntityId = challenge.EntityId,
                ToMasked = masked,
                Success = true,
                At = DateTime.UtcNow,
                ActorUserId = User.GetUserId()
            });
            await _context.SaveChangesAsync();

            // SMS provider adapter stub until wired — do not return raw code in production responses.
            // Development hint only when ASPNETCORE_ENVIRONMENT is Development:
            var payload = new Dictionary<string, object?>
            {
                ["otpChallengeId"] = challenge.OtpChallengeId,
                ["destinationMasked"] = masked,
                ["expiresAt"] = challenge.ExpiresAt,
                ["message"] = "OTP sent (SMS stub — provider not configured)."
            };
            if (HttpContext.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true)
                payload["devCode"] = code;

            return Ok(new { success = true, data = payload });
        }

        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpModel request)
        {
            if (request == null || request.OtpChallengeId <= 0 || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { success = false, message = "OtpChallengeId and Code are required." });

            var challenge = await _context.OtpChallenges
                .FirstOrDefaultAsync(c => c.OtpChallengeId == request.OtpChallengeId);
            if (challenge == null)
                return NotFound(new { success = false, message = "Challenge not found." });

            if (challenge.LockedUntil.HasValue && challenge.LockedUntil > DateTime.UtcNow)
            {
                await WriteAuditAsync(challenge, false);
                return StatusCode(StatusCodes.Status423Locked, new { success = false, message = "OTP locked due to too many attempts." });
            }

            if (challenge.VerifiedAt != null || challenge.ExpiresAt < DateTime.UtcNow)
            {
                await WriteAuditAsync(challenge, false);
                return BadRequest(new { success = false, message = "OTP expired or already used." });
            }

            challenge.AttemptCount++;
            var ok = string.Equals(
                SecurityTokenHash.Sha256Hex(request.Code.Trim()),
                challenge.OtpHash,
                StringComparison.OrdinalIgnoreCase);

            if (!ok)
            {
                if (challenge.AttemptCount >= MaxAttempts)
                    challenge.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _context.SaveChangesAsync();
                await WriteAuditAsync(challenge, false);
                return BadRequest(new { success = false, message = "Invalid OTP." });
            }

            challenge.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await WriteAuditAsync(challenge, true);

            return Ok(new { success = true, message = "OTP verified." });
        }

        /// <summary>SEC-07.03 — Account and Admin roles (masked destination).</summary>
        [HttpGet("Audit")]
        [Authorize]
        public async Task<IActionResult> Audit([FromQuery] int take = 100)
        {
            var roleName = AdminAuthorizationPolicies.GetRoleName(User);
            var isAccount = string.Equals(roleName, "Account", StringComparison.OrdinalIgnoreCase)
                || User.IsInRole("Account");
            if (!isAccount && !AdminAuthorizationPolicies.IsAdminPortalUser(User))
                return Forbid();

            take = Math.Clamp(take, 1, 500);
            var rows = await _context.OtpAuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.At)
                .Take(take)
                .Select(a => new
                {
                    a.OtpAuditLogId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.ToMasked,
                    a.Success,
                    a.At,
                    a.ActorUserId
                })
                .ToListAsync();

            return Ok(new { success = true, data = rows });
        }

        private async Task WriteAuditAsync(OtpChallenge challenge, bool success)
        {
            _context.OtpAuditLogs.Add(new OtpAuditLog
            {
                Action = "VerifyOtp",
                EntityType = challenge.EntityType,
                EntityId = challenge.EntityId,
                ToMasked = challenge.DestinationMasked,
                Success = success,
                At = DateTime.UtcNow,
                ActorUserId = User.GetUserId()
            });
            await _context.SaveChangesAsync();
        }

        private static string MaskDestination(string destination)
        {
            var d = destination.Trim();
            if (d.Contains('@'))
            {
                var at = d.IndexOf('@');
                if (at <= 1) return "***" + d[at..];
                return d[0] + "***" + d[at..];
            }

            if (d.Length <= 4) return "****";
            return new string('*', d.Length - 4) + d[^4..];
        }
    }
}

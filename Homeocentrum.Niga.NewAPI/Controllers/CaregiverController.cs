using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// M16 CON-02.02 — Caregiver grant / revoke / list + booking authorisation (mobile-reusable).
    /// PatientId comes from the logged-in account. Caregiver is identified by mobile or email, not UserId.
    /// </summary>
    [Route("api/Caregiver")]
    [ApiController]
    [Authorize]
    [ForbidMoneyRoles]
    public class CaregiverController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public CaregiverController(NIGACentrumContext context)
        {
            _context = context;
        }

        public const string GrantOtpAction = "GrantCaregiver";

        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userId = (long)User.GetUserId();
                if (userId <= 0)
                    return Unauthorized(new { success = false, message = "Not signed in." });

                var owner = await PatientPortalOwnerResolver.ResolveAsync(
                    _context, userId, preferActingFor: true, createIfMissing: false)
                    ?? await PatientPortalOwnerResolver.ResolveAsync(
                        _context, userId, preferActingFor: false, createIfMissing: true);
                if (owner == null)
                    return BadRequest(new { success = false, message = "Could not resolve a patient record for this login." });

                var user = await _context.UserMasters.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId && !u.DeleteStatus);
                var destination = FirstContact(user);

                return Ok(new
                {
                    success = true,
                    data = new CaregiverMeDto
                    {
                        OwnerPatientId = owner.PatientId,
                        OwnerPatientName = owner.PatientName,
                        OtpDestination = destination,
                        Linked = true,
                        IsActingAsCaregiver = owner.IsActingAsCaregiver
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("Lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string? contact)
        {
            var user = await FindUserByContactAsync(contact);
            if (user == null)
                return NotFound(new { success = false, message = "No Homeocentrum login found for that mobile or email. They must register first." });

            return Ok(new
            {
                success = true,
                data = ToLookup(user)
            });
        }

        [HttpPost("Grant")]
        public async Task<IActionResult> Grant([FromBody] CaregiverGrantRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Body is required." });

            var userId = (long)User.GetUserId();
            var isAdmin = AdminAuthorizationPolicies.IsAdminPortalUser(User);

            int patientId;
            if (isAdmin && request.PatientId.HasValue && request.PatientId.Value > 0)
            {
                patientId = request.PatientId.Value;
            }
            else
            {
                var owner = await PatientPortalOwnerResolver.ResolveAsync(
                    _context, userId, preferActingFor: false, createIfMissing: false);
                if (owner == null || owner.IsActingAsCaregiver)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "Only the patient account linked to this record may grant caregiver access." });
                patientId = owner.PatientId;
            }

            long caregiverUserId;
            if (request.CaregiverUserId.HasValue && request.CaregiverUserId.Value > 0
                && (isAdmin || string.IsNullOrWhiteSpace(request.CaregiverContact)))
            {
                caregiverUserId = request.CaregiverUserId.Value;
                var exists = await _context.UserMasters.AnyAsync(u =>
                    u.UserId == caregiverUserId && !u.DeleteStatus);
                if (!exists)
                    return NotFound(new { success = false, message = "Caregiver login not found." });
            }
            else
            {
                var caregiver = await FindUserByContactAsync(request.CaregiverContact);
                if (caregiver == null)
                    return NotFound(new { success = false, message = "No Homeocentrum login found for that mobile or email. They must register first." });
                caregiverUserId = caregiver.UserId;
            }

            if (caregiverUserId == userId)
                return BadRequest(new { success = false, message = "You cannot grant caregiver access to your own login." });

            request.PatientId = patientId;

            if (!isAdmin)
            {
                var owns = await _context.PatientUserMaps.AnyAsync(m =>
                    m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus);
                if (!owns)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "Only the patient account linked to this record may grant caregiver access." });

                var otpError = await VerifyGrantOtpAsync(request);
                if (otpError != null)
                    return otpError;
            }

            var active = await _context.CaregiverAuthorizations.FirstOrDefaultAsync(c =>
                c.PatientId == patientId
                && c.CaregiverUserId == caregiverUserId
                && !c.DeleteStatus
                && c.RevokedAt == null);
            if (active != null)
                return Ok(new { success = true, message = "Already granted.", data = (await ToDtoListAsync(new[] { active }))[0] });

            var row = new CaregiverAuthorization
            {
                PatientId = patientId,
                CaregiverUserId = caregiverUserId,
                GrantedByUserId = userId,
                GrantedAt = DateTime.UtcNow,
                Scope = string.IsNullOrWhiteSpace(request.Scope) ? "booking" : request.Scope.Trim(),
                DeleteStatus = false
            };
            _context.CaregiverAuthorizations.Add(row);

            var caregiverType = await _context.ConsentTypes
                .FirstOrDefaultAsync(t => t.Code == "Caregiver" && t.IsActive);
            if (caregiverType != null)
            {
                _context.ConsentRecords.Add(new ConsentRecord
                {
                    ConsentTypeId = caregiverType.ConsentTypeId,
                    SubjectType = "Patient",
                    SubjectId = patientId,
                    GrantedByUserId = userId,
                    GrantedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Notes = $"CaregiverUserId={caregiverUserId}"
                });
            }

            await _context.SaveChangesAsync();
            var dto = (await ToDtoListAsync(new[] { row }))[0];
            return Ok(new { success = true, data = dto });
        }

        [HttpPost("Revoke")]
        public async Task<IActionResult> Revoke([FromBody] CaregiverRevokeRequest request)
        {
            if (request == null || request.CaregiverAuthorizationId <= 0)
                return BadRequest(new { success = false, message = "CaregiverAuthorizationId is required." });

            var userId = (long)User.GetUserId();
            var row = await _context.CaregiverAuthorizations
                .FirstOrDefaultAsync(c => c.CaregiverAuthorizationId == request.CaregiverAuthorizationId && !c.DeleteStatus);
            if (row == null)
                return NotFound(new { success = false, message = "Grant not found." });

            if (!AdminAuthorizationPolicies.IsAdminPortalUser(User))
            {
                var owns = await _context.PatientUserMaps.AnyAsync(m =>
                    m.UserId == userId && m.PatientId == row.PatientId && !m.DeleteStatus);
                if (!owns && row.CaregiverUserId != userId)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "Access denied." });
            }

            if (row.RevokedAt == null)
                row.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            var dto = (await ToDtoListAsync(new[] { row }))[0];
            return Ok(new { success = true, message = "Revoked.", data = dto });
        }

        /// <summary>Grants where I am the patient (owner).</summary>
        [HttpGet("ListMine")]
        public async Task<IActionResult> ListMine()
        {
            var userId = (long)User.GetUserId();
            var myPatientIds = await _context.PatientUserMaps
                .Where(m => m.UserId == userId && !m.DeleteStatus)
                .Select(m => m.PatientId)
                .ToListAsync();

            var rows = await _context.CaregiverAuthorizations
                .Where(c => myPatientIds.Contains(c.PatientId) && !c.DeleteStatus)
                .OrderByDescending(c => c.GrantedAt)
                .ToListAsync();

            return Ok(new { success = true, data = await ToDtoListAsync(rows) });
        }

        /// <summary>Patients I may act for as caregiver.</summary>
        [HttpGet("ListActingFor")]
        public async Task<IActionResult> ListActingFor()
        {
            var userId = (long)User.GetUserId();
            var rows = await _context.CaregiverAuthorizations
                .Where(c => c.CaregiverUserId == userId && !c.DeleteStatus)
                .OrderByDescending(c => c.GrantedAt)
                .ToListAsync();

            return Ok(new { success = true, data = await ToDtoListAsync(rows) });
        }

        private async Task<IActionResult?> VerifyGrantOtpAsync(CaregiverGrantRequest request)
        {
            if (request.OtpChallengeId <= 0 || string.IsNullOrWhiteSpace(request.OtpCode))
                return BadRequest(new { success = false, message = "OTP is required to grant caregiver access. Request OTP first (Action=GrantCaregiver)." });

            var challenge = await _context.OtpChallenges
                .FirstOrDefaultAsync(c => c.OtpChallengeId == request.OtpChallengeId);
            if (challenge == null)
                return NotFound(new { success = false, message = "OTP challenge not found." });

            if (!string.Equals(challenge.Action, GrantOtpAction, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(challenge.EntityType, "Patient", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(challenge.EntityId, request.PatientId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "OTP does not match this caregiver grant." });
            }

            if (challenge.LockedUntil.HasValue && challenge.LockedUntil > DateTime.UtcNow)
                return StatusCode(StatusCodes.Status423Locked, new { success = false, message = "OTP locked due to too many attempts." });

            if (challenge.VerifiedAt != null || challenge.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { success = false, message = "OTP expired or already used." });

            challenge.AttemptCount++;
            var ok = string.Equals(
                SecurityTokenHash.Sha256Hex(request.OtpCode.Trim()),
                challenge.OtpHash,
                StringComparison.OrdinalIgnoreCase);

            _context.OtpAuditLogs.Add(new OtpAuditLog
            {
                Action = "VerifyOtp",
                EntityType = challenge.EntityType,
                EntityId = challenge.EntityId,
                ToMasked = challenge.DestinationMasked,
                Success = ok,
                At = DateTime.UtcNow,
                ActorUserId = User.GetUserId()
            });

            if (!ok)
            {
                if (challenge.AttemptCount >= 5)
                    challenge.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                return BadRequest(new { success = false, message = "Invalid OTP." });
            }

            challenge.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        private async Task<UserMaster?> FindUserByContactAsync(string? contact)
        {
            var q = contact?.Trim();
            if (string.IsNullOrWhiteSpace(q))
                return null;

            var lower = q.ToLower();
            var digits = new string(q.Where(char.IsDigit).ToArray());

            var matches = await _context.UserMasters.AsNoTracking()
                .Where(u => !u.DeleteStatus)
                .Where(u =>
                    (u.EmailId != null && u.EmailId.ToLower() == lower)
                    || (u.UserName != null && u.UserName.ToLower() == lower)
                    || (u.MobileNo != null && u.MobileNo == q)
                    || (digits.Length >= 10 && u.MobileNo != null && u.MobileNo == digits))
                .ToListAsync();

            return matches.FirstOrDefault();
        }

        private async Task<List<CaregiverAuthorizationDto>> ToDtoListAsync(IEnumerable<CaregiverAuthorization> rows)
        {
            var list = rows.ToList();
            if (list.Count == 0)
                return new List<CaregiverAuthorizationDto>();

            var patientIds = list.Select(r => r.PatientId).Distinct().ToList();
            var userIds = list.Select(r => r.CaregiverUserId).Distinct().ToList();

            var patients = await _context.Patients.AsNoTracking()
                .Where(p => patientIds.Contains(p.PatientId))
                .ToDictionaryAsync(p => p.PatientId);
            var users = await _context.UserMasters.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId);

            return list.Select(c =>
            {
                patients.TryGetValue(c.PatientId, out var patient);
                users.TryGetValue(c.CaregiverUserId, out var user);
                return new CaregiverAuthorizationDto
                {
                    CaregiverAuthorizationId = c.CaregiverAuthorizationId,
                    PatientId = c.PatientId,
                    PatientName = patient?.PatientName,
                    CaregiverUserId = c.CaregiverUserId,
                    CaregiverName = user == null ? null : DisplayName(user),
                    CaregiverContact = FirstContact(user),
                    GrantedAt = c.GrantedAt,
                    RevokedAt = c.RevokedAt,
                    Scope = c.Scope,
                    IsActive = c.RevokedAt == null && !c.DeleteStatus
                };
            }).ToList();
        }

        private static CaregiverLookupDto ToLookup(UserMaster user) => new()
        {
            CaregiverUserId = user.UserId,
            DisplayName = DisplayName(user),
            Contact = FirstContact(user)
        };

        private static string DisplayName(UserMaster user)
        {
            var name = string.Join(" ", new[] { user.FirstName, user.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            return string.IsNullOrWhiteSpace(name) ? user.UserName : name;
        }

        private static string? FirstContact(UserMaster? user)
        {
            if (user == null)
                return null;
            if (!string.IsNullOrWhiteSpace(user.EmailId))
                return user.EmailId;
            if (!string.IsNullOrWhiteSpace(user.MobileNo))
                return user.MobileNo;
            return user.UserName;
        }
    }
}

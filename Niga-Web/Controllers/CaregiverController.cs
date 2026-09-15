using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Authorization;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Master;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// M16 CON-02.02 — Caregiver grant / revoke / list + booking authorisation (mobile-reusable).
    /// Reuses ConsentType Caregiver via ConsentRecord (SEC-06) — does not fork a second consent store.
    /// </summary>
    [Route("api/Caregiver")]
    [ApiController]
    [Authorize]
    public class CaregiverController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public CaregiverController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpPost("Grant")]
        public async Task<IActionResult> Grant([FromBody] CaregiverGrantRequest request)
        {
            if (request == null || request.PatientId <= 0 || request.CaregiverUserId <= 0)
                return BadRequest(new { success = false, message = "PatientId and CaregiverUserId are required." });

            var userId = (long)User.GetUserId();
            if (!AdminAuthorizationPolicies.IsAdminPortalUser(User))
            {
                var owns = await _context.PatientUserMaps.AnyAsync(m =>
                    m.UserId == userId && m.PatientId == request.PatientId && !m.DeleteStatus);
                if (!owns)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "Only the patient account linked to this PatientId may grant caregiver access." });
            }

            var active = await _context.CaregiverAuthorizations.FirstOrDefaultAsync(c =>
                c.PatientId == request.PatientId
                && c.CaregiverUserId == request.CaregiverUserId
                && !c.DeleteStatus
                && c.RevokedAt == null);
            if (active != null)
                return Ok(new { success = true, message = "Already granted.", data = ToDto(active) });

            var row = new CaregiverAuthorization
            {
                PatientId = request.PatientId,
                CaregiverUserId = request.CaregiverUserId,
                GrantedByUserId = userId,
                GrantedAt = DateTime.UtcNow,
                Scope = string.IsNullOrWhiteSpace(request.Scope) ? "booking" : request.Scope.Trim(),
                DeleteStatus = false
            };
            _context.CaregiverAuthorizations.Add(row);

            // SEC-06 reuse — Caregiver consent type+time only (no clinical notes)
            var caregiverType = await _context.ConsentTypes
                .FirstOrDefaultAsync(t => t.Code == "Caregiver" && t.IsActive);
            if (caregiverType != null)
            {
                _context.ConsentRecords.Add(new ConsentRecord
                {
                    ConsentTypeId = caregiverType.ConsentTypeId,
                    SubjectType = "Patient",
                    SubjectId = request.PatientId,
                    GrantedByUserId = userId,
                    GrantedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    Notes = $"CaregiverUserId={request.CaregiverUserId}"
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = ToDto(row) });
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
            return Ok(new { success = true, message = "Revoked.", data = ToDto(row) });
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

            return Ok(new { success = true, data = rows.Select(ToDto).ToList() });
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

            return Ok(new { success = true, data = rows.Select(ToDto).ToList() });
        }

        private static CaregiverAuthorizationDto ToDto(CaregiverAuthorization c) => new()
        {
            CaregiverAuthorizationId = c.CaregiverAuthorizationId,
            PatientId = c.PatientId,
            CaregiverUserId = c.CaregiverUserId,
            GrantedAt = c.GrantedAt,
            RevokedAt = c.RevokedAt,
            Scope = c.Scope,
            IsActive = c.RevokedAt == null && !c.DeleteStatus
        };
    }
}

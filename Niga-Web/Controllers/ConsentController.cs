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
    /// <summary>SEC-06.02 — Consent grant / withdraw / list / admin audit (no clinical content).</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConsentController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public ConsentController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpPost("Grant")]
        public async Task<IActionResult> Grant([FromBody] ConsentGrantRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ConsentTypeCode))
                return BadRequest(new { success = false, message = "ConsentTypeCode is required." });

            var type = await _context.ConsentTypes
                .FirstOrDefaultAsync(t => t.Code == request.ConsentTypeCode.Trim() && t.IsActive);
            if (type == null)
                return BadRequest(new { success = false, message = "Unknown or inactive consent type." });

            var subjectType = string.IsNullOrWhiteSpace(request.SubjectType) ? "User" : request.SubjectType.Trim();
            var subjectId = request.SubjectId > 0 ? request.SubjectId : User.GetUserId();

            if (!User.IsAdminPortalUser()
                && subjectType.Equals("User", StringComparison.OrdinalIgnoreCase)
                && subjectId != User.GetUserId())
            {
                return Forbid();
            }

            var record = new ConsentRecord
            {
                ConsentTypeId = type.ConsentTypeId,
                SubjectType = subjectType,
                SubjectId = subjectId,
                GrantedByUserId = User.GetUserId(),
                GrantedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                Notes = request.Notes
            };

            _context.ConsentRecords.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    record.ConsentRecordId,
                    ConsentTypeCode = type.Code,
                    record.SubjectType,
                    record.SubjectId,
                    record.GrantedAt
                }
            });
        }

        [HttpPost("Withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] ConsentWithdrawRequest request)
        {
            if (request == null || request.ConsentRecordId <= 0)
                return BadRequest(new { success = false, message = "ConsentRecordId is required." });

            var record = await _context.ConsentRecords
                .FirstOrDefaultAsync(r => r.ConsentRecordId == request.ConsentRecordId);
            if (record == null)
                return NotFound(new { success = false, message = "Consent record not found." });

            if (!User.IsAdminPortalUser()
                && !(record.SubjectType.Equals("User", StringComparison.OrdinalIgnoreCase)
                     && record.SubjectId == User.GetUserId()))
            {
                return Forbid();
            }

            if (record.WithdrawnAt != null)
                return Ok(new { success = true, message = "Already withdrawn." });

            record.WithdrawnAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Consent withdrawn." });
        }

        [HttpGet("ListMine")]
        public async Task<IActionResult> ListMine()
        {
            var userId = User.GetUserId();
            var rows = await _context.ConsentRecords
                .AsNoTracking()
                .Include(r => r.ConsentType)
                .Where(r => r.SubjectType == "User" && r.SubjectId == userId)
                .OrderByDescending(r => r.GrantedAt)
                .Select(r => new
                {
                    r.ConsentRecordId,
                    ConsentTypeCode = r.ConsentType!.Code,
                    ConsentTypeName = r.ConsentType.Name,
                    r.GrantedAt,
                    r.WithdrawnAt,
                    IsActive = r.WithdrawnAt == null
                })
                .ToListAsync();

            return Ok(new { success = true, data = rows });
        }

        [HttpGet("AdminAudit")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> AdminAudit([FromQuery] int take = 100)
        {
            take = Math.Clamp(take, 1, 500);
            var rows = await _context.ConsentRecords
                .AsNoTracking()
                .Include(r => r.ConsentType)
                .OrderByDescending(r => r.GrantedAt)
                .Take(take)
                .Select(r => new
                {
                    r.ConsentRecordId,
                    ConsentTypeCode = r.ConsentType!.Code,
                    r.SubjectType,
                    r.SubjectId,
                    r.GrantedByUserId,
                    r.GrantedAt,
                    r.WithdrawnAt,
                    r.IpAddress
                })
                .ToListAsync();

            return Ok(new { success = true, data = rows });
        }
    }
}

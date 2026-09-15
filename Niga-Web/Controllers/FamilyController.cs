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
    /// M16 CON-01.02 — Family CRUD under one patient account (mobile-reusable).
    /// Member rows are real Patient records sharing PatientId with appointments.
    /// </summary>
    [Route("api/Family")]
    [ApiController]
    [Authorize]
    public class FamilyController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public FamilyController(NIGACentrumContext context)
        {
            _context = context;
        }

        /// <summary>Link JWT user to primary clinical PatientId (once per account).</summary>
        [HttpPost("LinkPrimary")]
        public async Task<IActionResult> LinkPrimary([FromBody] LinkPrimaryPatientRequest request)
        {
            if (request == null || request.PatientId <= 0)
                return BadRequest(new { success = false, message = "PatientId is required." });

            var userId = (long)User.GetUserId();
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientId == request.PatientId && p.DeleteStatus != true);
            if (patient == null)
                return NotFound(new { success = false, message = "Patient not found." });

            var existing = await _context.PatientUserMaps
                .FirstOrDefaultAsync(m => m.UserId == userId && m.IsPrimary && !m.DeleteStatus);
            if (existing != null && existing.PatientId != request.PatientId)
                return BadRequest(new { success = false, message = "Primary patient already linked for this user." });

            if (existing == null)
            {
                _context.PatientUserMaps.Add(new PatientUserMap
                {
                    UserId = userId,
                    PatientId = request.PatientId,
                    IsPrimary = true,
                    DeleteStatus = false,
                    EnteredBy = userId.ToString(),
                    EnteredDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, data = new { userId, patientId = request.PatientId } });
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = (long)User.GetUserId();
            var rows = await (
                from f in _context.PatientFamilyMembers
                join p in _context.Patients on f.MemberPatientId equals p.PatientId
                where f.OwnerUserId == userId && !f.DeleteStatus && p.DeleteStatus != true
                orderby f.FamilyMemberId
                select new FamilyMemberDto
                {
                    FamilyMemberId = f.FamilyMemberId,
                    OwnerPatientId = f.OwnerPatientId,
                    MemberPatientId = f.MemberPatientId,
                    Relation = f.Relation,
                    PatientName = p.PatientName,
                    MobileNo = p.MobileNo,
                    Email = p.Email
                }).ToListAsync();

            return Ok(new { success = true, data = rows });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FamilyMemberCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Relation))
                return BadRequest(new { success = false, message = "Relation is required." });
            if (request.OwnerPatientId <= 0)
                return BadRequest(new { success = false, message = "OwnerPatientId is required." });

            var userId = (long)User.GetUserId();
            if (!AdminAuthorizationPolicies.IsAdminPortalUser(User))
            {
                var linked = await EnsureOwnerMapAsync(userId, request.OwnerPatientId);
                if (linked == null)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "OwnerPatientId is not linked to this user. Call POST /api/Family/LinkPrimary first." });
            }

            int memberPatientId;
            if (request.ExistingMemberPatientId.HasValue && request.ExistingMemberPatientId.Value > 0)
            {
                memberPatientId = request.ExistingMemberPatientId.Value;
                var exists = await _context.Patients.AnyAsync(p =>
                    p.PatientId == memberPatientId && p.DeleteStatus != true);
                if (!exists)
                    return NotFound(new { success = false, message = "Existing member PatientId not found." });
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.PatientName))
                    return BadRequest(new { success = false, message = "PatientName is required when creating a member." });

                var member = new Patient
                {
                    PatientName = request.PatientName.Trim(),
                    MobileNo = request.MobileNo,
                    Email = request.Email,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Age = request.Age,
                    DeleteStatus = false,
                    EnteredBy = userId.ToString(),
                    EnteredDate = DateTime.UtcNow
                };
                _context.Patients.Add(member);
                await _context.SaveChangesAsync();
                memberPatientId = member.PatientId;
            }

            var row = new PatientFamilyMember
            {
                OwnerUserId = userId,
                OwnerPatientId = request.OwnerPatientId,
                MemberPatientId = memberPatientId,
                Relation = request.Relation.Trim(),
                DeleteStatus = false,
                EnteredBy = userId.ToString(),
                EnteredDate = DateTime.UtcNow
            };
            _context.PatientFamilyMembers.Add(row);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new FamilyMemberDto
                {
                    FamilyMemberId = row.FamilyMemberId,
                    OwnerPatientId = row.OwnerPatientId,
                    MemberPatientId = row.MemberPatientId,
                    Relation = row.Relation,
                    PatientName = request.PatientName,
                    MobileNo = request.MobileNo,
                    Email = request.Email
                }
            });
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] FamilyMemberUpdateRequest request)
        {
            var userId = (long)User.GetUserId();
            var row = await _context.PatientFamilyMembers
                .FirstOrDefaultAsync(f => f.FamilyMemberId == id && !f.DeleteStatus);
            if (row == null)
                return NotFound(new { success = false, message = "Family member not found." });

            if (!AdminAuthorizationPolicies.IsAdminPortalUser(User) && row.OwnerUserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Access denied." });

            if (!string.IsNullOrWhiteSpace(request?.Relation))
                row.Relation = request.Relation.Trim();

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == row.MemberPatientId);
            if (patient != null && request != null)
            {
                if (!string.IsNullOrWhiteSpace(request.PatientName))
                    patient.PatientName = request.PatientName.Trim();
                if (request.MobileNo != null)
                    patient.MobileNo = request.MobileNo;
                if (request.Email != null)
                    patient.Email = request.Email;
                if (request.DateOfBirth.HasValue)
                    patient.DateOfBirth = request.DateOfBirth;
                if (request.Gender.HasValue)
                    patient.Gender = request.Gender;
                if (request.Age.HasValue)
                    patient.Age = request.Age;
                patient.ChangedBy = userId.ToString();
                patient.ChangedDate = DateTime.UtcNow;
            }

            row.ChangedBy = userId.ToString();
            row.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Updated." });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> SoftDelete(long id)
        {
            var userId = (long)User.GetUserId();
            var row = await _context.PatientFamilyMembers
                .FirstOrDefaultAsync(f => f.FamilyMemberId == id && !f.DeleteStatus);
            if (row == null)
                return NotFound(new { success = false, message = "Family member not found." });

            if (!AdminAuthorizationPolicies.IsAdminPortalUser(User) && row.OwnerUserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Access denied." });

            row.DeleteStatus = true;
            row.ChangedBy = userId.ToString();
            row.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted." });
        }

        /// <summary>
        /// Book-as-member authorisation check used by clients before appointment create.
        /// Returns 200 when JWT user owns the family link (or is Admin / active caregiver).
        /// </summary>
        [HttpGet("CanBookAs/{patientId:int}")]
        public async Task<IActionResult> CanBookAs(int patientId)
        {
            var userId = (long)User.GetUserId();
            if (AdminAuthorizationPolicies.IsAdminPortalUser(User))
                return Ok(new { success = true, allowed = true, reason = "admin" });

            var asOwner = await _context.PatientUserMaps.AnyAsync(m =>
                m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus);
            if (asOwner)
                return Ok(new { success = true, allowed = true, reason = "self" });

            var asFamily = await _context.PatientFamilyMembers.AnyAsync(f =>
                f.OwnerUserId == userId && f.MemberPatientId == patientId && !f.DeleteStatus);
            if (asFamily)
                return Ok(new { success = true, allowed = true, reason = "family" });

            var asCaregiver = await _context.CaregiverAuthorizations.AnyAsync(c =>
                c.CaregiverUserId == userId
                && c.PatientId == patientId
                && !c.DeleteStatus
                && c.RevokedAt == null);
            if (asCaregiver)
                return Ok(new { success = true, allowed = true, reason = "caregiver" });

            return StatusCode(StatusCodes.Status403Forbidden,
                new { success = false, allowed = false, message = "Not authorised to book as this patient." });
        }

        private async Task<PatientUserMap?> EnsureOwnerMapAsync(long userId, int ownerPatientId)
        {
            var map = await _context.PatientUserMaps
                .FirstOrDefaultAsync(m => m.UserId == userId && !m.DeleteStatus && m.IsPrimary);
            if (map == null)
                return null;
            return map.PatientId == ownerPatientId ? map : null;
        }
    }
}

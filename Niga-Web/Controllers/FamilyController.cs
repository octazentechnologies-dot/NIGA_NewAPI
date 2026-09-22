using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Authorization;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Enums;
using Niga_Domain.Extensions;
using Niga_Domain.Master;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// M16 CON-01.02 — Family CRUD under one patient account (mobile-reusable).
    /// Owner PatientId is taken from the logged-in user (PatientUserMap), not typed in the UI.
    /// </summary>
    [Route("api/Family")]
    [ApiController]
    [Authorize]
    [ForbidMoneyRoles]
    public class FamilyController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public FamilyController(NIGACentrumContext context)
        {
            _context = context;
        }

        /// <summary>Primary clinical Patient for this login. Caregivers resolve to the patient they act for.</summary>
        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userId = (long)User.GetUserId();
                if (userId <= 0)
                    return Unauthorized(new { success = false, message = "Not signed in." });

                var owner = await EnsurePrimaryPatientAsync(userId);
                if (owner == null)
                    return BadRequest(new { success = false, message = "Could not resolve a patient record for this login." });

                return Ok(new
                {
                    success = true,
                    data = new FamilyOwnerDto
                    {
                        OwnerPatientId = owner.PatientId,
                        OwnerPatientName = owner.PatientName,
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

        [HttpGet("Relations")]
        public async Task<IActionResult> ListRelations()
        {
            var rows = await _context.FamilyRelationMasters.AsNoTracking()
                .Where(r => !r.DeleteStatus)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.RelationName)
                .Select(r => new FamilyRelationDto
                {
                    RelationId = r.RelationId,
                    RelationName = r.RelationName,
                    SortOrder = r.SortOrder
                })
                .ToListAsync();

            return Ok(new { success = true, data = rows });
        }

        [HttpPost("Relations")]
        public async Task<IActionResult> AddRelation([FromBody] FamilyRelationCreateRequest request)
        {
            var name = request?.RelationName?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { success = false, message = "Relation name is required." });
            if (name.Length > 50)
                return BadRequest(new { success = false, message = "Relation name must be 50 characters or less." });

            var userId = (long)User.GetUserId();
            var row = await UpsertRelationAsync(name, userId);
            return Ok(new
            {
                success = true,
                data = new FamilyRelationDto
                {
                    RelationId = row.RelationId,
                    RelationName = row.RelationName,
                    SortOrder = row.SortOrder
                }
            });
        }

        /// <summary>Link JWT user to primary clinical PatientId (once per account). Optional — Me/Create auto-link.</summary>
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
            try
            {
                var userId = (long)User.GetUserId();
                if (userId <= 0)
                    return Unauthorized(new { success = false, message = "Not signed in." });

                var owner = await EnsurePrimaryPatientAsync(userId);
                var ownerPatientId = owner?.PatientId ?? 0;
                var rows = ownerPatientId <= 0
                    ? new List<FamilyMemberDto>()
                    : await (
                        from f in _context.PatientFamilyMembers
                        join p in _context.Patients on f.MemberPatientId equals p.PatientId
                        where f.OwnerPatientId == ownerPatientId && !f.DeleteStatus && p.DeleteStatus != true
                        orderby f.FamilyMemberId
                        select new FamilyMemberDto
                        {
                            FamilyMemberId = f.FamilyMemberId,
                            OwnerPatientId = f.OwnerPatientId,
                            MemberPatientId = f.MemberPatientId,
                            RelationId = f.RelationId,
                            Relation = f.Relation,
                            PatientName = p.PatientName,
                            MobileNo = p.MobileNo,
                            Email = p.Email
                        }).ToListAsync();

                return Ok(new
                {
                    success = true,
                    ownerPatientId = owner?.PatientId,
                    ownerPatientName = owner?.PatientName,
                    isActingAsCaregiver = owner?.IsActingAsCaregiver ?? false,
                    data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FamilyMemberCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Body is required." });

            var userId = (long)User.GetUserId();
            if (userId <= 0)
                return Unauthorized(new { success = false, message = "Not signed in." });

            var relation = await ResolveRelationAsync(request.RelationId, request.Relation, userId);
            if (relation == null)
                return BadRequest(new { success = false, message = "Relation is required." });

            int ownerPatientId;
            long ownerUserId = userId;
            if (AdminAuthorizationPolicies.IsAdminPortalUser(User)
                && request.OwnerPatientId.HasValue
                && request.OwnerPatientId.Value > 0)
            {
                ownerPatientId = request.OwnerPatientId.Value;
            }
            else
            {
                var owner = await EnsurePrimaryPatientAsync(userId);
                if (owner == null)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "No patient record is linked to this login." });
                ownerPatientId = owner.PatientId;
                ownerUserId = owner.OwnerUserId;
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
                OwnerUserId = ownerUserId,
                OwnerPatientId = ownerPatientId,
                MemberPatientId = memberPatientId,
                RelationId = relation.Value.RelationId,
                Relation = relation.Value.RelationName,
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
                    RelationId = row.RelationId,
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

            if (!await CanManageFamilyMemberAsync(userId, row))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Access denied." });

            var relation = await ResolveRelationAsync(request?.RelationId, request?.Relation, userId);
            if (relation != null)
            {
                row.RelationId = relation.Value.RelationId;
                row.Relation = relation.Value.RelationName;
            }

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

            if (!await CanManageFamilyMemberAsync(userId, row))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Access denied." });

            row.DeleteStatus = true;
            row.ChangedBy = userId.ToString();
            row.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Deleted." });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var userId = (long)User.GetUserId();
            var row = await (
                from f in _context.PatientFamilyMembers
                join p in _context.Patients on f.MemberPatientId equals p.PatientId
                where f.FamilyMemberId == id && !f.DeleteStatus && p.DeleteStatus != true
                select new { f, p }).FirstOrDefaultAsync();
            if (row == null)
                return NotFound(new { success = false, message = "Family member not found." });

            if (!await CanManageFamilyMemberAsync(userId, row.f))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Access denied." });

            return Ok(new
            {
                success = true,
                data = new FamilyMemberDto
                {
                    FamilyMemberId = row.f.FamilyMemberId,
                    OwnerPatientId = row.f.OwnerPatientId,
                    MemberPatientId = row.f.MemberPatientId,
                    RelationId = row.f.RelationId,
                    Relation = row.f.Relation,
                    PatientName = row.p.PatientName,
                    MobileNo = row.p.MobileNo,
                    Email = row.p.Email
                }
            });
        }

        /// <summary>
        /// Book-as-member authorisation check used by clients before appointment create.
        /// Returns 200 when JWT user owns the family link (or is Admin / active caregiver).
        /// </summary>
        [HttpGet("CanBookAs/{patientId:int}")]
        public async Task<IActionResult> CanBookAs(int patientId)
        {
            var userId = (long)User.GetUserId();
            var allowed = await TryBookAsReasonAsync(userId, patientId);
            if (allowed == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, allowed = false, message = "Not authorised to book as this patient." });
            }

            return Ok(new { success = true, allowed = true, reason = allowed });
        }

        /// <summary>CON-01.02 — Create an appointment for a family member (or self / active caregiver patient).</summary>
        [HttpPost("BookAs")]
        public async Task<IActionResult> BookAs([FromBody] FamilyBookAsRequest request)
        {
            if (request == null || request.MemberPatientId <= 0 || request.DoctorId <= 0)
                return BadRequest(new { success = false, message = "MemberPatientId and DoctorId are required." });
            if (!TimeOnly.TryParse(request.AppointmentTime, out var time))
                return BadRequest(new { success = false, message = "AppointmentTime must be HH:mm." });

            var userId = (long)User.GetUserId();
            var reason = await TryBookAsReasonAsync(userId, request.MemberPatientId);
            if (reason == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Not authorised to book as this patient." });
            }

            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == request.DoctorId && !d.DeleteStatus);
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor not found." });

            var member = await _context.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatientId == request.MemberPatientId && p.DeleteStatus != true);
            if (member == null)
                return NotFound(new { success = false, message = "Family member patient not found." });

            var day = request.AppointmentDate.Date;
            var clash = await _context.PatientAppointments.AnyAsync(a =>
                a.DoctorId == request.DoctorId
                && a.DeleteStatus != true
                && a.AppointmentDate.HasValue
                && a.AppointmentDate.Value.Date == day
                && a.AppointmentTime == time);
            if (clash)
                return Conflict(new { success = false, message = "That slot is already booked." });

            var appt = new PatientAppointment
            {
                PatientId = request.MemberPatientId,
                DoctorId = request.DoctorId,
                UserId = doctor.UserId ?? 0,
                AppointmentDate = day,
                AppointmentTime = time,
                Status = PatientsStatus.NotArrived.GetDisplayName(),
                DeleteStatus = false,
                BookingToken = Guid.NewGuid().ToString("N"),
                VisitType = string.IsNullOrWhiteSpace(request.VisitType) ? "InClinic" : request.VisitType.Trim(),
                ConsultMode = string.IsNullOrWhiteSpace(request.ConsultMode) ? "InClinic" : request.ConsultMode.Trim(),
                IsTele = request.IsTele,
                PaymentStatus = "PENDING",
                HoldExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };
            _context.PatientAppointments.Add(appt);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                reason,
                data = new
                {
                    patientAppId = appt.PatientAppId,
                    patientId = appt.PatientId,
                    patientName = member.PatientName,
                    doctorId = appt.DoctorId,
                    appointmentDate = appt.AppointmentDate,
                    appointmentTime = appt.AppointmentTime.ToString(),
                    bookingToken = appt.BookingToken,
                    status = appt.Status
                }
            });
        }

        private async Task<string?> TryBookAsReasonAsync(long userId, int patientId)
        {
            if (AdminAuthorizationPolicies.IsAdminPortalUser(User))
                return "admin";

            var asOwner = await _context.PatientUserMaps.AnyAsync(m =>
                m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus);
            if (asOwner)
                return "self";

            var asFamily = await _context.PatientFamilyMembers.AnyAsync(f =>
                f.OwnerUserId == userId && f.MemberPatientId == patientId && !f.DeleteStatus);
            if (asFamily)
                return "family";

            var asCaregiver = await _context.CaregiverAuthorizations.AnyAsync(c =>
                c.CaregiverUserId == userId
                && c.PatientId == patientId
                && !c.DeleteStatus
                && c.RevokedAt == null);
            if (asCaregiver)
                return "caregiver";

            return null;
        }

        private Task<PatientPortalOwner?> EnsurePrimaryPatientAsync(long userId)
            => PatientPortalOwnerResolver.ResolveAsync(_context, userId, preferActingFor: true, createIfMissing: true);

        private async Task<bool> CanManageFamilyMemberAsync(long userId, PatientFamilyMember row)
        {
            if (AdminAuthorizationPolicies.IsAdminPortalUser(User))
                return true;
            if (row.OwnerUserId == userId)
                return true;
            return await _context.CaregiverAuthorizations.AnyAsync(c =>
                c.CaregiverUserId == userId
                && c.PatientId == row.OwnerPatientId
                && !c.DeleteStatus
                && c.RevokedAt == null);
        }

        private async Task<(int RelationId, string RelationName)?> ResolveRelationAsync(
            int? relationId,
            string? relationName,
            long userId)
        {
            if (relationId.HasValue && relationId.Value > 0)
            {
                var byId = await _context.FamilyRelationMasters
                    .FirstOrDefaultAsync(r => r.RelationId == relationId.Value && !r.DeleteStatus);
                if (byId != null)
                    return (byId.RelationId, byId.RelationName);
            }

            var name = relationName?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                var row = await UpsertRelationAsync(name, userId);
                return (row.RelationId, row.RelationName);
            }

            return null;
        }

        private async Task<FamilyRelationMaster> UpsertRelationAsync(string name, long userId)
        {
            var trimmed = name.Trim();
            var existing = await _context.FamilyRelationMasters
                .FirstOrDefaultAsync(r => !r.DeleteStatus && r.RelationName.ToLower() == trimmed.ToLower());
            if (existing != null)
                return existing;

            var maxSort = await _context.FamilyRelationMasters
                .Where(r => !r.DeleteStatus)
                .Select(r => (int?)r.SortOrder)
                .MaxAsync() ?? 0;

            var row = new FamilyRelationMaster
            {
                RelationName = trimmed,
                SortOrder = maxSort + 10,
                DeleteStatus = false,
                EnteredBy = userId.ToString(),
                EnteredDate = DateTime.UtcNow
            };
            _context.FamilyRelationMasters.Add(row);
            await _context.SaveChangesAsync();
            return row;
        }
    }
}

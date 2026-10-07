using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>PAT-08 / PAT-09 — patient home dashboard + care categories (mobile-reusable).</summary>
    [Route("api/PatientPortal")]
    [ApiController]
    [Authorize]
    public class PatientPortalController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public PatientPortalController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("Home")]
        public async Task<IActionResult> Home()
        {
            var userId = (long)User.GetUserId();
            if (userId <= 0)
                return Unauthorized(new { success = false, message = "Not signed in." });

            var owner = await PatientPortalOwnerResolver.ResolveAsync(
                _context, userId, preferActingFor: true, createIfMissing: false);

            var dto = new PatientHomeDashboardDto();
            if (owner == null)
                return Ok(new { success = true, data = dto });

            dto.PatientId = owner.PatientId;
            dto.PatientName = owner.PatientName;
            dto.FamilyCount = await _context.PatientFamilyMembers.CountAsync(f =>
                f.OwnerPatientId == owner.PatientId && !f.DeleteStatus);
            dto.CaregiverCount = await _context.CaregiverAuthorizations.CountAsync(c =>
                c.PatientId == owner.PatientId && !c.DeleteStatus && c.RevokedAt == null);

            var upcoming = await (
                from a in _context.PatientAppointments.AsNoTracking()
                join p in _context.Patients.AsNoTracking() on a.PatientId equals p.PatientId
                join d in _context.Doctors.AsNoTracking() on a.DoctorId equals d.DoctorId into doctors
                from d in doctors.DefaultIfEmpty()
                where a.PatientId == owner.PatientId && a.DeleteStatus != true && a.AppointmentDate >= DateTime.Today
                orderby a.AppointmentDate, a.AppointmentTime
                select new PatientAppointmentModel
                {
                    PatientAppId = a.PatientAppId,
                    PatientId = a.PatientId,
                    PatientName = p.PatientName,
                    MobileNo = p.MobileNo,
                    Email = p.Email,
                    AppointmentDate = a.AppointmentDate,
                    AppointmentTime = a.AppointmentTime,
                    Status = a.Status ?? string.Empty,
                    DeleteStatus = a.DeleteStatus,
                    DoctorId = a.DoctorId,
                    DoctorName = d != null ? ((d.FirstName ?? "") + " " + (d.LastName ?? "")).Trim() : null,
                    UserId = a.UserId,
                    CaseId = _context.CaseEntryDetails
                        .Where(c => c.PatientId == a.PatientId && c.DeleteStatus != true)
                        .OrderByDescending(c => c.CaseId)
                        .Select(c => c.CaseId)
                        .FirstOrDefault(),
                    Address = p.AddressLine1 ?? p.Address,
                    Age = p.Age,
                    Gender = p.Gender,
                    DateOfBirth = p.DateOfBirth,
                    IsWhatsAppOptIn = p.IsWhatsAppOptIn,
                    WhatsAppOptInDate = p.WhatsAppOptInDate,
                    VisitType = a.VisitType,
                    ConsultMode = a.ConsultMode,
                    PaymentStatus = a.PaymentStatus,
                    IsTele = a.IsTele,
                    BookingToken = a.BookingToken,
                    BookingChannel = a.BookingChannel,
                    PaymentMethod = a.PaymentMethod,
                    PayAtClinicAllowed = a.PayAtClinicAllowed,
                    CancelReasonCode = a.CancelReasonCode,
                    CalledAt = a.CalledAt,
                    QueuePosition = a.QueuePosition
                })
                .Take(5)
                .ToListAsync();
            dto.UpcomingAppointments = upcoming.Count;
            dto.NextAppointments = upcoming;
            return Ok(new { success = true, data = dto });
        }

        [HttpGet("CareCategories")]
        [AllowAnonymous]
        public async Task<IActionResult> CareCategories()
        {
            var rows = await _context.HumanSystemMasters.AsNoTracking()
                .OrderBy(h => h.HumanSystemName)
                .Select(h => new CareCategoryDto
                {
                    Id = h.HumanSystemId,
                    Name = h.HumanSystemName ?? string.Empty,
                    Description = h.Description
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }
    }
}

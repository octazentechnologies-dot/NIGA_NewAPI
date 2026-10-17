using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Repositories;
using Homeocentrum.Niga.API.Hosting;

namespace Homeocentrum.Niga.API.Controllers.Mobile;

/// <summary>
/// Endpoints only the Patient Mobile App calls. Shared patient endpoints (profile, family, booking, tele, records)
/// stay in their own controllers; see the "mobile-patient" Swagger document for the full app surface.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
[ApiAudience(ApiAudience.MobilePatient)]
public class MobilePatientController : ControllerBase
{
    private readonly NIGACentrumContext _context;
    private readonly IS4Week4Service _s4;
    private readonly IS3Week3Service _s3;
    private readonly MobileClinicQueryService _clinic;
    private readonly ILogger<MobilePatientController> _logger;

    public MobilePatientController(
        NIGACentrumContext context,
        IS4Week4Service s4,
        IS3Week3Service s3,
        MobileClinicQueryService clinic,
        ILogger<MobilePatientController> logger)
    {
        _context = context;
        _s4 = s4;
        _s3 = s3;
        _clinic = clinic;
        _logger = logger;
    }

    /// <summary>PAT-02.02 — welcome and introduction slides.</summary>
    [HttpGet("Welcome")]
    [LegacyRoute("/api/Welcome/Patient")]
    [AllowAnonymous]
    public async Task<IActionResult> Welcome()
    {
        List<object> slides;
        try
        {
            var rows = await _context.WelcomeSlides
                .AsNoTracking()
                .Where(s => s.IsActive && s.Audience == "Patient")
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
            slides = rows
                .Select(s => (object)new { s.WelcomeSlideId, s.SortOrder, s.Title, s.Body, s.Version })
                .ToList();
        }
        catch
        {
            slides = new List<object>();
        }

        if (slides.Count == 0)
        {
            slides = new List<object>
            {
                new { welcomeSlideId = 1L, sortOrder = 1, title = "Welcome to Homeocentrum", body = "Your homeopathy care in one place — appointments, family, and prescriptions.", version = "1" },
                new { welcomeSlideId = 2L, sortOrder = 2, title = "Family first", body = "Add family members and book for them with one account.", version = "1" },
                new { welcomeSlideId = 3L, sortOrder = 3, title = "Your privacy", body = "We ask for consent before sharing or recording. You can withdraw anytime.", version = "1" },
            };
        }

        return Ok(new { success = true, data = new { version = "1", audience = "Patient", slides } });
    }

    /// <summary>PAT-08.02 — home dashboard: family and caregiver counts and the next five appointments.</summary>
    [HttpGet("Home")]
    [LegacyRoute("/api/PatientPortal/Home")]
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

    /// <summary>PAT-20.02 — appointment detail (doctor, payment, tele, eRx, allowed actions).</summary>
    [HttpGet("Visits/{patientAppId:int}")]
    [LegacyRoute("/api/Patient/Visits/{patientAppId:int}")]
    public Task<IActionResult> VisitDetail(int patientAppId)
        => this.Respond(_s4.PatientVisitDetailAsync(patientAppId, this.S4Caller(_context)), _logger);

    /// <summary>
    /// PAT-23.02 — poll waitlist offer status (JOINED / OFFERED).
    /// Query: contactMobile and/or patientId; optional doctorId filter.
    /// </summary>
    [HttpGet("Waitlist/Offers")]
    [LegacyRoute("/api/Waitlist/Offers")]
    [AllowAnonymous]
    public async Task<IActionResult> WaitlistOffers(
        [FromQuery] string? contactMobile,
        [FromQuery] int? doctorId,
        [FromQuery] int? patientId)
    {
        var result = await _s3.GetWaitlistOffersAsync(contactMobile, doctorId, patientId);
        return StatusCode(result.StatusCode, result.Body);
    }

    [HttpPost("/api/Patient/Visits/{patientAppId:int}/CheckIn")]
    public Task<IActionResult> CheckIn(
        int patientAppId,
        [FromQuery] string? status,
        [FromBody] CheckInBody? body)
        => Done(_clinic.CheckInAsync(patientAppId, status ?? body?.Status, Caller()));

    [HttpGet("/api/Patient/Reminders")]
    public Task<IActionResult> Reminders() => Done(_clinic.ListRemindersAsync(Caller()));

    [HttpPost("/api/Patient/Reminders")]
    public Task<IActionResult> SaveReminder([FromBody] ReminderWrite request)
        => Done(_clinic.SaveReminderAsync(request, Caller()));

    [HttpPut("/api/Patient/Reminders/{id:int}")]
    public Task<IActionResult> UpdateReminder(int id, [FromBody] ReminderWrite request)
        => Done(_clinic.UpdateReminderAsync(id, request, Caller()));

    [HttpDelete("/api/Patient/Reminders/{id:int}")]
    public Task<IActionResult> DeleteReminder(int id) => Done(_clinic.DeleteReminderAsync(id, Caller()));

    [HttpPost("/api/Patient/AppointmentRequests")]
    public Task<IActionResult> CreateAppointmentRequest([FromBody] AppointmentRequestWrite request)
    {
        Request.Headers.TryGetValue("Idempotency-Key", out var key);
        return Done(_clinic.CreateAppointmentRequestAsync(request, Caller(), key.ToString()));
    }

    [HttpGet("/api/Patient/AppointmentRequests")]
    public Task<IActionResult> ListAppointmentRequests(
        [FromQuery] string? status,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
        => Done(_clinic.ListPatientAppointmentRequestsAsync(Caller(), status, pageNumber, pageSize));

    [HttpDelete("/api/Patient/AppointmentRequests/{id:int}")]
    public Task<IActionResult> CancelAppointmentRequest(int id)
        => Done(_clinic.CancelPatientAppointmentRequestAsync(id, Caller()));

    [HttpPost("/api/Patient/AppointmentRequests/{id:int}/Respond")]
    public Task<IActionResult> RespondAppointmentRequest(int id, [FromBody] AppointmentRequestRespondBody? body)
        => Done(_clinic.RespondPatientAppointmentRequestAsync(id, body?.Action, Caller()));

    // B-023 — DeliveryAddresses is canonical; Addresses is the alias the patient app names.
    [HttpGet("/api/Patient/DeliveryAddresses")]
    [HttpGet("/api/Patient/Addresses")]
    public Task<IActionResult> ListDeliveryAddresses()
        => Done(_clinic.ListDeliveryAddressesAsync(Caller()));

    [HttpPost("/api/Patient/DeliveryAddresses")]
    [HttpPost("/api/Patient/Addresses")]
    public Task<IActionResult> CreateDeliveryAddress([FromBody] DeliveryAddressWrite request)
        => Done(_clinic.SaveDeliveryAddressAsync(null, request, Caller()));

    [HttpPut("/api/Patient/DeliveryAddresses/{id:int}")]
    [HttpPut("/api/Patient/Addresses/{id:int}")]
    public Task<IActionResult> UpdateDeliveryAddress(int id, [FromBody] DeliveryAddressWrite request)
        => Done(_clinic.SaveDeliveryAddressAsync(id, request, Caller()));

    [HttpPost("/api/Patient/Waitlist/Offers/{id:int}/Accept")]
    public Task<IActionResult> AcceptWaitlistOffer(int id)
        => Done(_clinic.AcceptWaitlistOfferAsync(id, Caller()));

    [HttpDelete("/api/Patient/DeliveryAddresses/{id:int}")]
    [HttpDelete("/api/Patient/Addresses/{id:int}")]
    public Task<IActionResult> DeleteDeliveryAddress(int id)
        => Done(_clinic.DeleteDeliveryAddressAsync(id, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

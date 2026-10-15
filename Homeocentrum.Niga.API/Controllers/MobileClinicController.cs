using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Extra clinic routes used by the patient and doctor mobile apps.
/// Amounts are stored fees or payment rows. Missing profile data is null.
/// </summary>
[ApiController]
[Authorize]
public class MobileClinicController : ControllerBase
{
    private readonly MobileClinicQueryService _clinic;
    private readonly NIGACentrumContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MobileClinicController> _logger;

    public MobileClinicController(
        MobileClinicQueryService clinic,
        NIGACentrumContext context,
        IConfiguration configuration,
        ILogger<MobileClinicController> logger)
    {
        _clinic = clinic;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("/api/Public/AppConfig")]
    public IActionResult AppConfig()
    {
        return Ok(new
        {
            success = true,
            data = new
            {
                razorpayKeyId = Blank(_configuration["Razorpay:KeyId"]),
                siteUrl = Blank(_configuration["ConfigurationModel:SiteUrl"]),
                supportPhone = Blank(_configuration["Support:Phone"]),
                supportWhatsApp = Blank(_configuration["Support:WhatsApp"])
            }
        });
    }

    [HttpGet("/api/Doctor/FollowUps")]
    public Task<IActionResult> DoctorFollowUps() => Done(_clinic.DoctorFollowUpsAsync(Caller()));

    [HttpGet("/api/Doctor/PracticeStats")]
    public Task<IActionResult> PracticeStats() => Done(_clinic.PracticeStatsAsync(Caller()));

    [HttpGet("/api/PatientAppointment/GetAppointmentsByDateRange")]
    public Task<IActionResult> AppointmentsByDateRange([FromQuery] DateTime from, [FromQuery] DateTime to)
        => Done(_clinic.AppointmentsByDateRangeAsync(from, to, Caller()));

    [HttpPost("/api/Patient/Visits/{patientAppId:int}/CheckIn")]
    public Task<IActionResult> CheckIn(int patientAppId) => Done(_clinic.CheckInAsync(patientAppId, Caller()));

    [HttpPost("/api/PatientAppointment/SwitchToTele")]
    public Task<IActionResult> SwitchToTele([FromQuery] int patientAppId) => Done(_clinic.SwitchToTeleAsync(patientAppId, Caller()));

    [HttpPost("/api/Tele/Sessions/{teleSessionId:int}/RunningLate")]
    public Task<IActionResult> RunningLate(int teleSessionId, [FromQuery] int? minutes)
        => Done(_clinic.RunningLateAsync(teleSessionId, minutes, Caller()));

    [HttpGet("/api/Notifications/Preferences")]
    public Task<IActionResult> GetPreferences() => Done(_clinic.GetPreferencesAsync(Caller()));

    [HttpPut("/api/Notifications/Preferences")]
    public Task<IActionResult> SavePreferences([FromBody] PreferenceWrite request)
        => Done(_clinic.SavePreferencesAsync(request, Caller()));

    [HttpPost("/api/Notifications/MarkAllRead")]
    public Task<IActionResult> MarkAllRead() => Done(_clinic.MarkAllReadAsync(Caller()));

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

    [HttpGet("/api/Doctor/TimeOff")]
    public Task<IActionResult> TimeOff() => Done(_clinic.ListTimeOffAsync(Caller()));

    [HttpPost("/api/Doctor/TimeOff")]
    public Task<IActionResult> SaveTimeOff([FromBody] TimeOffWrite request)
        => Done(_clinic.SaveTimeOffAsync(request, Caller()));

    [HttpDelete("/api/Doctor/TimeOff/{id:int}")]
    public Task<IActionResult> DeleteTimeOff(int id) => Done(_clinic.DeleteTimeOffAsync(id, Caller()));

    [HttpGet("/api/Doctor/AppointmentRequests")]
    public Task<IActionResult> AppointmentRequests() => Done(_clinic.ListAppointmentRequestsAsync(Caller()));

    [HttpPost("/api/Patient/AppointmentRequests")]
    public Task<IActionResult> CreateAppointmentRequest([FromBody] AppointmentRequestWrite request)
        => Done(_clinic.CreateAppointmentRequestAsync(request, Caller()));

    [HttpPost("/api/Doctor/AppointmentRequests/{id:int}/Accept")]
    public Task<IActionResult> AcceptRequest(int id) => Done(_clinic.AcceptAppointmentRequestAsync(id, Caller()));

    [HttpPost("/api/Doctor/AppointmentRequests/{id:int}/Decline")]
    public Task<IActionResult> DeclineRequest(int id, [FromBody] ReasonBody body)
        => Done(_clinic.DeclineAppointmentRequestAsync(id, body?.Reason, Caller()));

    [HttpPost("/api/Doctor/AppointmentRequests/{id:int}/Propose")]
    public Task<IActionResult> ProposeRequest(int id, [FromBody] ProposeBody body)
        => Done(_clinic.ProposeAppointmentRequestAsync(id, body?.ProposedDate, body?.ProposedTime, Caller()));

    [HttpGet("/api/Doctor/CareLinks")]
    public Task<IActionResult> CareLinks() => Done(_clinic.ListCareLinksAsync(Caller()));

    [HttpPost("/api/Doctor/CareLinks")]
    public Task<IActionResult> CreateCareLink([FromBody] CareLinkWrite request)
        => Done(_clinic.CreateCareLinkAsync(request, Caller()));

    [HttpPost("/api/Doctor/CareLinks/{id:int}/Respond")]
    public Task<IActionResult> RespondCareLink(int id, [FromBody] StatusBody body)
        => Done(_clinic.RespondCareLinkAsync(id, body?.Status, Caller()));

    [HttpGet("/api/Doctor/PharmacyQueries")]
    public Task<IActionResult> PharmacyQueries() => Done(_clinic.ListPharmacyQueriesAsync(Caller()));

    [HttpPost("/api/Doctor/PharmacyQueries")]
    public Task<IActionResult> CreatePharmacyQuery([FromBody] PharmacyQueryWrite request)
        => Done(_clinic.CreatePharmacyQueryAsync(request, Caller()));

    [HttpPost("/api/Doctor/PharmacyQueries/{id:int}/Reply")]
    public Task<IActionResult> ReplyPharmacyQuery(int id, [FromBody] ReplyBody body)
        => Done(_clinic.ReplyPharmacyQueryAsync(id, body?.Reply, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public class ReasonBody
    {
        public string? Reason { get; set; }
    }

    public class ProposeBody
    {
        public DateTime? ProposedDate { get; set; }
        public string? ProposedTime { get; set; }
    }

    public class StatusBody
    {
        public string? Status { get; set; }
    }

    public class ReplyBody
    {
        public string? Reply { get; set; }
    }
}

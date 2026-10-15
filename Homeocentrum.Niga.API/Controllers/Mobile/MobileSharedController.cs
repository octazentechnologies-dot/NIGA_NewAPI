using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Repositories;
using Homeocentrum.Niga.API.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeocentrum.Niga.API.Controllers.Mobile;

/// <summary>
/// Routes used by both mobile apps. Amounts and phones come from config or stored rows.
/// Support phone and WhatsApp stay null until Support:Phone and Support:WhatsApp are set.
/// </summary>
[ApiController]
[Authorize]
[ApiAudience(ApiAudience.Common)]
public class MobileSharedController : ControllerBase
{
    private readonly MobileClinicQueryService _clinic;
    private readonly NIGACentrumContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MobileSharedController> _logger;

    public MobileSharedController(
        MobileClinicQueryService clinic,
        NIGACentrumContext context,
        IConfiguration configuration,
        ILogger<MobileSharedController> logger)
    {
        _clinic = clinic;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("/api/Public/AppConfig")]
    [HttpGet("/api/App/Config")]
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
                supportWhatsApp = Blank(_configuration["Support:WhatsApp"]),
                supportEmail = Blank(_configuration["Support:Email"])
            }
        });
    }

    [HttpGet("/api/Notifications/Preferences")]
    public Task<IActionResult> GetPreferences() => Done(_clinic.GetPreferencesAsync(Caller()));

    [HttpPut("/api/Notifications/Preferences")]
    public Task<IActionResult> SavePreferences([FromBody] PreferenceWrite request)
        => Done(_clinic.SavePreferencesAsync(request, Caller()));

    [HttpPost("/api/Notifications/MarkAllRead")]
    public Task<IActionResult> MarkAllRead() => Done(_clinic.MarkAllReadAsync(Caller()));

    [HttpPost("/api/Tele/Sessions/{teleSessionId:int}/RunningLate")]
    public Task<IActionResult> RunningLate(
        int teleSessionId,
        [FromQuery] int? minutes,
        [FromBody] LateBody? body)
        => Done(_clinic.RunningLateAsync(teleSessionId, minutes ?? body?.Minutes, Caller()));

    [HttpPost("/api/PatientAppointment/SwitchToTele")]
    public Task<IActionResult> SwitchToTele(
        [FromQuery] int patientAppId = 0,
        [FromBody] SwitchTeleBody? body = null)
        => Done(_clinic.SwitchToTeleAsync(patientAppId > 0 ? patientAppId : body?.PatientAppId ?? 0, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// SMS templates, sending, history, delivery events, and doctor SMS preferences.
/// Provider keys stay in appsettings.json and are never returned.
/// </summary>
[ApiController]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<SmsController> _logger;

    public SmsController(IS5Week5Service s5, NIGACentrumContext context, ILogger<SmsController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Sms/Templates")]
    public Task<IActionResult> Templates() => Done(_s5.ListSmsTemplatesAsync(Caller()));

    [HttpPost("/api/Sms/Templates")]
    public Task<IActionResult> SaveTemplate([FromBody] SmsTemplateWrite request) => Done(_s5.SaveSmsTemplateAsync(request, Caller()));

    [HttpPost("/api/Sms/Send")]
    public Task<IActionResult> SendSms([FromBody] SmsSendRequest request) => Done(_s5.SendSmsAsync(request, Caller()));

    [HttpGet("/api/Sms/History")]
    public Task<IActionResult> SmsHistory() => Done(_s5.SmsHistoryAsync(Caller()));

    [HttpGet("/api/Sms/Events")]
    public Task<IActionResult> SmsEvents() => Done(_s5.ListSmsEventsAsync(Caller()));

    [HttpPut("/api/Sms/Preferences")]
    public Task<IActionResult> SmsPreference([FromBody] DoctorSmsPreferenceWrite request)
        => Done(_s5.SaveSmsPreferenceAsync(request, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Doctor reminders.
/// </summary>
[ApiController]
[Authorize]
public class DoctorRemindersController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<DoctorRemindersController> _logger;

    public DoctorRemindersController(IS5Week5Service s5, NIGACentrumContext context, ILogger<DoctorRemindersController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Doctor/Reminders")]
    public Task<IActionResult> Reminders([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.ListRemindersAsync(from, to, Caller()));

    [HttpPost("/api/Doctor/Reminders")]
    public Task<IActionResult> CreateReminder([FromBody] DoctorReminderWrite request)
        => Done(_s5.SaveReminderAsync(null, request, Caller()));

    [HttpPut("/api/Doctor/Reminders/{id:int}")]
    public Task<IActionResult> UpdateReminder(int id, [FromBody] DoctorReminderWrite request)
        => Done(_s5.SaveReminderAsync(id, request, Caller()));

    [HttpDelete("/api/Doctor/Reminders/{id:int}")]
    public Task<IActionResult> DeleteReminder(int id) => Done(_s5.DeleteReminderAsync(id, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

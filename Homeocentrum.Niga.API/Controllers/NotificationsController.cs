using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// In-app notifications and push device registration.
/// </summary>
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IS5Week5Service s5, NIGACentrumContext context, ILogger<NotificationsController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpPost("/api/Devices/Register")]
    public Task<IActionResult> RegisterDevice([FromBody] DeviceRegisterRequest request)
        => Done(_s5.RegisterDeviceAsync(request, Caller()));

    [HttpPost("/api/Notifications/Send")]
    public Task<IActionResult> SendNote([FromBody] NotificationSendRequest request) => Done(_s5.SendNotificationAsync(request, Caller()));

    [HttpGet("/api/Notifications")]
    public Task<IActionResult> Notes() => Done(_s5.ListNotificationsAsync(Caller()));

    [HttpPatch("/api/Notifications/{id:long}")]
    public Task<IActionResult> PatchNote(long id, [FromQuery] bool isRead = true) => Done(_s5.PatchNotificationAsync(id, isRead, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

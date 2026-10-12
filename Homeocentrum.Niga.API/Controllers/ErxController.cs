using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Electronic prescriptions: potencies, remedy lines, signing, history, and the prescription PDF.
/// </summary>
[ApiController]
[Authorize]
public class ErxController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<ErxController> _logger;

    public ErxController(IS4Week4Service s4, NIGACentrumContext context, ILogger<ErxController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Erx/Potencies")]
    public Task<IActionResult> Potencies() => Done(_s4.ListPotenciesAsync());

    [HttpPut("/api/Erx/RemedyLines/{id:int}")]
    public Task<IActionResult> RemedyLine(int id, [FromBody] RemedyLineUpdateRequest request)
        => Done(_s4.UpdateRemedyLineAsync(id, request, Caller()));

    [HttpGet("/api/Erx/ByAppointment/{id:int}")]
    public Task<IActionResult> ErxByAppointment(int id) => Done(_s4.GetErxByAppointmentAsync(id, Caller(), false));

    [HttpPost("/api/Erx/Sign")]
    public Task<IActionResult> Sign([FromBody] SignErxRequest request) => Done(_s4.SignErxAsync(request, Caller()));

    [HttpGet("/api/Erx/History")]
    public Task<IActionResult> History([FromQuery] int? patientId, [FromQuery] int? patientAppId)
        => Done(_s4.ErxHistoryAsync(patientId, patientAppId, Caller()));

    [HttpGet("/api/Erx/Patient/{id:int}")]
    public Task<IActionResult> PatientErx(int id) => Done(_s4.GetErxByAppointmentAsync(id, Caller(), true));

    [HttpGet("/api/Erx/{id:int}/Pdf")]
    public Task<IActionResult> ErxPdf(int id) => Done(_s4.ErxPdfAsync(id, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Patient records: timeline, visits, consultation notes, documents, follow-ups, symptom diary, and progress.
/// </summary>
[ApiController]
[Authorize]
public class PatientRecordsController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<PatientRecordsController> _logger;

    public PatientRecordsController(IS4Week4Service s4, NIGACentrumContext context, ILogger<PatientRecordsController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Patient/Timeline")]
    public Task<IActionResult> Timeline([FromQuery] int? patientId) => Done(_s4.TimelineAsync(patientId, Caller()));

    [HttpGet("/api/Patient/Visits")]
    public Task<IActionResult> Visits([FromQuery] int? patientId) => Done(_s4.PatientVisitsAsync(patientId, Caller()));

    [HttpGet("/api/Patient/Consultations/{patientAppId:int}/Note")]
    public Task<IActionResult> Note(int patientAppId) => Done(_s4.ConsultationNoteAsync(patientAppId, Caller()));

    [HttpPost("/api/Patient/Documents")]
    [RequestSizeLimit(11_000_000)]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> Document(IFormFile file) => Done(_s4.SavePatientDocumentAsync(file, Caller()));

    [HttpGet("/api/Patient/Documents")]
    public Task<IActionResult> Documents([FromQuery] int? patientId) => Done(_s4.ListPatientDocumentsAsync(patientId, Caller()));

    [HttpGet("/api/Patient/Documents/{id:long}")]
    public Task<IActionResult> DocumentFile(long id) => Done(_s4.PatientDocumentFileAsync(id, Caller()));

    [HttpDelete("/api/Patient/Documents/{id:long}")]
    public Task<IActionResult> DeleteDocument(long id) => Done(_s4.DeletePatientDocumentAsync(id, Caller()));

    [HttpPost("/api/Patient/FollowUps")]
    public Task<IActionResult> FollowUp([FromBody] FollowUpCreateRequest request) => Done(_s4.SetFollowUpAsync(request, Caller()));

    [HttpGet("/api/Patient/FollowUps")]
    public Task<IActionResult> FollowUps([FromQuery] int? patientId) => Done(_s4.ListFollowUpsAsync(patientId, Caller()));

    [HttpPost("/api/Patient/FollowUps/{taskId:int}/Complete")]
    public Task<IActionResult> Complete(int taskId) => Done(_s4.CompleteFollowUpAsync(taskId, Caller()));

    [HttpPost("/api/Patient/Diary")]
    public Task<IActionResult> Diary([FromBody] DiaryWriteRequest request) => Done(_s4.SaveDiaryAsync(request, Caller()));

    [HttpGet("/api/Patient/Diary")]
    public Task<IActionResult> DiaryList([FromQuery] int? patientId) => Done(_s4.ListDiaryAsync(patientId, Caller()));

    [HttpPut("/api/Patient/Diary/{id:int}")]
    public Task<IActionResult> DiaryUpdate(int id, [FromBody] DiaryWriteRequest request) => Done(_s4.UpdateDiaryAsync(id, request, Caller()));

    [HttpDelete("/api/Patient/Diary/{id:int}")]
    public Task<IActionResult> DiaryDelete(int id) => Done(_s4.DeleteDiaryAsync(id, Caller()));

    [HttpGet("/api/Patient/Progress")]
    public Task<IActionResult> Progress([FromQuery] int? patientId) => Done(_s4.ProgressAsync(patientId, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

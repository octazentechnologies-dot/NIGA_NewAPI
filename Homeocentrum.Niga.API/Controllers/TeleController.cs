using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Tele consultation: doctor online status, tele queue and day board, sessions, video tokens, recording consent, in-room chat, consultation summary, and instant consult.
/// </summary>
[ApiController]
[Authorize]
public class TeleController : ControllerBase
{
    private readonly IS3Week3Service _s3;

    public TeleController(IS3Week3Service s3)
    {
        _s3 = s3;
    }

    [HttpPost("/api/Tele/Availability")]
    public Task<IActionResult> SetAvailability([FromBody] TeleAvailabilityUpdate request)
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.SetTeleAvailabilityAsync(doctorId.Id, request?.IsOnline ?? false));
    }

    [HttpGet("/api/Tele/Availability")]
    public Task<IActionResult> Availability()
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.GetTeleAvailabilityAsync(doctorId.Id));
    }

    [AllowAnonymous]
    [HttpGet("/api/Tele/Availability/{doctorId:int}")]
    public Task<IActionResult> PublicAvailability(int doctorId)
        => Done(_s3.GetTeleAvailabilityAsync(doctorId));

    [AllowAnonymous]
    [HttpGet("/api/Tele/DeviceCheck")]
    public IActionResult DeviceCheck()
        => Ok(new
        {
            // PAT-28.02 — client-side device probe checklist before live video
            success = true,
            camera = "client",
            microphone = "client",
            speaker = "client",
            connection = "client",
            vendor = "stub"
        });

    /// <summary>
    /// TEL-02.02 / TEL-02.04 — tele waiting queue (E-CONSULT + payment + wait minutes).
    /// Clients poll this URL on an interval. SignalR is not used in S3 (see API DOC).
    /// </summary>
    [HttpGet("/api/Tele/Queue")]
    public Task<IActionResult> TeleQueue()
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.GetTeleQueueAsync(doctorId.Id));
    }

    /// <summary>Doctor web tele board — all tele appointments for a day (default today).</summary>
    [HttpGet("/api/Tele/Day")]
    public Task<IActionResult> TeleDay([FromQuery] DateTime? date)
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.GetTeleDayAsync(doctorId.Id, date));
    }

    /// <summary>
    /// TEL-02.02 — open a Waiting tele session for a queue appointment (own doctor only).
    /// </summary>
    [HttpPost("/api/Tele/Sessions")]
    public Task<IActionResult> CreateSession([FromBody] TeleSessionStart request)
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.CreateSessionAsync(request?.PatientAppId ?? 0, doctorId.Id));
    }

    /// <summary>
    /// TEL-02.02 — start (activate) an existing tele session from Waiting → Active.
    /// </summary>
    [HttpPost("/api/Tele/Sessions/{sessionId:int}/Start")]
    public Task<IActionResult> StartSession(int sessionId)
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.StartSessionAsync(sessionId, doctorId.Id));
    }

    /// <summary>
    /// TEL-03.02 — end tele session (owning doctor). Sets Status=Ended.
    /// </summary>
    [HttpPost("/api/Tele/Sessions/{sessionId:int}/End")]
    public Task<IActionResult> EndSession(int sessionId)
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.EndSessionAsync(sessionId, doctorId.Id));
    }

    /// <summary>
    /// TEL-03.02 / TEL-04.01 / PAT-28.02 — vendor join token for live video (stub).
    /// Same URL + JSON for web and mobile. No clientType / platform body fields.
    /// Doctor, mapped patient, or admin JWT. TTL 60 minutes.
    /// </summary>
    [HttpPost("/api/Tele/Sessions/{sessionId:int}/Token")]
    public Task<IActionResult> Token(int sessionId)
        => Done(_s3.IssueTokenAsync(sessionId, Caller(), rejoin: false));

    /// <summary>
    /// TEL-03.02 / TEL-04.01 / TEL-08.01 / PAT-28.02 — rejoin token while Status=Active (dropped call).
    /// Same client-agnostic shape as Token.
    /// </summary>
    [HttpPost("/api/Tele/Sessions/{sessionId:int}/Rejoin")]
    public Task<IActionResult> Rejoin(int sessionId)
        => Done(_s3.IssueTokenAsync(sessionId, Caller(), rejoin: true));

    /// <summary>
    /// TEL-06.02 — waiting-room status poll. Patient JWT (mapped to the visit) or owning doctor/admin.
    /// Client polls until data.status becomes Active, then calls Token.
    /// </summary>
    [HttpGet("/api/Tele/Sessions/{sessionId:int}")]
    public Task<IActionResult> Session(int sessionId)
        => Done(_s3.GetSessionAsync(sessionId, Caller()));

    /// <summary>
    /// TEL-07.02 — capture recording consent (doctor or patient on the session).
    /// Sets TeleSession.RecordAllowed only when both sides have Accepted=true.
    /// </summary>
    [HttpPost("/api/Tele/Consent")]
    public Task<IActionResult> Consent([FromBody] TeleConsentRequest request)
        => Done(_s3.CaptureConsentAsync(request, Caller()));

    /// <summary>
    /// TEL-09.01 / PAT-30.02 — report join failure; returns retry/rejoin/supportPath for fallback UI.
    /// Codes: DEVICE_DENIED, NETWORK_ERROR, TOKEN_FAILED, VENDOR_ERROR, BROWSER_UNSUPPORTED, JOIN_FAILED.
    /// </summary>
    [HttpPost("/api/Tele/Sessions/{sessionId:int}/JoinFailure")]
    public Task<IActionResult> JoinFailure(int sessionId, [FromBody] JoinFailureBody? body)
        => Done(_s3.LogJoinFailureAsync(sessionId, body?.Code, Caller()));

    /// <summary>
    /// TEL-10.02 — post in-room chat. Doctor and mapped patient only (not reception).
    /// </summary>
    [HttpPost("/api/Tele/Chat")]
    public Task<IActionResult> Chat([FromBody] TeleChatRequest request)
        => Done(_s3.PostChatAsync(request, Caller()));

    /// <summary>
    /// TEL-10.02 — list chat for a tele session. Doctor and mapped patient only.
    /// </summary>
    [HttpGet("/api/Tele/Chat/{sessionId:int}")]
    public Task<IActionResult> ChatList(int sessionId)
        => Done(_s3.ListChatAsync(sessionId, Caller()));

    /// <summary>
    /// TEL-11.02 — doctor issues post-call consultation summary (PUT). Treating doctor only.
    /// </summary>
    [HttpPut("/api/Tele/Summary")]
    public Task<IActionResult> SaveSummary([FromBody] ConsultationSummaryRequest request)
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.SaveSummaryAsync(request, doctorId.Id));
    }

    /// <summary>
    /// TEL-11.02 — patient (or treating doctor/admin) reads consultation summary for an appointment.
    /// </summary>
    [HttpGet("/api/Tele/Summary/{patientAppId:int}")]
    public Task<IActionResult> Summary(int patientAppId)
        => Done(_s3.GetSummaryAsync(patientAppId, Caller()));

    /// <summary>
    /// TEL-12.02 — patient requests instant consult (queue position + match/offer or NO_DOCTOR).
    /// </summary>
    [HttpPost("/api/Tele/Instant")]
    public Task<IActionResult> Instant([FromBody] InstantConsultRequestBody request)
        => Done(_s3.RequestInstantAsync(request, Caller()));

    /// <summary>
    /// PAT-25.02 — patient polls queue position and doctor offer for an instant request.
    /// Patient (linked patient or same mobile), the offered doctor, or admin.
    /// </summary>
    [HttpGet("/api/Tele/Instant/{requestId:int}")]
    public Task<IActionResult> InstantStatus(int requestId)
        => Done(_s3.GetInstantStatusAsync(requestId, Caller()));

    /// <summary>PAT-25.02 — patient cancels an instant request before a doctor accepts.</summary>
    [HttpPost("/api/Tele/Instant/{requestId:int}/Cancel")]
    public Task<IActionResult> CancelInstant(int requestId)
        => Done(_s3.CancelInstantAsync(requestId, Caller()));

    /// <summary>TEL-12.02 — doctor lists open instant offers for self.</summary>
    [HttpGet("/api/Tele/Instant/Offers")]
    public Task<IActionResult> Offers()
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.ListInstantOffersAsync(doctorId.Id));
    }

    /// <summary>TEL-12.02 — doctor accepts an OFFERED instant request.</summary>
    [HttpPost("/api/Tele/Instant/{requestId:int}/Accept")]
    public Task<IActionResult> AcceptInstant(int requestId)
    {
        var doctorId = RequireSelfDoctor();
        if (doctorId.Error != null)
            return Task.FromResult(doctorId.Error);
        return Done(_s3.AcceptInstantAsync(requestId, doctorId.Id));
    }

    private Task<IActionResult> Done(Task<S3ActionResult> work) => this.Respond(work);
    private S3Caller Caller() => this.S3Caller();
    private (int Id, IActionResult? Error) RequireSelfDoctor() => ServiceResultSupport.RequireSelfDoctor(this);
    private IActionResult ForbidRole(string message) => ServiceResultSupport.ForbidRole(this, message);
}

public class TeleSessionStart
{
    public int PatientAppId { get; set; }
}

public class JoinFailureBody
{
    public string? Code { get; set; }
}

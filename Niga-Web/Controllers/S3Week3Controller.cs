using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Interfaces;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// S3 Week 3: waitlist, reception profile, case paper, tele stub, support, help, assisted booking.
    /// No SMS, WhatsApp, or Razorpay.
    /// </summary>
    [ApiController]
    [Authorize]
    public class S3Week3Controller : ControllerBase
    {
        private readonly IS3Week3Service _s3;

        public S3Week3Controller(IS3Week3Service s3)
        {
            _s3 = s3;
        }

        /// <summary>
        /// PAT-23.02 — join waitlist (anonymous). Does not reserve a slot; offer comes later as OFFERED.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("/api/Waitlist/Join")]
        public Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
            => Done(_s3.JoinWaitlistAsync(request));

        /// <summary>
        /// PAT-23.02 — patient poll for waitlist offer status (JOINED / OFFERED).
        /// Query: contactMobile and/or patientId; optional doctorId filter.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/api/Waitlist/Offers")]
        public Task<IActionResult> WaitlistOffers(
            [FromQuery] string? contactMobile,
            [FromQuery] int? doctorId,
            [FromQuery] int? patientId)
            => Done(_s3.GetWaitlistOffersAsync(contactMobile, doctorId, patientId));

        [HttpGet("/api/Waitlist")]
        public Task<IActionResult> Waitlist([FromQuery] int doctorId)
        {
            var deny = RequireDoctor(doctorId);
            return deny != null ? Task.FromResult(deny) : Done(_s3.GetWaitlistAsync(doctorId));
        }

        [HttpGet("/api/Reception/Profile")]
        public Task<IActionResult> ReceptionProfile()
        {
            if (!IsReception())
                return Task.FromResult<IActionResult>(ForbidRole("Reception profile is for reception staff."));
            return Done(_s3.GetReceptionProfileAsync(User.GetUserId()));
        }

        [HttpPut("/api/Reception/Profile")]
        public Task<IActionResult> UpdateReceptionProfile([FromBody] ReceptionProfileUpdate request)
        {
            if (!IsReception())
                return Task.FromResult<IActionResult>(ForbidRole("Reception profile is for reception staff."));
            return Done(_s3.UpdateReceptionProfileAsync(User.GetUserId(), request));
        }

        /// <summary>
        /// REC-12.02 — POST case-paper subset of SaveComplaints (CaseEntryChiefComplaint + CreatedByRole=Reception).
        /// </summary>
        [HttpPost("/api/Reception/CasePaper")]
        public Task<IActionResult> SaveCasePaper([FromBody] CasePaperRequest request)
        {
            if (!IsReception())
                return Task.FromResult<IActionResult>(ForbidRole("Only reception can log the pre-consult case paper."));
            var doctorId = DoctorOwnership.GetDoctorId(User);
            if (!doctorId.HasValue)
                return Task.FromResult<IActionResult>(ForbidRole("Doctor context is required."));
            return Done(_s3.SaveCasePaperAsync(request, doctorId.Value, User.GetUserId()));
        }

        /// <summary>
        /// REC-12.02 — GET chief complaints for board / reception (same complaints table as SaveComplaints).
        /// </summary>
        [HttpGet("/api/Reception/CasePaper")]
        public Task<IActionResult> CasePapers([FromQuery] int patientId)
        {
            var doctorId = DoctorOwnership.GetDoctorId(User);
            var deny = DoctorOwnership.ForbidIfNotOwner(User, doctorId);
            if (doctorId == null || deny != null)
                return Task.FromResult<IActionResult>(deny ?? ForbidRole("Doctor context is required."));
            return Done(_s3.GetCasePapersAsync(doctorId.Value, patientId));
        }

        /// <summary>REC-05.02 — reception row open. Destination is case paper or appointment, never repertory.</summary>
        [HttpGet("/api/Reception/PatientOpen")]
        public Task<IActionResult> PatientOpen([FromQuery] int patientId)
        {
            if (!IsReception())
                return Task.FromResult<IActionResult>(ForbidRole("Only reception can open a patient row this way."));
            var doctorId = DoctorOwnership.GetDoctorId(User);
            if (!doctorId.HasValue)
                return Task.FromResult<IActionResult>(ForbidRole("Doctor context is required."));
            return Done(_s3.OpenPatientRowAsync(doctorId.Value, patientId));
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
            => Done(_s3.RequestInstantAsync(request));

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

        /// <summary>SUP-01.02 — patient (or signed-in user) creates a support ticket.</summary>
        [HttpPost("/api/Support/Tickets")]
        public Task<IActionResult> CreateTicket([FromBody] SupportTicketCreate request)
            => Done(_s3.CreateTicketAsync(request, User.GetUserId(), DoctorOwnership.GetRoleName(User) ?? "Patient"));

        /// <summary>SUP-01.02 — list tickets the caller opened (patient Mine).</summary>
        [HttpGet("/api/Support/Tickets/Mine")]
        public Task<IActionResult> MyTickets()
            => Done(_s3.ListMyTicketsAsync(User.GetUserId()));

        /// <summary>
        /// SUP-03.02 — admin issue queue (optional status / priority filters). Admin only.
        /// </summary>
        [HttpGet("/api/Support/Tickets")]
        public Task<IActionResult> AdminTickets([FromQuery] string? status, [FromQuery] string? priority)
        {
            if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                return Task.FromResult<IActionResult>(ForbidRole("Admin ticket queue is for admin."));
            return Done(_s3.ListAdminTicketsAsync(status, priority));
        }

        /// <summary>
        /// SUP-03.02 — admin assign / set status / set priority. Admin only.
        /// </summary>
        [HttpPut("/api/Support/Tickets/{ticketId:int}")]
        public Task<IActionResult> UpdateTicket(int ticketId, [FromBody] SupportTicketUpdate request)
        {
            if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                return Task.FromResult<IActionResult>(ForbidRole("Admin ticket queue is for admin."));
            return Done(_s3.UpdateTicketAsync(ticketId, request));
        }

        /// <summary>SUP-04.01 — create message (+ optional attachment via fileName).</summary>
        [HttpPost("/api/Support/Tickets/{ticketId:int}/Messages")]
        public Task<IActionResult> AddMessage(int ticketId, [FromBody] SupportMessageCreate request)
            => Done(_s3.AddMessageAsync(ticketId, request, User.GetUserId(), DoctorOwnership.GetRoleName(User) ?? "", DoctorOwnership.IsGlobalAdminPortalUser(User)));

        /// <summary>SUP-04.01 — list messages + attachments (reporter or admin).</summary>
        [HttpGet("/api/Support/Tickets/{ticketId:int}/Messages")]
        public Task<IActionResult> Messages(int ticketId)
            => Done(_s3.ListMessagesAsync(ticketId, User.GetUserId(), DoctorOwnership.IsGlobalAdminPortalUser(User)));

        /// <summary>SUP-04.01 — update message body (author or admin).</summary>
        [HttpPut("/api/Support/Tickets/{ticketId:int}/Messages/{messageId:int}")]
        public Task<IActionResult> UpdateMessage(int ticketId, int messageId, [FromBody] SupportMessageCreate request)
            => Done(_s3.UpdateMessageAsync(ticketId, messageId, request, User.GetUserId(), DoctorOwnership.IsGlobalAdminPortalUser(User)));

        /// <summary>SUP-04.01 — delete message + linked attachments (author or admin).</summary>
        [HttpDelete("/api/Support/Tickets/{ticketId:int}/Messages/{messageId:int}")]
        public Task<IActionResult> DeleteMessage(int ticketId, int messageId)
            => Done(_s3.DeleteMessageAsync(ticketId, messageId, User.GetUserId(), DoctorOwnership.IsGlobalAdminPortalUser(User)));

        /// <summary>
        /// SUP-06.02 — public GET published help articles (no token). Unpublished rows are omitted.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/api/Help")]
        public Task<IActionResult> Help()
            => Done(_s3.ListHelpAsync(includeUnpublished: false));

        /// <summary>
        /// SUP-06.02 — public GET one published article by slug. Unpublished / unknown → 404.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("/api/Help/{slug}")]
        public Task<IActionResult> HelpArticle(string slug)
            => Done(_s3.GetHelpAsync(slug, includeUnpublished: false));

        [HttpPost("/api/Help")]
        public Task<IActionResult> SaveHelp([FromBody] HelpArticleWrite request)
        {
            if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                return Task.FromResult<IActionResult>(ForbidRole("Only admin can publish help articles."));
            return Done(_s3.SaveHelpAsync(request));
        }

        /// <summary>
        /// SUP-07.02 — patient requests booking assistance (AssistedRequest ticket).
        /// </summary>
        [HttpPost("/api/Support/AssistanceRequest")]
        public Task<IActionResult> RequestAssistance([FromBody] AssistanceRequestBody request)
        {
            var role = DoctorOwnership.GetRoleName(User) ?? "";
            if (!role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<IActionResult>(ForbidRole("Only a patient can request booking assistance."));
            return Done(_s3.RequestAssistanceAsync(request, User.GetUserId(), role));
        }

        /// <summary>
        /// SUP-07.02 — staff list of open AssistedRequest tickets.
        /// </summary>
        [HttpGet("/api/Support/AssistanceRequests")]
        public Task<IActionResult> AssistanceRequests()
        {
            var role = DoctorOwnership.GetRoleName(User) ?? "";
            var allowed = DoctorOwnership.IsGlobalAdminPortalUser(User)
                || role.Equals("Reception", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Doctor", StringComparison.OrdinalIgnoreCase);
            if (!allowed)
                return Task.FromResult<IActionResult>(ForbidRole("Assistance queue is for clinic staff."));
            return Done(_s3.ListAssistanceRequestsAsync());
        }

        /// <summary>
        /// SUP-07.02 — staff completes WEB-04 create on behalf (BookingChannel=Assisted).
        /// </summary>
        [HttpPost("/api/Support/AssistedBook")]
        public Task<IActionResult> Assisted([FromBody] AssistedBookRequest request)
        {
            var role = DoctorOwnership.GetRoleName(User) ?? "";
            var allowed = DoctorOwnership.IsGlobalAdminPortalUser(User)
                || role.Equals("Reception", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Doctor", StringComparison.OrdinalIgnoreCase);
            if (!allowed)
                return Task.FromResult<IActionResult>(ForbidRole("Assisted booking is for clinic staff."));
            var doctorId = request?.DoctorId ?? 0;
            if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
            {
                var deny = DoctorOwnership.ForbidIfNotOwner(User, doctorId);
                if (deny != null)
                    return Task.FromResult<IActionResult>(deny);
            }
            return Done(_s3.AssistedBookAsync(request!, User.GetUserId()));
        }

        [HttpGet("/api/DoctorMobile/Context/{patientAppId:int}")]
        public async Task<IActionResult> Context(int patientAppId)
        {
            var result = await _s3.GetDoctorContextAsync(patientAppId, Caller());
            if (result.StatusCode != 200)
                return StatusCode(result.StatusCode, result.Body);
            return StatusCode(result.StatusCode, result.Body);
        }

        [HttpGet("/api/Refill")]
        public Task<IActionResult> Refills()
        {
            if (DoctorOwnership.ForbidIfNotTreatingDoctor(User) != null)
                return Task.FromResult<IActionResult>(ForbidRole("Refill approval is for the treating doctor."));
            return Done(_s3.ListRefillsAsync());
        }

        [HttpPost("/api/Refill/{refillId:int}/Approve")]
        public Task<IActionResult> ApproveRefill(int refillId)
        {
            if (DoctorOwnership.ForbidIfNotTreatingDoctor(User) != null)
                return Task.FromResult<IActionResult>(ForbidRole("Refill approval is for the treating doctor."));
            return Done(_s3.DecideRefillAsync(refillId, true, null));
        }

        [HttpPost("/api/Refill/{refillId:int}/Reject")]
        public Task<IActionResult> RejectRefill(int refillId, [FromBody] RejectRefillBody? body)
        {
            if (DoctorOwnership.ForbidIfNotTreatingDoctor(User) != null)
                return Task.FromResult<IActionResult>(ForbidRole("Refill approval is for the treating doctor."));
            return Done(_s3.DecideRefillAsync(refillId, false, body?.Reason));
        }

        private async Task<IActionResult> Done(Task<S3ActionResult> work)
        {
            var result = await work;
            return StatusCode(result.StatusCode, result.Body);
        }

        private IActionResult? RequireDoctor(int doctorId)
            => DoctorOwnership.ForbidIfNotOwner(User, doctorId);

        private (int Id, IActionResult? Error) RequireSelfDoctor()
        {
            var denyTreat = DoctorOwnership.ForbidIfNotTreatingDoctor(User);
            if (denyTreat != null)
                return (0, denyTreat);
            var id = DoctorOwnership.GetDoctorId(User);
            if (!id.HasValue)
                return (0, ForbidRole("Doctor context is required."));
            return (id.Value, null);
        }

        private bool IsReception()
        {
            var role = DoctorOwnership.GetRoleName(User);
            return role != null && role.Equals("Reception", StringComparison.OrdinalIgnoreCase);
        }

        private S3Caller Caller()
            => new()
            {
                UserId = User.GetUserId(),
                DoctorId = DoctorOwnership.GetDoctorId(User),
                Role = DoctorOwnership.GetRoleName(User) ?? "",
                IsAdmin = DoctorOwnership.IsGlobalAdminPortalUser(User)
            };

        private IActionResult ForbidRole(string message)
            => StatusCode(StatusCodes.Status403Forbidden, new { success = false, message });
    }

    public class TeleSessionStart
    {
        public int PatientAppId { get; set; }
    }

    public class JoinFailureBody
    {
        public string? Code { get; set; }
    }

    public class RejectRefillBody
    {
        public string? Reason { get; set; }
    }
}

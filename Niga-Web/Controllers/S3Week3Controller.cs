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

        [AllowAnonymous]
        [HttpPost("/api/Waitlist/Join")]
        public Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
            => Done(_s3.JoinWaitlistAsync(request));

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

        [HttpGet("/api/Reception/CasePaper")]
        public Task<IActionResult> CasePapers([FromQuery] int patientId)
        {
            var doctorId = DoctorOwnership.GetDoctorId(User);
            var deny = DoctorOwnership.ForbidIfNotOwner(User, doctorId);
            if (doctorId == null || deny != null)
                return Task.FromResult<IActionResult>(deny ?? ForbidRole("Doctor context is required."));
            return Done(_s3.GetCasePapersAsync(doctorId.Value, patientId));
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
                success = true,
                camera = "client",
                microphone = "client",
                speaker = "client",
                connection = "client",
                vendor = "stub"
            });

        [HttpGet("/api/Tele/Queue")]
        public Task<IActionResult> TeleQueue()
        {
            var doctorId = RequireSelfDoctor();
            if (doctorId.Error != null)
                return Task.FromResult(doctorId.Error);
            return Done(_s3.GetTeleQueueAsync(doctorId.Id));
        }

        [HttpPost("/api/Tele/Sessions")]
        public Task<IActionResult> CreateSession([FromBody] TeleSessionStart request)
        {
            var doctorId = RequireSelfDoctor();
            if (doctorId.Error != null)
                return Task.FromResult(doctorId.Error);
            return Done(_s3.CreateSessionAsync(request?.PatientAppId ?? 0, doctorId.Id));
        }

        [HttpPost("/api/Tele/Sessions/{sessionId:int}/Start")]
        public Task<IActionResult> StartSession(int sessionId)
        {
            var doctorId = RequireSelfDoctor();
            if (doctorId.Error != null)
                return Task.FromResult(doctorId.Error);
            return Done(_s3.StartSessionAsync(sessionId, doctorId.Id));
        }

        [HttpPost("/api/Tele/Sessions/{sessionId:int}/End")]
        public Task<IActionResult> EndSession(int sessionId)
        {
            var doctorId = RequireSelfDoctor();
            if (doctorId.Error != null)
                return Task.FromResult(doctorId.Error);
            return Done(_s3.EndSessionAsync(sessionId, doctorId.Id));
        }

        [HttpPost("/api/Tele/Sessions/{sessionId:int}/Token")]
        public Task<IActionResult> Token(int sessionId)
            => Done(_s3.IssueTokenAsync(sessionId, Caller(), rejoin: false));

        [HttpPost("/api/Tele/Sessions/{sessionId:int}/Rejoin")]
        public Task<IActionResult> Rejoin(int sessionId)
            => Done(_s3.IssueTokenAsync(sessionId, Caller(), rejoin: true));

        [HttpGet("/api/Tele/Sessions/{sessionId:int}")]
        public Task<IActionResult> Session(int sessionId)
            => Done(_s3.GetSessionAsync(sessionId, Caller()));

        [HttpPost("/api/Tele/Consent")]
        public Task<IActionResult> Consent([FromBody] TeleConsentRequest request)
            => Done(_s3.CaptureConsentAsync(request, Caller()));

        [HttpPost("/api/Tele/Sessions/{sessionId:int}/JoinFailure")]
        public Task<IActionResult> JoinFailure(int sessionId, [FromBody] JoinFailureBody? body)
            => Done(_s3.LogJoinFailureAsync(sessionId, body?.Code, Caller()));

        [HttpPost("/api/Tele/Chat")]
        public Task<IActionResult> Chat([FromBody] TeleChatRequest request)
            => Done(_s3.PostChatAsync(request, Caller()));

        [HttpGet("/api/Tele/Chat/{sessionId:int}")]
        public Task<IActionResult> ChatList(int sessionId)
            => Done(_s3.ListChatAsync(sessionId, Caller()));

        [HttpPut("/api/Tele/Summary")]
        public Task<IActionResult> SaveSummary([FromBody] ConsultationSummaryRequest request)
        {
            var doctorId = RequireSelfDoctor();
            if (doctorId.Error != null)
                return Task.FromResult(doctorId.Error);
            return Done(_s3.SaveSummaryAsync(request, doctorId.Id));
        }

        [HttpGet("/api/Tele/Summary/{patientAppId:int}")]
        public Task<IActionResult> Summary(int patientAppId)
            => Done(_s3.GetSummaryAsync(patientAppId, Caller()));

        [HttpPost("/api/Tele/Instant")]
        public Task<IActionResult> Instant([FromBody] InstantConsultRequestBody request)
            => Done(_s3.RequestInstantAsync(request));

        [HttpGet("/api/Tele/Instant/Offers")]
        public Task<IActionResult> Offers()
        {
            var doctorId = RequireSelfDoctor();
            if (doctorId.Error != null)
                return Task.FromResult(doctorId.Error);
            return Done(_s3.ListInstantOffersAsync(doctorId.Id));
        }

        [HttpPost("/api/Tele/Instant/{requestId:int}/Accept")]
        public Task<IActionResult> AcceptInstant(int requestId)
        {
            var doctorId = RequireSelfDoctor();
            if (doctorId.Error != null)
                return Task.FromResult(doctorId.Error);
            return Done(_s3.AcceptInstantAsync(requestId, doctorId.Id));
        }

        [HttpPost("/api/Support/Tickets")]
        public Task<IActionResult> CreateTicket([FromBody] SupportTicketCreate request)
            => Done(_s3.CreateTicketAsync(request, User.GetUserId(), DoctorOwnership.GetRoleName(User) ?? "Patient"));

        [HttpGet("/api/Support/Tickets/Mine")]
        public Task<IActionResult> MyTickets()
            => Done(_s3.ListMyTicketsAsync(User.GetUserId()));

        [HttpGet("/api/Support/Tickets")]
        public Task<IActionResult> AdminTickets([FromQuery] string? status, [FromQuery] string? priority)
        {
            if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                return Task.FromResult<IActionResult>(ForbidRole("Admin ticket queue is for admin."));
            return Done(_s3.ListAdminTicketsAsync(status, priority));
        }

        [HttpPut("/api/Support/Tickets/{ticketId:int}")]
        public Task<IActionResult> UpdateTicket(int ticketId, [FromBody] SupportTicketUpdate request)
        {
            if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                return Task.FromResult<IActionResult>(ForbidRole("Admin ticket queue is for admin."));
            return Done(_s3.UpdateTicketAsync(ticketId, request));
        }

        [HttpPost("/api/Support/Tickets/{ticketId:int}/Messages")]
        public Task<IActionResult> AddMessage(int ticketId, [FromBody] SupportMessageCreate request)
            => Done(_s3.AddMessageAsync(ticketId, request, User.GetUserId(), DoctorOwnership.GetRoleName(User) ?? "", DoctorOwnership.IsGlobalAdminPortalUser(User)));

        [HttpGet("/api/Support/Tickets/{ticketId:int}/Messages")]
        public Task<IActionResult> Messages(int ticketId)
            => Done(_s3.ListMessagesAsync(ticketId, User.GetUserId(), DoctorOwnership.IsGlobalAdminPortalUser(User)));

        [AllowAnonymous]
        [HttpGet("/api/Help")]
        public Task<IActionResult> Help()
            => Done(_s3.ListHelpAsync(includeUnpublished: false));

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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Support tickets and messages, booking-assistance requests, and staff booking on a patient's behalf.
/// </summary>
[ApiController]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly IS3Week3Service _s3;

    public SupportController(IS3Week3Service s3)
    {
        _s3 = s3;
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

    private Task<IActionResult> Done(Task<S3ActionResult> work) => this.Respond(work);
    private IActionResult ForbidRole(string message) => ServiceResultSupport.ForbidRole(this, message);
}

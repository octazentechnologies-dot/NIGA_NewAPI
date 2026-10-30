using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Homeocentrum.Niga.API.Domain.Realtime;

/// <summary>
/// Patient-scoped realtime. Group is derived only from JWT user id — clients cannot join another user.
/// </summary>
[Authorize]
public sealed class PatientRealtimeHub : Hub
{
    public const string HubPath = "/hubs/patient";
    public const string AppointmentChangedMethod = "appointmentChanged";
    public const string InboxChangedMethod = "inboxChanged";

    public static string UserGroup(long userId) => "user:" + userId;

    public override async Task OnConnectedAsync()
    {
        var userId = RealtimeGroupResolver.ResolvePatientUserId(Context.User);
        if (userId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Explicit re-join after reconnect (idempotent).
    /// Optional <paramref name="requestedUserId"/> is accepted for API compatibility but ignored.
    /// </summary>
    public async Task JoinPatientInbox(long? requestedUserId = null)
    {
        var userId = RealtimeGroupResolver.ResolvePatientUserId(Context.User, requestedUserId);
        if (userId <= 0)
            throw new HubException("Patient session is required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
    }
}

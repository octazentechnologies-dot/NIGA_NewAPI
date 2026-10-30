using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Homeocentrum.Niga.API.Domain.Realtime;

/// <summary>Doctor clinic realtime hub. Clients join doctor:{doctorId} after JWT auth.</summary>
[Authorize]
public sealed class ClinicRealtimeHub : Hub
{
    public const string HubPath = "/hubs/clinic";
    public const string ClientMethod = "appointmentRequestChanged";
    public const string AppointmentChangedMethod = "appointmentChanged";

    public static string DoctorGroup(int doctorId) => "doctor:" + doctorId;

    public override async Task OnConnectedAsync()
    {
        var doctorId = RealtimeGroupResolver.ResolveClinicDoctorId(Context.User);
        if (doctorId <= 0)
            throw new HubException("Doctor or reception clinic context is required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, DoctorGroup(doctorId));
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Explicit re-join after reconnect (idempotent).
    /// Optional <paramref name="requestedDoctorId"/> is accepted for API compatibility but ignored.
    /// </summary>
    public async Task JoinDoctorInbox(int? requestedDoctorId = null)
    {
        var doctorId = RealtimeGroupResolver.ResolveClinicDoctorId(Context.User, requestedDoctorId);
        if (doctorId <= 0)
            throw new HubException("Doctor context is required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, DoctorGroup(doctorId));
    }
}

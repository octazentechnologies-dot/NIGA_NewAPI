using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Homeocentrum.Niga.API.Domain.Realtime;

/// <summary>Doctor clinic realtime hub. Clients join doctor:{doctorId} after JWT auth.</summary>
[Authorize]
public sealed class ClinicRealtimeHub : Hub
{
    public const string HubPath = "/hubs/clinic";
    public const string ClientMethod = "appointmentRequestChanged";

    public static string DoctorGroup(int doctorId) => "doctor:" + doctorId;

    public override async Task OnConnectedAsync()
    {
        var doctorId = ReadDoctorId(Context.User);
        if (doctorId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, DoctorGroup(doctorId));
        await base.OnConnectedAsync();
    }

    /// <summary>Explicit re-join after reconnect (idempotent).</summary>
    public async Task JoinDoctorInbox()
    {
        var doctorId = ReadDoctorId(Context.User);
        if (doctorId <= 0)
            throw new HubException("Doctor context is required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, DoctorGroup(doctorId));
    }

    private static int ReadDoctorId(ClaimsPrincipal? user)
    {
        if (user == null) return 0;
        var raw = user.FindFirst("DoctorID")?.Value
            ?? user.FindFirst("DoctorId")?.Value
            ?? user.FindFirst("doctorId")?.Value;
        return int.TryParse(raw, out var id) && id > 0 ? id : 0;
    }
}

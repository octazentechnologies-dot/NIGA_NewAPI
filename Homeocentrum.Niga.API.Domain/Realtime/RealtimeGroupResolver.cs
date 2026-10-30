using System.Security.Claims;
using Homeocentrum.Niga.API.Domain.Extensions;

namespace Homeocentrum.Niga.API.Domain.Realtime;

/// <summary>
/// Derives SignalR group membership from JWT claims only.
/// Any client-supplied user/doctor id is ignored (negative-auth contract).
/// </summary>
public static class RealtimeGroupResolver
{
    /// <summary>
    /// Patient hub group owner. <paramref name="clientRequestedUserId"/> is never used.
    /// </summary>
    public static long ResolvePatientUserId(ClaimsPrincipal? user, long? clientRequestedUserId = null)
    {
        _ = clientRequestedUserId; // forged client ids must not change group membership
        if (user == null) return 0;
        var id = user.GetUserId();
        return id > 0 ? id : 0;
    }

    /// <summary>
    /// Clinic hub group owner from DoctorID claim. <paramref name="clientRequestedDoctorId"/> is never used.
    /// </summary>
    public static int ResolveClinicDoctorId(ClaimsPrincipal? user, int? clientRequestedDoctorId = null)
    {
        _ = clientRequestedDoctorId;
        if (user == null) return 0;
        var raw = user.FindFirst("DoctorID")?.Value
            ?? user.FindFirst("DoctorId")?.Value
            ?? user.FindFirst("doctorId")?.Value;
        return int.TryParse(raw, out var id) && id > 0 ? id : 0;
    }

    public static string PatientGroupName(long userId) => PatientRealtimeHub.UserGroup(userId);

    public static string ClinicDoctorGroupName(int doctorId) => ClinicRealtimeHub.DoctorGroup(doctorId);
}

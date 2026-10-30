using System.Security.Claims;
using Homeocentrum.Niga.API.Domain.Realtime;
using Xunit;

namespace Homeocentrum.Niga.API.Domain.Tests.Realtime;

/// <summary>Negative authorization: forged client ids must never select another user's group.</summary>
public class RealtimeGroupAuthorizationTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "test"));

    [Fact]
    public void Patient_group_ignores_forged_client_user_id()
    {
        var user = Principal(new Claim("UserId", "42"), new Claim(ClaimTypes.NameIdentifier, "42"));
        // GetUserId reads UserId / NameIdentifier depending on extension — set both common shapes.
        var resolved = RealtimeGroupResolver.ResolvePatientUserId(user, clientRequestedUserId: 99999);
        Assert.Equal(42, resolved);
        Assert.Equal("user:42", RealtimeGroupResolver.PatientGroupName(resolved));
        Assert.NotEqual("user:99999", RealtimeGroupResolver.PatientGroupName(resolved));
    }

    [Fact]
    public void Clinic_group_ignores_forged_client_doctor_id()
    {
        var user = Principal(new Claim("DoctorID", "7"));
        var resolved = RealtimeGroupResolver.ResolveClinicDoctorId(user, clientRequestedDoctorId: 12345);
        Assert.Equal(7, resolved);
        Assert.Equal("doctor:7", RealtimeGroupResolver.ClinicDoctorGroupName(resolved));
        Assert.NotEqual("doctor:12345", RealtimeGroupResolver.ClinicDoctorGroupName(resolved));
    }

    [Fact]
    public void Patient_without_user_claim_cannot_join_any_group()
    {
        var user = Principal(new Claim("Role", "Patient"));
        Assert.Equal(0, RealtimeGroupResolver.ResolvePatientUserId(user, clientRequestedUserId: 1));
    }

    [Fact]
    public void Clinic_without_doctor_claim_cannot_join_doctor_group()
    {
        var user = Principal(new Claim("Role", "Admin"), new Claim("UserId", "1"));
        Assert.Equal(0, RealtimeGroupResolver.ResolveClinicDoctorId(user, clientRequestedDoctorId: 5));
    }

    [Fact]
    public void Cross_role_isolation_patient_group_is_not_doctor_group()
    {
        Assert.NotEqual(
            RealtimeGroupResolver.PatientGroupName(7),
            RealtimeGroupResolver.ClinicDoctorGroupName(7));
    }
}

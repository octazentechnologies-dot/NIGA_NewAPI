using Homeocentrum.Niga.API.Domain.Realtime;
using Xunit;

namespace Homeocentrum.Niga.API.Domain.Tests.Realtime;

public class RealtimeHubGroupTests
{
    [Fact]
    public void Patient_group_is_user_scoped()
        => Assert.Equal("user:42", PatientRealtimeHub.UserGroup(42));

    [Fact]
    public void Clinic_group_is_doctor_scoped()
        => Assert.Equal("doctor:7", ClinicRealtimeHub.DoctorGroup(7));

    [Fact]
    public void Hub_paths_are_stable_contracts()
    {
        Assert.Equal("/hubs/patient", PatientRealtimeHub.HubPath);
        Assert.Equal("/hubs/clinic", ClinicRealtimeHub.HubPath);
        Assert.Equal("appointmentChanged", PatientRealtimeHub.AppointmentChangedMethod);
        Assert.Equal("inboxChanged", PatientRealtimeHub.InboxChangedMethod);
        Assert.Equal("appointmentChanged", ClinicRealtimeHub.AppointmentChangedMethod);
        Assert.Equal("appointmentRequestChanged", ClinicRealtimeHub.ClientMethod);
    }
}

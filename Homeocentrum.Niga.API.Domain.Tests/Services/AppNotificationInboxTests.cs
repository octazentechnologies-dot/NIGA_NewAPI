using Homeocentrum.Niga.API.Domain.Services;
using Xunit;

namespace Homeocentrum.Niga.API.Domain.Tests.Services;

public class AppNotificationInboxTests
{
    [Theory]
    [InlineData("appointment_confirm", "APPOINTMENT")]
    [InlineData("appointment_reschedule", "APPOINTMENT")]
    [InlineData("appointment_cancel", "APPOINTMENT")]
    [InlineData("appointment_remind", "APPOINTMENT")]
    [InlineData("appointment_request", "APPOINTMENT")]
    [InlineData("tele_ready", "APPOINTMENT")]
    [InlineData("carelink", "APPOINTMENT")]
    [InlineData("APPT_MISSED", "APPOINTMENT")]
    [InlineData("medicine_status", "MEDICINE")]
    [InlineData("refill", "MEDICINE")]
    [InlineData("pay_link", "PAYMENT")]
    [InlineData("diary_remind", "SYSTEM")]
    [InlineData("admin_send", "SYSTEM")]
    public void Maps_action_to_inbox_type(string action, string expected)
        => Assert.Equal(expected, AppNotificationInbox.MapType(action, null, "Hello", "Body"));

    [Fact]
    public void Explicit_type_wins()
    {
        var data = new Dictionary<string, string> { ["type"] = "MEDICINE" };
        Assert.Equal("MEDICINE", AppNotificationInbox.MapType("appointment_confirm", data, null, null));
    }

    [Fact]
    public void Infers_appointment_from_title_when_action_is_general()
        => Assert.Equal("APPOINTMENT", AppNotificationInbox.MapType("general", null, "Appointment booked", "Dr. Rao"));

    [Theory]
    [InlineData("appointment_confirm", "/patientdashboard")]
    [InlineData("waitlist_offer", "/patient/waitlist")]
    [InlineData("appointment_request", "/patient/appointment-requests")]
    [InlineData("tele_ready", "/patient/rejoin")]
    [InlineData("APPT_MISSED", "/doctor/follow-ups")]
    [InlineData("carelink", "/doctor/carelink")]
    [InlineData("medicine_status", "/patient/medicine-orders")]
    [InlineData("pay_link", "/payments")]
    [InlineData("diary_remind", "/patient/reminders")]
    public void Maps_action_to_deep_link(string action, string expected)
        => Assert.Equal(expected, AppNotificationInbox.MapDeepLink(action, null));

    [Fact]
    public void Explicit_deep_link_wins()
    {
        var data = new Dictionary<string, string> { ["deepLink"] = "/book/pay/abc" };
        Assert.Equal("/book/pay/abc", AppNotificationInbox.MapDeepLink("pay_link", data));
    }

    [Fact]
    public void Doctor_home_link_is_clinic_dashboard()
        => Assert.Equal("/doctordashboard", AppNotificationInbox.DoctorHomeLink());

    [Fact]
    public void Doctor_appointment_link_includes_date_and_patient_app_id()
    {
        var link = AppNotificationInbox.DoctorAppointmentLink(
            new DateTime(2026, 10, 27),
            12345);
        Assert.Equal("/doctor/schedule?date=2026-10-27&patientAppId=12345", link);
    }

    [Fact]
    public void Doctor_appointment_link_without_date_uses_dashboard()
        => Assert.Equal("/doctordashboard", AppNotificationInbox.DoctorAppointmentLink());

    [Fact]
    public void Stable_delivery_id_is_same_for_same_appointment_notify()
    {
        var link = "/doctor/schedule?date=2026-11-02&patientAppId=29201";
        var a = AppNotificationInbox.StableDeliveryId("appointment_confirm", 20080, link, "New appointment", "A booked");
        var b = AppNotificationInbox.StableDeliveryId("appointment_confirm", 20080, link, "New appointment", "A booked again");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Stable_delivery_id_differs_by_user()
    {
        var link = "/doctor/schedule?date=2026-11-02&patientAppId=29201";
        var doctor = AppNotificationInbox.StableDeliveryId("appointment_confirm", 20080, link, "New appointment", "body");
        var staff = AppNotificationInbox.StableDeliveryId("appointment_confirm", 1, link, "New appointment", "body");
        Assert.NotEqual(doctor, staff);
    }
}

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
}

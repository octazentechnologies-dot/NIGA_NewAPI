using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Services;
using Xunit;

namespace Homeocentrum.Niga.API.Domain.Tests.Security;

public class WebHandoffPolicyTests
{
    [Theory]
    [InlineData("patient-board", "patient-board")]
    [InlineData("PatientBoard", "patient-board")]
    [InlineData("doctor-dashboard", "doctor-dashboard")]
    [InlineData("dashboard", "doctor-dashboard")]
    public void Purpose_is_limited_to_the_two_portal_landings(string raw, string expected)
    {
        Assert.Equal(expected, WebHandoffPolicy.NormalizePurpose(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("../doctor/patientboard")]
    public void Unknown_purpose_is_rejected(string raw)
    {
        Assert.Null(WebHandoffPolicy.NormalizePurpose(raw));
    }

    [Theory]
    [InlineData("nigadoctor://portal-return")]
    [InlineData("nigadoctor://portal-return?result=opened")]
    [InlineData("https://nigaui.homeocentrum.com/auth/app-return")]
    [InlineData("https://nigaui.homeocentrum.com/app-return")]
    [InlineData("http://localhost:3000/auth/app-return")]
    public void Return_url_allowlist_accepts_the_app_and_the_portal_page(string url)
    {
        Assert.True(WebHandoffPolicy.IsAllowedReturnUrl(url));
    }

    [Theory]
    [InlineData("https://evil.example/auth/app-return")]
    [InlineData("http://nigaui.homeocentrum.com/auth/app-return")]
    [InlineData("nigadoctor://other")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://nigaui.homeocentrum.com/login")]
    [InlineData("https://user:pass@nigaui.homeocentrum.com/auth/app-return")]
    public void Return_url_allowlist_rejects_anything_else(string url)
    {
        Assert.False(WebHandoffPolicy.IsAllowedReturnUrl(url));
    }

    [Fact]
    public void A_rejected_return_url_falls_back_to_the_app_scheme()
    {
        Assert.Equal(WebHandoffPolicy.DefaultReturnUrl, WebHandoffPolicy.ResolveReturnUrl("https://evil.example/"));
    }

    [Fact]
    public void Renewal_cannot_pass_the_five_hour_cap()
    {
        var now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        var absolute = now.AddMinutes(10);
        var capped = WebHandoffPolicy.CapExpiry(now, absolute);
        Assert.Equal(absolute, capped);

        var early = WebHandoffPolicy.CapExpiry(now, now.AddHours(5));
        Assert.Equal(now.Add(WebHandoffPolicy.AccessTokenLifetime), early);
    }

    [Fact]
    public void The_one_time_code_is_not_stored_as_itself()
    {
        var code = WebHandoffPolicy.CreateCode();
        var hash = SecurityTokenHash.Sha256Hex(code);
        Assert.True(code.Length >= 32);
        Assert.NotEqual(code, hash);
        Assert.Equal(64, hash.Length);
    }
}

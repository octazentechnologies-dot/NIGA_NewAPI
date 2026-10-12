using Homeocentrum.Niga.API.Domain.Logging;
using Homeocentrum.Niga.API.Domain.Security;
using Xunit;

namespace Homeocentrum.Niga.API.Domain.Tests;

public class HostSecurityTests
{
    [Fact]
    public void Signed_in_calls_share_one_user_bucket()
    {
        Assert.Equal("u:10032", HostSecurity.RatePartition("10032", "10.0.0.8"));
        Assert.Equal("u:10032", HostSecurity.RatePartition("10032", "10.0.0.9"));
    }

    [Fact]
    public void Anonymous_calls_share_one_ip_bucket()
    {
        Assert.Equal("ip:10.0.0.8", HostSecurity.RatePartition(null, "10.0.0.8"));
        Assert.Equal("ip:10.0.0.8", HostSecurity.RatePartition("0", "10.0.0.8"));
        Assert.Equal("ip:unknown", HostSecurity.RatePartition("", null));
    }

    [Fact]
    public void Health_and_swagger_are_not_counted()
    {
        Assert.True(HostSecurity.IsOpenPath("/health"));
        Assert.True(HostSecurity.IsOpenPath("/swagger/index.html"));
        Assert.False(HostSecurity.IsOpenPath("/api/Account/Login"));
    }

    [Fact]
    public void Csrf_applies_only_to_cookie_changes_without_a_bearer_token()
    {
        Assert.True(HostSecurity.NeedsCsrfCheck("POST", hasBearer: false, hasCookie: true));
        Assert.False(HostSecurity.NeedsCsrfCheck("POST", hasBearer: true, hasCookie: true));
        Assert.False(HostSecurity.NeedsCsrfCheck("GET", hasBearer: false, hasCookie: true));
        Assert.False(HostSecurity.NeedsCsrfCheck("POST", hasBearer: false, hasCookie: false));
        Assert.False(HostSecurity.NeedsCsrfCheck("POST", hasBearer: false, hasCookie: true, enabled: false));
    }

    [Fact]
    public void Errors_use_one_json_shape()
    {
        var json = ApiProblem.ToJson("trace-1", "Please check the highlighted fields.", new Dictionary<string, string[]>
        {
            ["Mobile"] = new[] { "Required" }
        });
        Assert.Contains("\"success\":false", json);
        Assert.Contains("\"traceId\":\"trace-1\"", json);
        Assert.Contains("\"Mobile\":[\"Required\"]", json);
    }

    [Fact]
    public void Traffic_counter_records_server_errors()
    {
        var before = ApiTraffic.Snapshot();
        ApiTraffic.Hit(200);
        ApiTraffic.Hit(500);
        var after = ApiTraffic.Snapshot();
        Assert.Equal(before.Total + 2, after.Total);
        Assert.Equal(before.ServerErrors + 1, after.ServerErrors);
    }
}

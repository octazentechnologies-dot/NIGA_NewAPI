using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests;

public class S3AppointmentRulesTests
{
    [Theory]
    [InlineData("In-clinic")]
    [InlineData("In clinic")]
    [InlineData("InClinic")]
    [InlineData("Clinic")]
    public void NormalizeMode_AcceptsInClinicAliases(string raw)
    {
        Assert.Equal(S3AppointmentRules.InClinic, S3AppointmentRules.NormalizeMode(raw));
    }

    [Fact]
    public void NormalizeMode_BlankDefaultsToInClinic()
    {
        Assert.Equal(S3AppointmentRules.InClinic, S3AppointmentRules.NormalizeMode(null));
        Assert.Equal(S3AppointmentRules.InClinic, S3AppointmentRules.NormalizeMode("  "));
    }

    [Theory]
    [InlineData("Tele")]
    [InlineData("Online")]
    [InlineData("E-CONSULT")]
    public void NormalizeMode_TeleDoesNotUseInClinic(string raw)
    {
        Assert.Equal(S3AppointmentRules.Tele, S3AppointmentRules.NormalizeMode(raw));
    }
}

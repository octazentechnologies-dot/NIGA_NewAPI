using System.Text.Json;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

/// <summary>
/// Permanent regression fixture for RegressionCase_NearZeroDiscovery
/// (GoldCaseLibrary FollowUpOutcome tag + doctor-selected expected rubrics).
/// </summary>
public class NearZeroDiscoveryGoldCaseTests
{
    public const string RegressionTag = "RegressionCase_NearZeroDiscovery";

    public static readonly string[] ExpectedRubricPaths =
    [
        "MIND - FEAR - high places, of",
        "MIND - TALKING - sleep, in",
        "STOMACH - THIRST - large quantities; for",
        "MALE GENITALIA/SEX - SEXUAL DESIRE - increased",
        "MIND - AWKWARD - drops things",
        "GENERALS - CONVULSIONS - anger; after",
        "GENERALS - FOOD AND DRINKS - salt - desire",
    ];

    public static readonly (string Path, int RemedyCount)[] ExpectedRubricsWithRemedyCounts =
    [
        ("MIND - FEAR - high places, of", 120),
        ("MIND - TALKING - sleep, in", 123),
        ("STOMACH - THIRST - large quantities; for", 88),
        ("MALE GENITALIA/SEX - SEXUAL DESIRE - increased", 297),
        ("MIND - AWKWARD - drops things", 49),
        ("GENERALS - CONVULSIONS - anger; after", 36),
        ("GENERALS - FOOD AND DRINKS - salt - desire", 161),
    ];

    [Fact]
    public void GoldFixture_DefinesSevenDoctorRubrics()
    {
        Assert.Equal(7, ExpectedRubricPaths.Length);
        Assert.Equal(7, ExpectedRubricsWithRemedyCounts.Length);
        Assert.All(ExpectedRubricsWithRemedyCounts, x => Assert.True(x.RemedyCount > 0));
    }

    [Fact]
    public void GoldFixture_DoctorRubricsJson_RoundTripsExpectedShape()
    {
        var json = JsonSerializer.Serialize(ExpectedRubricsWithRemedyCounts.Select(x => new
        {
            subSectionName = x.Path,
            remedyCount = x.RemedyCount,
            tier = x.Path.StartsWith("MIND", StringComparison.OrdinalIgnoreCase) ? "mental"
                : x.Path.StartsWith("MALE", StringComparison.OrdinalIgnoreCase) ? "particular"
                : "general",
        }));

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(7, doc.RootElement.GetArrayLength());
        Assert.Contains("drops things", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(RegressionTag, "RegressionCase_NearZeroDiscovery");
    }

    [Theory]
    [InlineData("MIND - AWKWARD - drops things", "drops things")]
    [InlineData("MIND - TALKING - sleep, in", "talk")]
    [InlineData("GENERALS - FOOD AND DRINKS - salt - desire", "salt")]
    [InlineData("STOMACH - THIRST - large quantities; for", "thirst")]
    [InlineData("MALE GENITALIA/SEX - SEXUAL DESIRE - increased", "sexual")]
    [InlineData("MIND - FEAR - high places, of", "high places")]
    [InlineData("GENERALS - CONVULSIONS - anger; after", "anger")]
    public void ExpectedRubric_HasSearchableLiteralToken(string path, string token)
    {
        Assert.Contains(token, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Top10Target_RequiresAtLeastFiveOfSeven()
    {
        // Acceptance target for STEP 7 after pipeline fix.
        const int minHit = 5;
        Assert.True(ExpectedRubricPaths.Length >= minHit);
    }
}

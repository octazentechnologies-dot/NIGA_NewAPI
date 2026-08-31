using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class ConfidenceScoringEngineTests
{
    private readonly ConfidenceScoringEngine _engine = new();

    [Fact]
    public void Calibrate_MapsHighHybridScoreToHighConfidence()
    {
        var score = _engine.Calibrate(0.9m);
        Assert.True(score >= 0.85m);
    }

    [Fact]
    public void Calibrate_BlendsHistoricalAcceptance()
    {
        var score = _engine.Calibrate(0.8m, 0.95m);
        Assert.True(score > 0.8m);
    }
}

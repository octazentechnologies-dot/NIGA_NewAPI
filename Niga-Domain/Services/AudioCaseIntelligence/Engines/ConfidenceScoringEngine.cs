using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class ConfidenceScoringEngine : IConfidenceScoringEngine
{
    public decimal Calibrate(decimal hybridScore, decimal? historicalAcceptance = null)
    {
        var clamped = Math.Clamp(hybridScore, 0m, 1m);
        var calibrated = Math.Min(0.99m, clamped * 0.92m + 0.08m);

        if (historicalAcceptance.HasValue)
        {
            calibrated = Math.Round((calibrated + Math.Clamp(historicalAcceptance.Value, 0m, 1m)) / 2m, 4);
        }

        return Math.Round(calibrated, 4);
    }
}

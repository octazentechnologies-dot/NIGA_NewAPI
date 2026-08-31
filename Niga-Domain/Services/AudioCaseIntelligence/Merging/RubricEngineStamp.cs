using Niga_Domain.Configuration;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>Resolves the engine stamp that must appear on sessions, telemetry, and health.</summary>
public static class RubricEngineStamp
{
    public const string FastFallback = "fast-f";

    public static string FromRuntime(RubricIntelligenceOptions options, IRubricIntelligenceSettingsService settings)
    {
        if (options.EnableFastClinicalRetrievalPipeline && settings.IsV2Active)
            return Normalize(options.FastPipelineEngineVersion, FastFallback);

        if (settings.IsV3Active)
            return options.EnableV35RecallEngine ? "v3.5" : "v3";

        return settings.IsV2Active ? "v2" : "v1";
    }

    public static string Normalize(string? value, string fallback)
    {
        var stamp = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return stamp.Length <= 10 ? stamp : stamp[..10];
    }

    public static bool IsFastPipeline(string? engineVersion) =>
        !string.IsNullOrWhiteSpace(engineVersion)
        && engineVersion.StartsWith("fast", StringComparison.OrdinalIgnoreCase);
}

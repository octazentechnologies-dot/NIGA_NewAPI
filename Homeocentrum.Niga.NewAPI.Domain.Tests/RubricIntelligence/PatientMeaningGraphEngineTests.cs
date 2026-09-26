using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class PatientMeaningGraphEngineTests
{
    [Fact]
    public async Task BuildAsync_MarathiEpilepsyProdrome_ExtractsVibrationBeforeSeizure()
    {
        var engine = new PatientMeaningGraphEngine(
            new FakeMeaningGptClient(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PatientMeaningGraphEngine>.Instance);

        var result = await engine.BuildAsync(
            "Patient says: फिट येण्याच्या आधी शरीरात कंपन येते. Also fear before the fit.",
            "mr");

        Assert.True(result.Success);
        Assert.NotEmpty(result.Meanings);
        Assert.Contains(result.Meanings, m =>
            m.NormalizedMeaning.Contains("vibr", StringComparison.OrdinalIgnoreCase)
            || m.NormalizedMeaning.Contains("shiver", StringComparison.OrdinalIgnoreCase)
            || m.NormalizedMeaning.Contains("tremor", StringComparison.OrdinalIgnoreCase));
        Assert.All(result.Meanings, m => Assert.False(string.IsNullOrWhiteSpace(m.NormalizedMeaning)));
        Assert.All(result.Meanings, m => Assert.Equal(PatientMeaningGraphEngine.ModelId, m.ModelVersion));
    }

    [Fact]
    public async Task BuildAsync_EmptyTranscript_ReturnsError()
    {
        var engine = new PatientMeaningGraphEngine(
            new FakeMeaningGptClient(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PatientMeaningGraphEngine>.Instance);

        var result = await engine.BuildAsync(string.Empty, "en");

        Assert.False(result.Success);
    }

    private sealed class FakeMeaningGptClient : IIntelligenceGptClient
    {
        public Task<IntelligenceGptResult<T>> CompleteJsonAsync<T>(
            string systemPrompt,
            string userPrompt,
            string stageName,
            CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(PatientMeaningExtractionGptModel))
            {
                var payload = new PatientMeaningExtractionGptModel
                {
                    Meanings =
                    [
                        new PatientMeaningExtractionItemGptModel
                        {
                            RawStatement = "फिट येण्याच्या आधी शरीरात कंपन येते",
                            NormalizedMeaning = "vibration in body before seizure",
                            LanguageCode = "mr",
                            Confidence = 0.96m,
                        },
                        new PatientMeaningExtractionItemGptModel
                        {
                            RawStatement = "fear before the fit",
                            NormalizedMeaning = "anticipatory fear before epileptic fit",
                            LanguageCode = "en",
                            Confidence = 0.94m,
                        },
                    ],
                };

                return Task.FromResult(new IntelligenceGptResult<T>
                {
                    Success = true,
                    Result = (T)(object)payload,
                    LatencyMs = 10,
                });
            }

            return Task.FromResult(new IntelligenceGptResult<T>
            {
                Success = false,
                Error = "Unexpected type in test.",
            });
        }
    }
}

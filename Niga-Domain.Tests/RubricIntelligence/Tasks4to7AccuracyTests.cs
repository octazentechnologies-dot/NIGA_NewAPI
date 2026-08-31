using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class DualLanguageMeaningGraphTests
{
    [Fact]
    public async Task BuildAsync_WithDualLanguage_SetsOriginalRawStatementForSensation()
    {
        var engine = new PatientMeaningGraphEngine(
            new DualLangFakeGptClient(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PatientMeaningGraphEngine>.Instance);

        var dual = new DualLanguageMeaningContext
        {
            EnglishTranscript = "Patient has burning in the stomach.",
            OriginalLanguageTranscript = "पोटाला जळजळ होते",
            LanguageCode = "mr",
            SensationHints =
            [
                new DualLanguageSensationHint
                {
                    EnglishPhrase = "burning in the stomach",
                    OriginalLanguageText = "पोटाला जळजळ होते",
                    LanguageCode = "mr",
                },
            ],
        };

        var result = await engine.BuildAsync(dual.EnglishTranscript, "mr", dual);

        Assert.True(result.Success);
        var sensation = Assert.Single(result.Meanings);
        Assert.True(sensation.IsSensationBearing);
        Assert.Equal("पोटाला जळजळ होते", sensation.RawStatement);
        Assert.Contains("burn", sensation.NormalizedMeaning, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("mr", sensation.LanguageCode);
    }
}

public class RubricUnifiedContractHelperTests
{
    [Fact]
    public void ApplyUnifiedContract_AssignsRankSourceAndScores_Additively()
    {
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 10,
                SubSectionName = "MIND - FEAR",
                MatchScore = 0.8m,
                ConfidenceScore = 0.8m,
                IsAiSuggested = false,
                RemedyCountForSort = 12,
            },
            new()
            {
                SubSectionId = -1,
                SubSectionName = "Invented rubric",
                MatchScore = 0.5m,
                IsAiSuggested = true,
            },
        };

        var unified = RubricUnifiedContractHelper.ApplyUnifiedContract(rubrics);

        Assert.Equal(1, unified[0].Rank);
        Assert.Equal("Database", unified[0].Source);
        Assert.Equal(12, unified[0].RemedyCount);
        Assert.NotNull(unified[0].Scores?.FinalHybridScore);
        Assert.Equal("AiSuggested", unified[1].Source);
        Assert.Equal(2, unified[1].Rank);
    }

    [Fact]
    public void ApplyUnifiedContract_PreservesExistingFields()
    {
        var rubric = new AudioCaseSuggestedRubricModel
        {
            SubSectionId = 5,
            SubSectionName = "HEAD - PAIN",
            MatchScore = 0.7m,
            WhySuggested = "exact match",
            EngineVersion = "v3",
        };

        var unified = RubricUnifiedContractHelper.ApplyUnifiedContract(new[] { rubric });
        Assert.Equal("exact match", unified[0].WhySuggested);
        Assert.Equal("v3", unified[0].EngineVersion);
        Assert.Equal(5, unified[0].SubSectionId);
    }
}

public class EvidenceChainCompleteGateTests
{
    [Fact]
    public void IncompleteChain_IsRejectedWhenGateEnforced()
    {
        var complete = new RubricDiscoveryNodeModel
        {
            SubSectionId = 1,
            SubSectionName = "MIND - FEAR",
            Confidence = 0.9m,
            EvidenceChain = new RubricEvidenceChainV3Model
            {
                TranscriptStatement = "fear before fit",
                PatientMeaning = "fear preceding convulsions",
                ClinicalConcept = "anticipatory fear",
                HomeopathicConcept = "fear before epilepsy",
                RubricName = "MIND - FEAR",
                IsComplete = true,
            },
        };
        var incomplete = new RubricDiscoveryNodeModel
        {
            SubSectionId = 2,
            SubSectionName = "BACK - VIBRATION",
            Confidence = 0.95m,
            EvidenceChain = new RubricEvidenceChainV3Model
            {
                RubricName = "BACK - VIBRATION",
                IsComplete = false,
            },
        };

        Assert.True(complete.EvidenceChain!.IsComplete);
        Assert.False(incomplete.EvidenceChain!.IsComplete);

        var doctorFacing = new[] { complete, incomplete }
            .Where(d => d.SubSectionId > 0 && d.EvidenceChain?.IsComplete == true)
            .ToList();

        Assert.Single(doctorFacing);
        Assert.Equal(1, doctorFacing[0].SubSectionId);
        Assert.All(doctorFacing, d => Assert.True(d.EvidenceChain!.IsComplete));
    }
}

internal sealed class DualLangFakeGptClient : IIntelligenceGptClient
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
                        RawStatement = "burning in the stomach",
                        NormalizedMeaning = "burning sensation in stomach",
                        LanguageCode = "mr",
                        Confidence = 0.92m,
                        IsSensationBearing = true,
                    },
                ],
            };

            return Task.FromResult(new IntelligenceGptResult<T>
            {
                Success = true,
                Result = (T)(object)payload,
                LatencyMs = 1,
            });
        }

        return Task.FromResult(new IntelligenceGptResult<T>
        {
            Success = false,
            Error = "Unexpected type in test.",
        });
    }
}

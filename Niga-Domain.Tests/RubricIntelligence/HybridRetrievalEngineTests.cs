using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class HybridRetrievalEngineTests
{
    [Fact]
    public async Task RetrieveAsync_CombinesAliasAndEmbeddingCandidates()
    {
        var engine = new HybridRetrievalEngine(
            Options.Create(new RubricIntelligenceOptions
            {
                EnableEmbeddingSearch = true,
                HybridWeights = new HybridWeightOptions(),
            }),
            new FakeEmbeddingSearchEngine(),
            new ConfidenceScoringEngine(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<HybridRetrievalEngine>.Instance);

        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                RawStatement = "vibration before fit",
                ClinicalMeaning = "aura before convulsion",
                SearchTerms = new List<string> { "vibration", "fit", "aura" },
            },
        };

        var aliasRubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 101,
                SubSectionName = "GENERALITIES - CONVULSIONS - aura",
                MatchScore = 0.8m,
                ConfidenceScore = 0.82m,
                MatchSource = "Alias",
            },
        };

        var result = await engine.RetrieveAsync(concepts, new List<AudioCaseSymptomModel>(), aliasRubrics);

        Assert.NotEmpty(result.Rubrics);
        Assert.Contains(result.Rubrics, r => r.SubSectionId == 101);
        Assert.Contains(result.Rubrics, r => r.SubSectionId == 202);
        Assert.All(result.Rubrics, r => Assert.True((r.ConfidenceScore ?? 0) > 0));
    }

    private sealed class FakeEmbeddingSearchEngine : IEmbeddingSearchEngine
    {
        public Task<EmbeddingSearchResult> SearchAsync(
            IReadOnlyList<ClinicalConceptModel> concepts,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmbeddingSearchResult
            {
                Candidates = new List<EmbeddingSearchCandidate>
                {
                    new()
                    {
                        SubSectionId = 202,
                        SubSectionName = "MIND - FEAR - fit, before",
                        CosineScore = 0.91m,
                        MatchedConceptText = "aura before convulsion",
                    },
                },
            });
        }
    }
}

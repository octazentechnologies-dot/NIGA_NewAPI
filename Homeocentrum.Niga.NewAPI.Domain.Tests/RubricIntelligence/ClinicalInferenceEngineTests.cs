using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class ClinicalInferenceEngineTests
{
    [Fact]
    public async Task InferAsync_AddsRubric_WhenConceptHasNoStrongMatch()
    {
        var engine = new ClinicalInferenceEngine(
            Options.Create(new RubricIntelligenceOptions
            {
                EnableClinicalInference = true,
                MinConceptConfidenceForInference = 0.85m,
                MinRetrievalScoreToSkipInference = 0.70m,
                MinEmbeddingNeighborForInference = 0.65m,
            }),
            new FakeEmbeddingSearchEngine());

        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "vibration before fit",
                ClinicalMeaning = "aura before convulsion",
                Confidence = 0.92m,
                SearchTerms = new List<string> { "vibration", "fit" },
            },
        };

        var result = await engine.InferAsync(concepts, Array.Empty<CausationLinkModel>(), new List<AudioCaseSuggestedRubricModel>());

        Assert.NotEmpty(result.Rubrics);
        Assert.Equal("Inference", result.Rubrics[0].RubricTier);
        Assert.NotEmpty(result.Logs);
    }

    [Fact]
    public async Task InferAsync_Skips_WhenStrongRetrievalExists()
    {
        var engine = new ClinicalInferenceEngine(
            Options.Create(new RubricIntelligenceOptions()),
            new FakeEmbeddingSearchEngine());

        var conceptId = Guid.NewGuid();
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = conceptId,
                RawStatement = "fear before fit",
                ClinicalMeaning = "fear before fit",
                Confidence = 0.9m,
                SearchTerms = new List<string> { "fear", "fit" },
            },
        };

        var existing = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 99,
                SubSectionName = "MIND - FEAR - fit, before",
                MatchedFrom = "fear before fit",
                MatchScore = 0.88m,
                ConfidenceScore = 0.9m,
            },
        };

        var result = await engine.InferAsync(concepts, Array.Empty<CausationLinkModel>(), existing);
        Assert.Empty(result.Rubrics);
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
                        SubSectionId = 501,
                        SubSectionName = "GENERALITIES - CONVULSIONS - aura",
                        CosineScore = 0.88m,
                    },
                },
            });
        }
    }
}

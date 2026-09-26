using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class HomeopathicWeightEngineTests
{
    [Fact]
    public async Task ApplyAsync_AssignsHigherWeightToSrpConcepts()
    {
        var engine = new HomeopathicWeightEngine(new FakeWeightRepository());
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "strange fear before fit",
                Category = "mental",
                IsSRP = true,
                Confidence = 0.9m,
            },
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "constipation",
                Category = "particular",
                Confidence = 0.7m,
            },
        };

        var result = await engine.ApplyAsync(concepts, Array.Empty<CausationLinkModel>());

        var srp = result.Concepts.First(c => c.IsSRP);
        var particular = result.Concepts.First(c => !c.IsSRP);
        Assert.True(srp.HomeopathicWeight > particular.HomeopathicWeight);
    }

    [Fact]
    public async Task ApplyAsync_BoostsEffectConceptInCausationChain()
    {
        var effectId = Guid.NewGuid();
        var engine = new HomeopathicWeightEngine(new FakeWeightRepository());
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "anger",
                Category = "causation",
                HomeopathicWeight = 7m,
                Confidence = 0.9m,
            },
            new()
            {
                ConceptId = effectId,
                RawStatement = "convulsion",
                Category = "particular",
                HomeopathicWeight = 4m,
                Confidence = 0.88m,
            },
        };

        var links = new List<CausationLinkModel>
        {
            new()
            {
                LinkId = Guid.NewGuid(),
                CauseConceptId = concepts[0].ConceptId,
                EffectConceptId = effectId,
                CauseText = "anger",
                EffectText = "convulsion",
                Confidence = 0.95m,
            },
        };

        var result = await engine.ApplyAsync(concepts, links);
        var effect = result.Concepts.Single(c => c.ConceptId == effectId);

        Assert.True(effect.HomeopathicWeight > 4m);
    }

    [Fact]
    public async Task ApplyWeightToRubric_IncreasesMatchScoreForMentalRubric()
    {
        var engine = new HomeopathicWeightEngine(new FakeWeightRepository());
        var rubric = new AudioCaseSuggestedRubricModel
        {
            SubSectionName = "MIND - FEAR",
            MatchScore = 0.6m,
            MatchedFrom = "fear before fit",
        };
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                RawStatement = "fear before fit",
                SearchTerms = new List<string> { "fear", "fit" },
                Category = "mental",
                HomeopathicWeight = 8m,
            },
        };

        await engine.ApplyWeightToRubricAsync(rubric, concepts);

        Assert.True(rubric.MatchScore > 0.6m);
        Assert.True(rubric.HomeopathicWeight >= 8m);
    }

    [Fact]
    public async Task ApplyAsync_UsesCausationMultiplierFromDatabaseRule()
    {
        var effectId = Guid.NewGuid();
        var engine = new HomeopathicWeightEngine(new FakeWeightRepository
        {
            Rules =
            [
                new() { RuleCode = "CausationLinked", Category = "causation", WeightValue = 7m, MultiplierValue = 1.4m, IsActive = true },
                new() { RuleCode = "Particular", Category = "particular", WeightValue = 4m, IsActive = true },
            ],
        });

        var result = await engine.ApplyAsync(
            [new() { ConceptId = effectId, RawStatement = "convulsion", Category = "particular" }],
            [new() { EffectConceptId = effectId }]);

        Assert.Equal(5.6m, result.Concepts.Single().HomeopathicWeight);
    }

    private sealed class FakeWeightRepository : IAudioCaseIntelligenceRepository
    {
        public List<Homeocentrum.Niga.NewAPI.Domain.Master.HomeopathicWeightRule> Rules { get; set; } = new();

        public Task SaveConceptsAsync(Guid sessionId, IReadOnlyList<ClinicalConceptModel> concepts, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveIntelligenceLogAsync(Guid sessionId, string? correlationId, string stageName, string status, string? message, string? detailsJson, int? latencyMs, CancellationToken cancellationToken = default, string engineVersion = "v2")
            => Task.CompletedTask;

        public Task<List<ClinicalConceptModel>> GetConceptsAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<ClinicalConceptModel>());

        public Task SaveCausationLinksAsync(Guid sessionId, IReadOnlyList<CausationLinkModel> links, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<CausationLinkModel>> GetCausationLinksAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<CausationLinkModel>());

        public Task<List<Homeocentrum.Niga.NewAPI.Domain.Master.HomeopathicWeightRule>> GetActiveWeightRulesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Rules);

        public Task SaveInferenceLogsAsync(Guid sessionId, IReadOnlyList<ClinicalInferenceLogModel> logs, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

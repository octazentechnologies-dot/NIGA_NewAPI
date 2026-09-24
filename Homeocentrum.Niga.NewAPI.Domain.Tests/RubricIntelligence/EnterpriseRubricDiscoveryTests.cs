using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class EnterpriseRubricDiscoveryTests
{
    [Fact]
    public void Merger_PrefersBootstrapOverEmbedding_WhenSameRubric()
    {
        var merger = new EnterpriseRubricCandidateMerger();
        var map = new Dictionary<int, EnterpriseRubricDiscoveryCandidate>();

        merger.MergeDiscoveries(map, new List<RubricDiscoveryNodeModel>
        {
            new()
            {
                SubSectionId = 101,
                SubSectionName = "GENERALITIES - CONVULSIONS - AURA",
                Confidence = 0.72m,
                DiscoveryMethod = "ConceptMapping",
            },
        }, new ConceptGraphFullModel(), RubricDiscoverySources.Bootstrap);

        merger.MergeDiscoveries(map, new List<RubricDiscoveryNodeModel>
        {
            new()
            {
                SubSectionId = 101,
                SubSectionName = "GENERALITIES - CONVULSIONS - AURA",
                Confidence = 0.90m,
                DiscoveryMethod = "AIConceptEmbedding",
            },
        }, new ConceptGraphFullModel(), RubricDiscoverySources.EnterpriseEmbedding);

        Assert.Equal(RubricDiscoverySources.Bootstrap, map[101].DiscoverySource);
    }

    [Fact]
    public void RankingEngine_ProducesScoreBetween0And100()
    {
        var options = new RubricIntelligenceOptions();
        var score = EnterpriseRubricRankingEngine.ComputeFinalScore(new EnterpriseRubricDiscoveryCandidate
        {
            SimilarityScore = 0.88m,
            ClinicalRelevanceScore = 0.80m,
            EvidenceScore = 0.75m,
            DoctorAcceptanceScore = 0.70m,
            DiscoverySource = RubricDiscoverySources.Bootstrap,
        }, options);

        Assert.InRange(score, 0m, 100m);
    }
}

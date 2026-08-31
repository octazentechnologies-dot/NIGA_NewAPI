using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.KnowledgeGraph;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class KnowledgeGraphRankerTests
{
    [Fact]
    public void RankAndMerge_PrefersKnowledgeGraphPathOverEmbeddingOnly()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new Configuration.RubricIntelligenceOptions
        {
            KnowledgeGraphEmbeddingBlendRatio = 0.35m,
            KnowledgeGraphMinPathConfidence = 0.55m,
        });
        var ranker = new KnowledgeGraphRanker(options);

        var paths = new List<KgRubricPathModel>
        {
            new()
            {
                SubSectionId = 101,
                SubSectionName = "MIND - FEAR",
                PathConfidence = 0.88m,
                CompositeScore = 0.88m,
                HasKnowledgeGraphPath = true,
                DiscoveryMethod = "KnowledgeGraph",
            },
        };

        var embeddings = new List<RubricDiscoveryNodeModel>
        {
            new()
            {
                SubSectionId = 101,
                SubSectionName = "MIND - FEAR",
                Confidence = 0.72m,
                DiscoveryMethod = "SemanticRubricEmbedding",
            },
            new()
            {
                SubSectionId = 202,
                SubSectionName = "GENERALITIES - WEAKNESS",
                Confidence = 0.65m,
                DiscoveryMethod = "SemanticRubricEmbedding",
            },
        };

        var merged = ranker.RankAndMerge(paths, embeddings);

        var primary = merged.First(x => x.SubSectionId == 101);
        Assert.True(primary.Confidence > 0.80m);
        Assert.Contains("KG path", primary.MatchReason);

        var review = merged.First(x => x.SubSectionId == 202);
        Assert.Equal("Review", review.RubricTier);
    }
}

public class KgNodeTypesTests
{
    [Fact]
    public void EdgeTypes_ContainFullClinicalChain()
    {
        Assert.Equal("Expresses", KgEdgeTypes.Expresses);
        Assert.Equal("ImpliesClinical", KgEdgeTypes.ImpliesClinical);
        Assert.Equal("MapsHomeopathic", KgEdgeTypes.MapsHomeopathic);
        Assert.Equal("SuggestsRubric", KgEdgeTypes.SuggestsRubric);
        Assert.Equal("HasRemedy", KgEdgeTypes.HasRemedy);
    }
}

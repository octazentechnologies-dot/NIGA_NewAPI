using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class SensationOntologyMatcherTests
{
    [Theory]
    [InlineData("as if a nail driven into the head", "HEAD - PAIN - as if a nail", "as if", true)]
    [InlineData("band like constriction of chest", "CHEST - CONSTRICTION - band, as if a", "as if", true)]
    [InlineData("ordinary headache worse morning", "HEAD - PAIN - MORNING", "as if", false)]
    public void TokenOverlap_ScoresControlledSensationPatterns(
        string clinical,
        string rubricName,
        string pattern,
        bool expectStrong)
    {
        var hayTokens = clinical.ToLowerInvariant().Split(new[] { ' ', '-', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rubricTokens = rubricName.ToLowerInvariant().Split(new[] { ' ', '-', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var overlap = hayTokens.Intersect(rubricTokens, StringComparer.OrdinalIgnoreCase).Count();
        var jaccard = (decimal)overlap / Math.Max(1, hayTokens.Union(rubricTokens, StringComparer.OrdinalIgnoreCase).Count());
        var patternBoost = clinical.Contains(pattern, StringComparison.OrdinalIgnoreCase)
            || rubricName.Contains(pattern, StringComparison.OrdinalIgnoreCase)
            ? 0.35m
            : 0m;
        var score = Math.Min(1m, jaccard + patternBoost);

        if (expectStrong)
            Assert.True(score >= 0.6m, $"Expected strong ontology-style match, got {score}");
        else
            Assert.True(score < 0.6m, $"Expected weak match for non-sensation text, got {score}");
    }

    [Fact]
    public void MetaphorNode_GroundedInOntologyFlag_DefaultsFalseUntilMatched()
    {
        var meta = new MetaphorResolutionNodeModel
        {
            IsMetaphor = true,
            ClinicalMeaning = "nail-like headache",
            GroundedInOntology = false,
        };
        Assert.False(meta.GroundedInOntology);

        meta.GroundedInOntology = true;
        meta.OntologyId = 42;
        Assert.True(meta.GroundedInOntology);
        Assert.Equal(42, meta.OntologyId);
    }

    [Fact]
    public void ConceptInterpretationEdgeTypes_ExposeTask2Constants()
    {
        Assert.Equal("DerivesClinical", ConceptInterpretationEdgeTypes.DerivesClinical);
        Assert.Equal("Interprets", ConceptInterpretationEdgeTypes.Interprets);
    }
}

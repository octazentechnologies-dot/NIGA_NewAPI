using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class RubricResultMergerTests
{
    [Fact]
    public void Merge_PrefersHigherScore_WhenSameSubSectionId()
    {
        var v1 = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 10, SubSectionName = "MIND - FEAR", MatchScore = 0.6m },
        };
        var v2 = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 10, SubSectionName = "MIND - FEAR", MatchScore = 0.5m, ConfidenceScore = 0.92m },
        };

        var merged = RubricResultMerger.Merge(v1, v2);

        Assert.Single(merged);
        Assert.Equal(0.92m, merged[0].ConfidenceScore);
    }

    [Fact]
    public void Merge_CombinesDistinctRubrics_AndRespectsMax()
    {
        var v1 = Enumerable.Range(1, 15)
            .Select(i => new AudioCaseSuggestedRubricModel
            {
                SubSectionId = i,
                SubSectionName = $"Rubric {i}",
                MatchScore = i * 0.01m,
            })
            .ToList();
        var v2 = Enumerable.Range(16, 10)
            .Select(i => new AudioCaseSuggestedRubricModel
            {
                SubSectionId = i,
                SubSectionName = $"Rubric {i}",
                MatchScore = i * 0.01m,
            })
            .ToList();

        var merged = RubricResultMerger.Merge(v1, v2, maxResults: 20);

        Assert.Equal(20, merged.Count);
        Assert.Equal(25, merged[0].SubSectionId);
    }

    [Fact]
    public void SelectDiscoveryPath_WhenStrictGatingOff_AlwaysUsesLegacyMerge()
    {
        var concepts = new List<HomeopathicConceptNodeModel>
        {
            new() { HomeopathicConceptId = 1, ConceptName = "aura before convulsion", Confidence = 0.9m },
        };
        var conceptRubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 100, SubSectionName = "GENERALITIES - CONVULSIONS - AURA", ConfidenceScore = 0.91m },
        };
        var v1Bad = EpilepsyV1FalsePositives();

        var result = RubricResultMerger.SelectDiscoveryPath(
            enableV3ConceptGraph: true,
            strictConceptGatedDiscovery: false,
            conceptGraphMinConfidence: 0.55m,
            homeopathicConcepts: concepts,
            conceptGraphRubrics: conceptRubrics,
            v1Rubrics: v1Bad,
            v2Rubrics: Array.Empty<AudioCaseSuggestedRubricModel>());

        Assert.Equal(DiscoveryPathKind.LegacyV1V2Merge, result.Path);
        Assert.Contains(result.Rubrics, r => IsBackVibration(r.SubSectionName));
    }

    [Fact]
    public void SelectDiscoveryPath_WhenStrictGatingOn_WithUsableConcepts_SkipsV1FalsePositives()
    {
        var concepts = new List<HomeopathicConceptNodeModel>
        {
            new()
            {
                HomeopathicConceptId = 1,
                ConceptName = "epileptic aura / prodrome",
                Confidence = 0.88m,
            },
        };
        var conceptRubrics = BuildEpilepsyConceptGraphRubrics();
        var v1Bad = EpilepsyV1FalsePositives();

        var result = RubricResultMerger.SelectDiscoveryPath(
            enableV3ConceptGraph: true,
            strictConceptGatedDiscovery: true,
            conceptGraphMinConfidence: 0.55m,
            homeopathicConcepts: concepts,
            conceptGraphRubrics: conceptRubrics,
            v1Rubrics: v1Bad,
            v2Rubrics: Array.Empty<AudioCaseSuggestedRubricModel>(),
            maxResults: 10);

        Assert.Equal(DiscoveryPathKind.ConceptGraphOnly, result.Path);
        Assert.Equal(1, result.UsableConceptCount);
        Assert.DoesNotContain(result.Rubrics, r => IsBackVibration(r.SubSectionName));
        Assert.DoesNotContain(result.Rubrics, r => IsMenses(r.SubSectionName));
        Assert.Contains(result.Rubrics, r => IsConvulsionsAuraFamily(r.SubSectionName));
    }

    [Theory]
    [InlineData("Vibration in hands 10 seconds before every fit at night.")]
    [InlineData("10 seconds before the fit, vibration starts in my hands.")]
    [InlineData("Fear before fit with shivering; male patient age 28.")]
    public void SelectDiscoveryPath_EpilepsyGoldTranscripts_ExcludesDocumentedFalsePositives(string transcript)
    {
        Assert.False(string.IsNullOrWhiteSpace(transcript));

        var concepts = new List<HomeopathicConceptNodeModel>
        {
            new()
            {
                HomeopathicConceptId = 11,
                ConceptName = "aura before convulsion",
                Confidence = 0.86m,
                Weight = 1.2m,
            },
            new()
            {
                HomeopathicConceptId = 12,
                ConceptName = "fear before fit",
                Confidence = 0.8m,
            },
        };
        var conceptRubrics = BuildEpilepsyConceptGraphRubrics();
        var v1Bad = EpilepsyV1FalsePositives();

        var result = RubricResultMerger.SelectDiscoveryPath(
            enableV3ConceptGraph: true,
            strictConceptGatedDiscovery: true,
            conceptGraphMinConfidence: 0.55m,
            homeopathicConcepts: concepts,
            conceptGraphRubrics: conceptRubrics,
            v1Rubrics: v1Bad,
            v2Rubrics: new List<AudioCaseSuggestedRubricModel>
            {
                // Unscoped embedding-style contaminant that historically leaked via merge
                new() { SubSectionId = 9001, SubSectionName = "BACK - VIBRATION", ConfidenceScore = 0.95m, MatchScore = 0.95m },
                new() { SubSectionId = 9002, SubSectionName = "MENSES - BEFORE", ConfidenceScore = 0.9m },
            },
            maxResults: 10);

        Assert.Equal(DiscoveryPathKind.ConceptGraphOnly, result.Path);
        var top10 = result.Rubrics.Take(10).ToList();
        Assert.DoesNotContain(top10, r => IsBackVibration(r.SubSectionName));
        Assert.DoesNotContain(top10, r => IsMenses(r.SubSectionName));
        Assert.Contains(top10, r => IsConvulsionsAuraFamily(r.SubSectionName));
    }

    [Fact]
    public void SelectDiscoveryPath_WhenNoUsableConcepts_FallsBackToLegacyMerge()
    {
        var lowConfidenceConcepts = new List<HomeopathicConceptNodeModel>
        {
            new() { HomeopathicConceptId = 99, ConceptName = "weak", Confidence = 0.2m },
        };
        var v1 = EpilepsyV1FalsePositives();

        var result = RubricResultMerger.SelectDiscoveryPath(
            enableV3ConceptGraph: true,
            strictConceptGatedDiscovery: true,
            conceptGraphMinConfidence: 0.55m,
            homeopathicConcepts: lowConfidenceConcepts,
            conceptGraphRubrics: BuildEpilepsyConceptGraphRubrics(),
            v1Rubrics: v1,
            v2Rubrics: Array.Empty<AudioCaseSuggestedRubricModel>());

        Assert.Equal(DiscoveryPathKind.LegacyV1V2Merge, result.Path);
        Assert.Equal(0, result.UsableConceptCount);
        Assert.Contains(result.Rubrics, r => IsBackVibration(r.SubSectionName));
    }

    [Fact]
    public void SelectDiscoveryPath_WhenOnlyAiClinicalConcept_FallsBackDespiteHighConfidence()
    {
        var concepts = new List<HomeopathicConceptNodeModel>
        {
            new() { HomeopathicConceptId = 1, ConceptName = "fear before convulsion", Confidence = 0.92m },
        };
        var conceptRubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 0,
                SubSectionName = "Fear Before Convulsion",
                ConfidenceScore = 0.92m,
                MatchScore = 0.92m,
                ResultKind = "AiClinicalConcept",
                MatchSource = "AiClinicalConcept",
                MatchLayer = "AiClinicalConcept",
            },
        };
        var v1Hits = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 301, SubSectionName = "MIND - AWKWARD - drops things", MatchScore = 0.8m },
            new() { SubSectionId = 302, SubSectionName = "MIND - TALKING - sleep, in", MatchScore = 0.78m },
            new() { SubSectionId = 303, SubSectionName = "GENERALS - FOOD AND DRINKS - salt - desire", MatchScore = 0.75m },
        };

        var result = RubricResultMerger.SelectDiscoveryPath(
            enableV3ConceptGraph: true,
            strictConceptGatedDiscovery: true,
            conceptGraphMinConfidence: 0.55m,
            homeopathicConcepts: concepts,
            conceptGraphRubrics: conceptRubrics,
            v1Rubrics: v1Hits,
            v2Rubrics: Array.Empty<AudioCaseSuggestedRubricModel>(),
            maxResults: 10,
            minRepertoryCandidatesForExclusivePath: 3);

        Assert.Equal(DiscoveryPathKind.LegacyV1V2Merge, result.Path);
        Assert.Equal(0, result.RepertoryCandidateCount);
        Assert.Contains(result.Rubrics, r => r.SubSectionName.Contains("drops things", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Rubrics, r => r.SubSectionName.Contains("salt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("no authoritative repertory", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectDiscoveryPath_WhenRepertoryBelowMin_SupplementsWithV1()
    {
        var concepts = new List<HomeopathicConceptNodeModel>
        {
            new() { HomeopathicConceptId = 1, ConceptName = "forgetfulness", Confidence = 0.9m },
        };
        var conceptRubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 501, SubSectionName = "MIND - FORGETFULNESS", ConfidenceScore = 0.9m, MatchScore = 0.9m },
        };
        var v1Hits = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 601, SubSectionName = "MIND - AWKWARD - drops things", MatchScore = 0.8m },
            new() { SubSectionId = 602, SubSectionName = "MIND - TALKING - sleep, in", MatchScore = 0.77m },
        };

        var result = RubricResultMerger.SelectDiscoveryPath(
            enableV3ConceptGraph: true,
            strictConceptGatedDiscovery: true,
            conceptGraphMinConfidence: 0.55m,
            homeopathicConcepts: concepts,
            conceptGraphRubrics: conceptRubrics,
            v1Rubrics: v1Hits,
            v2Rubrics: Array.Empty<AudioCaseSuggestedRubricModel>(),
            minRepertoryCandidatesForExclusivePath: 3);

        Assert.Equal(DiscoveryPathKind.LegacyV1V2Merge, result.Path);
        Assert.Equal(1, result.RepertoryCandidateCount);
        Assert.Contains(result.Rubrics, r => r.SubSectionId == 501);
        Assert.Contains(result.Rubrics, r => r.SubSectionId == 601);
    }

    private static List<AudioCaseSuggestedRubricModel> EpilepsyV1FalsePositives() =>
        new()
        {
            new() { SubSectionId = 501, SubSectionName = "BACK - VIBRATION", MatchScore = 0.82m },
            new() { SubSectionId = 502, SubSectionName = "CHEST - VIBRATION", MatchScore = 0.7m },
            new() { SubSectionId = 503, SubSectionName = "MENSES - BEFORE", MatchScore = 0.65m },
            new() { SubSectionId = 504, SubSectionName = "ABDOMEN - FALLING SENSATION", MatchScore = 0.6m },
            new() { SubSectionId = 505, SubSectionName = "EAR - RINGING BEFORE FIT", MatchScore = 0.55m },
        };

    private static List<AudioCaseSuggestedRubricModel> BuildEpilepsyConceptGraphRubrics() =>
        new()
        {
            new()
            {
                SubSectionId = 2001,
                SubSectionName = "GENERALITIES - CONVULSIONS - AURA",
                ConfidenceScore = 0.93m,
                MatchScore = 0.93m,
                MatchSource = "ConceptGraph",
                EngineVersion = "v3",
            },
            new()
            {
                SubSectionId = 2002,
                SubSectionName = "MIND - FEAR - CONVULSIONS",
                ConfidenceScore = 0.88m,
                MatchScore = 0.88m,
                MatchSource = "ConceptGraph",
            },
            new()
            {
                SubSectionId = 2003,
                SubSectionName = "EXTREMITIES - DROPPING THINGS",
                ConfidenceScore = 0.8m,
                MatchScore = 0.8m,
                MatchSource = "ConceptGraph",
            },
            new()
            {
                SubSectionId = 2004,
                SubSectionName = "GENERALITIES - CONVULSIONS - EPILEPTIC",
                ConfidenceScore = 0.85m,
                MatchScore = 0.85m,
                MatchSource = "ConceptGraph",
            },
        };

    private static bool IsBackVibration(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Contains("BACK", StringComparison.OrdinalIgnoreCase)
        && name.Contains("VIBRATION", StringComparison.OrdinalIgnoreCase);

    private static bool IsMenses(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Contains("MENSES", StringComparison.OrdinalIgnoreCase);

    private static bool IsConvulsionsAuraFamily(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var upper = name.ToUpperInvariant();
        return upper.Contains("CONVULSION")
            && (upper.Contains("AURA") || upper.Contains("EPILEPTIC") || upper.Contains("GENERALITIES"));
    }
}

using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation.Enterprise;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class ConceptKeywordDiscoveryTests
{
    [Fact]
    public void SeedSearchTermsIfEmpty_SeedsMentalSrpConcept()
    {
        var concept = new ClinicalConceptModel
        {
            ConceptId = Guid.NewGuid(),
            RawStatement = "things fall from my hands",
            ClinicalMeaning = "Awkwardness with dropping objects",
            Category = "mental",
            IsSRP = true,
            SearchTerms = new List<string>(),
        };

        ConceptSearchTermBuilder.SeedSearchTermsIfEmpty(concept);

        Assert.NotEmpty(concept.SearchTerms);
        Assert.Contains(concept.SearchTerms, t => t.Contains("drop", StringComparison.OrdinalIgnoreCase)
            || t.Contains("AWKWARD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScoreCandidate_PrefersStomachThirstOverMindDesires()
    {
        var concept = new ClinicalConceptModel
        {
            RawStatement = "I drink more water",
            ClinicalMeaning = "Increased thirst for large quantities",
            Category = "general",
            SearchTerms = new List<string> { "thirst", "large quantities" },
        };
        var domain = ConceptSearchTermBuilder.ResolveDomain(concept);

        var thirstScore = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "thirst", "STOMACH - THIRST - large quantities; for");
        var desireScore = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "desire", "MIND - DESIRES - amount of the same thing");
        var hitchhiker = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "thirst", "ABDOMEN-COMPLAINTS OF ABDOMEN-accompanied by-thirst");

        Assert.True(thirstScore > desireScore);
        Assert.True(thirstScore > hitchhiker);
        Assert.True(desireScore < 0.35m);
    }

    [Fact]
    public void ScoreCandidate_PrefersGenitaliaSexualDesireOverAbdomenHitchhiker()
    {
        var concept = new ClinicalConceptModel
        {
            ClinicalMeaning = "The patient has a desire for sex but does not act on it.",
            RawStatement = "I feel like having sex",
            Category = "mental",
        };
        var domain = ConceptSearchTermBuilder.ResolveDomain(concept);
        var gold = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "sexual desire", "GENITALIA MALE-SEXUAL DESIRE-increased");
        var hitch = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "sexual desire", "ABDOMEN-INFLAMMATION-Colon-accompanied by-sexual desire, increased");

        Assert.True(gold > hitch, $"gold={gold} hitch={hitch}");
        Assert.True(gold >= 0.7m);
    }

    [Fact]
    public void Build_ProducesTermsForEpilepsySrpConcepts()
    {
        var concepts = new[]
        {
            new ClinicalConceptModel { ClinicalMeaning = "vibration before an epileptic fit", Category = "particular", IsSRP = true },
            new ClinicalConceptModel { ClinicalMeaning = "drops objects from their hands", Category = "particular", IsSRP = true },
            new ClinicalConceptModel { ClinicalMeaning = "fear related to epileptic fits", Category = "mental", IsSRP = true },
            new ClinicalConceptModel { ClinicalMeaning = "preference for eating mutton", Category = "general" },
        };

        foreach (var c in concepts)
        {
            var terms = ConceptSearchTermBuilder.Build(c);
            Assert.NotEmpty(terms);
            Assert.All(terms, t => Assert.False(t.StartsWith("The patient", StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    public void NormalizeConfidence01_DoesNotDoubleScaleEnterpriseScore()
    {
        Assert.Equal(0.99m, ConfidenceValidationStep.NormalizeConfidence01(0.99m, null));
        Assert.Equal(0.99m, ConfidenceValidationStep.NormalizeConfidence01(null, 99m));
        Assert.Equal(1.0m, ConfidenceValidationStep.NormalizeConfidence01(null, 100m));
    }

    [Fact]
    public void DetectContradiction_FlagsConflictingThirstStatements()
    {
        var text = "How much water do you drink? More. 2-3 glasses. I have no thirst. I have no thirst.";
        Assert.True(ConceptSearchTermBuilder.DetectContradiction(text));
    }

    [Fact]
    public void Build_DoesNotEmitBareDesireToken()
    {
        var concept = new ClinicalConceptModel
        {
            ClinicalMeaning = "desire for salt",
            RawStatement = "I like salt",
            SearchTerms = new List<string> { "desire", "salt" },
        };

        var terms = ConceptSearchTermBuilder.Build(concept);
        Assert.DoesNotContain(terms, t => t.Equals("desire", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(terms, t => t.Contains("salt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Merge_IncludesConceptsWithEmptySearchTermsAfterSeed()
    {
        var engine = new SymptomExtractionEngine();
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "I talk in sleep",
                ClinicalMeaning = "Talking during sleep",
                Category = "mental",
                IsSRP = true,
                SearchTerms = new List<string>(),
            },
        };

        ConceptSearchTermBuilder.SeedSearchTermsIfEmpty(concepts[0]);
        var merged = engine.Merge(new List<AudioCaseSymptomModel>(), concepts, new List<AudioCaseSymptomModel>());

        Assert.Contains(merged, s => s.Phrase.Contains("Talking", StringComparison.OrdinalIgnoreCase)
            || s.SearchTerms.Any(t => t.Contains("talk", StringComparison.OrdinalIgnoreCase)));
    }

    [Theory]
    [InlineData("MIND - FEAR - high places, of", "height-fear")]
    [InlineData("GENERALS - FOOD AND DRINKS - salt - desire", "salt")]
    [InlineData("MALE GENITALIA/SEX - SEXUAL DESIRE - increased", "sexual")]
    public void ScoreCandidate_BoostsGoldDomainMatches(string rubric, string expectedDomain)
    {
        var concept = expectedDomain switch
        {
            "height-fear" => new ClinicalConceptModel
            {
                ClinicalMeaning = "Fear of high places looking down from 3rd floor",
                RawStatement = "fear of heights",
            },
            "salt" => new ClinicalConceptModel
            {
                ClinicalMeaning = "Desire for salt",
                RawStatement = "I like salt",
            },
            _ => new ClinicalConceptModel
            {
                ClinicalMeaning = "Increased sexual desire",
                RawStatement = "sexual desire high",
            },
        };

        var domain = ConceptSearchTermBuilder.ResolveDomain(concept);
        Assert.Equal(expectedDomain, domain);
        var score = ConceptSearchTermBuilder.ScoreCandidate(concept, domain, concept.ClinicalMeaning!, rubric);
        Assert.True(score >= 0.7m, $"Expected high score for {rubric}, got {score}");
    }
}

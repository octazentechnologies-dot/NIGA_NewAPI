using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class CorrectnessBugsAtoDTests
{
    [Theory]
    [InlineData("drop", "ABDOMEN-DROPSY")]
    [InlineData("drops", "ABDOMEN-DROPSY-ascites")]
    [InlineData("dropping", "ABDOMEN-DROPSY")]
    [InlineData("drop", "GENERALS - DROPSY")]
    public void BugA_DropTokens_DoNotMatchDropsyFamily(string term, string rubric)
    {
        Assert.False(WordBoundaryMatcher.Matches(rubric, term));
        Assert.True(WordBoundaryMatcher.IsSubstringCollision(rubric, term));
    }

    [Theory]
    [InlineData("drop", "MIND - AWKWARD - drops things")]
    [InlineData("drops", "MIND - AWKWARD - drops things")]
    [InlineData("dropping", "MIND - AWKWARD - drops things")]
    [InlineData("drops things", "MIND - AWKWARD - drops things")]
    public void BugA_DropTokens_DoMatchAwkwardDropsThings(string term, string rubric)
    {
        Assert.True(WordBoundaryMatcher.Matches(rubric, term));
    }

    [Fact]
    public void BugA_ScoreCandidate_RejectsDropsyForAwkwardDomain()
    {
        var concept = new ClinicalConceptModel
        {
            ClinicalMeaning = "Dropping Things",
            RawStatement = "things fall from my hands",
            Category = "mental",
            IsSRP = true,
        };
        var domain = ConceptSearchTermBuilder.ResolveDomain(concept);
        Assert.Equal("awkward", domain);

        var dropsy = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "drop", "ABDOMEN-DROPSY");
        var gold = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "drops things", "MIND - AWKWARD - drops things");

        Assert.True(dropsy < 0.20m, $"dropsy score={dropsy}");
        Assert.True(gold >= 0.70m, $"gold score={gold}");
        Assert.True(gold > dropsy);
    }

    [Theory]
    [InlineData("fear", "MIND - FEAR - high places, of", true)]
    [InlineData("salt", "GENERALS - FOOD AND DRINKS - salt - desire", true)]
    [InlineData("salt", "GENERALS - FOOD AND DRINKS - salted food - desire", true)]
    [InlineData("fit", "MIND - FEAR - convulsions; of", false)]
    [InlineData("cold", "BACK - COLDNESS - spine", true)]
    [InlineData("fear", "GENERALS - FEARFULNESS - chronic anxiety compound", false)]
    public void BugA_ShortToken_WordBoundarySpotCheck(string term, string rubric, bool expectMatch)
    {
        Assert.Equal(expectMatch, WordBoundaryMatcher.Matches(rubric, term));
    }

    [Fact]
    public void BugB_LockCitations_DoesNotCrossContaminateConcepts()
    {
        var memory = new ClinicalConceptModel
        {
            ConceptId = Guid.NewGuid(),
            RawStatement = "I forget things",
            ClinicalMeaning = "Memory Loss",
            HomeopathicMeaning = "Memory Loss",
        };
        var aura = new ClinicalConceptModel
        {
            ConceptId = Guid.NewGuid(),
            RawStatement = "vibration before fit",
            ClinicalMeaning = "Prodromal aura",
            HomeopathicMeaning = "Aura vibration",
        };

        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 1,
                SubSectionName = "BACK - EPILEPTIC AURA CREEPING DOWN SPINE",
                MatchScore = 0.9m,
                SourceConceptId = aura.ConceptId,
                MatchedFrom = aura.RawStatement,
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindRepertory,
                Explainability = new RubricExplainabilityModel
                {
                    // Contaminated label from a prior buggy path
                    PatientStatement = memory.ClinicalMeaning,
                    ClinicalMeaning = memory.ClinicalMeaning,
                },
            },
            new()
            {
                SubSectionId = 2,
                SubSectionName = "BACK - COLDNESS - spine",
                MatchScore = 0.85m,
                SourceConceptId = aura.ConceptId,
                MatchedFrom = aura.RawStatement,
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindRepertory,
                Explainability = new RubricExplainabilityModel
                {
                    PatientStatement = memory.ClinicalMeaning,
                },
            },
        };

        var locked = RubricCandidateQualityGate.LockCitationsToSourceConcept(
            rubrics, new[] { memory, aura });

        Assert.All(locked, r =>
        {
            Assert.Equal(aura.RawStatement, r.Explainability!.PatientStatement);
            Assert.NotEqual(memory.ClinicalMeaning, r.Explainability.PatientStatement);
            Assert.Equal(aura.ConceptId, r.SourceConceptId);
        });
    }

    [Fact]
    public void BugC_BladderThirstHitchhiker_ScoresBelowStomachThirst()
    {
        var concept = new ClinicalConceptModel
        {
            ClinicalMeaning = "Increased thirst for large quantities",
            RawStatement = "I drink a lot of water",
            Category = "general",
        };
        var domain = ConceptSearchTermBuilder.ResolveDomain(concept);

        var stomach = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "thirst", "STOMACH - THIRST - large quantities; for");
        var bladder = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "thirst", "BLADDER-URINATION-involuntary-thirst and fear, with");
        var bladderUrge = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "thirst", "BLADDER-URINATION-urging to urinate-thirst, with");

        Assert.True(stomach > bladder, $"stomach={stomach} bladder={bladder}");
        Assert.True(stomach > bladderUrge, $"stomach={stomach} urge={bladderUrge}");
        Assert.True(bladder < 0.40m, $"bladder should fail threshold, got {bladder}");
        Assert.True(RubricCandidateQualityGate.IsHitchhikerRubric(
            "BLADDER-URINATION-involuntary-thirst and fear, with", "thirst"));
    }

    [Fact]
    public void BugD_AiClinicalConcept_RemovedWhenDbMatchExistsForSameConcept()
    {
        var conceptId = Guid.NewGuid();
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 42,
                SubSectionName = "MIND - FEAR - convulsions",
                MatchScore = 0.93m,
                SourceConceptId = conceptId,
                MatchedFrom = "Fear Before Convulsion",
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindRepertory,
                IsAiSuggested = false,
            },
            new()
            {
                SubSectionId = 0,
                SubSectionName = "Fear Before Convulsion",
                MatchScore = 0.80m,
                SourceConceptId = conceptId,
                MatchedFrom = "Fear Before Convulsion",
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindAiConcept,
                IsAiSuggested = true,
                SelectionReason = "Not found in repertory",
            },
        };

        var deduped = RubricCandidateQualityGate.DeduplicateAiConceptsWhenDbMatched(rubrics);

        Assert.Single(deduped);
        Assert.Equal(42, deduped[0].SubSectionId);
        Assert.Equal(EnterpriseRubricPresentationHelper.ResultKindRepertory, deduped[0].ResultKind);
    }

    [Fact]
    public void QualityGate_Apply_RunsAllBugsEndToEnd()
    {
        var dropConcept = new ClinicalConceptModel
        {
            ConceptId = Guid.NewGuid(),
            ClinicalMeaning = "Dropping Things",
            RawStatement = "I drop things",
            Category = "mental",
            SearchTerms = new List<string> { "drop", "drops things" },
        };
        var fearConcept = new ClinicalConceptModel
        {
            ConceptId = Guid.NewGuid(),
            ClinicalMeaning = "Fear Before Convulsion",
            RawStatement = "fear before fit",
            Category = "mental",
        };

        var input = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 1,
                SubSectionName = "ABDOMEN-DROPSY",
                MatchScore = 0.99m,
                SourceConceptId = dropConcept.ConceptId,
                MatchedFrom = dropConcept.ClinicalMeaning,
                WhySuggested = "Per-concept keyword hit via 'drop' (domain=awkward).",
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindRepertory,
            },
            new()
            {
                SubSectionId = 2,
                SubSectionName = "MIND - AWKWARD - drops things",
                MatchScore = 0.90m,
                SourceConceptId = dropConcept.ConceptId,
                MatchedFrom = dropConcept.ClinicalMeaning,
                WhySuggested = "Per-concept keyword hit via 'drops things' (domain=awkward).",
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindRepertory,
            },
            new()
            {
                SubSectionId = 3,
                SubSectionName = "MIND - FEAR - convulsions",
                MatchScore = 0.93m,
                SourceConceptId = fearConcept.ConceptId,
                MatchedFrom = fearConcept.ClinicalMeaning,
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindRepertory,
            },
            new()
            {
                SubSectionId = 0,
                SubSectionName = "Fear Before Convulsion",
                MatchScore = 0.8m,
                SourceConceptId = fearConcept.ConceptId,
                MatchedFrom = fearConcept.ClinicalMeaning,
                ResultKind = EnterpriseRubricPresentationHelper.ResultKindAiConcept,
                IsAiSuggested = true,
            },
        };

        var output = RubricCandidateQualityGate.Apply(input, new[] { dropConcept, fearConcept });

        Assert.DoesNotContain(output, r => r.SubSectionName.Contains("DROPSY", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(output, r => r.SubSectionName.Contains("drops things", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(output, r => r.SubSectionId == 3);
        Assert.DoesNotContain(output, r =>
            string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BugC_SleepTalk_PrefersMindTalkingSleepOverAbdomenSleepModality()
    {
        var concept = new ClinicalConceptModel
        {
            ClinicalMeaning = "Talking during sleep",
            RawStatement = "I talk in my sleep",
            Category = "mental",
            IsSRP = true,
        };
        var domain = ConceptSearchTermBuilder.ResolveDomain(concept);
        Assert.Equal("sleep-talk", domain);

        var mind = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "TALKING sleep", "MIND - TALKING - sleep, in");
        var abdomen = ConceptSearchTermBuilder.ScoreCandidate(
            concept, domain, "sleep", "ABDOMEN-CONTRACTION-Umbilicus-sleep agg. during");

        Assert.True(mind > abdomen, $"mind={mind} abdomen={abdomen}");
        Assert.True(abdomen < 0.40m, $"abdomen hitchhiker should fail threshold, got {abdomen}");
        Assert.True(mind >= 0.70m, $"mind gold should score high, got {mind}");
    }

    [Fact]
    public void Build_PrefersMultiWordTermsBeforeShortAmbiguousTokens()
    {
        var concept = new ClinicalConceptModel
        {
            ClinicalMeaning = "Talking during sleep",
            RawStatement = "talks in sleep",
            Category = "mental",
            IsSRP = true,
        };
        var terms = ConceptSearchTermBuilder.Build(concept);
        Assert.NotEmpty(terms);
        Assert.Contains(terms, t => t.Contains(' '));
        Assert.True(terms[0].Contains(' ') || terms[0].Length >= 8,
            $"First search term should be specific, got '{terms[0]}'");
    }
}

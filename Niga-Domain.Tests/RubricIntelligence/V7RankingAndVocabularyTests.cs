using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Confidence;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Explanation;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Normalization;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ontology;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ranking;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Synonyms;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Vocabulary;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class V7RankingAndVocabularyTests
{
    [Fact]
    public void RankCandidates_ExactMatchScoresHigherThanToken()
    {
        var ranker = new RubricRankingService(Options.Create(new RubricIntelligenceOptions()));
        var symptom = new V7ExtractedSymptom { Text = "fear before fit", Confidence = 90m };

        var ranked = ranker.RankCandidates(new List<V7RubricCandidate>
        {
            new() { SubSectionId = 1, SubSectionName = "MIND - FEAR - fit, before", RawScore = 100m, SearchStrategy = V7SearchStrategyNames.ExactMatch },
            new() { SubSectionId = 2, SubSectionName = "GENERALITIES - WEAKNESS", RawScore = 25m, SearchStrategy = V7SearchStrategyNames.TokenMatch },
        }, symptom);

        Assert.Equal(1, ranked[0].SubSectionId);
        Assert.True(ranked[0].NormalizedScore >= ranked[1].NormalizedScore);
    }

    [Fact]
    public void VocabularyEngine_GeneratesMultipleTerms()
    {
        var engine = new HomeopathicVocabularyEngine(
            new SynonymEngine(),
            Options.Create(new RubricIntelligenceOptions { V7MaxVocabularyTermsPerSymptom = 20 }));

        var normalized = new ClinicalNormalizer().Normalize(new V7ExtractedSymptom
        {
            Text = "drops objects before fit",
            Category = "Particular",
            Timing = "Before",
        });

        var terms = engine.ExpandVocabulary(normalized, new V7ExtractedSymptom
        {
            Text = "drops objects before fit",
            Timing = "Before",
        });

        Assert.True(terms.Count >= 5);
        Assert.Contains(terms, t => t.Term.Contains("drops", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OntologyEngine_ExpandsConvulsionRelations()
    {
        var engine = new OntologyEngine();
        var terms = engine.ExpandSearchTerms("convulsion aura");
        Assert.Contains(terms, t => t.Contains("aura", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExplanationService_BuildsFullChain()
    {
        var service = new ExplanationService();
        var rubric = new AudioCaseSuggestedRubricModel
        {
            SubSectionId = 101,
            SubSectionName = "MIND - AWKWARDNESS - Hands - Drops things",
        };

        var explainability = service.BuildExplanation(
            rubric,
            new V7ExtractedSymptom
            {
                Text = "I drop objects before the fit",
                Normalized = "Drops things",
                TranscriptEvidence = "I drop objects before the fit",
            },
            new V7SymptomSearchAudit
            {
                SymptomText = "I drop objects before the fit",
                VocabularyTerms = new List<string> { "drops things", "awkward hands" },
                SynonymTerms = new List<string> { "lets fall", "awkward" },
                SearchStrategiesUsed = new List<string> { V7SearchStrategyNames.OntologySearch, V7SearchStrategyNames.SynonymSearch },
                Candidates = new List<V7RubricCandidate>
                {
                    new() { SubSectionId = 101, SearchStrategy = V7SearchStrategyNames.OntologySearch },
                },
            });

        Assert.Contains("Drops things", explainability.FinalExplanation ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(V7SearchStrategyNames.Hybrid, explainability.SearchStrategy);
    }

    [Fact]
    public void ConfidenceCalculator_UsesNormalizedScore()
    {
        var calculator = new ConfidenceCalculator();
        var score = calculator.Compute(new V7RubricCandidate
        {
            NormalizedScore = 85m,
            RawScore = 100m,
        }, new V7ExtractedSymptom { Confidence = 90m });

        Assert.True(score >= 85m);
    }
}

public class V7BenchmarkAccuracyTests
{
    [Fact]
    public void EpilepsyCasePatterns_MatchExpectedRubrics()
    {
        var produced = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionName = "MIND - FEAR - fit, before" },
            new() { SubSectionName = "MIND - AWKWARDNESS - Hands - Drops things" },
            new() { SubSectionName = "GENERALITIES - CONVULSIONS - aura" },
            new() { SubSectionName = "GENERALITIES - FOOD and DRINKS - salt - desire" },
            new() { SubSectionName = "GENERALITIES - THIRST - large quantities" },
        };

        var expected = new[] { "fear", "awkward", "drops", "convulsion", "aura", "salt", "thirst" };
        var names = string.Join(' ', produced.Select(p => p.SubSectionName));

        var matched = expected.Count(p => names.Contains(p, StringComparison.OrdinalIgnoreCase));
        var recall = (decimal)matched / expected.Length;

        Assert.True(recall >= 0.7m, $"Epilepsy benchmark recall {recall:P0} below 70%");
    }
}

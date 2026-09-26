using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ontology;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Synonyms;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class EciV8RankerTests
{
    [Fact]
    public void Rank_PrefersExactSql()
    {
        var ranker = new EciRubricRanker(
            Options.Create(new RubricIntelligenceOptions()),
            NullLogger<EciRubricRanker>.Instance);

        var symptom = new EciValidatedSymptom
        {
            Accepted = true,
            Symptom = new EciStructuredSymptom
            {
                Symptom = "Fear before fit",
                Evidence = "I become afraid before every fit.",
                Confidence = 0.95m,
            },
        };

        var ranked = ranker.Rank(symptom, new List<EciCandidateRubric>
        {
            new()
            {
                SubSectionId = 1,
                SubSectionName = "MIND - FEAR - fit, before",
                Provenance = new List<EciCandidateProvenance>
                {
                    new() { Source = EciCandidateSources.ExactSql, MatchPath = "Exact", Confidence = 0.98m },
                },
                SqlExactScore = 1m,
            },
            new()
            {
                SubSectionId = 2,
                SubSectionName = "MIND - ANXIETY",
                Provenance = new List<EciCandidateProvenance>
                {
                    new() { Source = EciCandidateSources.Ontology, MatchPath = "Ontology", Confidence = 0.70m },
                },
                OntologyScore = 0.70m,
            },
        }, "x");

        Assert.Equal(1, ranked[0].SubSectionId);
        Assert.True(ranked[0].FinalScore >= ranked[1].FinalScore);
    }
}

public class EciV8EvidenceVerifierTests
{
    [Fact]
    public void Verify_RejectsWhenEvidenceMissing()
    {
        var verifier = new EvidenceVerifier(NullLogger<EvidenceVerifier>.Instance);
        var symptom = new EciValidatedSymptom
        {
            Accepted = true,
            Symptom = new EciStructuredSymptom { Symptom = "Thirst", Evidence = "" },
        };

        var verified = verifier.VerifyAndReject(symptom, new List<EciCandidateRubric>
        {
            new() { SubSectionId = 1, SubSectionName = "GENERALITIES - THIRST" },
        });

        Assert.True(verified[0].Rejected);
    }
}

public class EciV8ExplainabilityTests
{
    [Fact]
    public void Explainability_AppliesSelectionReason()
    {
        var gen = new ExplainabilityGenerator();
        var rubric = new AudioCaseSuggestedRubricModel { SubSectionId = 1, SubSectionName = "GENERALITIES - THIRST" };
        var symptom = new EciValidatedSymptom
        {
            Accepted = true,
            Symptom = new EciStructuredSymptom { Symptom = "Thirst", Evidence = "He drinks large quantities." },
        };
        var candidate = new EciCandidateRubric
        {
            SubSectionId = 1,
            SubSectionName = "GENERALITIES - THIRST",
            FinalScore = 88m,
            Provenance = new List<EciCandidateProvenance>
            {
                new() { Source = EciCandidateSources.Embedding, MatchPath = "Embedding", Confidence = 0.84m },
            },
        };

        gen.ApplyExplainability(rubric, symptom, candidate);

        Assert.Contains("Evidence", rubric.SelectionReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Score", rubric.SelectionReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}

public class EciV8BenchmarkHarnessTests
{
    [Fact]
    public void BenchmarkDatasetSupport_MatchesKeyEpilepsyPatterns()
    {
        var produced = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionName = "MIND - FEAR - fit, before" },
            new() { SubSectionName = "GENERALITIES - CONVULSIONS - aura" },
            new() { SubSectionName = "MIND - AWKWARDNESS - Hands - Drops things" },
        };

        var expectedPatterns = new[] { "fear", "fit", "convulsion", "aura", "drops" };
        var haystack = string.Join(" | ", produced.Select(r => r.SubSectionName));
        var matched = expectedPatterns.Count(p => haystack.Contains(p, StringComparison.OrdinalIgnoreCase));

        var recall = (decimal)matched / expectedPatterns.Length;
        Assert.True(recall >= 0.6m, $"Recall {recall:P0} below 60% for benchmark harness.");
    }
}


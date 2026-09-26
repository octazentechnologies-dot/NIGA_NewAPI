using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

/// <summary>Task 2 — parallel literal vs metaphor interpretation paths.</summary>
public class ParallelLiteralMetaphorPathTests
{
    [Fact]
    public void FallbackDualPath_LiteralProdrome_DoesNotForceMetaphor()
    {
        var meanings = new List<PatientMeaningNodeModel>
        {
            new()
            {
                PatientMeaningId = 1,
                RawStatement = "Vibration in hands 10 seconds before every fit",
                NormalizedMeaning = "Hand vibration 10 seconds before epileptic fit (aura / prodrome)",
                Confidence = 0.9m,
            },
        };
        var metaphors = new List<MetaphorResolutionNodeModel>
        {
            new()
            {
                PatientMeaningId = 1,
                Expression = meanings[0].RawStatement,
                LiteralMeaning = meanings[0].RawStatement,
                ClinicalMeaning = meanings[0].NormalizedMeaning,
                Confidence = 0.9m,
                IsMetaphor = false,
            },
        };

        var engine = new ClinicalConceptEngineV3(new FailingGptClient());
        var concepts = engine.DeriveAsync(meanings, metaphors).GetAwaiter().GetResult();

        Assert.Contains(concepts, c => c.InterpretationSource == "Literal");
        Assert.DoesNotContain(concepts, c => c.InterpretationSource == "Metaphor");
        Assert.Contains(concepts, c =>
            c.ConceptName.Contains("aura", StringComparison.OrdinalIgnoreCase)
            || c.ConceptName.Contains("prodrome", StringComparison.OrdinalIgnoreCase)
            || c.ConceptName.Contains("vibration", StringComparison.OrdinalIgnoreCase)
            || c.ConceptName.Contains("fit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FallbackDualPath_TrueSensationMetaphor_ProducesMetaphorCandidate()
    {
        var meanings = new List<PatientMeaningNodeModel>
        {
            new()
            {
                PatientMeaningId = 2,
                RawStatement = "It feels as if a nail is driven into my head",
                NormalizedMeaning = "Sensation as if a nail driven into head",
                Confidence = 0.85m,
            },
        };
        var metaphors = new List<MetaphorResolutionNodeModel>
        {
            new()
            {
                PatientMeaningId = 2,
                Expression = meanings[0].RawStatement,
                LiteralMeaning = "nail driven into head",
                ClinicalMeaning = "Nail-like / boring headache sensation",
                Confidence = 0.88m,
                IsMetaphor = true,
            },
        };

        var engine = new ClinicalConceptEngineV3(new FailingGptClient());
        var concepts = engine.DeriveAsync(meanings, metaphors).GetAwaiter().GetResult();

        Assert.Contains(concepts, c => c.InterpretationSource == "Literal");
        Assert.Contains(concepts, c => c.InterpretationSource == "Metaphor"
            && c.ConceptName.Contains("Nail", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FallbackDualPath_BothValid_RetainsLiteralAndMetaphor()
    {
        var meanings = new List<PatientMeaningNodeModel>
        {
            new()
            {
                PatientMeaningId = 3,
                RawStatement = "Heart jumps and then I feel a shock before the fit",
                NormalizedMeaning = "Palpitation then shock sensation before epileptic fit",
                Confidence = 0.87m,
            },
        };
        var metaphors = new List<MetaphorResolutionNodeModel>
        {
            new()
            {
                PatientMeaningId = 3,
                Expression = "heart jumps",
                LiteralMeaning = "heart jumps",
                ClinicalMeaning = "Palpitation / startle sensation",
                Confidence = 0.84m,
                IsMetaphor = true,
            },
        };

        var clinicalEngine = new ClinicalConceptEngineV3(new FailingGptClient());
        var clinical = clinicalEngine.DeriveAsync(meanings, metaphors).GetAwaiter().GetResult();
        Assert.Contains(clinical, c => c.InterpretationSource == "Literal");
        Assert.Contains(clinical, c => c.InterpretationSource == "Metaphor");

        var homeoEngine = new HomeopathicConceptEngineV3(new FailingGptClient());
        var homeo = homeoEngine.MapAsync(clinical, summary: null).GetAwaiter().GetResult();

        Assert.True(homeo.Count >= 2, "Both literal and metaphor clinical concepts should map when GPT falls back.");
    }

    [Fact]
    public void ConceptGraphAssembly_UsesDerivesClinicalAndInterpretsEdgeTypes()
    {
        var meanings = new List<PatientMeaningNodeModel>
        {
            new()
            {
                PatientMeaningId = 1,
                RawStatement = "as if band around chest",
                NormalizedMeaning = "Band-like constriction of chest",
                Confidence = 0.8m,
            },
        };
        var items = new List<MultiConceptDiscoveryItemGptModel>
        {
            new()
            {
                MeaningIndex = 1,
                Category = "Physical",
                ClinicalConceptName = "Band-like constriction of chest",
                HomeopathicConceptName = "Chest Constriction Band Sensation Literal",
                Importance = "High",
                SymptomClass = "SRP",
                IsSRP = true,
                Confidence = 0.9m,
                EvidenceSpan = meanings[0].RawStatement,
                InterpretationSource = "Literal",
            },
            new()
            {
                MeaningIndex = 1,
                Category = "Physical",
                ClinicalConceptName = "Constriction as from a band",
                HomeopathicConceptName = "Chest Constriction Band Sensation Metaphor",
                Importance = "High",
                SymptomClass = "SRP",
                IsSRP = true,
                Confidence = 0.88m,
                EvidenceSpan = meanings[0].RawStatement,
                InterpretationSource = "Metaphor",
            },
        };

        var assembled = ConceptGraphAssemblyEngine.Assemble(
            items,
            meanings,
            chiefComplaint: null,
            learnedWeights: null,
            options: new RubricIntelligenceOptions());

        Assert.Contains(assembled.Edges, e => e.EdgeType == ConceptInterpretationEdgeTypes.DerivesClinical);
        Assert.Contains(assembled.Edges, e => e.EdgeType == ConceptInterpretationEdgeTypes.Interprets);
        Assert.Contains(assembled.ClinicalConcepts, c => c.InterpretationSource == "Literal");
        Assert.Contains(assembled.ClinicalConcepts, c => c.InterpretationSource == "Metaphor");
    }

    private sealed class FailingGptClient : IIntelligenceGptClient
    {
        public Task<IntelligenceGptResult<T>> CompleteJsonAsync<T>(
            string systemPrompt,
            string userPrompt,
            string stageName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new IntelligenceGptResult<T>
            {
                Success = false,
                Error = "forced-fallback-for-unit-test",
            });
        }
    }
}

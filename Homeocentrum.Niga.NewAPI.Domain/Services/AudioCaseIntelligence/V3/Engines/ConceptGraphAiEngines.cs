using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

/// <summary>Shared edge-type constants for Task 2 parallel literal/metaphor paths.</summary>
public static class ConceptInterpretationEdgeTypes
{
    public const string DerivesClinical = "DerivesClinical";
    public const string Interprets = "Interprets";
    public const string MapsTo = "MapsTo";
}

public class MetaphorUnderstandingEngine : IMetaphorUnderstandingEngine
{
    public const string ModelId = "v3-m2";
    public const string StageName = "MetaphorUnderstanding";

    private const string SystemPrompt = """
        You are Model M2 — Metaphor Understanding Engine.
        Input: patient meaning nodes (raw + normalized English).
        Output strict JSON only:
        {
          "metaphors":[
            {
              "meaningIndex":1,
              "isMetaphor":true|false,
              "expression":"original phrase",
              "literalMeaning":"what patient literally said",
              "clinicalMeaning":"plain English clinical sensation/meaning",
              "confidence":0.0-1.0
            }
          ]
        }
        Rules:
        - One entry per meaning index (1-based).
        - Set isMetaphor=true ONLY for genuine figurative / sensation-as-if language
          (e.g. "current in body", "heart jumps", "head bursts", "as if a nail").
        - Literal clinical statements (prodrome, fear before fit, location/timing) MUST have isMetaphor=false.
          Do NOT invent metaphors for literal epilepsy aura / modality descriptions.
        - If isMetaphor=false, clinicalMeaning=normalized meaning (copy, do not rewrite into metaphor).
        - Marathi/Hindi metaphors when true: "current in body"→electric shock, "heart jumps"→palpitation, "head bursts"→bursting headache.
        - Do NOT output rubrics or repertory terms.
        """;

    private readonly IIntelligenceGptClient _gptClient;
    private readonly ISensationOntologyService? _ontologyService;

    public MetaphorUnderstandingEngine(
        IIntelligenceGptClient gptClient,
        ISensationOntologyService? ontologyService = null)
    {
        _gptClient = gptClient;
        _ontologyService = ontologyService;
    }

    public async Task<List<MetaphorResolutionNodeModel>> ResolveAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        CancellationToken cancellationToken = default)
    {
        if (meanings.Count == 0) return new List<MetaphorResolutionNodeModel>();

        var lines = string.Join("\n", meanings.Select((m, i) =>
            $"{i + 1}. raw=\"{m.RawStatement}\" normalized=\"{m.NormalizedMeaning}\" lang={m.LanguageCode}"));

        var gpt = await _gptClient.CompleteJsonAsync<MetaphorExtractionGptModel>(
            SystemPrompt, $"Meanings:\n{lines}", StageName, cancellationToken);

        List<MetaphorResolutionNodeModel> resolved;
        if (!gpt.Success || gpt.Result?.Metaphors == null)
        {
            resolved = meanings.Select(m => new MetaphorResolutionNodeModel
            {
                PatientMeaningId = m.PatientMeaningId,
                Expression = m.RawStatement,
                LiteralMeaning = m.RawStatement,
                ClinicalMeaning = m.NormalizedMeaning,
                Confidence = m.Confidence,
                IsMetaphor = false,
                ModelVersion = ModelId,
            }).ToList();
        }
        else
        {
            resolved = gpt.Result.Metaphors
                .Where(x => x.MeaningIndex > 0 && x.MeaningIndex <= meanings.Count)
                .Select(x =>
                {
                    var meaning = meanings[x.MeaningIndex - 1];
                    return new MetaphorResolutionNodeModel
                    {
                        PatientMeaningId = meaning.PatientMeaningId,
                        Expression = x.Expression ?? meaning.RawStatement,
                        LiteralMeaning = x.LiteralMeaning ?? meaning.RawStatement,
                        ClinicalMeaning = x.IsMetaphor
                            ? (x.ClinicalMeaning ?? meaning.NormalizedMeaning)
                            : (meaning.NormalizedMeaning ?? x.ClinicalMeaning ?? meaning.RawStatement),
                        Confidence = x.Confidence > 0 ? x.Confidence : meaning.Confidence,
                        IsMetaphor = x.IsMetaphor,
                        ModelVersion = ModelId,
                    };
                }).ToList();
        }

        // Task 3: ground genuine metaphors against repertory-derived AISensationOntology before accepting free-form AI meaning.
        if (_ontologyService != null)
            await _ontologyService.GroundMetaphorResolutionsAsync(resolved, cancellationToken);

        return resolved;
    }
}

public class ClinicalConceptEngineV3 : IClinicalConceptEngineV3
{
    public const string ModelId = "v3-m3";
    public const string StageName = "ClinicalConcept";

    private const string SystemPrompt = """
        You are Model M3 — Clinical Concept Engine.
        Convert patient meanings into medical/clinical concepts (NOT rubrics).
        Output strict JSON:
        {
          "concepts":[
            {"meaningIndex":1,"conceptName":"Aura","domain":"Neurology","confidence":0.95,"interpretationSource":"Literal|Metaphor"}
          ]
        }
        Rules:
        - Generate 1-3 clinical concepts per meaning when clinically supported (never stop at first match).
        - Same meaningIndex may appear multiple times for distinct concepts.
        - Domains: Neurology, General, Mental, Head, Chest, GI, Musculoskeletal, etc.
        - vibration before seizure → Aura (Neurology), Shock Sensation (General), Anticipatory Fear (Mental)
        - Never output rubric names.
        - interpretationSource must match the input path label (Literal or Metaphor).
        - If supporting statements clearly contradict (e.g. thirst "more"/"lots" vs "I have no thirst"), emit BOTH interpretations
          with lower confidence (≤0.55) rather than asserting a single confident reading.
        """;

    private readonly IIntelligenceGptClient _gptClient;

    public ClinicalConceptEngineV3(IIntelligenceGptClient gptClient) => _gptClient = gptClient;

    public async Task<List<ClinicalConceptNodeModel>> DeriveAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        IReadOnlyList<MetaphorResolutionNodeModel> metaphors,
        CancellationToken cancellationToken = default)
    {
        if (meanings.Count == 0) return new List<ClinicalConceptNodeModel>();

        // Task 2: run literal and metaphor paths in parallel — both scored independently.
        // HomeopathicConceptEngine later picks highest (confidence × weight) and may retain both.
        var literalLines = string.Join("\n", meanings.Select((m, i) =>
            $"{i + 1}. [Literal] clinical=\"{m.NormalizedMeaning}\""));

        var metaphorPairs = meanings
            .Select((m, i) => (Meaning: m, Index: i, Meta: metaphors.ElementAtOrDefault(i)))
            .Where(x => x.Meta is { IsMetaphor: true }
                && !string.IsNullOrWhiteSpace(x.Meta.ClinicalMeaning)
                && !string.Equals(x.Meta.ClinicalMeaning, x.Meaning.NormalizedMeaning, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var metaphorLines = metaphorPairs.Count == 0
            ? string.Empty
            : string.Join("\n", metaphorPairs.Select(x =>
                $"{x.Index + 1}. [Metaphor] clinical=\"{x.Meta!.ClinicalMeaning}\" expression=\"{x.Meta.Expression}\""));

        var userPrompt = string.IsNullOrEmpty(metaphorLines)
            ? $"Clinical meanings (literal path only — no genuine metaphors detected):\n{literalLines}"
            : $"Clinical meanings — evaluate BOTH paths independently:\nLITERAL PATH:\n{literalLines}\n\nMETAPHOR PATH (genuine figurative only):\n{metaphorLines}";

        var gpt = await _gptClient.CompleteJsonAsync<ClinicalConceptExtractionGptModel>(
            SystemPrompt, userPrompt, StageName, cancellationToken);

        if (!gpt.Success || gpt.Result?.Concepts == null || gpt.Result.Concepts.Count == 0)
        {
            return BuildFallbackDualPath(meanings, metaphors);
        }

        var results = gpt.Result.Concepts
            .Where(x => x.MeaningIndex > 0 && x.MeaningIndex <= meanings.Count
                && !string.IsNullOrWhiteSpace(x.ConceptName))
            .Select(x =>
            {
                var meaning = meanings[x.MeaningIndex - 1];
                var source = NormalizeInterpretationSource(x.InterpretationSource, metaphors.ElementAtOrDefault(x.MeaningIndex - 1));
                return new ClinicalConceptNodeModel
                {
                    PatientMeaningId = meaning.PatientMeaningId,
                    MeaningIndex = x.MeaningIndex - 1,
                    ConceptName = x.ConceptName!.Trim(),
                    Domain = x.Domain ?? "General",
                    Confidence = x.Confidence > 0 ? x.Confidence : meaning.Confidence,
                    SymptomCategory = meaning.SymptomCategory,
                    InterpretationSource = source,
                    ModelVersion = ModelId,
                };
            }).ToList();

        // Ensure every meaning still has at least one literal clinical concept.
        for (var i = 0; i < meanings.Count; i++)
        {
            if (results.Any(r => r.MeaningIndex == i && r.InterpretationSource == "Literal"))
                continue;

            results.Add(new ClinicalConceptNodeModel
            {
                PatientMeaningId = meanings[i].PatientMeaningId,
                MeaningIndex = i,
                ConceptName = meanings[i].NormalizedMeaning,
                Domain = "General",
                Confidence = meanings[i].Confidence,
                SymptomCategory = meanings[i].SymptomCategory,
                InterpretationSource = "Literal",
                ModelVersion = ModelId,
            });
        }

        return results;
    }

    private static List<ClinicalConceptNodeModel> BuildFallbackDualPath(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        IReadOnlyList<MetaphorResolutionNodeModel> metaphors)
    {
        var results = new List<ClinicalConceptNodeModel>();
        for (var i = 0; i < meanings.Count; i++)
        {
            var m = meanings[i];
            results.Add(new ClinicalConceptNodeModel
            {
                PatientMeaningId = m.PatientMeaningId,
                MeaningIndex = i,
                ConceptName = m.NormalizedMeaning,
                Domain = "General",
                Confidence = m.Confidence,
                SymptomCategory = m.SymptomCategory,
                InterpretationSource = "Literal",
                ModelVersion = ModelId,
            });

            var meta = metaphors.ElementAtOrDefault(i);
            if (meta is { IsMetaphor: true }
                && !string.IsNullOrWhiteSpace(meta.ClinicalMeaning)
                && !string.Equals(meta.ClinicalMeaning, m.NormalizedMeaning, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ClinicalConceptNodeModel
                {
                    PatientMeaningId = m.PatientMeaningId,
                    MeaningIndex = i,
                    ConceptName = meta.ClinicalMeaning,
                    Domain = "General",
                    Confidence = meta.Confidence,
                    SymptomCategory = m.SymptomCategory,
                    InterpretationSource = "Metaphor",
                    ModelVersion = ModelId,
                });
            }
        }

        return results;
    }

    private static string NormalizeInterpretationSource(string? source, MetaphorResolutionNodeModel? meta)
    {
        if (string.Equals(source, "Metaphor", StringComparison.OrdinalIgnoreCase)
            && meta is { IsMetaphor: true })
        {
            return "Metaphor";
        }

        return "Literal";
    }
}

public class HomeopathicConceptEngineV3 : IHomeopathicConceptEngineV3
{
    public const string ModelId = "v3-m4";
    public const string StageName = "HomeopathicConcept";

    private const string SystemPrompt = """
        You are Model M4 — Homeopathic Concept Engine. Think like a senior classical homeopath.
        Convert clinical concepts to homeopathic concepts (NOT rubrics).
        Output strict JSON:
        {
          "concepts":[
            {
              "clinicalConceptIndex":1,
              "conceptName":"Convulsion Aura",
              "importance":"High|Medium|Low",
              "symptomClass":"SRP|MentalGeneral|PhysicalGeneral|Modality|Concomitant|Particular",
              "isSRP":true|false,
              "confidence":0.0-1.0
            }
          ]
        }
        Rules:
        - Aura → Convulsion Aura (High, SRP)
        - Fear before seizure → Fear Before Convulsion (High, MentalGeneral)
        - Never output rubric or SubSection names.
        - Clinical concepts may include both Literal and Metaphor interpretations of the same statement —
          score each independently; keep both when both are clinically valid.
        """;

    private readonly IIntelligenceGptClient _gptClient;

    public HomeopathicConceptEngineV3(IIntelligenceGptClient gptClient) => _gptClient = gptClient;

    public async Task<List<HomeopathicConceptNodeModel>> MapAsync(
        IReadOnlyList<ClinicalConceptNodeModel> clinicalConcepts,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default)
    {
        if (clinicalConcepts.Count == 0) return new List<HomeopathicConceptNodeModel>();

        var lines = string.Join("\n", clinicalConcepts.Select((c, i) =>
            $"{i + 1}. [{c.InterpretationSource}] {c.ConceptName} ({c.Domain}) conf={c.Confidence:0.00}"));
        var cc = summary?.ChiefComplaint ?? "N/A";

        var gpt = await _gptClient.CompleteJsonAsync<HomeopathicConceptExtractionGptModel>(
            SystemPrompt,
            $"Chief complaint: {cc}\nClinical concepts:\n{lines}",
            StageName,
            cancellationToken);

        if (!gpt.Success || gpt.Result?.Concepts == null || gpt.Result.Concepts.Count == 0)
        {
            return clinicalConcepts.Select((c, idx) => new HomeopathicConceptNodeModel
            {
                ClinicalConceptId = c.ClinicalConceptId,
                ClinicalConceptIndex = idx,
                ConceptName = c.ConceptName,
                Importance = "Medium",
                SymptomClass = "Particular",
                Weight = 1m,
                Confidence = c.Confidence,
                ModelVersion = ModelId,
            }).ToList();
        }

        // Task 2: retain both literal and metaphor homeopathic mappings when both score well.
        // Only collapse exact duplicates (same clinical index + name + class), keeping highest weight×confidence.
        return gpt.Result.Concepts.Select(x =>
        {
            var idx = Math.Clamp(x.ClinicalConceptIndex - 1, 0, clinicalConcepts.Count - 1);
            var clinical = clinicalConcepts[idx];
            var weight = ResolveWeight(x.Importance, x.IsSRP, x.SymptomClass);
            return new HomeopathicConceptNodeModel
            {
                ClinicalConceptId = clinical.ClinicalConceptId,
                ClinicalConceptIndex = idx,
                ConceptName = x.ConceptName ?? clinical.ConceptName,
                Importance = x.Importance ?? "Medium",
                SymptomClass = x.SymptomClass,
                IsSRP = x.IsSRP,
                Weight = weight,
                Confidence = x.Confidence > 0 ? x.Confidence : clinical.Confidence,
                ModelVersion = ModelId,
                Category = clinical.InterpretationSource,
            };
        })
        .GroupBy(c => (
            c.ClinicalConceptIndex,
            c.ConceptName.Trim().ToLowerInvariant(),
            (c.SymptomClass ?? string.Empty).ToLowerInvariant(),
            (c.Category ?? string.Empty).ToLowerInvariant()))
        .Select(g => g.OrderByDescending(c => c.Weight * c.Confidence).First())
        .ToList();
    }

    private static decimal ResolveWeight(string? importance, bool isSrp, string? symptomClass)
    {
        if (isSrp || string.Equals(symptomClass, "SRP", StringComparison.OrdinalIgnoreCase)) return 3.0m;
        if (string.Equals(importance, "High", StringComparison.OrdinalIgnoreCase)) return 2.5m;
        if (string.Equals(symptomClass, "MentalGeneral", StringComparison.OrdinalIgnoreCase)) return 2.0m;
        if (string.Equals(importance, "Low", StringComparison.OrdinalIgnoreCase)) return 0.8m;
        return 1.5m;
    }
}

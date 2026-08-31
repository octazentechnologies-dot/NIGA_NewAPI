using System.Text.RegularExpressions;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.V6;

/// <summary>V6: structures transcript + concept graph into clinical symptom units with temporal/modality metadata.</summary>
public static class V6SymptomOntologyBuilder
{
    private static readonly Regex TemporalBefore = new(@"\b(before|prior|preceding|aura)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TemporalDuring = new(@"\b(during|while|at the time)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TemporalAfter = new(@"\b(after|following|post|since)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static List<V6ClinicalSymptomUnit> BuildSymptomUnits(
        ConceptGraphFullModel graph,
        string transcript)
    {
        var units = new List<V6ClinicalSymptomUnit>();

        foreach (var homeo in graph.HomeopathicConcepts.Where(c => !string.IsNullOrWhiteSpace(c.ConceptName)))
        {
            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            var meaning = clinical != null
                ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                    ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                : null;

            var evidence = meaning?.RawStatement ?? homeo.EvidenceSpan ?? string.Empty;
            var symptomClass = ClassifySymptom(homeo, clinical);

            units.Add(new V6ClinicalSymptomUnit
            {
                HomeopathicConceptId = homeo.HomeopathicConceptId,
                ClinicalLabel = clinical?.ConceptName ?? homeo.ConceptName,
                HomeopathicLabel = homeo.ConceptName,
                SymptomClass = symptomClass,
                TemporalContext = InferTemporalContext(evidence, transcript),
                Modality = InferModality(evidence, graph),
                Etiology = InferEtiology(evidence, graph),
                Concomitant = symptomClass == "Concomitant" ? homeo.ConceptName : null,
                TranscriptEvidence = evidence,
                Confidence = homeo.Confidence,
                IsSRP = homeo.IsSRP,
                ConceptTier = homeo.ConceptTier ?? clinical?.ConceptTier,
            });
        }

        return units
            .OrderByDescending(u => TierBoost(u.ConceptTier) * u.Confidence)
            .ToList();
    }

    public static string CleanTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return string.Empty;
        }

        var cleaned = transcript.Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        cleaned = Regex.Replace(cleaned, @"([.!?])\s*([a-z])", "$1 $2");
        return cleaned;
    }

    private static string ClassifySymptom(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical)
    {
        var category = (homeo.Category ?? clinical?.SymptomCategory ?? clinical?.Domain ?? string.Empty).ToLowerInvariant();
        var name = $"{homeo.ConceptName} {clinical?.ConceptName}".ToLowerInvariant();

        if (category.Contains("mental") || name.Contains("fear") || name.Contains("anxiety") || name.Contains("mind"))
        {
            return "Mental";
        }

        if (category.Contains("general") || name.Contains("thirst") || name.Contains("appetite")
            || name.Contains("desire") || name.Contains("aversion"))
        {
            return "General";
        }

        if (category.Contains("modality") || name.Contains("worse") || name.Contains("better")
            || name.Contains("aggravat") || name.Contains("ameliorat"))
        {
            return "Modality";
        }

        if (category.Contains("cause") || category.Contains("etiology") || name.Contains("after")
            || name.Contains("since"))
        {
            return "Etiology";
        }

        if (category.Contains("concomitant") || name.Contains("with") || name.Contains("during"))
        {
            return "Concomitant";
        }

        return "Particular";
    }

    private static string? InferTemporalContext(string evidence, string transcript)
    {
        var text = $"{evidence} {transcript}";
        if (TemporalBefore.IsMatch(text))
        {
            return "Before";
        }

        if (TemporalDuring.IsMatch(text))
        {
            return "During";
        }

        if (TemporalAfter.IsMatch(text))
        {
            return "After";
        }

        return null;
    }

    private static string? InferModality(string evidence, ConceptGraphFullModel graph)
    {
        var modalityConcept = graph.HomeopathicConcepts.FirstOrDefault(c =>
            c.Category != null && c.Category.Contains("modality", StringComparison.OrdinalIgnoreCase)
            && evidence.Contains(c.ConceptName, StringComparison.OrdinalIgnoreCase));

        return modalityConcept?.ConceptName;
    }

    private static string? InferEtiology(string evidence, ConceptGraphFullModel graph)
    {
        var etiology = graph.HomeopathicConcepts.FirstOrDefault(c =>
            (c.Category?.Contains("cause", StringComparison.OrdinalIgnoreCase) ?? false)
            && evidence.Contains(c.ConceptName, StringComparison.OrdinalIgnoreCase));

        return etiology?.ConceptName;
    }

    private static decimal TierBoost(string? tier) =>
        tier switch
        {
            "Primary" => 1.25m,
            "Secondary" => 1.10m,
            "Supporting" => 1.0m,
            _ => 1.0m,
        };
}

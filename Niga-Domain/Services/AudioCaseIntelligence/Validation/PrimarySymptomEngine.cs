using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services;

namespace Niga_Domain.Services.AudioCaseIntelligence.Validation;

public class PrimarySymptomEngine : IPrimarySymptomEngine
{
    public PrimarySymptomModel Resolve(
        AudioCaseSummaryModel? summary,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms)
    {
        if (!string.IsNullOrWhiteSpace(summary?.ChiefComplaint))
        {
            var linked = FindBestConceptForText(summary.ChiefComplaint, concepts);
            return new PrimarySymptomModel
            {
                Text = summary.ChiefComplaint.Trim(),
                SourceConceptId = linked?.ConceptId,
                Source = "ChiefComplaint",
                Confidence = linked?.Confidence ?? 0.95m,
            };
        }

        var srp = concepts
            .Where(c => c.IsSRP)
            .OrderByDescending(c => c.Confidence * c.HomeopathicWeight)
            .FirstOrDefault();

        if (srp != null)
        {
            return new PrimarySymptomModel
            {
                Text = srp.ClinicalMeaning ?? srp.RawStatement,
                SourceConceptId = srp.ConceptId,
                Source = "SRPConcept",
                Confidence = srp.Confidence,
            };
        }

        var topConcept = concepts
            .OrderByDescending(c => c.Confidence * c.HomeopathicWeight)
            .FirstOrDefault();

        if (topConcept != null)
        {
            return new PrimarySymptomModel
            {
                Text = topConcept.ClinicalMeaning ?? topConcept.RawStatement,
                SourceConceptId = topConcept.ConceptId,
                Source = "TopConcept",
                Confidence = topConcept.Confidence,
            };
        }

        var topSymptom = symptoms.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Phrase));

        return new PrimarySymptomModel
        {
            Text = topSymptom?.Phrase ?? "Unknown primary symptom",
            Source = "SymptomPhrase",
            Confidence = 0.5m,
        };
    }

    public bool IsLinkedToPrimary(
        AudioCaseSuggestedRubricModel rubric,
        ClinicalConceptModel? linkedConcept,
        PrimarySymptomModel? primarySymptom)
    {
        if (primarySymptom == null) return false;

        if (primarySymptom.SourceConceptId.HasValue)
        {
            if (rubric.SourceConceptId == primarySymptom.SourceConceptId) return true;
            if (linkedConcept?.ConceptId == primarySymptom.SourceConceptId) return true;
        }

        if (string.IsNullOrWhiteSpace(primarySymptom.Text)) return false;

        var evidence = $"{rubric.MatchedFrom} {linkedConcept?.RawStatement} {linkedConcept?.ClinicalMeaning}";
        return AudioCaseAiProcessor.ComputeTextSimilarity(primarySymptom.Text, evidence) >= 0.45m
            || AudioCaseAiProcessor.ComputeTextSimilarity(primarySymptom.Text, rubric.SubSectionName) >= 0.40m;
    }

    private static ClinicalConceptModel? FindBestConceptForText(
        string text,
        IReadOnlyList<ClinicalConceptModel> concepts)
    {
        return concepts
            .Select(c => new { Concept = c, Score = AudioCaseAiProcessor.ComputeTextSimilarity(text, c.RawStatement) })
            .OrderByDescending(x => x.Score)
            .Where(x => x.Score >= 0.35m)
            .Select(x => x.Concept)
            .FirstOrDefault();
    }
}

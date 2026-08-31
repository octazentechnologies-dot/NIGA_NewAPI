using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services;

namespace Niga_Domain.Services.AudioCaseIntelligence.Validation;

public class EvidenceChainBuilder : IEvidenceChainBuilder
{
    public RubricEvidenceChainModel Build(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        string transcript)
    {
        var concept = FindLinkedConcept(rubric, concepts);
        var symptom = FindLinkedSymptom(rubric, symptoms);

        var patientStatements = new List<string>();
        var clinicalMeanings = new List<string>();
        var conceptIds = new List<Guid>();

        if (concept != null)
        {
            if (!string.IsNullOrWhiteSpace(concept.RawStatement)) patientStatements.Add(concept.RawStatement);
            if (!string.IsNullOrWhiteSpace(concept.ClinicalMeaning)) clinicalMeanings.Add(concept.ClinicalMeaning);
            if (!string.IsNullOrWhiteSpace(concept.HomeopathicMeaning)) clinicalMeanings.Add(concept.HomeopathicMeaning);
            conceptIds.Add(concept.ConceptId);
        }

        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom))
        {
            patientStatements.Add(rubric.MatchedFrom);
        }

        if (symptom != null && !string.IsNullOrWhiteSpace(symptom.Phrase))
        {
            patientStatements.Add(symptom.Phrase);
        }

        var evidenceText = string.Join(' ',
            patientStatements.Concat(clinicalMeanings).Where(x => !string.IsNullOrWhiteSpace(x)));

        var rubricTail = ExtractTail(rubric.SubSectionName);
        var strength = string.IsNullOrWhiteSpace(evidenceText)
            ? 0m
            : AudioCaseAiProcessor.ComputeTextSimilarity(rubricTail, evidenceText);

        var excerpt = FindTranscriptExcerpt(transcript, patientStatements.FirstOrDefault() ?? rubric.MatchedFrom);

        return new RubricEvidenceChainModel
        {
            PatientStatements = patientStatements.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ClinicalMeanings = clinicalMeanings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            SourceConceptIds = conceptIds,
            TranscriptExcerpt = excerpt,
            EvidenceStrength = strength,
            MatchedSymptomPhrase = symptom?.Phrase ?? rubric.MatchedFrom,
        };
    }

    public ClinicalConceptModel? FindLinkedConcept(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts)
    {
        if (rubric.SourceConceptId.HasValue)
        {
            var linked = concepts.FirstOrDefault(c => c.ConceptId == rubric.SourceConceptId.Value);
            if (linked != null) return linked;
        }

        var rubricText = $"{rubric.MatchedFrom} {rubric.SubSectionName}".ToLowerInvariant();
        return concepts
            .Select(c => new { Concept = c, Score = ScoreConcept(c, rubricText) })
            .Where(x => x.Score > 0.30m)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Concept)
            .FirstOrDefault();
    }

    private static AudioCaseSymptomModel? FindLinkedSymptom(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<AudioCaseSymptomModel> symptoms)
    {
        if (string.IsNullOrWhiteSpace(rubric.MatchedFrom)) return null;

        return symptoms
            .Select(s => new { Symptom = s, Score = AudioCaseAiProcessor.ComputeTextSimilarity(s.Phrase, rubric.MatchedFrom) })
            .OrderByDescending(x => x.Score)
            .Where(x => x.Score >= 0.50m)
            .Select(x => x.Symptom)
            .FirstOrDefault();
    }

    private static decimal ScoreConcept(ClinicalConceptModel concept, string rubricText)
    {
        return Math.Max(
            AudioCaseAiProcessor.ComputeTextSimilarity(concept.RawStatement, rubricText),
            AudioCaseAiProcessor.ComputeTextSimilarity(concept.ClinicalMeaning ?? string.Empty, rubricText));
    }

    private static string ExtractTail(string rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName)) return string.Empty;
        var dash = rubricName.IndexOf('-');
        return dash > 0 && dash < rubricName.Length - 1
            ? rubricName[(dash + 1)..].Trim()
            : rubricName.Trim();
    }

    private static string? FindTranscriptExcerpt(string transcript, string? needle)
    {
        if (string.IsNullOrWhiteSpace(transcript) || string.IsNullOrWhiteSpace(needle)) return null;

        var words = needle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return null;

        var index = transcript.IndexOf(words[0], StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;

        var start = Math.Max(0, index - 40);
        var length = Math.Min(transcript.Length - start, Math.Min(needle.Length + 80, 180));
        return transcript.Substring(start, length).Trim();
    }
}

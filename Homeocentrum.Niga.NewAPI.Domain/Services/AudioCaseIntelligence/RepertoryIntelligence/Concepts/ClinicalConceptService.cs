using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Concepts;

/// <summary>V7: AI clinical concepts — fallback only when no database rubric exists for symptom.</summary>
public interface IClinicalConceptService
{
    List<AudioCaseSuggestedRubricModel> CreateFallbackConcepts(
        IReadOnlyList<V7ExtractedSymptom> symptoms,
        IReadOnlySet<long?> mappedConceptIds,
        ConceptGraphFullModel graph,
        int maxConcepts);
}

public class ClinicalConceptService : IClinicalConceptService
{
    public List<AudioCaseSuggestedRubricModel> CreateFallbackConcepts(
        IReadOnlyList<V7ExtractedSymptom> symptoms,
        IReadOnlySet<long?> mappedConceptIds,
        ConceptGraphFullModel graph,
        int maxConcepts)
    {
        var concepts = new List<AudioCaseSuggestedRubricModel>();

        foreach (var symptom in symptoms)
        {
            if (symptom.SourceConceptId.HasValue && mappedConceptIds.Contains(symptom.SourceConceptId))
            {
                continue;
            }

            var homeo = graph.HomeopathicConcepts.FirstOrDefault(h =>
                h.HomeopathicConceptId == symptom.SourceConceptId);

            if (homeo == null)
            {
                continue;
            }

            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            var meaning = clinical != null
                ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                    ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                : null;

            concepts.Add(EnterpriseRubricPresentationHelper.CreateAiClinicalConcept(
                homeo, clinical, meaning, "v7.0"));

            if (concepts.Count >= maxConcepts)
            {
                break;
            }
        }

        return concepts;
    }
}

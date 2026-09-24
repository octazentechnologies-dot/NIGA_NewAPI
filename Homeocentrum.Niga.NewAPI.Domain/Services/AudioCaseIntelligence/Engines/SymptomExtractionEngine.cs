using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

public class SymptomExtractionEngine : ISymptomExtractionEngine
{
    public List<AudioCaseSymptomModel> Merge(
        IReadOnlyList<AudioCaseSymptomModel> v1Symptoms,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> gptEnhancedSymptoms)
    {
        var merged = new Dictionary<string, AudioCaseSymptomModel>(StringComparer.OrdinalIgnoreCase);

        void AddSymptom(AudioCaseSymptomModel symptom)
        {
            if (string.IsNullOrWhiteSpace(symptom.Phrase)) return;
            var key = symptom.Phrase.Trim().ToUpperInvariant();
            if (merged.TryGetValue(key, out var existing))
            {
                existing.SearchTerms = existing.SearchTerms
                    .Union(symptom.SearchTerms ?? [], StringComparer.OrdinalIgnoreCase)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (symptom.IntensityHint > existing.IntensityHint)
                {
                    existing.IntensityHint = symptom.IntensityHint;
                }
            }
            else
            {
                merged[key] = new AudioCaseSymptomModel
                {
                    Phrase = symptom.Phrase.Trim(),
                    SearchTerms = symptom.SearchTerms?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new(),
                    Category = symptom.Category ?? "particular",
                    IntensityHint = symptom.IntensityHint is >= 1 and <= 4 ? symptom.IntensityHint : 2,
                };
            }
        }

        foreach (var symptom in v1Symptoms) AddSymptom(symptom);
        foreach (var symptom in gptEnhancedSymptoms) AddSymptom(symptom);

        // Always map concepts into V1 search symptoms — seed terms when GPT left SearchTerms empty
        // (empty SearchTerms previously skipped mental/particular/SRP concepts entirely).
        foreach (var concept in concepts)
        {
            ConceptSearchTermBuilder.SeedSearchTermsIfEmpty(concept);
            var phrase = concept.ClinicalMeaning ?? concept.RawStatement;
            if (string.IsNullOrWhiteSpace(phrase))
                continue;

            AddSymptom(new AudioCaseSymptomModel
            {
                Phrase = phrase,
                SearchTerms = concept.SearchTerms,
                Category = concept.Category ?? "particular",
                IntensityHint = concept.IsSRP ? 3 : 2,
            });
        }

        return merged.Values
            .OrderByDescending(s => s.IntensityHint)
            .ThenBy(s => s.Phrase, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }
}

using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Synonyms;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Vocabulary;

/// <summary>V7: expands normalized symptoms into repertory vocabulary (10–30 terms).</summary>
public interface IHomeopathicVocabularyEngine
{
    IReadOnlyList<V7VocabularyTerm> ExpandVocabulary(
        V7NormalizedConcept normalized,
        V7ExtractedSymptom symptom);
}

public class HomeopathicVocabularyEngine : IHomeopathicVocabularyEngine
{
    private readonly ISynonymEngine _synonymEngine;
    private readonly RubricIntelligenceOptions _options;

    public HomeopathicVocabularyEngine(
        ISynonymEngine synonymEngine,
        IOptions<RubricIntelligenceOptions> options)
    {
        _synonymEngine = synonymEngine;
        _options = options.Value;
    }

    public IReadOnlyList<V7VocabularyTerm> ExpandVocabulary(
        V7NormalizedConcept normalized,
        V7ExtractedSymptom symptom)
    {
        var terms = new Dictionary<string, V7VocabularyTerm>(StringComparer.OrdinalIgnoreCase);
        var maxTerms = _options.V7MaxVocabularyTermsPerSymptom;

        void Add(string term, string source, decimal weight)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 3 || terms.Count >= maxTerms)
            {
                return;
            }

            if (!terms.TryGetValue(term, out var existing) || weight > existing.Weight)
            {
                terms[term] = new V7VocabularyTerm { Term = term, Source = source, Weight = weight };
            }
        }

        Add(normalized.FinalConcept, "Normalized", 1.0m);
        foreach (var step in normalized.NormalizationChain)
        {
            Add(step, "NormalizationChain", 0.9m);
        }

        Add(symptom.Text, "Original", 0.85m);
        if (!string.IsNullOrWhiteSpace(symptom.Normalized))
        {
            Add(symptom.Normalized, "GptNormalized", 0.88m);
        }

        if (!string.IsNullOrWhiteSpace(symptom.Timing))
        {
            Add($"{symptom.Timing} {normalized.FinalConcept}", "Timing", 0.92m);
            Add(symptom.Timing, "TimingOnly", 0.7m);
        }

        if (!string.IsNullOrWhiteSpace(symptom.Location))
        {
            Add($"{normalized.FinalConcept} {symptom.Location}", "Location", 0.85m);
            Add(symptom.Location, "LocationOnly", 0.65m);
        }

        var synonymSources = normalized.NormalizationChain
            .Concat(new[] { normalized.FinalConcept, symptom.Text })
            .Where(s => !string.IsNullOrWhiteSpace(s));

        foreach (var synonym in _synonymEngine.ExpandAll(synonymSources, maxTerms))
        {
            Add(synonym, "Synonym", 0.75m);
        }

        ApplyCategoryVocabulary(symptom, normalized, Add);

        return terms.Values
            .OrderByDescending(t => t.Weight)
            .Take(maxTerms)
            .ToList();
    }

    private static void ApplyCategoryVocabulary(
        V7ExtractedSymptom symptom,
        V7NormalizedConcept normalized,
        Action<string, string, decimal> add)
    {
        var category = symptom.Category?.ToLowerInvariant() ?? string.Empty;
        var concept = normalized.FinalConcept.ToLowerInvariant();

        if (category.Contains("mental") || concept.Contains("fear") || concept.Contains("anxiety"))
        {
            add("mind fear", "CategoryMental", 0.8m);
            add("mind anxiety", "CategoryMental", 0.75m);
        }

        if (category.Contains("general") || concept.Contains("thirst") || concept.Contains("salt"))
        {
            add("generals", "CategoryGeneral", 0.7m);
        }

        if (concept.Contains("convulsion") || concept.Contains("fit") || concept.Contains("seizure"))
        {
            add("convulsions", "CategoryConvulsion", 0.85m);
            add("epilepsy", "CategoryConvulsion", 0.8m);
        }

        if (concept.Contains("drops") || concept.Contains("awkward") || concept.Contains("hands"))
        {
            add("awkwardness hands", "CategoryHands", 0.82m);
            add("drops things", "CategoryHands", 0.88m);
        }
    }
}

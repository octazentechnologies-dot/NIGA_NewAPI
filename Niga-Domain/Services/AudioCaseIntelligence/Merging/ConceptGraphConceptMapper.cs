using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>Maps V3 concept graph nodes to V2 ClinicalConceptModel for keyword discovery / validation.</summary>
public static class ConceptGraphConceptMapper
{
    /// <summary>
    /// Stage C fast path: build searchable clinical concepts from GPT extraction symptoms
    /// without a second multi-engine concept graph (V7/Enterprise).
    /// </summary>
    public static List<ClinicalConceptModel> FromSymptoms(
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary = null)
    {
        var concepts = new List<ClinicalConceptModel>();
        var order = 1;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string? raw, string? category, IEnumerable<string>? searchTerms, decimal confidence)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return;

            var key = NormalizeKey(raw);
            if (string.IsNullOrEmpty(key) || !seen.Add(key))
                return;

            var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in searchTerms ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(t))
                    terms.Add(t.Trim());
            }

            foreach (var built in BuildSearchTerms(raw))
                terms.Add(built);

            // Strip unsupported disease inferences unless the patient phrase itself contains them.
            StripUnsupportedDiseaseInferences(raw, terms);

            concepts.Add(new ClinicalConceptModel
            {
                ConceptId = Guid.NewGuid(),
                SequenceOrder = order++,
                RawStatement = raw.Trim(),
                ClinicalMeaning = raw.Trim(),
                HomeopathicMeaning = raw.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "Particular" : category.Trim(),
                Confidence = confidence,
                ConceptTier = "Primary",
                SearchTerms = terms.ToList(),
            });
        }

        foreach (var symptom in symptoms ?? Array.Empty<AudioCaseSymptomModel>())
        {
            TryAdd(
                symptom.Phrase,
                symptom.Category,
                symptom.SearchTerms,
                confidence: 0.85m);
        }

        if (summary != null)
        {
            TryAdd(summary.ChiefComplaint, "ChiefComplaint", null, 0.9m);
            foreach (var m in summary.Mentals ?? new List<string>())
                TryAdd(m, "Mental", null, 0.8m);
            foreach (var g in summary.Generals ?? new List<string>())
                TryAdd(g, "General", null, 0.8m);
            foreach (var p in summary.Particulars ?? new List<string>())
                TryAdd(p, "Particular", null, 0.8m);
            foreach (var mod in summary.Modalities ?? new List<string>())
                TryAdd(mod, "Modality", null, 0.75m);
        }

        return concepts;
    }

    public static List<ClinicalConceptModel> FromGraph(ConceptGraphFullModel graph)
    {
        var concepts = new List<ClinicalConceptModel>();
        var order = 1;

        foreach (var homeo in graph.HomeopathicConcepts)
        {
            if (string.IsNullOrWhiteSpace(homeo.ConceptName))
                continue;

            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            if (clinical == null)
                continue;

            var meaning = graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId);

            concepts.Add(new ClinicalConceptModel
            {
                ConceptId = homeo.HomeopathicConceptId is > 0
                    ? ConceptIdentity.FromHomeopathicConceptId(homeo.HomeopathicConceptId.Value)
                    : Guid.NewGuid(),
                SequenceOrder = order++,
                RawStatement = meaning?.RawStatement ?? clinical.ConceptName,
                ClinicalMeaning = clinical.ConceptName,
                HomeopathicMeaning = homeo.ConceptName,
                Category = homeo.Category ?? clinical.SymptomCategory ?? clinical.Domain,
                IsSRP = homeo.IsSRP,
                Confidence = homeo.Confidence,
                HomeopathicWeight = homeo.Weight,
                ConceptTier = homeo.ConceptTier ?? clinical.ConceptTier,
                SearchTerms = BuildSearchTerms(homeo.ConceptName, clinical.ConceptName, meaning?.NormalizedMeaning),
            });
        }

        if (concepts.Count > 0)
            return concepts;

        return graph.ClinicalConcepts
            .Where(c => !string.IsNullOrWhiteSpace(c.ConceptName))
            .Select((c, i) =>
            {
                var meaning = graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == c.PatientMeaningId);
                return new ClinicalConceptModel
                {
                    ConceptId = Guid.NewGuid(),
                    SequenceOrder = i + 1,
                    RawStatement = meaning?.RawStatement ?? c.ConceptName,
                    ClinicalMeaning = c.ConceptName,
                    HomeopathicMeaning = c.ConceptName,
                    Category = c.SymptomCategory ?? c.Domain,
                    Confidence = c.Confidence,
                    ConceptTier = c.ConceptTier,
                    SearchTerms = BuildSearchTerms(c.ConceptName, null, meaning?.NormalizedMeaning),
                };
            })
            .ToList();
    }

    public static List<ClinicalConceptModel> MergePreferringSearchTerms(
        IReadOnlyList<ClinicalConceptModel> primary,
        IReadOnlyList<ClinicalConceptModel> fallback)
    {
        if (primary.Count == 0)
            return fallback.ToList();
        if (fallback.Count == 0)
            return primary.ToList();

        var map = primary
            .GroupBy(c => NormalizeKey(c.ClinicalMeaning ?? c.RawStatement))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var concept in fallback)
        {
            var key = NormalizeKey(concept.ClinicalMeaning ?? concept.RawStatement);
            if (!map.ContainsKey(key))
            {
                map[key] = concept;
                continue;
            }

            if (map[key].SearchTerms.Count == 0 && concept.SearchTerms.Count > 0)
                map[key] = concept;
        }

        return map.Values.OrderBy(c => c.SequenceOrder).ToList();
    }

    private static List<string> BuildSearchTerms(params string?[] parts)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            terms.Add(part.Trim());
            foreach (var token in part.Split(new[] { ' ', '-', ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length >= 3)
                    terms.Add(token.Trim());
            }
        }

        return terms.ToList();
    }

    private static string NormalizeKey(string? text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().ToLowerInvariant();

    private static readonly string[] DiseaseInferenceTerms =
    {
        "epilepsy", "epileptic", "convulsion", "convulsions", "seizure", "seizures",
    };

    /// <summary>
    /// "fit" must not silently expand to epilepsy/convulsion search terms without patient evidence.
    /// </summary>
    private static void StripUnsupportedDiseaseInferences(string rawPhrase, HashSet<string> terms)
    {
        var raw = rawPhrase ?? string.Empty;
        foreach (var term in DiseaseInferenceTerms)
        {
            if (WordBoundaryMatcher.ContainsWord(raw, term))
                continue;
            terms.RemoveWhere(t =>
                string.Equals(t, term, StringComparison.OrdinalIgnoreCase)
                || WordBoundaryMatcher.ContainsWord(t, term));
        }
    }
}

using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Phases 40–42: expand symptoms into multi-query blocks for higher recall
/// (exact phrase, sensation+location, search-term variants) without inventing disease terms.
/// </summary>
public static class FastClinicalSymptomBlockBuilder
{
    private static readonly string[] LocationHints =
    {
        "hand", "hands", "foot", "feet", "head", "chest", "abdomen", "back",
        "leg", "legs", "arm", "arms", "throat", "eye", "eyes", "ear", "ears",
        "face", "neck", "stomach", "mind", "extremities", "left", "right",
    };

    private static readonly string[] SensationHints =
    {
        "vibration", "trembling", "numbness", "tingling", "burning", "throbbing",
        "stitching", "pressing", "drawing", "cramping", "itching", "pain",
        "fear", "anxiety", "thirst", "desire", "aversion", "weakness",
    };

    public static List<ClinicalConceptModel> ExpandMultiQuery(
        IReadOnlyList<ClinicalConceptModel> concepts,
        int maxExtraQueriesPerConcept = 4)
    {
        if (concepts.Count == 0)
            return new List<ClinicalConceptModel>();

        maxExtraQueriesPerConcept = Math.Clamp(maxExtraQueriesPerConcept, 1, 8);
        var result = new List<ClinicalConceptModel>(concepts);
        var seen = new HashSet<string>(
            concepts.Select(c => Normalize(c.RawStatement)),
            StringComparer.OrdinalIgnoreCase);

        var order = concepts.Count > 0 ? concepts.Max(c => c.SequenceOrder) + 1 : 1;

        foreach (var concept in concepts)
        {
            foreach (var query in BuildQueries(concept).Take(maxExtraQueriesPerConcept))
            {
                var key = Normalize(query);
                if (string.IsNullOrEmpty(key) || !seen.Add(key))
                    continue;

                var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { query };
                foreach (var token in query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (token.Length >= 3)
                        terms.Add(token);
                }

                result.Add(new ClinicalConceptModel
                {
                    ConceptId = Guid.NewGuid(),
                    SequenceOrder = order++,
                    RawStatement = query,
                    ClinicalMeaning = concept.ClinicalMeaning ?? concept.RawStatement,
                    HomeopathicMeaning = concept.HomeopathicMeaning ?? query,
                    Category = concept.Category,
                    Confidence = Math.Max(0.55m, concept.Confidence - 0.1m),
                    ConceptTier = "Secondary",
                    SearchTerms = terms.ToList(),
                    IsSRP = concept.IsSRP,
                    HomeopathicWeight = concept.HomeopathicWeight,
                });
            }
        }

        return result;
    }

    private static IEnumerable<string> BuildQueries(ClinicalConceptModel concept)
    {
        var raw = (concept.RawStatement ?? string.Empty).Trim();
        if (raw.Length >= 3)
            yield return raw;

        var tokens = Tokenize(string.Join(' ',
            new[] { raw, concept.ClinicalMeaning, concept.HomeopathicMeaning }
                .Concat(concept.SearchTerms ?? new List<string>())));

        var locations = tokens.Where(t => LocationHints.Any(h =>
            string.Equals(h, t, StringComparison.OrdinalIgnoreCase)
            || t.StartsWith(h, StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sensations = tokens.Where(t => SensationHints.Any(h =>
            string.Equals(h, t, StringComparison.OrdinalIgnoreCase)
            || t.StartsWith(h, StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var s in sensations)
        {
            foreach (var loc in locations)
                yield return $"{s} {loc}";
        }

        // Multi-word search terms as dedicated queries
        foreach (var term in concept.SearchTerms ?? new List<string>())
        {
            var t = term?.Trim();
            if (string.IsNullOrWhiteSpace(t) || t.Length < 4)
                continue;
            if (t.Contains(' ', StringComparison.Ordinal) || t.Contains('-', StringComparison.Ordinal))
                yield return t;
        }
    }

    private static List<string> Tokenize(string? text)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
            return list;
        foreach (var part in text.Split(
                     new[] { ' ', '-', ',', ';', '/', '(', ')' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim().ToLowerInvariant();
            if (t.Length >= 3)
                list.Add(t);
        }

        return list;
    }

    private static string Normalize(string? text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().ToLowerInvariant();
}

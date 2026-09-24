using System.Text.RegularExpressions;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Synonyms;

/// <summary>V7: deterministic synonym expansion — never relies on GPT memory.</summary>
public interface ISynonymEngine
{
    IReadOnlyList<string> ExpandSynonyms(string term);

    IReadOnlyList<string> ExpandAll(IEnumerable<string> terms, int maxTerms = 40);
}

public class SynonymEngine : ISynonymEngine
{
    private static readonly Dictionary<string, string[]> SynonymGroups = BuildSynonymGroups();

    public IReadOnlyList<string> ExpandSynonyms(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Array.Empty<string>();
        }

        var normalized = Normalize(term);
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { term.Trim() };

        foreach (var (key, synonyms) in SynonymGroups)
        {
            if (normalized.Contains(key, StringComparison.OrdinalIgnoreCase)
                || synonyms.Any(s => normalized.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(key);
                foreach (var synonym in synonyms)
                {
                    results.Add(synonym);
                }
            }
        }

        return results.OrderByDescending(s => s.Length).ToList();
    }

    public IReadOnlyList<string> ExpandAll(IEnumerable<string> terms, int maxTerms = 40)
    {
        var expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var term in terms.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            foreach (var synonym in ExpandSynonyms(term))
            {
                expanded.Add(synonym);
                if (expanded.Count >= maxTerms)
                {
                    return expanded.ToList();
                }
            }
        }

        return expanded.ToList();
    }

    private static string Normalize(string text) =>
        Regex.Replace(text.Trim().ToLowerInvariant(), @"\s+", " ");

    private static Dictionary<string, string[]> BuildSynonymGroups() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["fit"] = new[] { "convulsion", "seizure", "attack", "epilepsy", "epileptic", "spasm" },
            ["convulsion"] = new[] { "fit", "seizure", "attack", "epilepsy", "spasm" },
            ["seizure"] = new[] { "fit", "convulsion", "attack", "epilepsy" },
            ["fear"] = new[] { "scared", "afraid", "terror", "dread", "anxiety", "fright" },
            ["anxiety"] = new[] { "fear", "worry", "nervous", "apprehension" },
            ["aura"] = new[] { "premonitory", "warning", "prodrome", "premonition" },
            ["vibration"] = new[] { "tremor", "shaking", "quivering", "trembling" },
            ["shock"] = new[] { "jolt", "startle", "electric shock", "sudden fright" },
            ["drops"] = new[] { "lets fall", "drops things", "awkward", "cannot hold", "slip", "weak grasp" },
            ["salt"] = new[] { "craves salt", "desire salt", "salt appetite", "wants salt" },
            ["thirst"] = new[] { "drinks much", "large quantities", "much thirst", "polydipsia" },
            ["forget"] = new[] { "memory loss", "forgetfulness", "amnesia", "cannot remember" },
            ["sleep"] = new[] { "somnambulism", "talking in sleep", "sleepwalking", "dreams" },
            ["height"] = new[] { "high places", "altitude", "vertigo height", "fear heights" },
            ["meat"] = new[] { "desire meat", "craves meat", "wants meat", "appetite meat" },
            ["awkward"] = new[] { "clumsy", "uncoordinated", "drops things", "lets fall" },
            ["before"] = new[] { "prior", "preceding", "premonitory", "aura" },
            ["after"] = new[] { "following", "post", "since", "subsequent" },
        };
}

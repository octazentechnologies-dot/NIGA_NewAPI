namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Parse Kent-style repertory paths from SubSectionName (Phases 17 / 39).
/// Example: "EXTREMITIES - VIBRATION - Hands" → [EXTREMITIES, VIBRATION, Hands]
/// </summary>
public static class FastClinicalHierarchyParser
{
    public sealed class HierarchyPath
    {
        public IReadOnlyList<string> Segments { get; init; } = Array.Empty<string>();

        public int Depth => Segments.Count;

        public string Root => Segments.Count > 0 ? Segments[0] : string.Empty;

        public string Leaf => Segments.Count > 0 ? Segments[^1] : string.Empty;

        public string Joined => string.Join(" > ", Segments);
    }

    public static HierarchyPath Parse(string? subSectionName)
    {
        if (string.IsNullOrWhiteSpace(subSectionName))
            return new HierarchyPath();

        var raw = subSectionName.Trim();
        // Prefer " - " separators; also split on lone '-' when segments are long enough.
        var parts = raw.Contains(" - ", StringComparison.Ordinal)
            ? raw.Split(" - ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : raw.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var segments = parts
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        return new HierarchyPath { Segments = segments };
    }

    /// <summary>
    /// Leaf / deep modifiers that need patient evidence (location, laterality, rare qualifiers).
    /// Section roots and common sensation words are treated as structural.
    /// </summary>
    public static IReadOnlyList<string> SpecificityTokens(HierarchyPath path)
    {
        if (path.Depth <= 2)
            return Array.Empty<string>();

        // Depth 3+: everything after root+primary sensation must be evidenced.
        return path.Segments.Skip(2)
            .SelectMany(Tokenize)
            .Where(t => t.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        foreach (var part in text.Split(
                     new[] { ' ', ',', ';', '/', '(', ')', '.' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim().ToLowerInvariant();
            if (t.Length >= 3 && t is not ("the" or "and" or "with" or "from" or "for" or "side"))
                yield return t;
        }
    }
}

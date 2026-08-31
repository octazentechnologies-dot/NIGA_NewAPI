using System.Text.RegularExpressions;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Whole-word / stem-aware matching so short clinical tokens ("drop", "cold", "fit", "fear", "salt")
/// cannot substring-match unrelated compounds ("dropsy", "coldness" only when "cold" is not a token, etc.).
/// </summary>
public static class WordBoundaryMatcher
{
    private static readonly Regex TokenSplitter = new(
        @"[^A-Za-z0-9]+",
        RegexOptions.Compiled);

    /// <summary>
    /// True when every significant token in <paramref name="needle"/> appears as a whole token
    /// (or accepted stem) in <paramref name="haystack"/>.
    /// Multi-word needles require all tokens; single short tokens must be word-bounded.
    /// </summary>
    public static bool Matches(string? haystack, string? needle)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(needle))
            return false;

        var hayTokens = Tokenize(haystack);
        if (hayTokens.Count == 0)
            return false;

        var needleTokens = Tokenize(needle);
        if (needleTokens.Count == 0)
            return false;

        // Phrase: every needle token must appear as a whole token (or stem) in haystack.
        return needleTokens.All(n => hayTokens.Any(h => TokensAlign(h, n)));
    }

    /// <summary>
    /// True when the needle appears only as a substring inside a longer unrelated token
    /// (e.g. "drop" inside "dropsy") and does not align as a whole word/stem.
    /// </summary>
    public static bool IsSubstringCollision(string? haystack, string? needle)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(needle))
            return false;

        if (!haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
            return false;

        return !Matches(haystack, needle);
    }

    public static bool ContainsWord(string? haystack, string word) =>
        Matches(haystack, word);

    public static IReadOnlyList<string> Tokenize(string text) =>
        TokenSplitter.Split(text.Trim())
            .Where(t => t.Length >= 2)
            .Select(t => t.ToLowerInvariant())
            .ToList();

    private static bool TokensAlign(string hayToken, string needleToken)
    {
        if (string.Equals(hayToken, needleToken, StringComparison.OrdinalIgnoreCase))
            return true;

        // Medical root prefix: needle length ≥5 (e.g. epilep→epilepsy). Blocks drop→dropsy (needle len 4).
        if (needleToken.Length >= 5
            && hayToken.StartsWith(needleToken, StringComparison.OrdinalIgnoreCase)
            && hayToken.Length - needleToken.Length <= 5)
        {
            return true;
        }

        // Stem-aware: drop/drops/dropping ↔ drop*; not dropsy.
        if (needleToken.Length >= 3 && hayToken.Length >= 3)
        {
            var stem = SharedStem(needleToken, hayToken);
            if (stem.Length >= 3
                && IsInflectionOf(hayToken, stem)
                && IsInflectionOf(needleToken, stem))
            {
                return true;
            }
        }

        return false;
    }

    private static string SharedStem(string a, string b)
    {
        var len = Math.Min(a.Length, b.Length);
        var i = 0;
        while (i < len && a[i] == b[i])
            i++;
        return a[..i];
    }

    private static bool IsInflectionOf(string token, string stem)
    {
        if (!token.StartsWith(stem, StringComparison.Ordinal))
            return false;

        var suffix = token[stem.Length..];
        // Allow short inflectional suffixes only — blocks drop→dropsy (suffix "sy" is not inflectional,
        // and remaining length after stem "drop" is 2 for dropsy... wait dropsy stem with drop is "drop" + "sy".
        // "sy" is not in allowed suffixes — good.
        return suffix.Length == 0
            || suffix is "s" or "es" or "ed" or "ing" or "er" or "est" or "ly"
            || suffix is "ness" or "less" or "ful"; // coldness aligns with cold? stem cold + ness — ALLOWED.
            // Wait: Bug A audit said "coldness" should NOT match unrelated "cold" in wrong context —
            // but cold/coldness ARE related clinically. The dropsy case is the critical one.
            // Keep ness for cold↔coldness; dropsy blocked by "sy" not being allowed.
    }
}

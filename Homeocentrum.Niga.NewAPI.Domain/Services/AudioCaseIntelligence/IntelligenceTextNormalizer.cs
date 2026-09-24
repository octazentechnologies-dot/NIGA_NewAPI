using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence;

public static partial class IntelligenceTextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var lower = text.Trim().ToLowerInvariant();
        lower = lower.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        var cleaned = NonAlphaNumeric().Replace(sb.ToString(), " ");
        return Whitespace().Replace(cleaned, " ").Trim();
    }

    [GeneratedRegex(@"[^a-z0-9\s]", RegexOptions.Compiled)]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex Whitespace();
}

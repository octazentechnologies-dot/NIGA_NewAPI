using Homeocentrum.Niga.NewAPI.Domain.Configuration;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public static class ClinicalConceptInputGuard
{
    public static (bool IsValid, string? Error) Validate(string? input, AiEmbeddingInfrastructureOptions options)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (false, "Clinical concept is required.");

        var trimmed = input.Trim();
        if (trimmed.Length > options.MaxClinicalConceptInputLength)
            return (false, $"Clinical concept exceeds maximum length of {options.MaxClinicalConceptInputLength} characters.");

        var wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        if (wordCount >= options.TranscriptRejectionMinWords)
            return (false, "Input appears to be a transcript. Provide a clinical concept only.");

        if (LooksLikeTranscript(trimmed))
            return (false, "Input appears to be a transcript. Provide a clinical concept only.");

        return (true, null);
    }

    private static bool LooksLikeTranscript(string text)
    {
        var sentenceCount = text.Count(c => c is '.' or '?' or '!');
        if (sentenceCount >= 2)
            return true;

        var lower = text.ToLowerInvariant();
        string[] transcriptMarkers =
        {
            "patient said",
            "patient says",
            "he said",
            "she said",
            "i said",
            "doctor asked",
            "transcript",
            "verbatim",
        };

        return transcriptMarkers.Any(marker => lower.Contains(marker, StringComparison.Ordinal));
    }
}

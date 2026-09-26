using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Conversation;

public interface IEciConversationParser
{
    Task<EciConversationParseResult> ParseAsync(
        string transcript,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// ECI v8: deterministic transcript cleaner + speaker hinting.
/// Speaker separation can be upgraded later, but this module must remain independently replaceable.
/// </summary>
public sealed class EciConversationParser : IEciConversationParser
{
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex WhisperFragment = new(@"\b(um+|uh+|ah+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DoctorPrefix = new(@"^\s*(doctor|dr)\s*[:\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PatientPrefix = new(@"^\s*(patient|pt)\s*[:\-]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<EciConversationParser> _logger;

    public EciConversationParser(ILogger<EciConversationParser> logger)
    {
        _logger = logger;
    }

    public Task<EciConversationParseResult> ParseAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return Task.FromResult(new EciConversationParseResult
            {
                Success = false,
                Error = "Transcript is empty.",
            });
        }

        var lines = transcript
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var turns = new List<EciDialogueTurn>();
        var cleanedLines = new List<string>();

        for (var i = 0; i < lines.Count; i++)
        {
            var line = NormalizeLine(lines[i]);
            if (line.Length == 0)
            {
                continue;
            }

            var speaker = InferSpeaker(line);
            var text = StripSpeakerPrefix(line);

            if (text.Length == 0)
            {
                continue;
            }

            // Basic dedup for repeated fragments.
            if (cleanedLines.Count > 0 && string.Equals(cleanedLines.Last(), text, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            cleanedLines.Add(text);
            turns.Add(new EciDialogueTurn
            {
                Speaker = speaker,
                Text = text,
                TurnIndex = turns.Count,
                Confidence = 0.90m,
            });
        }

        return Task.FromResult(new EciConversationParseResult
        {
            Success = turns.Count > 0,
            Turns = turns,
            CleanTranscript = string.Join("\n", cleanedLines),
        });
    }

    private static string NormalizeLine(string line)
    {
        var cleaned = line.Trim();
        cleaned = WhisperFragment.Replace(cleaned, string.Empty);
        cleaned = MultiSpace.Replace(cleaned, " ").Trim();
        return cleaned;
    }

    private static EciSpeakerRole InferSpeaker(string line)
    {
        if (DoctorPrefix.IsMatch(line))
        {
            return EciSpeakerRole.Doctor;
        }

        if (PatientPrefix.IsMatch(line))
        {
            return EciSpeakerRole.Patient;
        }

        return EciSpeakerRole.Unknown;
    }

    private static string StripSpeakerPrefix(string line)
    {
        var stripped = DoctorPrefix.Replace(line, string.Empty);
        stripped = PatientPrefix.Replace(stripped, string.Empty);
        return stripped.Trim();
    }
}


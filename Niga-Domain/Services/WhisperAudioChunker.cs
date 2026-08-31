using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Niga_Domain.Services;

/// <summary>
/// Splits long audio into ordered chunks for parallel Whisper calls.
/// Falls back to null (caller uses the full file) when ffmpeg/ffprobe is missing.
/// Never logs transcript text.
/// </summary>
public static class WhisperAudioChunker
{
    public static async Task<double?> TryGetDurationSecondsAsync(
        string audioFilePath,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var (ok, stdout, _) = await TryRunAsync(
            "ffprobe",
            $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{audioFilePath}\"",
            TimeSpan.FromSeconds(20),
            logger,
            cancellationToken);

        if (!ok || string.IsNullOrWhiteSpace(stdout))
            return null;

        return double.TryParse(stdout.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && seconds > 0
            ? seconds
            : null;
    }

    public static async Task<List<string>?> TrySplitAsync(
        string audioFilePath,
        double durationSeconds,
        int chunkSeconds,
        int overlapSeconds,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        chunkSeconds = Math.Clamp(chunkSeconds, 30, 240);
        overlapSeconds = Math.Clamp(overlapSeconds, 0, 10);
        if (durationSeconds <= chunkSeconds)
            return null;

        var workDir = Path.Combine(Path.GetTempPath(), "niga-whisper-chunks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        try
        {
            var paths = new List<string>();
            var start = 0d;
            var index = 0;
            while (start < durationSeconds - 0.5)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var length = Math.Min(chunkSeconds, durationSeconds - start);
                var chunkPath = Path.Combine(workDir, $"chunk-{index:D3}.wav");
                var startText = start.ToString("0.###", CultureInfo.InvariantCulture);
                var lengthText = length.ToString("0.###", CultureInfo.InvariantCulture);
                var (ok, _, stderr) = await TryRunAsync(
                    "ffmpeg",
                    $"-y -ss {startText} -t {lengthText} -i \"{audioFilePath}\" -ac 1 -ar 16000 -c:a pcm_s16le \"{chunkPath}\"",
                    TimeSpan.FromMinutes(2),
                    logger,
                    cancellationToken);

                if (!ok || !File.Exists(chunkPath))
                {
                    logger.LogWarning("Whisper chunk ffmpeg failed at start={Start}s. Falling back to full-file transcription.", start);
                    CleanupDir(workDir);
                    return null;
                }

                paths.Add(chunkPath);
                index++;
                var next = start + chunkSeconds - overlapSeconds;
                if (next <= start)
                    break;
                start = next;
            }

            logger.LogInformation("Whisper chunked audio into {Count} files (durationSec={Duration:F0}).", paths.Count, durationSeconds);
            return paths;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Whisper chunk split failed. Falling back to full-file transcription.");
            CleanupDir(workDir);
            return null;
        }
    }

    public static string JoinTranscripts(IReadOnlyList<(int Index, string Transcript, string? ResponseJson)> orderedChunks, int overlapSeconds)
    {
        var parts = new List<string>(orderedChunks.Count);
        foreach (var chunk in orderedChunks.OrderBy(x => x.Index))
        {
            var text = ExtractTranscriptSkippingOverlap(chunk.ResponseJson, chunk.Transcript, chunk.Index == 0 ? 0 : overlapSeconds);
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text.Trim());
        }

        return string.Join(" ", parts);
    }

    public static void CleanupFiles(IEnumerable<string> paths)
    {
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    dirs.Add(dir);
            }
            catch
            {
                // temp cleanup is best-effort
            }
        }

        foreach (var dir in dirs)
            CleanupDir(dir);
    }

    private static string ExtractTranscriptSkippingOverlap(string? responseJson, string fallback, int overlapSeconds)
    {
        if (string.IsNullOrWhiteSpace(responseJson) || overlapSeconds <= 0)
            return fallback;

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (!doc.RootElement.TryGetProperty("segments", out var segments)
                || segments.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var skipBefore = Math.Max(0, overlapSeconds - 0.35);
            var sb = new StringBuilder();
            foreach (var segment in segments.EnumerateArray())
            {
                var start = segment.TryGetProperty("start", out var startEl) ? startEl.GetDouble() : 0;
                if (start < skipBefore)
                    continue;
                var text = segment.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
                if (!string.IsNullOrWhiteSpace(text))
                    sb.Append(text);
            }

            var joined = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(joined) ? fallback : joined;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static async Task<(bool Ok, string Stdout, string Stderr)> TryRunAsync(
        string fileName,
        string arguments,
        TimeSpan timeout,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                return (false, string.Empty, "timeout");
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            return (process.ExitCode == 0, stdout ?? string.Empty, stderr ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not run {Tool} for Whisper chunking.", fileName);
            return (false, string.Empty, ex.Message);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
    }

    private static void CleanupDir(string workDir)
    {
        try
        {
            if (Directory.Exists(workDir))
                Directory.Delete(workDir, recursive: true);
        }
        catch
        {
            // ignore
        }
    }
}

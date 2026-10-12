using System.Diagnostics;
using System.Runtime.InteropServices;
using Homeocentrum.Niga.API.Domain.Logging;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.API.Domain.Security.Uploads;

public enum AntivirusStatus { Clean, Infected, Unavailable }

public sealed record AntivirusResult(AntivirusStatus Status, string Engine, string? Detail = null);

public interface IAntivirusScanner
{
    bool Enabled { get; }
    bool FailClosed { get; }
    Task<AntivirusResult> ScanAsync(byte[] content, string contentName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Scans uploads with Microsoft Defender. Small files go through AMSI in-process (fast); large files, or hosts where
/// AMSI is not available, are written to a private temp file and scanned with MpCmdRun.exe.
/// </summary>
public sealed class DefenderAntivirusScanner : IAntivirusScanner
{
    private const int AmsiMaxBytes = 32 * 1024 * 1024;
    private const int AmsiResultDetected = 32768;
    private readonly string? _mpCmdRun;
    private readonly TimeSpan _timeout;

    public bool Enabled { get; }
    public bool FailClosed { get; }

    public DefenderAntivirusScanner(IConfiguration config)
    {
        Enabled = config.GetValue("UploadSecurity:AntivirusEnabled", true);
        FailClosed = config.GetValue("UploadSecurity:FailClosed", false);
        _timeout = TimeSpan.FromSeconds(Math.Clamp(config.GetValue("UploadSecurity:ScanTimeoutSeconds", 60), 5, 600));
        _mpCmdRun = FindMpCmdRun(config["UploadSecurity:MpCmdRunPath"]);
    }

    public async Task<AntivirusResult> ScanAsync(byte[] content, string contentName, CancellationToken cancellationToken = default)
    {
        if (!Enabled)
            return new AntivirusResult(AntivirusStatus.Unavailable, "disabled");
        if (!OperatingSystem.IsWindows())
            return new AntivirusResult(AntivirusStatus.Unavailable, "none", "Defender is only available on Windows.");

        if (content.Length <= AmsiMaxBytes)
        {
            var amsi = AmsiScan(content, contentName);
            if (amsi.Status != AntivirusStatus.Unavailable)
                return amsi;
        }
        return await MpCmdRunScanAsync(content, cancellationToken);
    }

    private static AntivirusResult AmsiScan(byte[] content, string contentName)
    {
        IntPtr context = IntPtr.Zero, session = IntPtr.Zero;
        try
        {
            if (AmsiInitialize("Homeocentrum.Upload", out context) != 0 || context == IntPtr.Zero)
                return new AntivirusResult(AntivirusStatus.Unavailable, "amsi");
            if (AmsiOpenSession(context, out session) != 0)
                return new AntivirusResult(AntivirusStatus.Unavailable, "amsi");
            var hr = AmsiScanBuffer(context, content, (uint)content.Length, Path.GetFileName(contentName), session, out var result);
            if (hr != 0)
                return new AntivirusResult(AntivirusStatus.Unavailable, "amsi", "AmsiScanBuffer failed 0x" + hr.ToString("X8"));
            return result >= AmsiResultDetected
                ? new AntivirusResult(AntivirusStatus.Infected, "amsi")
                : new AntivirusResult(AntivirusStatus.Clean, "amsi");
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return new AntivirusResult(AntivirusStatus.Unavailable, "amsi", ex.GetType().Name);
        }
        finally
        {
            try
            {
                if (session != IntPtr.Zero) AmsiCloseSession(context, session);
                if (context != IntPtr.Zero) AmsiUninitialize(context);
            }
            catch { /* best effort */ }
        }
    }

    private async Task<AntivirusResult> MpCmdRunScanAsync(byte[] content, CancellationToken cancellationToken)
    {
        if (_mpCmdRun == null)
            return new AntivirusResult(AntivirusStatus.Unavailable, "mpcmdrun", "MpCmdRun.exe was not found.");

        var folder = Path.Combine(Path.GetTempPath(), "hc-upload-scan");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            await File.WriteAllBytesAsync(path, content, cancellationToken);
            await Task.Delay(150, cancellationToken);
            if (!File.Exists(path))
                return new AntivirusResult(AntivirusStatus.Infected, "defender-realtime", "Removed by real-time protection.");

            var psi = new ProcessStartInfo(_mpCmdRun, $"-Scan -ScanType 3 -File \"{path}\" -DisableRemediation")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(psi);
            if (process == null)
                return new AntivirusResult(AntivirusStatus.Unavailable, "mpcmdrun", "Could not start the scanner.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);
            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return new AntivirusResult(AntivirusStatus.Unavailable, "mpcmdrun", "Scan timed out.");
            }
            return process.ExitCode switch
            {
                0 => new AntivirusResult(AntivirusStatus.Clean, "mpcmdrun"),
                2 => new AntivirusResult(AntivirusStatus.Infected, "mpcmdrun"),
                _ => new AntivirusResult(AntivirusStatus.Unavailable, "mpcmdrun", "Exit code " + process.ExitCode),
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            AppFileLog.Write("errors", "WARN", "Antivirus", "MpCmdRun scan failed.", ex, null, sendAlert: false);
            return new AntivirusResult(AntivirusStatus.Unavailable, "mpcmdrun", ex.GetType().Name);
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    private static string? FindMpCmdRun(string? configured)
    {
        if (!OperatingSystem.IsWindows()) return null;
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured)) return configured;
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var classic = Path.Combine(programFiles, "Windows Defender", "MpCmdRun.exe");
        var platformRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microsoft", "Windows Defender", "Platform");
        try
        {
            if (Directory.Exists(platformRoot))
            {
                var latest = Directory.GetDirectories(platformRoot)
                    .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                    .Select(d => Path.Combine(d, "MpCmdRun.exe"))
                    .FirstOrDefault(File.Exists);
                if (latest != null) return latest;
            }
        }
        catch { /* fall back to the classic path */ }
        return File.Exists(classic) ? classic : null;
    }

    [DllImport("amsi.dll", CharSet = CharSet.Unicode)]
    private static extern int AmsiInitialize(string appName, out IntPtr amsiContext);

    [DllImport("amsi.dll")]
    private static extern void AmsiUninitialize(IntPtr amsiContext);

    [DllImport("amsi.dll")]
    private static extern int AmsiOpenSession(IntPtr amsiContext, out IntPtr session);

    [DllImport("amsi.dll")]
    private static extern void AmsiCloseSession(IntPtr amsiContext, IntPtr session);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode)]
    private static extern int AmsiScanBuffer(IntPtr amsiContext, byte[] buffer, uint length, string contentName, IntPtr session, out int result);
}

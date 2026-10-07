using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Homeocentrum.Niga.NewAPI.Domain.Logging;

/// <summary>
/// Emails an ops alert when a drive's free space is at or below DiskSpaceAlert:MinFreePercent (default 10),
/// repeats every DiskSpaceAlert:RepeatHours while it stays low, and mails once when it recovers.
/// DiskSpaceAlert:Drives limits the check (for example ["C:\\", "D:\\"]); empty checks every fixed drive.
/// Settings are read on every pass, so a changed appsettings.json applies without a restart.
/// </summary>
public sealed class DiskSpaceMonitor : BackgroundService
{
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromMinutes(1);

    private readonly IConfiguration _config;
    private readonly HashSet<string> _lowDrives = new(StringComparer.OrdinalIgnoreCase);

    public DiskSpaceMonitor(IConfiguration config)
    {
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstCheckDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = Read();
                if (settings.Enabled)
                    Check(settings);
                await Task.Delay(TimeSpan.FromMinutes(settings.IntervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void Check(Settings settings)
    {
        foreach (var drive in Drives(settings.Drives))
        {
            try
            {
                if (!drive.IsReady || drive.TotalSize <= 0)
                    continue;
                var freePercent = drive.AvailableFreeSpace * 100.0 / drive.TotalSize;
                var name = drive.Name.TrimEnd('\\');
                var facts = new Dictionary<string, string>
                {
                    ["Drive"] = name,
                    ["Free"] = Gb(drive.AvailableFreeSpace) + " GB of " + Gb(drive.TotalSize) + " GB",
                    ["Free %"] = freePercent.ToString("0.0", CultureInfo.InvariantCulture) + "%",
                    ["Alert at"] = settings.MinFreePercent.ToString(CultureInfo.InvariantCulture) + "% free or less (DiskSpaceAlert:MinFreePercent)",
                };

                if (freePercent <= settings.MinFreePercent)
                {
                    _lowDrives.Add(name);
                    facts["Action"] = "Free space now: clear old logs, backups, temp files and IIS logs, or extend the disk. SQL Server and uploads fail when the disk is full.";
                    AppFileLog.SendOpsAlert("disk-low|" + name, "Disk " + name + " almost full", facts, cooldownMinutes: settings.RepeatHours * 60);
                }
                else if (_lowDrives.Remove(name))
                {
                    AppFileLog.SendOpsAlert("disk-ok|" + name, "Disk " + name + " space recovered", facts, cooldownMinutes: 0);
                }
            }
            catch (Exception ex)
            {
                AppFileLog.Write("errors", "WARN", nameof(DiskSpaceMonitor), "Disk check failed for " + drive.Name, ex, sendAlert: false);
            }
        }
    }

    private static IEnumerable<DriveInfo> Drives(string[] configured)
    {
        if (configured.Length > 0)
            return configured.Select(d => new DriveInfo(d));
        return DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed);
    }

    private static string Gb(long bytes) => (bytes / 1073741824.0).ToString("0.0", CultureInfo.InvariantCulture);

    private Settings Read()
    {
        var section = _config.GetSection("DiskSpaceAlert");
        var percent = double.TryParse(section["MinFreePercent"], NumberStyles.Float, CultureInfo.InvariantCulture, out var p) && p > 0 && p < 100 ? p : 10;
        var interval = int.TryParse(section["CheckIntervalMinutes"], out var i) && i > 0 ? i : 15;
        var repeat = int.TryParse(section["RepeatHours"], out var r) && r > 0 ? r : 6;
        var enabled = !bool.TryParse(section["Enabled"], out var e) || e;
        var drives = section.GetSection("Drives").GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .ToArray();
        return new Settings(enabled, percent, interval, repeat, drives);
    }

    private sealed record Settings(bool Enabled, double MinFreePercent, int IntervalMinutes, int RepeatHours, string[] Drives);
}

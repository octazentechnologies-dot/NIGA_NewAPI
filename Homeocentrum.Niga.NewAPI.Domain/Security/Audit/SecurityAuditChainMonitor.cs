using Homeocentrum.Niga.NewAPI.Domain.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Homeocentrum.Niga.NewAPI.Domain.Security.Audit;

/// <summary>
/// Verifies the SecurityAuditLog HMAC chain shortly after start and then daily, and emails an ops alert
/// when rows were altered, removed or signed with an unknown key.
/// </summary>
public sealed class SecurityAuditChainMonitor : BackgroundService
{
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly SecurityAuditLog _audit;
    private readonly ILogger<SecurityAuditChainMonitor> _logger;

    public SecurityAuditChainMonitor(SecurityAuditLog audit, ILogger<SecurityAuditChainMonitor> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstCheckDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _audit.VerifyAsync(cancellationToken);
            if (result.Problem == null)
            {
                _logger.LogInformation("Security audit chain intact ({Rows} rows).", result.RowsChecked);
                return;
            }
            AppFileLog.SendOpsAlert("audit-chain", "Security audit trail failed verification", new Dictionary<string, string>
            {
                ["Problem"] = result.Problem,
                ["First broken row"] = result.FirstBrokenId?.ToString() ?? "",
                ["Rows checked"] = result.RowsChecked.ToString(),
                ["Action"] = "Treat as a possible SEV1 incident: preserve the database and logs before changing anything",
            }, cooldownMinutes: 12 * 60);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppFileLog.Write("errors", "WARN", "SecurityAuditChainMonitor", "Audit chain check could not run", ex, sendAlert: false);
        }
    }
}

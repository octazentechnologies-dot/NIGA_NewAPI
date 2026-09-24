using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services;

/// <summary>
/// Marks audio case sessions stuck in Processing (e.g. worker died during RubricDiscovery) as Failed.
/// </summary>
public class AudioCaseZombieSessionSweeperBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AudioCaseTakingOptions _options;
    private readonly ILogger<AudioCaseZombieSessionSweeperBackgroundService> _logger;

    public AudioCaseZombieSessionSweeperBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AudioCaseTakingOptions> options,
        ILogger<AudioCaseZombieSessionSweeperBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableZombieSessionRecovery)
        {
            _logger.LogInformation("Audio case zombie session sweeper is disabled.");
            return;
        }

        var intervalMinutes = Math.Max(1, _options.ZombieSweeperIntervalMinutes);
        var startupDelay = TimeSpan.FromMinutes(1);

        try
        {
            await Task.Delay(startupDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IAudioCaseTakingService>();
                var recovered = await service.RecoverStaleProcessingSessionsAsync(stoppingToken);
                if (recovered > 0)
                {
                    _logger.LogWarning(
                        "Marked {Count} stale audio case session(s) as Failed (no progress for {StaleMinutes}+ minutes).",
                        recovered,
                        _options.ZombieSessionStaleMinutes);
                }
            }
            catch (Exception ex) when (IsSqlTimeout(ex))
            {
                _logger.LogWarning("Audio case zombie session sweep skipped because SQL was busy. The next interval will retry.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio case zombie session sweep failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static bool IsSqlTimeout(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number == -2)
                return true;
        }
        return false;
    }
}

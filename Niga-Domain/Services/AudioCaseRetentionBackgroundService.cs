using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services;

public class AudioCaseRetentionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AudioCaseTakingOptions _options;
    private readonly ILogger<AudioCaseRetentionBackgroundService> _logger;

    public AudioCaseRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AudioCaseTakingOptions> options,
        ILogger<AudioCaseRetentionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.AudioRetentionDays <= 0)
        {
            _logger.LogInformation("Audio case retention is disabled. Recordings are kept indefinitely.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IAudioCaseTakingService>();
                var purged = await service.PurgeExpiredAudioFilesAsync(stoppingToken);
                if (purged > 0)
                {
                    _logger.LogInformation("Audio case retention purged {Count} recording(s).", purged);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio case retention job failed.");
            }

            var delayHours = Math.Max(1, _options.RetentionJobIntervalHours);
            await Task.Delay(TimeSpan.FromHours(delayHours), stoppingToken);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.API.Domain.Interfaces;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Fires due doctor reminders on the server and pushes SignalR <c>reminderDue</c>
/// so open doctor tabs get instant toasts without polling GET /Doctor/Reminders.
/// Interval is short (10s) for near-real-time due detection.
/// </summary>
public sealed class DoctorReminderDueWorker : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DoctorReminderDueWorker> _logger;

    public DoctorReminderDueWorker(
        IServiceScopeFactory scopes,
        ILogger<DoctorReminderDueWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var s5 = scope.ServiceProvider.GetRequiredService<IS5Week5Service>();
                await s5.FireDueDoctorRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Doctor reminder due sweep failed");
            }

            try
            {
                await Task.Delay(SweepInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}

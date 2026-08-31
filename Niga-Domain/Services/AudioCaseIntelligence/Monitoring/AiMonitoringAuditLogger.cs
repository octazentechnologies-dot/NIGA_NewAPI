using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Services.AudioCaseIntelligence.Monitoring;

public class AiMonitoringAuditLogger
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<AiMonitoringAuditLogger> _logger;

    public AiMonitoringAuditLogger(NIGACentrumContext context, ILogger<AiMonitoringAuditLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<T> TrackAsync<T>(
        string operation,
        int? actorUserId,
        Func<Task<T>> action,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            stopwatch.Stop();
            await WriteAsync(
                "DashboardQuery",
                operation,
                actorUserId,
                correlationId,
                details,
                stopwatch.ElapsedMilliseconds,
                true,
                null,
                cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await WriteAsync(
                "DashboardQuery",
                operation,
                actorUserId,
                correlationId,
                details,
                stopwatch.ElapsedMilliseconds,
                false,
                ex.Message,
                cancellationToken);
            throw;
        }
    }

    public Task LogSnapshotRefreshAsync(
        int? actorUserId,
        DateTime snapshotDate,
        bool success,
        string? error,
        int durationMs,
        CancellationToken cancellationToken = default) =>
        WriteAsync(
            "SnapshotRefresh",
            "RefreshDailySnapshot",
            actorUserId,
            Guid.NewGuid(),
            new { snapshotDate, success },
            durationMs,
            success,
            error,
            cancellationToken);

    private async Task WriteAsync(
        string eventType,
        string operation,
        int? actorUserId,
        Guid correlationId,
        object? details,
        long durationMs,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var entry = new AiMonitoringAuditLog
        {
            EventType = eventType,
            Operation = operation,
            ActorUserId = actorUserId,
            CorrelationId = correlationId,
            DetailsJson = details == null ? null : JsonSerializer.Serialize(details),
            DurationMs = (int)Math.Min(int.MaxValue, durationMs),
            Success = success,
            ErrorMessage = errorMessage,
            EnteredDate = DateTime.UtcNow,
        };

        _context.AiMonitoringAuditLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        if (success)
        {
            _logger.LogInformation(
                "AIMonitoring {EventType} {Operation} correlation={CorrelationId} actor={ActorUserId} durationMs={DurationMs}",
                eventType,
                operation,
                correlationId,
                actorUserId,
                durationMs);
        }
        else
        {
            _logger.LogWarning(
                "AIMonitoring {EventType} {Operation} failed correlation={CorrelationId} actor={ActorUserId} durationMs={DurationMs} error={Error}",
                eventType,
                operation,
                correlationId,
                actorUserId,
                durationMs,
                errorMessage);
        }
    }
}

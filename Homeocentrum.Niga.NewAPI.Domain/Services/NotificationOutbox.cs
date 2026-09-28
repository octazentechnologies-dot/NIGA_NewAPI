using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;

namespace Homeocentrum.Niga.NewAPI.Domain.Services;

public interface INotificationOutbox
{
    Task EnqueueAsync(string channel, string eventType, string destination, string body, string status, string? lastError = null);
}

/// <summary>
/// PAY-04.04 / APT notify — durable SMS/WhatsApp rows until Msg91 / Meta keys are filled.
/// </summary>
public sealed class NotificationOutbox : INotificationOutbox
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<NotificationOutbox> _logger;

    public NotificationOutbox(IServiceScopeFactory scopes, ILogger<NotificationOutbox> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task EnqueueAsync(string channel, string eventType, string destination, string body, string status, string? lastError = null)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.NotificationOutbox (Channel, EventType, Destination, Body, Status, LastError, CreatedAt)
                VALUES ({channel}, {eventType}, {destination}, {body}, {status}, {lastError}, {DateTime.Now})");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotificationOutbox insert failed for {Channel} {EventType}", channel, eventType);
        }
    }
}

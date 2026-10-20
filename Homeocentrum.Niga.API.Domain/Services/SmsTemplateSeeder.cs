using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.API.Domain.Data;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Inserts missing SMS template rows. Does not update a row the clinic already saved, and does not send SMS.
/// </summary>
public sealed class SmsTemplateSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SmsTemplateSeeder> _logger;

    public SmsTemplateSeeder(IServiceScopeFactory scopes, ILogger<SmsTemplateSeeder> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
            var added = 0;
            foreach (var (code, body) in SmsTemplateCatalog.All)
            {
                var inserted = await db.Database.ExecuteSqlInterpolatedAsync($@"
                    IF OBJECT_ID(N'dbo.SmsTemplate', N'U') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM dbo.SmsTemplate WHERE Code = {code})
                    INSERT INTO dbo.SmsTemplate (Code, Body, IsActive) VALUES ({code}, {body}, 1)", cancellationToken);
                added += inserted;
            }
            _logger.LogInformation("SMS templates ready. Inserted {Added}. No message was sent.", added);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMS templates were not seeded. No message was sent.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

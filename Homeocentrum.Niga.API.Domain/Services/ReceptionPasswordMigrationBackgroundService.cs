using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Runs once at startup: re-hashes reception staff passwords still stored in the legacy reversible encoding (or plaintext)
/// with PBKDF2, so no recoverable password stays in the database. Staff keep the same password.
/// </summary>
public class ReceptionPasswordMigrationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReceptionPasswordMigrationBackgroundService> _logger;

    public ReceptionPasswordMigrationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReceptionPasswordMigrationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
            var legacy = await context.DoctorReceptionStaffs
                .Where(s => s.Password != null && s.Password != "" && !s.Password.StartsWith(UserPasswordHasher.Prefix))
                .ToListAsync(stoppingToken);
            if (legacy.Count == 0)
                return;

            foreach (var staff in legacy)
                staff.Password = ReceptionStaffPasswordHelper.HashPassword(ReceptionStaffPasswordHelper.LegacyPlaintext(staff.Password));
            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Re-hashed {Count} legacy reception staff password(s) with PBKDF2.", legacy.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reception staff password migration failed; legacy rows are re-hashed on next login instead.");
        }
    }
}

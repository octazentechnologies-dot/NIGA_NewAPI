using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Data;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// COM-01.02 — sends queued SMS rows once Msg91 or Twilio keys are present.
/// Rows stay pending while keys are empty. A failed send does not delete the row.
/// Does not send on API/IIS start: waits for <see cref="SmsOptions.StartupGraceMinutes"/>,
/// then suppresses outbox rows that already existed before this process started.
/// </summary>
public sealed class SmsOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<SmsOptions> _sms;
    private readonly SmsStartupGate _startup;
    private readonly ILogger<SmsOutboxWorker> _logger;
    private bool _suppressedPreStartRows;

    public SmsOutboxWorker(
        IServiceScopeFactory scopes,
        IOptions<SmsOptions> sms,
        SmsStartupGate startup,
        ILogger<SmsOutboxWorker> logger)
    {
        _scopes = scopes;
        _sms = sms;
        _startup = startup;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Never drain on the first tick after process start (IIS recycle / deploy).
        var initialWait = _startup.RemainingGrace;
        if (initialWait > TimeSpan.Zero)
        {
            _logger.LogInformation(
                "SMS outbox waiting {Minutes:0.#} min after process start before sending.",
                initialWait.TotalMinutes);
            try { await Task.Delay(initialWait, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_startup.IsInGracePeriod)
                {
                    _logger.LogDebug("SMS outbox skipped — still in startup grace period.");
                }
                else
                {
                    await DrainAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SMS outbox drain failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        var options = _sms.Value ?? new SmsOptions();
        if (!options.Enabled)
            return;
        var provider = (options.Provider ?? "Stub").Trim();
        var ready = provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase)
            ? options.Twilio.IsConfigured()
            : provider.Equals("Msg91", StringComparison.OrdinalIgnoreCase) && options.Msg91.IsConfigured();
        if (!ready)
            return;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISmsSender>();

        if (!_suppressedPreStartRows)
        {
            // Local CreatedAt is used elsewhere as DateTime.Now — compare in local clock.
            var processStartedLocal = _startup.ProcessStartedUtc.ToLocalTime();
            var suppressed = await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.NotificationOutbox
                SET Status = N'SKIPPED_STARTUP',
                    LastError = N'Suppressed on API/IIS process start (deploy or app-pool recycle).'
                WHERE Channel = N'SMS'
                  AND Status IN (N'PENDING', N'PENDING_KEYS', N'QUEUED')
                  AND CreatedAt < {processStartedLocal}", cancellationToken);
            _suppressedPreStartRows = true;
            if (suppressed > 0)
            {
                _logger.LogInformation(
                    "Suppressed {Count} pre-start SMS outbox row(s); they will not send after this process start.",
                    suppressed);
            }
        }

        var rows = await db.Database.SqlQuery<OutboxRow>($@"
            SELECT TOP 20 NotificationOutboxId, EventType, Destination, Body
            FROM dbo.NotificationOutbox
            WHERE Channel = N'SMS' AND Status IN (N'PENDING', N'PENDING_KEYS', N'QUEUED')
            ORDER BY NotificationOutboxId").ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            var mobile = Digits(row.Destination);
            if (mobile.Length < 10)
            {
                await MarkAsync(db, row.NotificationOutboxId, "FAILED", "Destination is not a mobile number.");
                continue;
            }
            var opted = await db.Database.SqlQuery<CountRow>($@"
                SELECT COUNT(1) AS Value FROM dbo.Patient
                WHERE REPLACE(REPLACE(ISNULL(MobileNo,''),' ',''),'-','') LIKE {'%' + mobile}
                  AND SmsOptOut = 1").FirstAsync(cancellationToken);
            if (opted.Value > 0)
            {
                await MarkAsync(db, row.NotificationOutboxId, "SKIPPED", "Patient opted out of SMS.");
                continue;
            }

            var sent = await sender.SendAsync(mobile, row.Body ?? "", cancellationToken);
            var status = sent ? "SENT" : "FAILED";
            await MarkAsync(db, row.NotificationOutboxId, status, sent ? null : "Provider did not accept the SMS.");
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SmsMessageLog (TemplateCode, Mobile, Body, Status)
                VALUES ({row.EventType}, {mobile}, {row.Body ?? ""}, {status})", cancellationToken);
        }
    }

    private static Task MarkAsync(NIGACentrumContext db, long id, string status, string? error)
        => db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.NotificationOutbox
            SET Status = {status}, LastError = {error}
            WHERE NotificationOutboxId = {id}");

    private static string Digits(string? value)
    {
        var text = new string((value ?? "").Where(char.IsDigit).ToArray());
        return text.Length > 10 ? text.Substring(text.Length - 10) : text;
    }

    private sealed class OutboxRow
    {
        public long NotificationOutboxId { get; set; }
        public string EventType { get; set; } = "";
        public string? Destination { get; set; }
        public string? Body { get; set; }
    }

    private sealed class CountRow
    {
        public int Value { get; set; }
    }
}

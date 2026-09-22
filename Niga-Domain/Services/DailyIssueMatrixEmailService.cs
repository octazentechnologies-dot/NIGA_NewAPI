using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Niga_Domain.Logging;

namespace Niga_Domain.Services
{
    /// <summary>
    /// At local midnight, emails the issue matrix for the calendar day that just ended.
    /// ErrorAlert:DailyMatrixEnabled turns this job on or off. It does not change instant error emails.
    /// </summary>
    public class DailyIssueMatrixEmailService : BackgroundService
    {
        private readonly ILogger<DailyIssueMatrixEmailService> _logger;

        public DailyIssueMatrixEmailService(ILogger<DailyIssueMatrixEmailService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!AppFileLog.IsDailyMatrixEnabled)
            {
                _logger.LogInformation("Daily issue matrix email is off (ErrorAlert:DailyMatrixEnabled=false).");
                return;
            }

            var preview = DailyIssueMatrix.Read(AppFileLog.LogsDirectory, DateTime.Today, "New API");
            _logger.LogInformation(
                "Daily issue matrix email is on. Next send is local midnight. Issues logged so far today: {Count}.",
                preview.Sum(row => row.Count));
            foreach (var row in preview)
            {
                _logger.LogInformation(
                    "Daily matrix preview {Source} x{Count}: {RootCause}",
                    row.Source, row.Count, row.RootCause);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(DelayUntilNextLocalMidnight(DateTime.Now), stoppingToken);
                    var day = DateTime.Now.Date.AddDays(-1);
                    AppFileLog.SendDailyMatrix(day, "New API");
                    _logger.LogInformation("Daily issue matrix email sent for {Day}.", day.ToString("dd-MMM-yyyy"));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Daily issue matrix email failed.");
                }
            }
        }

        /// <summary>Wait until the next local 00:00. A start exactly at midnight waits a full day so it does not send twice.</summary>
        public static TimeSpan DelayUntilNextLocalMidnight(DateTime now)
        {
            var next = now.Date.AddDays(1);
            var delay = next - now;
            return delay < TimeSpan.FromSeconds(1) ? TimeSpan.FromDays(1) : delay;
        }
    }
}

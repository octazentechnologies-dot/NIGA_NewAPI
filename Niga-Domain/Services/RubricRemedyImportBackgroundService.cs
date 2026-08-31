using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Niga_Domain.Interface;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services
{
    public class RubricRemedyImportBackgroundService : BackgroundService
    {
        private readonly IRubricRemedyImportQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RubricRemedyImportBackgroundService> _logger;

        public RubricRemedyImportBackgroundService(
            IRubricRemedyImportQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<RubricRemedyImportBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Starting rubric import job {JobId} for {FileName}", job.JobId, job.OriginalFileName);
                    _queue.MarkRunning(job.JobId, 0);

                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var importService = scope.ServiceProvider.GetRequiredService<IRubricRemedyDetailsService>();

                    var result = await importService.ImportFromExcelFileAsync(
                        job.FilePath,
                        job.OriginalFileName,
                        (processed, total) => _queue.ReportProgress(job.JobId, processed, total),
                        stoppingToken);

                    if (!string.IsNullOrEmpty(result.Message) && result.Message.StartsWith("Error during import:", StringComparison.OrdinalIgnoreCase)
                        && result.SuccessCount == 0 && result.FailureCount == result.TotalRows && result.TotalRows > 0
                        && result.Errors != null && result.Errors.Count > 0)
                    {
                        _queue.MarkFailed(job.JobId, result.Errors[0]);
                    }
                    else
                    {
                        _queue.MarkCompleted(job.JobId, result);
                    }

                    _logger.LogInformation(
                        "Finished rubric import job {JobId}. Success={Success} Failed={Failed} Total={Total}",
                        job.JobId, result.SuccessCount, result.FailureCount, result.TotalRows);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rubric import job {JobId} failed", job.JobId);
                    _queue.MarkFailed(job.JobId, ex.Message);
                }
                finally
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(job.FilePath) && File.Exists(job.FilePath))
                            File.Delete(job.FilePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to delete temp import file for job {JobId}", job.JobId);
                    }
                }
            }
        }
    }
}

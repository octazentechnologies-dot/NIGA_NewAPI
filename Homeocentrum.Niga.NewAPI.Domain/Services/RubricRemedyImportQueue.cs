using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services
{
    public class RubricRemedyImportQueue : IRubricRemedyImportQueue
    {
        private readonly Channel<RubricRemedyImportJob> _channel = Channel.CreateUnbounded<RubricRemedyImportJob>(
            new UnboundedChannelOptions { SingleReader = true });

        private readonly ConcurrentDictionary<string, ImportJobStatusModel> _jobs = new();

        public ChannelReader<RubricRemedyImportJob> Reader => _channel.Reader;

        public async Task<ImportJobStartResult> EnqueueAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File not selected or empty");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".csv")
                throw new ArgumentException("Please upload a valid file (.xlsx or .csv)");

            var jobId = Guid.NewGuid().ToString("N");
            var importDir = Path.Combine(Path.GetTempPath(), "niga-rubric-imports");
            Directory.CreateDirectory(importDir);

            var safeName = Path.GetFileName(file.FileName) ?? $"import{ext}";
            var filePath = Path.Combine(importDir, $"{jobId}_{safeName}");

            await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs, cancellationToken);
            }

            var status = new ImportJobStatusModel
            {
                JobId = jobId,
                Status = ImportJobStatuses.Queued,
                FileName = file.FileName,
                Message = "Import queued. Processing will start shortly.",
                CreatedAtUtc = DateTime.UtcNow,
            };
            _jobs[jobId] = status;

            await _channel.Writer.WriteAsync(new RubricRemedyImportJob
            {
                JobId = jobId,
                FilePath = filePath,
                OriginalFileName = file.FileName,
            }, cancellationToken);

            return new ImportJobStartResult
            {
                JobId = jobId,
                Status = ImportJobStatuses.Queued,
                Message = "Import started in background. Poll ImportFromExcel/Status/{jobId} for progress.",
            };
        }

        public ImportJobStatusModel GetStatus(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return null;
            return _jobs.TryGetValue(jobId, out var status) ? CloneStatus(status) : null;
        }

        public void MarkRunning(string jobId, int totalRows)
        {
            if (!_jobs.TryGetValue(jobId, out var status))
                return;

            lock (status)
            {
                status.Status = ImportJobStatuses.Running;
                status.StartedAtUtc ??= DateTime.UtcNow;
                status.TotalRows = totalRows;
                status.Message = totalRows > 0
                    ? $"Processing {totalRows} rows..."
                    : "Processing...";
                UpdateProgress(status);
            }
        }

        public void ReportProgress(string jobId, int processedRows, int totalRows)
        {
            if (!_jobs.TryGetValue(jobId, out var status))
                return;

            lock (status)
            {
                status.Status = ImportJobStatuses.Running;
                status.ProcessedRows = processedRows;
                status.TotalRows = totalRows;
                status.Message = $"Processed {processedRows} of {totalRows} rows...";
                UpdateProgress(status);
            }
        }

        public void MarkCompleted(string jobId, ImportResultModel result)
        {
            if (!_jobs.TryGetValue(jobId, out var status))
                return;

            lock (status)
            {
                status.Status = ImportJobStatuses.Completed;
                status.CompletedAtUtc = DateTime.UtcNow;
                status.Result = result;
                status.TotalRows = result?.TotalRows ?? status.TotalRows;
                status.ProcessedRows = result?.TotalRows ?? status.ProcessedRows;
                status.ProgressPercent = 100;
                status.Message = result?.Message ?? "Import completed.";
                status.Error = null;
            }
        }

        public void MarkFailed(string jobId, string error)
        {
            if (!_jobs.TryGetValue(jobId, out var status))
                return;

            lock (status)
            {
                status.Status = ImportJobStatuses.Failed;
                status.CompletedAtUtc = DateTime.UtcNow;
                status.Error = error;
                status.Message = "Import failed: " + error;
            }
        }

        private static void UpdateProgress(ImportJobStatusModel status)
        {
            if (status.TotalRows <= 0)
            {
                status.ProgressPercent = 0;
                return;
            }

            status.ProgressPercent = Math.Clamp(
                (int)Math.Round(100.0 * status.ProcessedRows / status.TotalRows),
                0,
                99);
        }

        private static ImportJobStatusModel CloneStatus(ImportJobStatusModel status)
        {
            lock (status)
            {
                return new ImportJobStatusModel
                {
                    JobId = status.JobId,
                    Status = status.Status,
                    FileName = status.FileName,
                    ProcessedRows = status.ProcessedRows,
                    TotalRows = status.TotalRows,
                    ProgressPercent = status.ProgressPercent,
                    Message = status.Message,
                    Error = status.Error,
                    CreatedAtUtc = status.CreatedAtUtc,
                    StartedAtUtc = status.StartedAtUtc,
                    CompletedAtUtc = status.CompletedAtUtc,
                    Result = status.Result,
                };
            }
        }
    }
}

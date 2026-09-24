using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    public interface IRubricRemedyImportQueue
    {
        ChannelReader<RubricRemedyImportJob> Reader { get; }
        Task<ImportJobStartResult> EnqueueAsync(IFormFile file, CancellationToken cancellationToken = default);
        ImportJobStatusModel GetStatus(string jobId);
        void MarkRunning(string jobId, int totalRows);
        void ReportProgress(string jobId, int processedRows, int totalRows);
        void MarkCompleted(string jobId, ImportResultModel result);
        void MarkFailed(string jobId, string error);
    }
}

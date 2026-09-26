using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

/// <summary>
/// Hangfire-ready adapter. Example recurring registration:
/// RecurringJob.AddOrUpdate&lt;IAiEmbeddingRefreshJobRunner&gt;(
///     "ai-embedding-incremental",
///     runner => runner.RunAsync(new IncrementalEmbeddingRefreshRequest { TriggerSource = "Hangfire" }, CancellationToken.None),
///     Cron.DayInterval(2));
/// </summary>
public class AiEmbeddingRefreshJobRunner : IAiEmbeddingRefreshJobRunner
{
    private readonly IAiIncrementalEmbeddingRefreshService _refreshService;

    public AiEmbeddingRefreshJobRunner(IAiIncrementalEmbeddingRefreshService refreshService) =>
        _refreshService = refreshService;

    public Task<IncrementalEmbeddingRefreshResult> RunAsync(
        IncrementalEmbeddingRefreshRequest request,
        CancellationToken cancellationToken = default) =>
        _refreshService.RefreshAsync(request, cancellationToken);
}

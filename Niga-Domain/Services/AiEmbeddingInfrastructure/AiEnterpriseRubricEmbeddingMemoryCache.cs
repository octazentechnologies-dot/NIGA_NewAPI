using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AiEmbeddingInfrastructure;

public class AiEnterpriseRubricEmbeddingMemoryCache : IAiEnterpriseRubricEmbeddingMemoryCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiEnterpriseRubricEmbeddingMemoryCache> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly object _sync = new();
    private List<AiEnterpriseRubricEmbeddingCacheEntry> _entries = new();

    public AiEnterpriseRubricEmbeddingMemoryCache(
        IServiceScopeFactory scopeFactory,
        ILogger<AiEnterpriseRubricEmbeddingMemoryCache> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Guid? EmbeddingVersionId { get; private set; }

    public IReadOnlyList<AiEnterpriseRubricEmbeddingCacheEntry> Entries
    {
        get
        {
            lock (_sync)
            {
                return _entries;
            }
        }
    }

    public DateTime? LastRefreshedUtc { get; private set; }

    public async Task RefreshAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var sw = Stopwatch.StartNew();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IAiEmbeddingUnitOfWork>();
            var loaded = await unitOfWork.RubricEmbeddings.LoadActiveCacheEntriesAsync(embeddingVersionId, cancellationToken);
            lock (_sync)
            {
                _entries = loaded;
                EmbeddingVersionId = embeddingVersionId;
                LastRefreshedUtc = DateTime.UtcNow;
            }

            _logger.LogInformation(
                "Enterprise rubric embedding cache refreshed for version {VersionId} with {Count} vector(s) in {ElapsedMs}ms.",
                embeddingVersionId,
                loaded.Count,
                sw.ElapsedMilliseconds);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}

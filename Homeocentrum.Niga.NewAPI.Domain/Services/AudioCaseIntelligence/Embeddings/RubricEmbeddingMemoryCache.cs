using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;

public class RubricEmbeddingMemoryCache : IRubricEmbeddingMemoryCache
{
    private readonly IRubricEmbeddingRepository _repository;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ILogger<RubricEmbeddingMemoryCache> _logger;
    private readonly object _sync = new();
    private List<RubricEmbeddingCacheEntry> _entries = new();

    public RubricEmbeddingMemoryCache(
        IRubricEmbeddingRepository repository,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<RubricEmbeddingMemoryCache> logger)
    {
        _repository = repository;
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
    }

    public IReadOnlyList<RubricEmbeddingCacheEntry> Entries
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

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var loaded = await _repository.LoadAllAsync(_openAiOptions.EmbeddingModel, cancellationToken);
        lock (_sync)
        {
            _entries = loaded;
            LastRefreshedUtc = DateTime.UtcNow;
        }

        _logger.LogInformation("Rubric embedding cache refreshed with {Count} vector(s).", loaded.Count);
    }
}

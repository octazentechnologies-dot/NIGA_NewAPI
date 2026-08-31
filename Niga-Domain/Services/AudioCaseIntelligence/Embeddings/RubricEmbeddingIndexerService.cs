using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Embeddings;

namespace Niga_Domain.Services.AudioCaseIntelligence.Embeddings;

public class RubricEmbeddingIndexerService : IRubricEmbeddingIndexerService
{
    private readonly NIGACentrumContext _context;
    private readonly IRubricEmbeddingRepository _repository;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IRubricEmbeddingMemoryCache _cache;
    private readonly OpenAiOptions _openAiOptions;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<RubricEmbeddingIndexerService> _logger;

    public RubricEmbeddingIndexerService(
        NIGACentrumContext context,
        IRubricEmbeddingRepository repository,
        IEmbeddingClient embeddingClient,
        IRubricEmbeddingMemoryCache cache,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<RubricIntelligenceOptions> intelligenceOptions,
        ILogger<RubricEmbeddingIndexerService> logger)
    {
        _context = context;
        _repository = repository;
        _embeddingClient = embeddingClient;
        _cache = cache;
        _openAiOptions = openAiOptions.Value;
        _options = intelligenceOptions.Value;
        _logger = logger;
    }

    public async Task<EmbeddingReindexResult> ReindexAsync(
        int? maxRubrics = null,
        CancellationToken cancellationToken = default)
    {
        var result = new EmbeddingReindexResult();
        var limit = maxRubrics ?? _options.EmbeddingIndexerMaxRubricsPerRun;

        if (!_embeddingClient.IsConfigured)
        {
            result.Error = "Embedding API is not configured. Set OpenAI:ApiKey or disable indexer until configured.";
            return result;
        }

        var rubrics = await _context.SubSectionMasters
            .AsNoTracking()
            .Where(x => !x.DeleteStatus && x.SubSectionName != null && x.SubSectionName != "")
            .OrderBy(x => x.SubSectionId)
            .Take(limit)
            .Select(x => new { x.SubSectionId, x.SubSectionName })
            .ToListAsync(cancellationToken);

        var existingHashes = await _repository.GetExistingHashesAsync(
            rubrics.Select(x => x.SubSectionId),
            _openAiOptions.EmbeddingModel,
            cancellationToken);

        var pending = rubrics
            .Where(r =>
            {
                var hash = RubricEmbeddingRepository.ComputeTextHash(r.SubSectionName!);
                return !existingHashes.TryGetValue(r.SubSectionId, out var existing) || existing != hash;
            })
            .ToList();

        result.Skipped = rubrics.Count - pending.Count;

        for (var offset = 0; offset < pending.Count; offset += _options.EmbeddingIndexerBatchSize)
        {
            var batch = pending.Skip(offset).Take(_options.EmbeddingIndexerBatchSize).ToList();
            var texts = batch.Select(x => x.SubSectionName!.Trim()).ToList();
            var embedResult = await _embeddingClient.EmbedTextsAsync(texts, cancellationToken);

            if (!embedResult.Success || embedResult.Vectors.Count != batch.Count)
            {
                result.Failed += batch.Count;
                _logger.LogWarning("Embedding batch failed at offset {Offset}: {Error}", offset, embedResult.Error);
                continue;
            }

            for (var i = 0; i < batch.Count; i++)
            {
                try
                {
                    var item = batch[i];
                    var hash = RubricEmbeddingRepository.ComputeTextHash(item.SubSectionName!);
                    var existed = existingHashes.ContainsKey(item.SubSectionId);
                    await _repository.UpsertAsync(
                        item.SubSectionId,
                        item.SubSectionName!,
                        embedResult.Vectors[i],
                        _openAiOptions.EmbeddingModel,
                        hash,
                        "SubSection",
                        cancellationToken);

                    result.Processed++;
                    if (existed) result.Updated++;
                    else result.Created++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    _logger.LogWarning(ex, "Failed to upsert embedding for rubric {RubricId}", batch[i].SubSectionId);
                }
            }
        }

        try
        {
            await _cache.RefreshAsync(cancellationToken);
            result.CacheRefreshed = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding cache refresh failed after reindex.");
        }

        _logger.LogInformation(
            "Embedding reindex complete: processed={Processed}, created={Created}, updated={Updated}, skipped={Skipped}, failed={Failed}",
            result.Processed, result.Created, result.Updated, result.Skipped, result.Failed);

        return result;
    }
}

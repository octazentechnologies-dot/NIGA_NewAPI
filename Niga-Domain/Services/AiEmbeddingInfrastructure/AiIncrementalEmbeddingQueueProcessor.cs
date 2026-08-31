using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Services.AiEmbeddingInfrastructure;

public class AiIncrementalEmbeddingQueueProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IRepertoryRubricCatalogReader _catalogReader;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IAiEmbeddingQueueService _queueService;
    private readonly IAiEmbeddingAuditService _auditService;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiIncrementalEmbeddingQueueProcessor> _logger;

    public AiIncrementalEmbeddingQueueProcessor(
        IAiEmbeddingUnitOfWork unitOfWork,
        IRepertoryRubricCatalogReader catalogReader,
        IEmbeddingClient embeddingClient,
        IAiEmbeddingQueueService queueService,
        IAiEmbeddingAuditService auditService,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiIncrementalEmbeddingQueueProcessor> logger)
    {
        _unitOfWork = unitOfWork;
        _catalogReader = catalogReader;
        _embeddingClient = embeddingClient;
        _queueService = queueService;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(int Processed, int Created, int Updated, int Skipped, int Archived, int Failed, int DeadLetter)> ProcessJobAsync(
        Guid jobId,
        Guid embeddingVersionId,
        AiEmbeddingVersionModel version,
        int? maxItems,
        int? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var processed = 0;
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var archived = 0;
        var failed = 0;
        var deadLetter = 0;
        var workerId = _options.IncrementalWorkerId;
        var batchSize = Math.Max(1, _options.IncrementalQueueWorkerBatchSize);
        var max = maxItems ?? _options.IncrementalMaxQueueItemsPerRun;
        if (max <= 0)
            max = int.MaxValue;

        await _unitOfWork.Queue.ReleaseStaleLocksAsync(DateTime.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        while (processed + skipped + archived + failed + deadLetter < max)
        {
            var take = Math.Min(batchSize, max - (processed + skipped + archived + failed + deadLetter));
            var batch = await _unitOfWork.Queue.DequeueBatchForJobAsync(
                jobId,
                take,
                workerId,
                TimeSpan.FromMinutes(_options.QueueLockMinutes),
                cancellationToken);

            if (batch.Count == 0)
                break;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var embedCandidates = new List<(AiEmbeddingQueue Item, RubricSemanticDocument Document)>();
            var archiveItems = new List<AiEmbeddingQueue>();
            var embedQueueItems = new List<AiEmbeddingQueue>();

            foreach (var item in batch)
            {
                if (item.RubricId == null)
                {
                    await FailItemAsync(item, "Queue item missing RubricId.", actorUserId, cancellationToken);
                    failed++;
                    continue;
                }

                var payload = ParsePayload(item.PayloadJson);
                if (string.Equals(payload.ChangeType, AiEmbeddingQueueChangeTypes.Deleted, StringComparison.OrdinalIgnoreCase))
                {
                    archiveItems.Add(item);
                    continue;
                }

                embedQueueItems.Add(item);
            }

            var catalogById = (await _catalogReader.ReadByIdsAsync(
                embedQueueItems.Select(x => x.RubricId!.Value),
                cancellationToken)).ToDictionary(x => x.RubricId);

            var hashMap = await _unitOfWork.RubricEmbeddings.GetActiveTextHashesAsync(
                embeddingVersionId,
                embedQueueItems.Select(x => x.RubricId!.Value),
                cancellationToken);

            foreach (var item in embedQueueItems)
            {
                if (!catalogById.TryGetValue(item.RubricId!.Value, out var catalogItem)
                    || string.IsNullOrWhiteSpace(catalogItem.RubricName))
                {
                    await FailItemAsync(item, $"Rubric {item.RubricId} not found in catalog.", actorUserId, cancellationToken);
                    failed++;
                    continue;
                }

                var document = RubricSemanticDocumentBuilder.Build(catalogItem);
                if (RubricSemanticDocumentBuilder.IsTitleOnly(document))
                {
                    await CompleteSkippedAsync(item, actorUserId, cancellationToken);
                    skipped++;
                    continue;
                }

                if (hashMap.TryGetValue(item.RubricId.Value, out var existingHash)
                    && string.Equals(existingHash, document.TextHash, StringComparison.OrdinalIgnoreCase))
                {
                    await CompleteSkippedAsync(item, actorUserId, cancellationToken);
                    skipped++;
                    continue;
                }

                embedCandidates.Add((item, document));
            }

            foreach (var item in archiveItems)
            {
                try
                {
                    var archivedNow = await _unitOfWork.RubricEmbeddings.ArchiveActiveByRubricAsync(
                        embeddingVersionId,
                        item.RubricId!.Value,
                        jobId,
                        cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    if (archivedNow)
                    {
                        await _queueService.MarkCompletedAsync(item.QueueId, actorUserId, cancellationToken);
                        archived++;
                        processed++;
                        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
                        {
                            EntityType = AiEmbeddingEntityTypes.RubricEmbedding,
                            EntityId = item.RubricId!.Value.ToString(),
                            Action = "Archive",
                            NewStatus = AiEmbeddingStatuses.Archived,
                            JobId = jobId,
                            QueueId = item.QueueId,
                            EmbeddingVersionId = embeddingVersionId,
                            ActorUserId = actorUserId,
                        }, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        await CompleteSkippedAsync(item, actorUserId, cancellationToken);
                        skipped++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to archive rubric embedding {RubricId}", item.RubricId);
                    await FailItemAsync(item, ex.Message, actorUserId, cancellationToken);
                    failed++;
                }
            }

            for (var offset = 0; offset < embedCandidates.Count; offset += batchSize)
            {
                var slice = embedCandidates.Skip(offset).Take(batchSize).ToList();
                var texts = slice.Select(x => x.Document.SourceText).ToList();
                var embedResult = await _embeddingClient.EmbedTextsAsync(texts, version.ModelName, cancellationToken);

                if (!embedResult.Success || embedResult.Vectors.Count != slice.Count)
                {
                    foreach (var (item, _) in slice)
                    {
                        await FailItemAsync(item, embedResult.Error ?? "Embedding API batch failed.", actorUserId, cancellationToken);
                        failed++;
                    }

                    continue;
                }

                for (var i = 0; i < slice.Count; i++)
                {
                    var (item, document) = slice[i];
                    try
                    {
                        var hadExisting = (await _unitOfWork.RubricEmbeddings.GetActiveTextHashesAsync(
                            embeddingVersionId,
                            new[] { document.RubricId },
                            cancellationToken)).ContainsKey(document.RubricId);

                        var outcome = await _unitOfWork.RubricEmbeddings.UpsertActiveEmbeddingAsync(
                            embeddingVersionId,
                            document.RubricId,
                            document.SourceText,
                            document.TextHash,
                            embedResult.Vectors[i],
                            version.DimensionCount > 0 ? version.DimensionCount : embedResult.Vectors[i].Length,
                            jobId,
                            cancellationToken);

                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        if (outcome == AiRubricEmbeddingUpsertOutcome.Skipped)
                        {
                            await CompleteSkippedAsync(item, actorUserId, cancellationToken);
                            skipped++;
                            continue;
                        }

                        await _queueService.MarkCompletedAsync(item.QueueId, actorUserId, cancellationToken);
                        processed++;

                        if (outcome == AiRubricEmbeddingUpsertOutcome.Created)
                            created++;
                        else
                            updated++;

                        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
                        {
                            EntityType = AiEmbeddingEntityTypes.RubricEmbedding,
                            EntityId = document.RubricId.ToString(),
                            Action = hadExisting ? "Update" : "Create",
                            NewStatus = AiEmbeddingStatuses.Active,
                            JobId = jobId,
                            QueueId = item.QueueId,
                            EmbeddingVersionId = embeddingVersionId,
                            ActorUserId = actorUserId,
                            DetailsJson = JsonSerializer.Serialize(new { document.TextHash, payload = ParsePayload(item.PayloadJson) }, JsonOptions),
                        }, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process queue item {QueueId} rubric {RubricId}", item.QueueId, document.RubricId);
                        await FailItemAsync(item, ex.Message, actorUserId, cancellationToken);
                        failed++;
                    }
                }
            }

            var pendingAfterBatch = await _unitOfWork.Queue.CountByJobAndStatusAsync(jobId, AiEmbeddingStatuses.Pending, cancellationToken);
            if (pendingAfterBatch == 0)
                break;
        }

        deadLetter = await _unitOfWork.Queue.CountByJobAndStatusAsync(jobId, AiEmbeddingStatuses.DeadLetter, cancellationToken);

        return (processed, created, updated, skipped, archived, failed, deadLetter);
    }

    private async Task CompleteSkippedAsync(AiEmbeddingQueue item, int? actorUserId, CancellationToken cancellationToken)
    {
        await _queueService.MarkSkippedAsync(item.QueueId, actorUserId, cancellationToken);
    }

    private async Task FailItemAsync(
        AiEmbeddingQueue item,
        string error,
        int? actorUserId,
        CancellationToken cancellationToken)
    {
        await _queueService.MarkFailedAsync(item.QueueId, error, actorUserId, cancellationToken);
    }

    public static AiEmbeddingQueuePayload ParsePayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new AiEmbeddingQueuePayload { DetectedAtUtc = DateTime.UtcNow };

        try
        {
            return JsonSerializer.Deserialize<AiEmbeddingQueuePayload>(payloadJson, JsonOptions)
                ?? new AiEmbeddingQueuePayload { DetectedAtUtc = DateTime.UtcNow };
        }
        catch
        {
            return new AiEmbeddingQueuePayload { DetectedAtUtc = DateTime.UtcNow };
        }
    }

    public static string SerializePayload(RubricEmbeddingChangeCandidate candidate) =>
        JsonSerializer.Serialize(new AiEmbeddingQueuePayload
        {
            ChangeType = candidate.ChangeType,
            Reasons = candidate.Reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            DetectedAtUtc = candidate.DetectedAtUtc,
        }, JsonOptions);
}

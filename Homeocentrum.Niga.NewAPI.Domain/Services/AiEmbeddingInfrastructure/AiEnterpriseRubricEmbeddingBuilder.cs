using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public class AiEnterpriseRubricEmbeddingBuilder : IAiEnterpriseRubricEmbeddingBuilder
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IRepertoryRubricCatalogReader _catalogReader;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IAiEmbeddingVersionService _versionService;
    private readonly IAiEmbeddingJobService _jobService;
    private readonly IAiEmbeddingAuditService _auditService;
    private readonly IAiEmbeddingStatisticsService _statisticsService;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ILogger<AiEnterpriseRubricEmbeddingBuilder> _logger;

    public AiEnterpriseRubricEmbeddingBuilder(
        IAiEmbeddingUnitOfWork unitOfWork,
        IRepertoryRubricCatalogReader catalogReader,
        IEmbeddingClient embeddingClient,
        IAiEmbeddingVersionService versionService,
        IAiEmbeddingJobService jobService,
        IAiEmbeddingAuditService auditService,
        IAiEmbeddingStatisticsService statisticsService,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<AiEnterpriseRubricEmbeddingBuilder> logger)
    {
        _unitOfWork = unitOfWork;
        _catalogReader = catalogReader;
        _embeddingClient = embeddingClient;
        _versionService = versionService;
        _jobService = jobService;
        _auditService = auditService;
        _statisticsService = statisticsService;
        _options = options.Value;
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
    }

    public async Task<EnterpriseRubricEmbeddingBuildResult> BuildAsync(
        BuildEnterpriseRubricEmbeddingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new EnterpriseRubricEmbeddingBuildResult();

        if (!_options.Enabled || !_options.EnableEnterpriseBuilder)
        {
            result.Error = "Enterprise embedding builder is disabled.";
            return result;
        }

        if (!_embeddingClient.IsConfigured)
        {
            result.Error = "Embedding API is not configured.";
            return result;
        }

        var version = await ResolveVersionAsync(request.EmbeddingVersionId, request.ActorUserId, cancellationToken);
        if (version == null)
        {
            result.Error = "No embedding version is available.";
            return result;
        }

        result.EmbeddingVersionId = version.EmbeddingVersionId;
        result.VersionCode = version.VersionCode;

        var jobType = request.FullReindex ? AiEmbeddingJobTypes.FullReindex : AiEmbeddingJobTypes.Incremental;
        var (jobCreated, jobMessage, jobModel) = await _jobService.CreateJobAsync(new CreateAiEmbeddingJobRequest
        {
            EmbeddingVersionId = version.EmbeddingVersionId,
            JobType = jobType,
            TriggerSource = request.TriggerSource ?? "EnterpriseBuilder",
            CreatedByUserId = request.ActorUserId,
        }, cancellationToken);

        if (!jobCreated || jobModel == null)
        {
            result.Error = jobMessage;
            return result;
        }

        result.JobId = jobModel.JobId;
        await _jobService.MarkJobRunningAsync(jobModel.JobId, request.ActorUserId, cancellationToken);

        var totalRubrics = await _catalogReader.CountRubricsAsync(cancellationToken);
        var maxRubrics = request.MaxRubrics ?? _options.BuilderMaxRubricsPerRun;
        if (maxRubrics > 0)
            totalRubrics = Math.Min(totalRubrics, maxRubrics);

        result.TotalCatalogued = totalRubrics;
        await UpdateJobProgressAsync(jobModel.JobId, totalRubrics, 0, 0, 0, cancellationToken);

        var pageSize = Math.Max(1, _options.BuilderCatalogPageSize);
        var batchSize = Math.Max(1, _options.BuilderBatchSize);
        var skip = 0;

        try
        {
            while (skip < totalRubrics)
            {
                var take = Math.Min(pageSize, totalRubrics - skip);
                var page = await _catalogReader.ReadPageAsync(skip, take, cancellationToken);
                if (page.Count == 0)
                    break;

                var documents = page
                    .Select(RubricSemanticDocumentBuilder.Build)
                    .Where(doc => !RubricSemanticDocumentBuilder.IsTitleOnly(doc))
                    .ToList();

                var existingHashes = request.FullReindex
                    ? new Dictionary<int, string>()
                    : await _unitOfWork.RubricEmbeddings.GetActiveTextHashesAsync(
                        version.EmbeddingVersionId,
                        documents.Select(x => x.RubricId),
                        cancellationToken);

                var pending = documents
                    .Where(doc => !existingHashes.TryGetValue(doc.RubricId, out var hash)
                        || !string.Equals(hash, doc.TextHash, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                result.Skipped += documents.Count - pending.Count;

                for (var offset = 0; offset < pending.Count; offset += batchSize)
                {
                    var batch = pending.Skip(offset).Take(batchSize).ToList();
                    var texts = batch.Select(x => x.SourceText).ToList();
                    var embedResult = await _embeddingClient.EmbedTextsAsync(
                        texts,
                        version.ModelName,
                        cancellationToken);

                    if (!embedResult.Success || embedResult.Vectors.Count != batch.Count)
                    {
                        result.Failed += batch.Count;
                        _logger.LogWarning(
                            "Enterprise embedding batch failed at skip={Skip} offset={Offset}: {Error}",
                            skip,
                            offset,
                            embedResult.Error);
                        continue;
                    }

                    for (var i = 0; i < batch.Count; i++)
                    {
                        try
                        {
                            var doc = batch[i];
                            var hadExisting = existingHashes.ContainsKey(doc.RubricId);
                            var outcome = await _unitOfWork.RubricEmbeddings.UpsertActiveEmbeddingAsync(
                                version.EmbeddingVersionId,
                                doc.RubricId,
                                doc.SourceText,
                                doc.TextHash,
                                embedResult.Vectors[i],
                                version.DimensionCount > 0 ? version.DimensionCount : embedResult.Vectors[i].Length,
                                jobModel.JobId,
                                cancellationToken);

                            await _unitOfWork.SaveChangesAsync(cancellationToken);

                            switch (outcome)
                            {
                                case AiRubricEmbeddingUpsertOutcome.Created:
                                    result.Created++;
                                    result.Processed++;
                                    break;
                                case AiRubricEmbeddingUpsertOutcome.Updated:
                                    result.Updated++;
                                    result.Processed++;
                                    break;
                                case AiRubricEmbeddingUpsertOutcome.Skipped:
                                    result.Skipped++;
                                    break;
                            }

                            if (outcome != AiRubricEmbeddingUpsertOutcome.Skipped)
                            {
                                await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
                                {
                                    EntityType = AiEmbeddingEntityTypes.RubricEmbedding,
                                    EntityId = doc.RubricId.ToString(),
                                    Action = hadExisting ? "Update" : "Create",
                                    NewStatus = AiEmbeddingStatuses.Active,
                                    JobId = jobModel.JobId,
                                    EmbeddingVersionId = version.EmbeddingVersionId,
                                    ActorUserId = request.ActorUserId,
                                    DetailsJson = $"{{\"textHash\":\"{doc.TextHash}\",\"revisionSource\":\"SemanticDocument\"}}",
                                }, cancellationToken);
                                await _unitOfWork.SaveChangesAsync(cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Failed++;
                            _logger.LogWarning(ex, "Failed to store enterprise embedding for rubric {RubricId}", batch[i].RubricId);
                        }
                    }
                }

                skip += page.Count;
                await UpdateJobProgressAsync(
                    jobModel.JobId,
                    totalRubrics,
                    result.Processed,
                    result.Skipped,
                    result.Failed,
                    cancellationToken);
            }

            await _jobService.MarkJobCompletedAsync(jobModel.JobId, request.ActorUserId, cancellationToken);
            await _statisticsService.RefreshDailySnapshotAsync(version.EmbeddingVersionId, cancellationToken: cancellationToken);

            result.Success = result.Failed == 0 || result.Processed > 0 || result.Skipped > 0;
            _logger.LogInformation(
                "Enterprise rubric embedding build complete version={VersionCode} processed={Processed} created={Created} updated={Updated} skipped={Skipped} failed={Failed}",
                version.VersionCode,
                result.Processed,
                result.Created,
                result.Updated,
                result.Skipped,
                result.Failed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enterprise rubric embedding build failed for job {JobId}", jobModel.JobId);
            await _jobService.MarkJobFailedAsync(jobModel.JobId, ex.Message, request.ActorUserId, cancellationToken);
            result.Error = ex.Message;
            return result;
        }
    }

    private async Task<AiEmbeddingVersionModel?> ResolveVersionAsync(
        Guid? requestedVersionId,
        int? actorUserId,
        CancellationToken cancellationToken)
    {
        if (requestedVersionId.HasValue)
        {
            var explicitVersion = await _unitOfWork.Versions.GetByIdAsync(requestedVersionId.Value, cancellationToken);
            return explicitVersion == null ? null : MapVersion(explicitVersion);
        }

        var current = await _versionService.GetCurrentVersionAsync(cancellationToken);
        if (current != null)
            return current;

        if (!_options.AutoEnsureEmbeddingVersion)
            return null;

        var modelName = string.IsNullOrWhiteSpace(_openAiOptions.EmbeddingModel)
            ? _options.DefaultModelName
            : _openAiOptions.EmbeddingModel;
        var versionCode = BuildVersionCode(modelName, _options.DefaultDimensionCount);

        if (await _unitOfWork.Versions.VersionCodeExistsAsync(versionCode, cancellationToken))
        {
            var versions = await _unitOfWork.Versions.ListActiveAsync(cancellationToken);
            var existing = versions.FirstOrDefault(x => x.VersionCode == versionCode);
            if (existing != null)
            {
                await _versionService.ActivateVersionAsync(existing.EmbeddingVersionId, actorUserId, cancellationToken);
                return MapVersion(existing);
            }
        }

        var (registered, _, registeredModel) = await _versionService.RegisterVersionAsync(new CreateAiEmbeddingVersionRequest
        {
            VersionCode = versionCode,
            ModelProvider = _options.DefaultModelProvider,
            ModelName = modelName,
            DimensionCount = _options.DefaultDimensionCount,
            Description = "Auto-registered by enterprise rubric embedding builder.",
            CreatedByUserId = actorUserId,
        }, cancellationToken);

        if (!registered || registeredModel == null)
            return null;

        await _versionService.ActivateVersionAsync(registeredModel.EmbeddingVersionId, actorUserId, cancellationToken);
        return registeredModel;
    }

    private string BuildVersionCode(string modelName, int dimensionCount)
    {
        var sanitizedModel = modelName
            .Replace('.', '-')
            .Replace(':', '-')
            .Replace(' ', '-')
            .ToLowerInvariant();
        return $"{_options.DefaultVersionCodePrefix}-{sanitizedModel}-{dimensionCount}";
    }

    private async Task UpdateJobProgressAsync(
        Guid jobId,
        int totalItems,
        int processedItems,
        int skippedItems,
        int failedItems,
        CancellationToken cancellationToken)
    {
        var job = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return;

        job.TotalItems = totalItems;
        job.ProcessedItems = processedItems;
        job.SkippedItems = skippedItems;
        job.FailedItems = failedItems;
        job.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static AiEmbeddingVersionModel MapVersion(AiEmbeddingVersion entity) => new()
    {
        EmbeddingVersionId = entity.EmbeddingVersionId,
        VersionCode = entity.VersionCode,
        ModelProvider = entity.ModelProvider,
        ModelName = entity.ModelName,
        ModelVersion = entity.ModelVersion,
        DimensionCount = entity.DimensionCount,
        VectorFormat = entity.VectorFormat,
        IsActive = entity.IsActive,
        IsCurrent = entity.IsCurrent,
        Status = entity.Status,
        Description = entity.Description,
        CreatedDate = entity.CreatedDate,
        UpdatedDate = entity.UpdatedDate,
    };
}

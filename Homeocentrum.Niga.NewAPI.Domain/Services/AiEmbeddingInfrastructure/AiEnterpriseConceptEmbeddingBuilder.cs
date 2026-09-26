using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Repositories.AiEmbeddingInfrastructure;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public class AiEnterpriseConceptEmbeddingBuilder : IAiEnterpriseConceptEmbeddingBuilder
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IRepertoryRubricCatalogReader _catalogReader;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IAiEmbeddingVersionService _versionService;
    private readonly IAiEmbeddingJobService _jobService;
    private readonly IAiConceptEmbeddingMemoryCache _conceptCache;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEnterpriseConceptEmbeddingBuilder> _logger;

    public AiEnterpriseConceptEmbeddingBuilder(
        IAiEmbeddingUnitOfWork unitOfWork,
        IRepertoryRubricCatalogReader catalogReader,
        IEmbeddingClient embeddingClient,
        IAiEmbeddingVersionService versionService,
        IAiEmbeddingJobService jobService,
        IAiConceptEmbeddingMemoryCache conceptCache,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEnterpriseConceptEmbeddingBuilder> logger)
    {
        _unitOfWork = unitOfWork;
        _catalogReader = catalogReader;
        _embeddingClient = embeddingClient;
        _versionService = versionService;
        _jobService = jobService;
        _conceptCache = conceptCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EnterpriseConceptEmbeddingBuildResult> BuildAsync(
        BuildEnterpriseConceptEmbeddingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new EnterpriseConceptEmbeddingBuildResult();
        if (!_options.Enabled || !_options.EnableEnterpriseSemanticSearch)
        {
            result.Error = "Enterprise concept embedding builder is disabled.";
            return result;
        }

        if (!_embeddingClient.IsConfigured)
        {
            result.Error = "Embedding API is not configured.";
            return result;
        }

        var version = await ResolveVersionAsync(request.EmbeddingVersionId, cancellationToken);
        if (version == null)
        {
            result.Error = "No embedding version is available.";
            return result;
        }

        result.EmbeddingVersionId = version.EmbeddingVersionId;
        result.VersionCode = version.VersionCode;

        var (jobCreated, jobMessage, jobModel) = await _jobService.CreateJobAsync(new CreateAiEmbeddingJobRequest
        {
            EmbeddingVersionId = version.EmbeddingVersionId,
            JobType = AiEmbeddingJobTypes.ConceptOnly,
            TriggerSource = request.TriggerSource ?? "ConceptEmbeddingBuilder",
            CreatedByUserId = request.ActorUserId,
        }, cancellationToken);

        if (!jobCreated || jobModel == null)
        {
            result.Error = jobMessage;
            return result;
        }

        result.JobId = jobModel.JobId;
        await _jobService.MarkJobRunningAsync(jobModel.JobId, request.ActorUserId, cancellationToken);

        try
        {
            return await ExecuteConceptBuildAsync(
                request,
                version,
                jobModel,
                result,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enterprise concept embedding build failed for job {JobId}", jobModel.JobId);
            await _jobService.MarkJobFailedAsync(jobModel.JobId, ex.Message, request.ActorUserId, cancellationToken);
            result.Error = ex.Message;
            return result;
        }
    }

    private async Task<EnterpriseConceptEmbeddingBuildResult> ExecuteConceptBuildAsync(
        BuildEnterpriseConceptEmbeddingsRequest request,
        AiEmbeddingVersionModel version,
        AiEmbeddingJobModel jobModel,
        EnterpriseConceptEmbeddingBuildResult result,
        CancellationToken cancellationToken)
    {
        var aggregates = await BuildConceptAggregatesAsync(cancellationToken);
        var maxConcepts = request.MaxConcepts ?? 0;
        if (maxConcepts > 0)
            aggregates = aggregates.Take(maxConcepts).ToList();

        result.TotalCatalogued = aggregates.Count;

        var existingHashes = await _unitOfWork.ConceptEmbeddings.GetActiveTextHashesAsync(
            version.EmbeddingVersionId,
            AiConceptEmbeddingTypes.Clinical,
            aggregates.Select(x => x.ConceptKey),
            cancellationToken);

        var pending = aggregates
            .Where(item => !existingHashes.TryGetValue(item.ConceptKey, out var hash)
                || !string.Equals(hash, item.TextHash, StringComparison.OrdinalIgnoreCase))
            .ToList();

        result.Skipped = aggregates.Count - pending.Count;
        var batchSize = Math.Max(1, _options.BuilderBatchSize);

        for (var offset = 0; offset < pending.Count; offset += batchSize)
        {
            var batch = pending.Skip(offset).Take(batchSize).ToList();
            var texts = batch.Select(x => x.SourceText).ToList();
            var embedResult = await _embeddingClient.EmbedTextsAsync(texts, version.ModelName, cancellationToken);
            if (!embedResult.Success || embedResult.Vectors.Count != batch.Count)
            {
                result.Failed += batch.Count;
                continue;
            }

            for (var i = 0; i < batch.Count; i++)
            {
                try
                {
                    var item = batch[i];
                    var outcome = await _unitOfWork.ConceptEmbeddings.UpsertActiveEmbeddingAsync(
                        version.EmbeddingVersionId,
                        item.ConceptKey,
                        AiConceptEmbeddingTypes.Clinical,
                        item.SourceDomain,
                        item.SourceText,
                        item.TextHash,
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
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    _logger.LogWarning(ex, "Failed to upsert concept embedding for {ConceptKey}", batch[i].ConceptKey);
                }
            }
        }

        await _jobService.MarkJobCompletedAsync(jobModel.JobId, request.ActorUserId, cancellationToken);
        await _conceptCache.RefreshAsync(version.EmbeddingVersionId, cancellationToken);
        result.Success = result.Failed == 0 || result.Processed > 0 || result.Skipped > 0;
        _logger.LogInformation(
            "Enterprise concept embedding build complete version={VersionCode} catalogued={Catalogued} processed={Processed} skipped={Skipped} failed={Failed}",
            version.VersionCode,
            result.TotalCatalogued,
            result.Processed,
            result.Skipped,
            result.Failed);
        return result;
    }

    private async Task<List<ConceptAggregate>> BuildConceptAggregatesAsync(CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, ConceptAggregate>(StringComparer.OrdinalIgnoreCase);
        var total = await _catalogReader.CountRubricsAsync(cancellationToken);
        var pageSize = Math.Max(1, _options.BuilderCatalogPageSize);

        for (var skip = 0; skip < total; skip += pageSize)
        {
            var page = await _catalogReader.ReadPageAsync(skip, pageSize, cancellationToken);
            foreach (var rubric in page)
            {
                foreach (var clinicalConcept in rubric.ClinicalConcepts.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    AddConcept(map, clinicalConcept, rubric, clinicalConcept);
                }

                if (rubric.ClinicalConcepts.Count == 0)
                {
                    var derived = $"{rubric.SectionName}: {ExtractTail(rubric.RubricName)}";
                    AddConcept(map, derived, rubric, derived);
                }

                foreach (var homeopathic in rubric.HomeopathicConcepts.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var key = NormalizeConceptKey(homeopathic);
                    if (map.TryGetValue(key, out var existing))
                    {
                        existing.HomeopathicConcepts.Add(homeopathic.Trim());
                        existing.LinkedRubricIds.Add(rubric.RubricId);
                        existing.Synonyms.UnionWith(rubric.KnownSynonyms);
                        Recompute(existing);
                        continue;
                    }
                }
            }
        }

        return map.Values.OrderBy(x => x.ConceptKey).ToList();
    }

    private static void AddConcept(
        Dictionary<string, ConceptAggregate> map,
        string clinicalConcept,
        RepertoryRubricCatalogItem rubric,
        string clinicalMeaning)
    {
        var key = NormalizeConceptKey(clinicalConcept);
        if (!map.TryGetValue(key, out var aggregate))
        {
            aggregate = new ConceptAggregate
            {
                ConceptKey = key,
                ClinicalConcept = clinicalMeaning.Trim(),
                SourceDomain = rubric.SectionName,
            };
            map[key] = aggregate;
        }

        aggregate.LinkedRubricIds.Add(rubric.RubricId);
        aggregate.Synonyms.UnionWith(rubric.KnownSynonyms);
        aggregate.HomeopathicConcepts.UnionWith(rubric.HomeopathicConcepts);
        aggregate.Meanings.UnionWith(rubric.Meanings);
        Recompute(aggregate);
    }

    private static void Recompute(ConceptAggregate aggregate)
    {
        aggregate.SourceText = ConceptSemanticDocumentParser.Compose(
            aggregate.ClinicalConcept,
            aggregate.HomeopathicConcepts.FirstOrDefault(),
            aggregate.Synonyms,
            aggregate.Meanings,
            aggregate.LinkedRubricIds);

        aggregate.TextHash = AiEmbeddingHashHelper.ComputeSha256(aggregate.SourceText);
    }

    private static string NormalizeConceptKey(string value)
    {
        var normalized = IntelligenceTextNormalizer.Normalize(value);
        return normalized.Length > 200 ? normalized[..200] : normalized;
    }

    private static string ExtractTail(string rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName))
            return string.Empty;

        var parts = rubricName.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? rubricName.Trim() : parts[^1];
    }

    private async Task<AiEmbeddingVersionModel?> ResolveVersionAsync(
        Guid? requestedVersionId,
        CancellationToken cancellationToken)
    {
        if (requestedVersionId.HasValue)
        {
            var explicitVersion = await _unitOfWork.Versions.GetByIdAsync(requestedVersionId.Value, cancellationToken);
            return explicitVersion == null ? null : MapVersion(explicitVersion);
        }

        return await _versionService.GetCurrentVersionAsync(cancellationToken);
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

    private sealed class ConceptAggregate
    {
        public string ConceptKey { get; set; } = string.Empty;
        public string ClinicalConcept { get; set; } = string.Empty;
        public string SourceDomain { get; set; } = string.Empty;
        public HashSet<string> HomeopathicConcepts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Synonyms { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Meanings { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<int> LinkedRubricIds { get; } = new();
        public string SourceText { get; set; } = string.Empty;
        public string TextHash { get; set; } = string.Empty;
    }
}

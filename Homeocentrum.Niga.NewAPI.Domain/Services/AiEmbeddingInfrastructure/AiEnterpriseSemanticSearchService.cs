using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Repositories.AiEmbeddingInfrastructure;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public class AiEnterpriseSemanticSearchService : IAiEnterpriseSemanticSearchService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IAiEmbeddingVersionService _versionService;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IAiConceptEmbeddingMemoryCache _conceptCache;
    private readonly IAiEnterpriseRubricEmbeddingMemoryCache _rubricCache;
    private readonly EnterpriseConceptToRubricMapper _rubricMapper;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEnterpriseSemanticSearchService> _logger;

    public AiEnterpriseSemanticSearchService(
        IAiEmbeddingUnitOfWork unitOfWork,
        IAiEmbeddingVersionService versionService,
        IEmbeddingClient embeddingClient,
        IAiConceptEmbeddingMemoryCache conceptCache,
        IAiEnterpriseRubricEmbeddingMemoryCache rubricCache,
        EnterpriseConceptToRubricMapper rubricMapper,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEnterpriseSemanticSearchService> logger)
    {
        _unitOfWork = unitOfWork;
        _versionService = versionService;
        _embeddingClient = embeddingClient;
        _conceptCache = conceptCache;
        _rubricCache = rubricCache;
        _rubricMapper = rubricMapper;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EnterpriseSemanticSearchResult> SearchAsync(
        EnterpriseSemanticSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new EnterpriseSemanticSearchResult
        {
            QueryClinicalConcept = request.ClinicalConcept?.Trim() ?? string.Empty,
        };

        if (!_options.Enabled || !_options.EnableEnterpriseSemanticSearch)
        {
            result.Error = "Enterprise semantic search is disabled.";
            return result;
        }

        var (inputValid, inputError) = ClinicalConceptInputGuard.Validate(request.ClinicalConcept, _options);
        if (!inputValid)
        {
            result.Error = inputError;
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

        await EnsureCachesAsync(version.EmbeddingVersionId, cancellationToken);

        if (_conceptCache.Entries.Count == 0)
        {
            result.Error = "No AIConceptEmbedding vectors are indexed for the current version.";
            return result;
        }

        var queryText = ClinicalConceptQueryBuilder.BuildQueryText(result.QueryClinicalConcept);
        result.QueryText = queryText;

        var embedResult = await _embeddingClient.EmbedTextsAsync(
            new[] { queryText },
            version.ModelName,
            cancellationToken);

        if (!embedResult.Success || embedResult.Vectors.Count == 0)
        {
            result.Error = embedResult.Error ?? "Failed to embed clinical concept query.";
            return result;
        }

        var queryVector = embedResult.Vectors[0];
        var topConcepts = request.TopConcepts ?? _options.SemanticSearchTopConcepts;
        var conceptMatches = EnterpriseEmbeddingVectorSearch.TopConceptMatches(
            queryVector,
            _conceptCache.Entries,
            topConcepts,
            _options.MinConceptCosineScore);

        result.Concepts = conceptMatches
            .Select((match, index) => MapConceptMatch(match.Entry, match.Score, match.RawCosine, index + 1))
            .ToList();

        if (request.IncludeRubricMapping && _rubricCache.Entries.Count > 0)
        {
            result.Rubrics = await _rubricMapper.MapAsync(
                queryVector,
                result.Concepts,
                _rubricCache.Entries,
                request.IncludeValidation,
                cancellationToken);
        }

        result.Success = result.Concepts.Count > 0;
        if (!result.Success)
            result.Error = "No concept matches met the minimum cosine threshold.";

        _logger.LogInformation(
            "Enterprise semantic search concept='{Concept}' concepts={ConceptCount} rubrics={RubricCount}",
            result.QueryClinicalConcept,
            result.Concepts.Count,
            result.Rubrics.Count);

        return result;
    }

    private async Task EnsureCachesAsync(Guid embeddingVersionId, CancellationToken cancellationToken)
    {
        var cacheExpired = _conceptCache.LastRefreshedUtc == null
            || _conceptCache.EmbeddingVersionId != embeddingVersionId
            || DateTime.UtcNow - _conceptCache.LastRefreshedUtc.Value > TimeSpan.FromMinutes(_options.ConceptEmbeddingCacheMinutes);

        if (cacheExpired || _conceptCache.Entries.Count == 0)
            await _conceptCache.RefreshAsync(embeddingVersionId, cancellationToken);

        var rubricCacheExpired = _rubricCache.LastRefreshedUtc == null
            || _rubricCache.EmbeddingVersionId != embeddingVersionId
            || DateTime.UtcNow - _rubricCache.LastRefreshedUtc.Value > TimeSpan.FromMinutes(_options.RubricEmbeddingCacheMinutes);

        if (rubricCacheExpired || _rubricCache.Entries.Count == 0)
            await _rubricCache.RefreshAsync(embeddingVersionId, cancellationToken);
    }

    private static EnterpriseSemanticConceptMatchModel MapConceptMatch(
        AiConceptEmbeddingCacheEntry entry,
        decimal score,
        float rawCosine,
        int rank)
    {
        var parsed = ConceptSemanticDocumentParser.Parse(entry.SourceText);
        parsed.LinkedRubricIds.ForEach(id =>
        {
            if (!entry.LinkedRubricIds.Contains(id))
                entry.LinkedRubricIds.Add(id);
        });

        return new EnterpriseSemanticConceptMatchModel
        {
            Rank = rank,
            ConceptKey = entry.ConceptKey,
            ConceptType = entry.ConceptType,
            SimilarityScore = score,
            RawCosine = rawCosine,
            MatchedSynonyms = parsed.KnownSynonyms,
            MatchedClinicalMeaning = parsed.ClinicalConcept ?? entry.ConceptKey,
            MatchedHomeopathicMeaning = parsed.HomeopathicConcept,
            LinkedRubricIds = parsed.LinkedRubricIds.Count > 0 ? parsed.LinkedRubricIds : entry.LinkedRubricIds,
        };
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
}

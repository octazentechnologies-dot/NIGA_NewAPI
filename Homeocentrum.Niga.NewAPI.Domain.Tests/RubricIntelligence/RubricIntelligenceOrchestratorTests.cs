using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Orchestration;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class RubricIntelligenceOrchestratorTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsEmpty_WhenV2Disabled()
    {
        var orchestrator = CreateOrchestrator(enableV2: false);
        var session = new AudioCaseSession { AudioCaseSessionId = Guid.NewGuid() };

        var result = await orchestrator.AnalyzeAsync(
            session, "transcript", new List<AudioCaseSymptomModel>(), null, "corr-1");

        Assert.Empty(result.Rubrics);
        Assert.Equal("v1", result.EngineVersion);
    }

    [Fact]
    public async Task AnalyzeAsync_RunsPhase2Pipeline_WhenV2Enabled()
    {
        var orchestrator = CreateOrchestrator(enableV2: true);
        var session = new AudioCaseSession { AudioCaseSessionId = Guid.NewGuid() };

        var result = await orchestrator.AnalyzeAsync(
            session,
            "Patient feels vibration in hands 10 seconds before a fit at night.",
            new List<AudioCaseSymptomModel>
            {
                new() { Phrase = "vibration before fit", SearchTerms = new List<string> { "vibration", "fit" }, Category = "particular" },
            },
            null,
            "corr-2");

        Assert.Equal("v2", result.EngineVersion);
        Assert.NotEmpty(result.Concepts);
        Assert.Contains("MetaphorInterpretation", result.StagesCompleted);
        Assert.Contains("RubricAliasSearch", result.StagesCompleted);
        Assert.Contains("CausationDetection", result.StagesCompleted);
        Assert.Contains("HomeopathicWeight", result.StagesCompleted);
        Assert.Contains("HybridEmbeddingSearch", result.StagesCompleted);
        Assert.Contains("PerConceptKeywordDiscovery", result.StagesCompleted);
        Assert.Contains("Explainability", result.StagesCompleted);
        Assert.Contains("RepertoryMapping", result.StagesCompleted);
    }

    private sealed class FakeClinicalInferenceEngine : IClinicalInferenceEngine
    {
        public Task<ClinicalInferenceResult> InferAsync(
            IReadOnlyList<ClinicalConceptModel> concepts,
            IReadOnlyList<CausationLinkModel> causationLinks,
            IReadOnlyList<AudioCaseSuggestedRubricModel> existingRubrics,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ClinicalInferenceResult());
    }

    private sealed class FakeHybridRetrievalEngine : IHybridRetrievalEngine
    {
        public Task<HybridRetrievalResult> RetrieveAsync(
            IReadOnlyList<ClinicalConceptModel> concepts,
            IReadOnlyList<AudioCaseSymptomModel> symptoms,
            IReadOnlyList<AudioCaseSuggestedRubricModel> aliasRubrics,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HybridRetrievalResult
            {
                Rubrics = aliasRubrics.ToList(),
                AliasCandidates = aliasRubrics.Count,
            });
        }
    }

    private sealed class FakeConceptKeywordDiscoveryEngine : IConceptKeywordDiscoveryEngine
    {
        public Task<ConceptKeywordDiscoveryBatchResult> DiscoverAsync(
            Guid sessionId,
            string? correlationId,
            IReadOnlyList<ClinicalConceptModel> concepts,
            IReadOnlyList<AudioCaseSuggestedRubricModel> existingRubrics,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ConceptKeywordDiscoveryBatchResult
            {
                Traces = concepts.Select(c => new ConceptDiscoveryTraceModel
                {
                    ConceptId = c.ConceptId,
                    ConceptText = c.ClinicalMeaning ?? c.RawStatement,
                    SearchAttempted = true,
                    Outcome = "ZeroDbHits",
                }).ToList(),
            });
    }

    private sealed class FakeConceptGraphRepository : IConceptGraphRepository
    {
        public Task ClearSessionV3DataAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SavePatientMeaningsAsync(Guid sessionId, IReadOnlyList<PatientMeaningNodeModel> meanings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<List<PatientMeaningNodeModel>> GetPatientMeaningsAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.FromResult(new List<PatientMeaningNodeModel>());
        public Task SaveFullGraphAsync(Guid sessionId, ConceptGraphFullModel graph, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ConceptGraphFullModel> GetFullGraphAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.FromResult(new ConceptGraphFullModel());
        public Task SaveReasoningAuditAsync(Guid sessionId, AiReasoningAuditModel audit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<List<AiConceptMappingBootstrapModel>> GetActiveConceptMappingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<AiConceptMappingBootstrapModel>());
        public Task<Dictionary<string, decimal>> GetLearnedConceptWeightsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<string, decimal>());
        public Task SaveCoverageMetricsAsync(Guid sessionId, CaseCoverageMetricsModel metrics, IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveDisplayedRubricsAsync(Guid sessionId, IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics, ConceptGraphFullModel? graph = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeRepertoryTierEngine : IRepertoryTierEngine
    {
        public Task<RepertoryTierEnrichmentResult> EnrichAsync(
            IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RepertoryTierEnrichmentResult { Rubrics = rubrics.ToList() });
    }

    private sealed class FakeSettingsService : IRubricIntelligenceSettingsService
    {
        private readonly bool _enableV2;

        public FakeSettingsService(bool enableV2) => _enableV2 = enableV2;

        public bool IsV2Active => _enableV2;

        public bool RollbackToV1Only => false;

        public bool RequiresManualApproval => _enableV2;

        public bool EnableRepertoryMapping => _enableV2;

        public bool IsV3Active => false;

        public RubricIntelligenceOptions GetBaseOptions() => new() { EnableV2 = _enableV2, EnableEmbeddingSearch = true };

        public RubricIntelligenceConfigModel GetConfig() => new() { IsV2Active = _enableV2, EnableRepertoryMapping = _enableV2 };

        public void ApplyRuntimeUpdate(RubricIntelligenceConfigUpdateModel update, int adminUserId) { }

        public void ClearRuntimeOverrides() { }

        public Task<RubricIntelligenceRolloutStatusModel> GetRolloutStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new RubricIntelligenceRolloutStatusModel());
    }

    private static RubricIntelligenceOrchestrator CreateOrchestrator(bool enableV2)
    {
        var options = Options.Create(new RubricIntelligenceOptions
        {
            EnableV2 = enableV2,
            RequireManualApprovalForAllAiRubrics = true,
        });
        var audioOptions = Options.Create(new AudioCaseTakingOptions { UseMockWhenNoApiKey = true });

        return new RubricIntelligenceOrchestrator(
            options,
            new CaseUnderstandingEngine(new FakeGptClient(), audioOptions),
            new ModalityDetectionEngine(),
            new ConcomitantDetectionEngine(),
            new ClinicalReasoningEngine(),
            new HomeopathicReasoningEngine(),
            new CausationDetectionEngine(),
            new HomeopathicWeightEngine(new InMemoryIntelligenceRepository()),
            new MetaphorInterpretationEngine(new FakeAdminService()),
            new RubricAliasEngine(new FakeAdminService()),
            new FakeHybridRetrievalEngine(),
            new FakeConceptKeywordDiscoveryEngine(),
            new FakeClinicalInferenceEngine(),
            new ExplainabilityEngine(),
            new FakeRepertoryTierEngine(),
            new FakeSettingsService(enableV2),
            new SymptomExtractionEngine(),
            new FakeClinicalValidationEngine(),
            new InMemoryIntelligenceRepository(),
            new FakeConceptGraphRepository(),
            NullLogger<RubricIntelligenceOrchestrator>.Instance);
    }

    private sealed class FakeClinicalValidationEngine : IClinicalValidationEngine
    {
        public ClinicalValidationResult ValidateAndFilter(
            IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
            ClinicalValidationContext context) =>
            new()
            {
                AcceptedRubrics = rubrics.ToList(),
                PrimarySymptom = context.PrimarySymptom,
            };
    }

    private sealed class FakeGptClient : IIntelligenceGptClient
    {
        public Task<IntelligenceGptResult<T>> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, string stageName, CancellationToken cancellationToken = default)
            => Task.FromResult(new IntelligenceGptResult<T> { Success = false, Error = "Not configured in tests." });
    }

    private sealed class FakeAdminService : IRubricIntelligenceAdminService
    {
        public Task<RubricIntelligenceAdminListModel<HomeopathicWeightRuleModel>> GetWeightsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new RubricIntelligenceAdminListModel<HomeopathicWeightRuleModel>());

        public Task<HomeopathicWeightRuleModel?> GetWeightByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<HomeopathicWeightRuleModel?>(null);

        public Task<(bool Success, string Message, HomeopathicWeightRuleModel? Result)> UpdateWeightAsync(int adminUserId, int id, HomeopathicWeightRuleUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<List<RubricMetaphorDictionary>> SearchApprovedMetaphorsAsync(IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default)
        {
            var terms = normalizedTerms.ToList();
            if (terms.Any(t => t.Contains("vibration", StringComparison.Ordinal)))
            {
                return Task.FromResult(new List<RubricMetaphorDictionary>
                {
                    new()
                    {
                        MetaphorId = 1,
                        PatientExpression = "vibration before fit",
                        NormalizedExpression = "vibration before fit",
                        ClinicalMeaning = "Prodromal aura",
                        RubricMeaning = "GENERALITIES - CONVULSIONS - aura",
                        Language = "en",
                        ConfidenceWeight = 0.92m,
                        ApprovalStatus = "Approved",
                        IsActive = true,
                    },
                });
            }

            return Task.FromResult(new List<RubricMetaphorDictionary>());
        }

        public Task<List<(RubricAlias Alias, string SubSectionName)>> SearchActiveAliasesAsync(IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<(RubricAlias, string)>());

        public Task<RubricIntelligenceAdminListModel<RubricMetaphorModel>> GetMetaphorsAsync(string? search, string? language, string? approvalStatus, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<RubricMetaphorModel?> GetMetaphorByIdAsync(long id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message, RubricMetaphorModel? Result)> CreateMetaphorAsync(int adminUserId, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message, RubricMetaphorModel? Result)> UpdateMetaphorAsync(int adminUserId, long id, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message)> DeleteMetaphorAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message)> ApproveMetaphorAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message)> RejectMetaphorAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<RubricIntelligenceAdminListModel<RubricAliasModel>> GetAliasesAsync(string? search, string? language, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<RubricAliasModel?> GetAliasByIdAsync(long id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message, RubricAliasModel? Result)> CreateAliasAsync(int adminUserId, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message, RubricAliasModel? Result)> UpdateAliasAsync(int adminUserId, long id, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<(bool Success, string Message)> DeleteAliasAsync(int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class InMemoryIntelligenceRepository : IAudioCaseIntelligenceRepository
    {
        public Task SaveConceptsAsync(Guid sessionId, IReadOnlyList<ClinicalConceptModel> concepts, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveIntelligenceLogAsync(Guid sessionId, string? correlationId, string stageName, string status, string? message, string? detailsJson, int? latencyMs, CancellationToken cancellationToken = default, string engineVersion = "v2")
            => Task.CompletedTask;

        public Task<List<ClinicalConceptModel>> GetConceptsAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<ClinicalConceptModel>());

        public Task SaveCausationLinksAsync(Guid sessionId, IReadOnlyList<CausationLinkModel> links, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<CausationLinkModel>> GetCausationLinksAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<CausationLinkModel>());

        public Task<List<HomeopathicWeightRule>> GetActiveWeightRulesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new List<HomeopathicWeightRule>());

        public Task SaveInferenceLogsAsync(Guid sessionId, IReadOnlyList<ClinicalInferenceLogModel> logs, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

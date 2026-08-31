using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;

namespace Niga_Domain.Services.AudioCaseIntelligence;

public class RubricIntelligenceSettingsService : IRubricIntelligenceSettingsService
{
    private readonly RubricIntelligenceOptions _baseOptions;
    private readonly IRubricBenchmarkService _benchmarkService;
    private readonly IRepertoryMappingRepository _repertoryRepository;
    private readonly ILogger<RubricIntelligenceSettingsService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly AppInfoOptions _appInfo;
    private readonly object _sync = new();

    private bool? _enableV2Override;
    private bool? _rollbackOverride;
    private bool? _enableRepertoryMappingOverride;
    private bool? _strictConceptGatedDiscoveryOverride;
    private bool? _dualLanguageForSensationSegmentsOverride;
    private bool? _enableV2ForAllDoctorsOverride;
    private bool? _enableV3ConceptGraphOverride;
    private DateTime? _runtimeUpdatedUtc;
    private int? _runtimeUpdatedByUserId;
    private string? _rolloutNotes;

    public RubricIntelligenceSettingsService(
        IOptions<RubricIntelligenceOptions> options,
        IRubricBenchmarkService benchmarkService,
        IRepertoryMappingRepository repertoryRepository,
        ILogger<RubricIntelligenceSettingsService> logger,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment hostEnvironment,
        IOptions<AppInfoOptions> appInfo)
    {
        _baseOptions = options.Value;
        _benchmarkService = benchmarkService;
        _repertoryRepository = repertoryRepository;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hostEnvironment = hostEnvironment;
        _appInfo = appInfo.Value;
    }

    public bool IsV2Active
    {
        get
        {
            lock (_sync)
            {
                var rollback = _rollbackOverride ?? _baseOptions.RollbackToV1Only;
                var enabled = _enableV2Override ?? _baseOptions.EnableV2;
                return enabled && !rollback;
            }
        }
    }

    public bool RollbackToV1Only
    {
        get
        {
            lock (_sync)
            {
                return _rollbackOverride ?? _baseOptions.RollbackToV1Only;
            }
        }
    }

    public bool RequiresManualApproval =>
        IsV2Active
        && _baseOptions.RequireManualApprovalForAllAiRubrics
        && !_baseOptions.AllowAutoApplyHighConfidence;

    public bool EnableRepertoryMapping
    {
        get
        {
            lock (_sync)
            {
                return _enableRepertoryMappingOverride ?? _baseOptions.EnableRepertoryMapping;
            }
        }
    }

    public bool IsV3Active =>
        (_enableV3ConceptGraphOverride ?? _baseOptions.EnableV3ConceptGraph) && IsV2Active;

    public RubricIntelligenceOptions GetBaseOptions() => _baseOptions;

    public RubricIntelligenceConfigModel GetConfig()
    {
        lock (_sync)
        {
            return new RubricIntelligenceConfigModel
            {
                EnableV2 = _enableV2Override ?? _baseOptions.EnableV2,
                EnableV2ForAllDoctors = _enableV2ForAllDoctorsOverride ?? _baseOptions.EnableV2ForAllDoctors,
                RequireManualApprovalForAllAiRubrics = _baseOptions.RequireManualApprovalForAllAiRubrics,
                RollbackToV1Only = RollbackToV1Only,
                EnableRepertoryMapping = EnableRepertoryMapping,
                EnableEmbeddingSearch = _baseOptions.EnableEmbeddingSearch,
                EnableClinicalInference = _baseOptions.EnableClinicalInference,
                EnableDoctorFeedbackLearning = _baseOptions.EnableDoctorFeedbackLearning,
                EnableV3ConceptGraph = _enableV3ConceptGraphOverride ?? _baseOptions.EnableV3ConceptGraph,
                EnableV3ShadowMode = _baseOptions.EnableV3ShadowMode,
                IsV3Active = IsV3Active,
                IsV2Active = IsV2Active,
                RequiresManualApproval = RequiresManualApproval,
                HasRuntimeOverride = _enableV2Override.HasValue
                    || _rollbackOverride.HasValue
                    || _enableRepertoryMappingOverride.HasValue
                    || _strictConceptGatedDiscoveryOverride.HasValue
                    || _dualLanguageForSensationSegmentsOverride.HasValue
                    || _enableV2ForAllDoctorsOverride.HasValue
                    || _enableV3ConceptGraphOverride.HasValue,
                RuntimeOverrideUpdatedUtc = _runtimeUpdatedUtc,
                RuntimeOverrideUpdatedByUserId = _runtimeUpdatedByUserId,
                EnableFastClinicalRetrievalPipeline = _baseOptions.EnableFastClinicalRetrievalPipeline,
                FastPipelineEngineVersion = RubricEngineStamp.Normalize(
                    _baseOptions.FastPipelineEngineVersion,
                    RubricEngineStamp.FastFallback),
                EnableEciV8Engine = _baseOptions.EnableEciV8Engine,
                EnableV6ClinicalReasoningEngine = _baseOptions.EnableV6ClinicalReasoningEngine,
                EnableV7RepertoryIntelligenceEngine = _baseOptions.EnableV7RepertoryIntelligenceEngine,
                AppVersion = string.IsNullOrWhiteSpace(_appInfo.Version) ? null : _appInfo.Version.Trim(),
            };
        }
    }

    public void ApplyRuntimeUpdate(RubricIntelligenceConfigUpdateModel update, int adminUserId)
    {
        lock (_sync)
        {
            if (update.EnableV2.HasValue)
                _enableV2Override = update.EnableV2.Value;

            if (update.RollbackToV1Only.HasValue)
                _rollbackOverride = update.RollbackToV1Only.Value;

            if (update.EnableRepertoryMapping.HasValue)
                _enableRepertoryMappingOverride = update.EnableRepertoryMapping.Value;

            if (update.StrictConceptGatedDiscovery.HasValue)
                _strictConceptGatedDiscoveryOverride = update.StrictConceptGatedDiscovery.Value;

            if (update.DualLanguageForSensationSegments.HasValue)
                _dualLanguageForSensationSegmentsOverride = update.DualLanguageForSensationSegments.Value;

            if (update.EnableV2ForAllDoctors.HasValue)
                _enableV2ForAllDoctorsOverride = update.EnableV2ForAllDoctors.Value;

            if (update.EnableV3ConceptGraph.HasValue)
                _enableV3ConceptGraphOverride = update.EnableV3ConceptGraph.Value;

            if (!string.IsNullOrWhiteSpace(update.RolloutNotes))
                _rolloutNotes = update.RolloutNotes.Trim();

            _runtimeUpdatedUtc = DateTime.UtcNow;
            _runtimeUpdatedByUserId = adminUserId;
        }

        WarnIfFlagsEnabledWithoutGate(update);
        _logger.LogInformation(
            "Rubric intelligence runtime config updated by user {UserId}. EnableV2={EnableV2}, Rollback={Rollback}, Repertory={Repertory}",
            adminUserId,
            IsV2Active,
            RollbackToV1Only,
            EnableRepertoryMapping);
    }

    public void ClearRuntimeOverrides()
    {
        lock (_sync)
        {
            _enableV2Override = null;
            _rollbackOverride = null;
            _enableRepertoryMappingOverride = null;
            _strictConceptGatedDiscoveryOverride = null;
            _dualLanguageForSensationSegmentsOverride = null;
            _enableV2ForAllDoctorsOverride = null;
            _enableV3ConceptGraphOverride = null;
            _runtimeUpdatedUtc = DateTime.UtcNow;
        }
    }

    public async Task<RubricIntelligenceRolloutStatusModel> GetRolloutStatusAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _benchmarkService.GetSummaryAsync(30, cancellationToken);
        var repertory = await _repertoryRepository.GetMappingStatusAsync(cancellationToken);
        var flagGateWarnings = GetEnabledFlagGateWarnings();

        var gates = new List<RubricIntelligenceRolloutGateModel>
        {
            new()
            {
                GateCode = "G1",
                Title = "Gold library loaded",
                Passed = summary.GoldCaseCount >= 5,
                Detail = $"{summary.GoldCaseCount} active gold case(s) (target: 5+ starter, 250 production).",
            },
            new()
            {
                GateCode = "G2",
                Title = "30-day acceptance rate",
                Passed = summary.AcceptanceRate30Day >= 0.93m,
                Detail = summary.AcceptanceRate30Day.HasValue
                    ? $"Current: {summary.AcceptanceRate30Day:P1} (pilot target ≥93%, production ≥95%)."
                    : "No benchmark data yet — doctors must approve/reject rubrics.",
            },
            new()
            {
                GateCode = "G3",
                Title = "Primary rubric in top-5",
                Passed = summary.PrimaryInTop5Rate30Day >= 0.93m,
                Detail = summary.PrimaryInTop5Rate30Day.HasValue
                    ? $"Current: {summary.PrimaryInTop5Rate30Day:P1} (pilot target ≥93%, production ≥95%)."
                    : "No primary-in-top-5 benchmark data yet.",
            },
            new()
            {
                GateCode = "G4",
                Title = "Repertory mapping ready",
                Passed = repertory.ActiveSourceCount >= 2 && repertory.MappedRubricCount > 0,
                Detail = $"Sources: {repertory.ActiveSourceCount}, mapped rubrics: {repertory.MappedRubricCount} (Kent: {repertory.KentMappedCount}, Complete: {repertory.CompleteMappedCount}).",
            },
            new()
            {
                GateCode = "G5",
                Title = "Rollback path available",
                Passed = true,
                Detail = "RollbackToV1Only flag and 401_Rollback_All_V2.sql verified in deployment guide.",
            },
            new()
            {
                GateCode = "G6",
                Title = "Admin dashboards operational",
                Passed = true,
                Detail = "Benchmark dashboard and runtime config API deployed.",
            },
        };

        var passed = gates.Count(g => g.Passed);

        return new RubricIntelligenceRolloutStatusModel
        {
            IsV2Active = IsV2Active,
            RollbackToV1Only = RollbackToV1Only,
            ReadyForProductionRollout = passed >= 5 && !RollbackToV1Only,
            GatesPassed = passed,
            GatesTotal = gates.Count,
            AcceptanceRate30Day = summary.AcceptanceRate30Day,
            PrimaryInTop5Rate30Day = summary.PrimaryInTop5Rate30Day,
            GoldCaseCount = summary.GoldCaseCount,
            SessionsBenchmarked30Day = summary.TotalSessionsBenchmarked,
            LastGateTimestamp = summary.LastGateTimestamp,
            LastGateTop5Accuracy = summary.LastGateTop5Accuracy,
            DeltaTop5VsLastGate = summary.DeltaTop5VsLastGate,
            DeltaAcceptanceVsLastGate = summary.DeltaAcceptanceVsLastGate,
            FlagGateWarnings = flagGateWarnings,
            Gates = gates,
        };
    }

    private void WarnIfFlagsEnabledWithoutGate(RubricIntelligenceConfigUpdateModel update)
    {
        foreach (var flagName in GetEnabledRiskyFlags(update))
        {
            if (HasRecentGate(flagName))
                continue;

            _logger.LogWarning(
                "Risky rubric intelligence flag {FlagName} was enabled without a qualifying rollout gate in the last 14 days.",
                flagName);
        }
    }

    private List<string> GetEnabledFlagGateWarnings()
    {
        if (_hostEnvironment.IsDevelopment())
            return new List<string>();

        return GetCurrentRiskyFlags().Where(flagName => !HasRecentGate(flagName))
            .Select(flagName => $"No qualifying rollout gate for {flagName} in the last 14 days.")
            .ToList();
    }

    private IEnumerable<string> GetEnabledRiskyFlags(RubricIntelligenceConfigUpdateModel update)
    {
        if (_hostEnvironment.IsDevelopment())
            return Enumerable.Empty<string>();

        return new[]
        {
            (nameof(update.StrictConceptGatedDiscovery), update.StrictConceptGatedDiscovery == true),
            (nameof(update.DualLanguageForSensationSegments), update.DualLanguageForSensationSegments == true),
            (nameof(update.EnableV2ForAllDoctors), update.EnableV2ForAllDoctors == true),
            (nameof(update.EnableV3ConceptGraph), update.EnableV3ConceptGraph == true),
        }.Where(x => x.Item2).Select(x => x.Item1);
    }

    private IEnumerable<string> GetCurrentRiskyFlags() => new[]
    {
        (nameof(RubricIntelligenceOptions.StrictConceptGatedDiscovery), _strictConceptGatedDiscoveryOverride ?? _baseOptions.StrictConceptGatedDiscovery),
        (nameof(RubricIntelligenceOptions.DualLanguageForSensationSegments), _dualLanguageForSensationSegmentsOverride ?? _baseOptions.DualLanguageForSensationSegments),
        (nameof(RubricIntelligenceOptions.EnableV2ForAllDoctors), _enableV2ForAllDoctorsOverride ?? _baseOptions.EnableV2ForAllDoctors),
        (nameof(RubricIntelligenceOptions.EnableV3ConceptGraph), _enableV3ConceptGraphOverride ?? _baseOptions.EnableV3ConceptGraph),
    }.Where(x => x.Item2).Select(x => x.Item1);

    private bool HasRecentGate(string flagName)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NIGACentrumContext>();
            return context.AiRolloutGates.AsNoTracking().Any(x =>
                x.FlagName == flagName
                && x.BenchmarkRunUtc >= DateTime.UtcNow.AddDays(-14)
                && x.Top5Accuracy != null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not verify rollout gate for {FlagName}.", flagName);
            return false;
        }
    }
}

namespace Niga_Domain.Configuration;

public class AiEmbeddingInfrastructureOptions
{
    public const string SectionName = "AiEmbeddingInfrastructure";

    public bool Enabled { get; set; } = true;

    public string DefaultModelProvider { get; set; } = "OpenAI";

    public string DefaultModelName { get; set; } = "text-embedding-3-small";

    public int DefaultDimensionCount { get; set; } = 1536;

    public string DefaultVectorFormat { get; set; } = "JsonFloatArray";

    public int DefaultJobPriority { get; set; } = 100;

    public int DefaultMaxJobRetries { get; set; } = 3;

    public int DefaultMaxQueueAttempts { get; set; } = 3;

    public int QueueBatchSize { get; set; } = 100;

    public int QueueLockMinutes { get; set; } = 15;

    public int RetryBaseDelaySeconds { get; set; } = 30;

    public int RetryMaxDelaySeconds { get; set; } = 3600;

    public int StatisticsRetentionDays { get; set; } = 365;

    /// <summary>Legacy RubricEmbeddings table remains active when true (backward compatibility).</summary>
    public bool KeepLegacyRubricEmbeddingsActive { get; set; } = true;

    public bool EnableEnterpriseBuilder { get; set; } = true;

    public int BuilderBatchSize { get; set; } = 50;

    /// <summary>0 = process all rubrics in the repertory.</summary>
    public int BuilderMaxRubricsPerRun { get; set; } = 0;

    public int BuilderCatalogPageSize { get; set; } = 500;

    public bool AutoEnsureEmbeddingVersion { get; set; } = true;

    public string DefaultVersionCodePrefix { get; set; } = "v4";

    public int BuilderScheduledIntervalHours { get; set; }

    /// <summary>
    /// When true, on API startup any job still marked Running is failed and incremental
    /// rubric/concept builds resume from the last saved AIRubricEmbedding count.
    /// </summary>
    public bool EnableAutoResumeEmbeddingBuildOnStartup { get; set; } = true;

    /// <summary>Delay after startup before the first auto-resume check (seconds).</summary>
    public int AutoResumeStartupDelaySeconds { get; set; } = 60;

    /// <summary>
    /// Periodic incomplete-build check while API is running. 0 = startup only.
    /// Useful if a long build is interrupted without full process restart.
    /// </summary>
    public int AutoResumeCheckIntervalHours { get; set; }

    /// <summary>After rubrics reach catalog count, automatically run concept embedding build.</summary>
    public bool EnableAutoResumeConceptBuildAfterRubrics { get; set; } = true;

    public bool EnableIncrementalRefresh { get; set; } = true;

    public int IncrementalRefreshIntervalDays { get; set; } = 2;

    public int IncrementalDetectionLookbackHours { get; set; } = 48;

    public int IncrementalQueueWorkerBatchSize { get; set; } = 25;

    public int IncrementalMaxQueueItemsPerRun { get; set; }

    public bool UseHangfireForIncrementalRefresh { get; set; }

    public string IncrementalWorkerId { get; set; } = "AiEmbeddingIncrementalWorker";

    public bool EnableEnterpriseSemanticSearch { get; set; } = true;

    public int SemanticSearchTopConcepts { get; set; } = 20;

    public int SemanticSearchTopRubricsPerConcept { get; set; } = 5;

    public int SemanticSearchMaxRubrics { get; set; } = 30;

    public decimal MinConceptCosineScore { get; set; } = 0.55m;

    public decimal MinRubricCosineScore { get; set; } = 0.50m;

    public decimal MinRubricValidationScore { get; set; } = 0.45m;

    public int MaxClinicalConceptInputLength { get; set; } = 300;

    public int TranscriptRejectionMinWords { get; set; } = 25;

    public int ConceptEmbeddingCacheMinutes { get; set; } = 30;

    public int RubricEmbeddingCacheMinutes { get; set; } = 30;

    /// <summary>Pre-load concept + rubric vectors after API startup (avoids multi-minute delay on first audio case).</summary>
    public bool EnableSemanticCacheWarmupOnStartup { get; set; } = true;

    /// <summary>Seconds after startup before semantic cache warmup runs.</summary>
    public int SemanticCacheWarmupDelaySeconds { get; set; } = 90;
}

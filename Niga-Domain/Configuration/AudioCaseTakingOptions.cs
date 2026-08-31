namespace Niga_Domain.Configuration;

public class AudioCaseTakingOptions
{
    public const string SectionName = "AudioCaseTaking";

    public string StoragePath { get; set; } = "Data/AudioCaseTaking";

    public long MaxFileSizeBytes { get; set; } = 52_428_800;

    public int MaxAudioDurationMinutes { get; set; } = 45;

    public string ConsentTextVersion { get; set; } = "v1.0-2026-06-23";

    public bool UseMockWhenNoApiKey { get; set; } = true;

    public bool EnableSemanticRubricMatch { get; set; } = true;

    /// <summary>Translate audio/transcript output to English for repertory matching.</summary>
    public bool OutputEnglishOnly { get; set; } = true;

    /// <summary>When DB match is low, GPT suggests additional rubrics not in database.</summary>
    public bool EnableAiSuggestedRubrics { get; set; } = true;

    public int MaxAiSuggestedRubrics { get; set; } = 10;

    /// <summary>Days to keep audio files. 0 or less means keep forever (no automatic delete).</summary>
    public int AudioRetentionDays { get; set; } = 0;

    public int RetentionJobIntervalHours { get; set; } = 24;

    /// <summary>Maximum wall-clock minutes for a single background analysis job (Whisper + GPT + rubric discovery).</summary>
    public int MaxProcessingMinutes { get; set; } = 10;

    /// <summary>Mark Processing sessions with no progress as Failed after this many minutes.</summary>
    public bool EnableZombieSessionRecovery { get; set; } = true;

    public int ZombieSessionStaleMinutes { get; set; } = 20;

    public int ZombieSweeperIntervalMinutes { get; set; } = 5;

    /// <summary>Mark Uploaded sessions stuck without background pickup as Failed after this many minutes.</summary>
    public int UploadedStaleMinutes { get; set; } = 15;

    /// <summary>Re-queue orphaned Uploaded sessions when the API background worker starts.</summary>
    public bool RequeueOrphanedUploadedOnStartup { get; set; } = true;

    /// <summary>
    /// Max minutes to wait for embedding cache warmup before proceeding without full cache.
    /// Prefer a short wait (default 2) over multi-minute Gap-1 stalls; set 0 to skip waiting.
    /// </summary>
    public int SemanticCacheMaxWaitMinutes { get; set; } = 2;

    /// <summary>Concurrent background jobs reading the same queue. Respects per-session processing gates.</summary>
    public int MaxConcurrentProcessingWorkers { get; set; } = 3;

    /// <summary>Chunk Whisper when audio is longer than this (seconds). Requires ffmpeg/ffprobe.</summary>
    public bool EnableWhisperChunking { get; set; } = true;

    public int WhisperChunkThresholdSeconds { get; set; } = 180;

    public int WhisperChunkSeconds { get; set; } = 90;

    public int WhisperChunkOverlapSeconds { get; set; } = 2;

    public int WhisperChunkMaxParallel { get; set; } = 4;

    /// <summary>Cap GPT extraction prompt size (characters of transcript). Does not log the text.</summary>
    public int ExtractionMaxTranscriptChars { get; set; } = 16000;

    /// <summary>Soft budget: warn (do not fail) when total pipeline exceeds this many minutes.</summary>
    public int PipelineSoftBudgetMinutes { get; set; } = 4;

    /// <summary>Hard budget: error-level log when total pipeline exceeds this many minutes. Still not a job failure.</summary>
    public int PipelineHardBudgetMinutes { get; set; } = 5;
}

public class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string DeploymentNameGpt4o { get; set; } = "gpt-4o";

    public string DeploymentNameEmbedding { get; set; } = "text-embedding-3-small";

    public string ApiVersion { get; set; } = "2024-08-01-preview";

    public bool PreferAzureOverOpenAi { get; set; } = false;
}

public class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string WhisperModel { get; set; } = "whisper-1";

    public string ChatModel { get; set; } = "gpt-4o";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

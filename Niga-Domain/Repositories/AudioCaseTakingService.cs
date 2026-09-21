using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services;
using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;
using Niga_Domain.Services.AudioCaseIntelligence.Orchestration;
using Niga_Domain.Services.AudioCaseIntelligence.Validation;

namespace Niga_Domain.Repositories;

public class AudioCaseTakingService : IAudioCaseTakingService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".webm", ".ogg", ".m4a", ".aac", ".mp4",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly NIGACentrumContext _context;
    private readonly ISubSectionRepository _subSectionRepository;
    private readonly AudioCaseAiProcessor _aiProcessor;
    private readonly IAudioCaseTakingQueue _queue;
    private readonly AudioCaseTakingOptions _options;
    private readonly RubricIntelligenceOptions _intelligenceOptions;
    private readonly IRubricIntelligenceSettingsService _intelligenceSettings;
    private readonly IRubricIntelligenceOrchestrator _intelligenceOrchestrator;
    private readonly IHomeopathicWeightEngine _weightEngine;
    private readonly IClinicalValidationEngine _clinicalValidationEngine;
    private readonly IConceptGraphOrchestrator _conceptGraphOrchestrator;
    private readonly IConceptGraphRepository _conceptGraphRepository;
    private readonly IConceptKeywordDiscoveryEngine _conceptKeywordDiscoveryEngine;
    private readonly IAudioCaseIntelligenceRepository _intelligenceRepository;
    private readonly AiSuggestedRubricReconciler _aiRubricReconciler;
    private readonly IFastClinicalRetrievalOrchestrator _fastClinicalRetrieval;
    private readonly IRubricPipelineTelemetry _pipelineTelemetry;
    private readonly IAudioCaseSessionProcessingGate _sessionGate;
    private readonly ILogger<AudioCaseTakingService> _logger;

    public AudioCaseTakingService(
        NIGACentrumContext context,
        ISubSectionRepository subSectionRepository,
        AudioCaseAiProcessor aiProcessor,
        IAudioCaseTakingQueue queue,
        IOptions<AudioCaseTakingOptions> options,
        IOptions<RubricIntelligenceOptions> intelligenceOptions,
        IRubricIntelligenceSettingsService intelligenceSettings,
        IRubricIntelligenceOrchestrator intelligenceOrchestrator,
        IHomeopathicWeightEngine weightEngine,
        IClinicalValidationEngine clinicalValidationEngine,
        IConceptGraphOrchestrator conceptGraphOrchestrator,
        IConceptGraphRepository conceptGraphRepository,
        IConceptKeywordDiscoveryEngine conceptKeywordDiscoveryEngine,
        IAudioCaseIntelligenceRepository intelligenceRepository,
        AiSuggestedRubricReconciler aiRubricReconciler,
        IFastClinicalRetrievalOrchestrator fastClinicalRetrieval,
        IRubricPipelineTelemetry pipelineTelemetry,
        IAudioCaseSessionProcessingGate sessionGate,
        ILogger<AudioCaseTakingService> logger)
    {
        _context = context;
        _subSectionRepository = subSectionRepository;
        _aiProcessor = aiProcessor;
        _queue = queue;
        _options = options.Value;
        _intelligenceOptions = intelligenceOptions.Value;
        _intelligenceSettings = intelligenceSettings;
        _intelligenceOrchestrator = intelligenceOrchestrator;
        _weightEngine = weightEngine;
        _clinicalValidationEngine = clinicalValidationEngine;
        _conceptGraphOrchestrator = conceptGraphOrchestrator;
        _conceptGraphRepository = conceptGraphRepository;
        _conceptKeywordDiscoveryEngine = conceptKeywordDiscoveryEngine;
        _intelligenceRepository = intelligenceRepository;
        _aiRubricReconciler = aiRubricReconciler;
        _fastClinicalRetrieval = fastClinicalRetrieval;
        _pipelineTelemetry = pipelineTelemetry;
        _sessionGate = sessionGate;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AudioCaseUploadResultModel? Result)> UploadAsync(
        int doctorUserId,
        AudioCaseUploadRequestModel request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (doctorUserId <= 0) return (false, "Invalid user.", null);
        if (request.AudioFile == null || request.AudioFile.Length == 0) return (false, "Audio file is required.", null);
        if (request.PatientId <= 0) return (false, "Patient id is required.", null);
        if (!request.ConsentGiven) return (false, "Patient consent is required before upload.", null);
        if (request.AudioFile.Length > _options.MaxFileSizeBytes) return (false, "Audio file exceeds maximum allowed size.", null);

        var extension = Path.GetExtension(request.OriginalFileName ?? request.AudioFile.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return (false, "Unsupported audio file type.", null);

        var sessionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        var now = DateTime.UtcNow;
        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), _options.StoragePath);
        Directory.CreateDirectory(storageRoot);

        var storedFileName = $"{sessionId:N}{extension}";
        var storedPath = Path.Combine(storageRoot, storedFileName);

        await using (var stream = File.Create(storedPath))
        {
            await request.AudioFile.CopyToAsync(stream, cancellationToken);
        }

        var audioSource = string.IsNullOrWhiteSpace(request.AudioSource) ? "LiveRecording" : request.AudioSource.Trim();

        var session = new AudioCaseSession
        {
            AudioCaseSessionId = sessionId,
            PatientId = request.PatientId,
            CaseId = request.CaseId,
            DoctorUserId = doctorUserId,
            PatientAppId = request.PatientAppId,
            AudioSourceType = audioSource,
            Status = "Uploaded",
            CurrentStep = "Uploaded",
            AudioFilePath = storedPath,
            AudioFileName = request.OriginalFileName ?? request.AudioFile.FileName,
            AudioMimeType = request.AudioFile.ContentType,
            AudioFileSizeBytes = request.AudioFile.Length,
            AudioSha256Hash = AudioCaseAiProcessor.ComputeSha256File(storedPath),
            LanguageOverride = request.Language,
            CorrelationId = correlationId,
            EnteredBy = doctorUserId,
            EnteredDate = now,
            DeleteStatus = false,
        };

        _context.AudioCaseSessions.Add(session);
        _context.AudioCaseConsentLogs.Add(new AudioCaseConsentLog
        {
            AudioCaseSessionId = sessionId,
            PatientId = request.PatientId,
            DoctorUserId = doctorUserId,
            ConsentType = "AudioRecordingClinical",
            ConsentTextVersion = _options.ConsentTextVersion,
            ConsentGiven = true,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            EnteredDate = now,
        });

        var consentType = await _context.ConsentTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && (t.Code == "TeleRecording" || t.Code == "AudioRecording"), cancellationToken);
        if (consentType != null)
        {
            _context.ConsentRecords.Add(new ConsentRecord
            {
                ConsentTypeId = consentType.ConsentTypeId,
                SubjectType = "Patient",
                SubjectId = request.PatientId,
                GrantedByUserId = doctorUserId,
                GrantedAt = now,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Notes = "AudioCaseConsentLog dual-write session " + sessionId
            });
        }

        await LogEventAsync(sessionId, correlationId, "SessionCreated", "Success", "Audio case session created.", doctorUserId, ipAddress);
        await LogEventAsync(sessionId, correlationId,
            audioSource.Equals("FileUpload", StringComparison.OrdinalIgnoreCase) ? "FileUploaded" : "AudioUploaded",
            "Success", "Audio uploaded successfully.", doctorUserId, ipAddress,
            new { request.OriginalFileName, request.AudioFile.Length });
        await LogEventAsync(sessionId, correlationId, "ConsentRecorded", "Success", "Consent recorded.", doctorUserId, ipAddress);
        await _context.SaveChangesAsync(cancellationToken);

        await _queue.EnqueueAsync(new AudioCaseTakingJob { SessionId = sessionId, CorrelationId = correlationId, JobType = "ProcessAudio" }, cancellationToken);

        return (true, "Audio uploaded successfully. Processing started.", new AudioCaseUploadResultModel { SessionId = sessionId, Status = session.Status });
    }

    public async Task<(bool Success, string Message, AudioCaseUploadResultModel? Result)> ReAnalyzeFromTranscriptAsync(
        int doctorUserId, Guid sessionId, string transcript, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var session = await _context.AudioCaseSessions.FirstOrDefaultAsync(
            x => x.AudioCaseSessionId == sessionId && x.DoctorUserId == doctorUserId && !x.DeleteStatus, cancellationToken);
        if (session == null) return (false, "Session not found.", null);
        if (string.IsNullOrWhiteSpace(transcript)) return (false, "Transcript is required.", null);
        if (_sessionGate.IsInFlight(sessionId)
            || string.Equals(session.Status, "Processing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "Transcribing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "Extracting", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "MatchingRubrics", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Session is already processing. Wait for completion before re-analyzing.", null);
        }

        session.TranscriptRaw = transcript.Trim();
        session.Status = "Processing";
        session.CurrentStep = "ReAnalysisRequested";
        session.ReAnalysisCount += 1;
        session.ChangedDate = DateTime.UtcNow;
        session.ErrorCode = null;
        session.ErrorMessage = null;

        await LogEventAsync(sessionId, session.CorrelationId, "TranscriptEdited", "Success", "Transcript updated for re-analysis.", doctorUserId, ipAddress);
        await LogEventAsync(sessionId, session.CorrelationId, "ReAnalysisRequested", "InProgress", "Re-analysis queued.", doctorUserId, ipAddress);
        await _context.SaveChangesAsync(cancellationToken);

        await _queue.EnqueueAsync(new AudioCaseTakingJob
        {
            SessionId = sessionId,
            CorrelationId = session.CorrelationId ?? sessionId.ToString("N")[..12],
            JobType = "ReAnalyze",
            EditedTranscript = transcript.Trim(),
        }, cancellationToken);

        return (true, "Re-analysis started.", new AudioCaseUploadResultModel { SessionId = sessionId, Status = "Processing" });
    }

    public async Task<(bool Success, string Message)> LogDoctorActionAsync(
        int doctorUserId, Guid sessionId, AudioCaseDoctorActionRequestModel request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var exists = await _context.AudioCaseSessions.AnyAsync(
            x => x.AudioCaseSessionId == sessionId && x.DoctorUserId == doctorUserId && !x.DeleteStatus, cancellationToken);
        if (!exists) return (false, "Session not found.");

        _context.AudioCaseDoctorActionLogs.Add(new AudioCaseDoctorActionLog
        {
            AudioCaseSessionId = sessionId,
            DoctorUserId = doctorUserId,
            ActionType = request.ActionType,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            BeforeJson = request.BeforeJson,
            AfterJson = request.AfterJson,
            Notes = request.Notes,
            IpAddress = ipAddress,
            EnteredDate = DateTime.UtcNow,
        });

        await LogEventAsync(sessionId, null, request.ActionType, "Success", request.Notes ?? request.ActionType, doctorUserId, ipAddress);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, "Doctor action logged.");
    }

    public async Task<(bool Success, string Message, AudioCaseLatestSessionModel? Result)> GetLatestSessionAsync(
        int doctorUserId, long patientId, long? caseId, CancellationToken cancellationToken = default)
    {
        var query = _context.AudioCaseSessions.AsNoTracking()
            .Where(x => x.DoctorUserId == doctorUserId && x.PatientId == patientId && !x.DeleteStatus);

        if (caseId.HasValue)
            query = query.Where(x => x.CaseId == caseId);

        var session = await query.OrderByDescending(x => x.EnteredDate).FirstOrDefaultAsync(cancellationToken);
        if (session == null)
            return (true, "No session found.", new AudioCaseLatestSessionModel());

        var isStale = IsSessionStale(session);
        var canResume = !isStale
            && !string.Equals(session.Status, "Failed", StringComparison.OrdinalIgnoreCase);

        AudioCaseResultModel? result = string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            ? MapResult(session)
            : null;

        return (true, "Latest session fetched.", new AudioCaseLatestSessionModel
        {
            SessionId = session.AudioCaseSessionId,
            Status = session.Status,
            ProgressStep = session.CurrentStep,
            AudioFileName = session.AudioFileName,
            EnteredDate = session.EnteredDate,
            ErrorMessage = session.ErrorMessage,
            IsStale = isStale,
            CanResume = canResume,
            Result = result,
        });
    }

    public async Task<(bool Success, string Message, AudioCaseSessionListModel? Result)> GetSessionsAsync(
        int doctorUserId,
        long patientId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (patientId <= 0)
            return (false, "Patient is required.", null);

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AudioCaseSessions.AsNoTracking()
            .Where(x => x.DoctorUserId == doctorUserId && x.PatientId == patientId && !x.DeleteStatus);

        var statusRows = await query
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(params string[] names) => statusRows
            .Where(x => names.Any(name => string.Equals(x.Status, name, StringComparison.OrdinalIgnoreCase)))
            .Sum(x => x.Count);

        var totalCount = statusRows.Sum(x => x.Count);
        var completedCount = CountOf("Completed");
        var failedCount = CountOf("Failed", "Cancelled");
        var processingCount = Math.Max(0, totalCount - completedCount - failedCount);

        var sessions = await query
            .OrderByDescending(x => x.EnteredDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = sessions.Select(session =>
        {
            var isStale = IsSessionStale(session);
            var status = session.Status ?? string.Empty;
            var isCompleted = string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase);
            var isFailed = string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase);

            return new AudioCaseSessionListItemModel
            {
                SessionId = session.AudioCaseSessionId,
                Status = status,
                ProgressStep = session.CurrentStep,
                StageLabel = MapStageLabel(status, session.CurrentStep),
                AudioSourceType = session.AudioSourceType,
                AudioFileName = session.AudioFileName,
                AudioDurationSeconds = session.AudioDurationSeconds,
                EnteredDate = session.EnteredDate,
                CompletedAtUtc = session.CompletedAtUtc,
                ErrorMessage = session.ErrorMessage,
                HasAudioFile = HasStoredAudioFile(session),
                IsStale = isStale,
                CanOpen = isCompleted || isFailed || !isStale,
                ChiefComplaintSnippet = ExtractChiefComplaintSnippet(session.SummaryJson),
                MessageCount = CountJsonArray(session.ConversationJson),
                RubricCount = CountJsonArray(session.SuggestedRubricsJson),
            };
        }).ToList();

        return (true, "Sessions fetched.", new AudioCaseSessionListModel
        {
            Items = items,
            TotalCount = totalCount,
            CompletedCount = completedCount,
            ProcessingCount = processingCount,
            FailedCount = failedCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasMore = pageNumber * pageSize < totalCount,
        });
    }

    private bool IsSessionStale(AudioCaseSession session)
    {
        if (string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(session.Status, "Uploaded", StringComparison.OrdinalIgnoreCase))
        {
            var uploadedStaleMinutes = Math.Max(5, _options.UploadedStaleMinutes);
            return session.EnteredDate.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-uploadedStaleMinutes);
        }

        if (string.Equals(session.Status, "Processing", StringComparison.OrdinalIgnoreCase))
        {
            var processingStaleMinutes = Math.Max(5, _options.ZombieSessionStaleMinutes);
            var lastChange = session.ChangedDate ?? session.EnteredDate;
            return lastChange.ToUniversalTime() < DateTime.UtcNow.AddMinutes(-processingStaleMinutes);
        }

        return false;
    }

    public async Task<(bool Success, string Message, AudioCaseStatusModel? Result)> GetStatusAsync(
        int doctorUserId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(doctorUserId, sessionId, cancellationToken);
        if (session == null) return (false, "Session not found.", null);

        return (true, "Status fetched successfully.", new AudioCaseStatusModel
        {
            SessionId = session.AudioCaseSessionId,
            Status = session.Status,
            ProgressStep = session.CurrentStep,
            StageLabel = MapStageLabel(session.Status, session.CurrentStep),
            Percent = MapProgressPercent(session.Status, session.CurrentStep),
            ErrorMessage = session.ErrorMessage,
            ProcessingStartedAt = session.EnteredDate,
            EngineVersion = session.IntelligenceEngineVersion ?? session.ConceptGraphEngineVersion,
            // EnteredDate/ChangedDate are stored as UTC wall-clock values (Unspecified Kind from SQL).
            // Do NOT call ToUniversalTime() — that treats them as local and inflates elapsed by the TZ offset (e.g. ~330m in IST).
            ElapsedSeconds = ComputeElapsedSeconds(session),
        });
    }

    private static int ComputeElapsedSeconds(AudioCaseSession session)
    {
        // AudioCaseSession timestamps are persisted via SaveChanges as DateTime.Now (local wall clock).
        // Comparing them to DateTime.UtcNow (or calling ToUniversalTime on Unspecified values) inflates
        // elapsed time by the server timezone offset (e.g. ~5h30m / ~330 minutes in IST).
        var start = session.EnteredDate;
        var isTerminal =
            string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "Failed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);

        var end = isTerminal && session.ChangedDate.HasValue
            ? session.ChangedDate.Value
            : DateTime.Now;

        var elapsed = (end - start).TotalSeconds;
        if (elapsed < 0 || elapsed > TimeSpan.FromDays(2).TotalSeconds)
        {
            // Guard against mixed UTC/local corruption from older rows.
            elapsed = Math.Max(0, (DateTime.Now - start).TotalSeconds);
            if (elapsed > TimeSpan.FromDays(2).TotalSeconds)
            {
                elapsed = 0;
            }
        }

        return (int)elapsed;
    }

    public async Task<(bool Success, string Message, AudioCaseResultModel? Result)> GetResultAsync(
        int doctorUserId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(doctorUserId, sessionId, cancellationToken);
        if (session == null) return (false, "Session not found.", null);
        if (!string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            return (false, "Analysis is not completed yet.", null);

        var result = MapResult(session);
        result.SuggestedRubrics = await ApplyLiveSubSectionMasterAsync(result.SuggestedRubrics, cancellationToken);
        result.ProcessingMetrics = await TryLoadProcessingMetricsAsync(sessionId, cancellationToken);
        return (true, "Result fetched successfully.", result);
    }

    public async Task<(bool Success, string Message, (byte[] Bytes, string FileName, string ContentType)? File)> DownloadAsync(
        int doctorUserId, Guid sessionId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(doctorUserId, sessionId, cancellationToken);
        if (session == null) return (false, "Session not found.", null);
        if (string.IsNullOrWhiteSpace(session.AudioFilePath) || !File.Exists(session.AudioFilePath))
            return (false, "Recording file is no longer available.", null);

        var bytes = await File.ReadAllBytesAsync(session.AudioFilePath, cancellationToken);
        await LogEventAsync(sessionId, session.CorrelationId, "AudioDownloaded", "Success", "Recording downloaded.", doctorUserId, ipAddress);
        await _context.SaveChangesAsync(cancellationToken);

        return (true, "Recording downloaded successfully.", (bytes, session.AudioFileName ?? $"{sessionId:N}.webm", session.AudioMimeType ?? "application/octet-stream"));
    }

    public async Task ProcessSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.AudioCaseSessions.FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId && !x.DeleteStatus, cancellationToken);
        if (session == null) return;

        var correlationId = session.CorrelationId ?? sessionId.ToString("N")[..12];
        if (!_pipelineTelemetry.IsActive)
            _pipelineTelemetry.BeginSession(sessionId, correlationId);
        _pipelineTelemetry.SetEngineVersion(RubricEngineStamp.FromRuntime(_intelligenceOptions, _intelligenceSettings));

        using var processingTimeout = CreateProcessingTimeoutSource(cancellationToken);
        var processingToken = processingTimeout.Token;
        try
        {
            await UpdateSessionProgressAsync(session, "Processing", "ProcessingStarted", 20);
            await LogEventAsync(sessionId, correlationId, "ProcessingStarted", "InProgress", "Background processing started.");

            var language = session.LanguageOverride;
            await UpdateSessionProgressAsync(session, "Transcribing", "TranscriptionStarted", 35);

            string englishTranscript;
            string? detectedLanguage;

            var whisperStart = DateTime.UtcNow;
            var whisperSw = Stopwatch.StartNew();
            if (_options.OutputEnglishOnly)
            {
                var translation = await _aiProcessor.TranslateAudioToEnglishAsync(session.AudioFilePath!, processingToken);
                await LogAiRequestAsync(sessionId, correlationId, "OpenAI", "Translation", "whisper-1", translation.LatencyMs,
                    translation.RequestJson, translation.ResponseJson, translation.Success, translation.Error);

                if (!translation.Success || string.IsNullOrWhiteSpace(translation.Transcript))
                    throw new InvalidOperationException(translation.Error ?? "English translation failed.");

                englishTranscript = translation.Transcript!;
                detectedLanguage = translation.Language ?? "en";
            }
            else
            {
                var transcription = await _aiProcessor.TranscribeAsync(session.AudioFilePath!, language, processingToken);
                await LogAiRequestAsync(sessionId, correlationId, "OpenAI", "Transcription", "whisper-1", transcription.LatencyMs,
                    transcription.RequestJson, transcription.ResponseJson, transcription.Success, transcription.Error);

                if (!transcription.Success || string.IsNullOrWhiteSpace(transcription.Transcript))
                    throw new InvalidOperationException(transcription.Error ?? "Transcription failed.");

                englishTranscript = transcription.Transcript!;
                detectedLanguage = transcription.Language;
            }

            whisperSw.Stop();
            await _pipelineTelemetry.RecordStageAsync(
                "Whisper",
                whisperSw.ElapsedMilliseconds,
                whisperStart,
                DateTime.UtcNow,
                message: $"detectedLanguage={detectedLanguage}",
                cancellationToken: processingToken);

            session.TranscriptRaw = englishTranscript;
            session.DetectedLanguage = detectedLanguage;
            await UpdateSessionProgressAsync(session, "Transcribing", "TranscriptionCompleted", 50);
            await LogEventAsync(sessionId, correlationId, "TranscriptionCompleted", "Success", "English transcript ready.");

            await RunExtractionAndRubricsAsync(session, correlationId, englishTranscript, processingToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await FailSessionAsync(
                session,
                correlationId,
                new TimeoutException(BuildProcessingTimeoutMessage(session.CurrentStep)));
        }
        catch (Exception ex)
        {
            await FailSessionAsync(session, correlationId, ex);
        }
    }

    public async Task ProcessReAnalyzeAsync(Guid sessionId, string transcript, CancellationToken cancellationToken = default)
    {
        var session = await _context.AudioCaseSessions.FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId && !x.DeleteStatus, cancellationToken);
        if (session == null) return;

        var correlationId = session.CorrelationId ?? sessionId.ToString("N")[..12];
        if (!_pipelineTelemetry.IsActive)
            _pipelineTelemetry.BeginSession(sessionId, correlationId);
        _pipelineTelemetry.SetEngineVersion(RubricEngineStamp.FromRuntime(_intelligenceOptions, _intelligenceSettings));

        using var processingTimeout = CreateProcessingTimeoutSource(cancellationToken);
        var processingToken = processingTimeout.Token;
        try
        {
            session.TranscriptRaw = transcript;
            session.Status = "Processing";
            session.CurrentStep = "LlmExtractionStarted";
            await _context.SaveChangesAsync(processingToken);
            await RunExtractionAndRubricsAsync(session, correlationId, transcript, processingToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await FailSessionAsync(
                session,
                correlationId,
                new TimeoutException(BuildProcessingTimeoutMessage(session.CurrentStep)));
        }
        catch (Exception ex)
        {
            await FailSessionAsync(session, correlationId, ex);
        }
    }

    public async Task<int> PurgeExpiredAudioFilesAsync(CancellationToken cancellationToken = default)
    {
        // 0 or less = keep recordings forever; never delete automatically.
        if (_options.AudioRetentionDays <= 0) return 0;

        var cutoff = DateTime.UtcNow.AddDays(-_options.AudioRetentionDays);
        var sessions = await _context.AudioCaseSessions
            .Where(x => !x.DeleteStatus && x.CompletedAtUtc != null && x.CompletedAtUtc < cutoff && x.AudioPurgedAtUtc == null)
            .ToListAsync(cancellationToken);

        var purged = 0;
        foreach (var session in sessions)
        {
            if (!string.IsNullOrWhiteSpace(session.AudioFilePath) && File.Exists(session.AudioFilePath))
            {
                try { File.Delete(session.AudioFilePath); } catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete audio file {Path}", session.AudioFilePath); }
            }

            session.AudioPurgedAtUtc = DateTime.UtcNow;
            session.ChangedDate = DateTime.UtcNow;
            _context.AudioCaseRetentionLogs.Add(new AudioCaseRetentionLog
            {
                AudioCaseSessionId = session.AudioCaseSessionId,
                ActionType = "AudioBlobDeleted",
                Reason = $"RetentionPolicy{_options.AudioRetentionDays}Days",
                PerformedBy = "SystemJob",
                EnteredDate = DateTime.UtcNow,
            });
            purged++;
        }

        if (purged > 0) await _context.SaveChangesAsync(cancellationToken);
        return purged;
    }

    public async Task<int> RecoverStaleProcessingSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableZombieSessionRecovery) return 0;

        var processingStaleMinutes = Math.Max(5, _options.ZombieSessionStaleMinutes);
        var uploadedStaleMinutes = Math.Max(5, _options.UploadedStaleMinutes);
        var processingCutoff = DateTime.UtcNow.AddMinutes(-processingStaleMinutes);
        var uploadedCutoff = DateTime.UtcNow.AddMinutes(-uploadedStaleMinutes);
        var recovered = 0;

        var staleProcessing = await _context.AudioCaseSessions
            .Where(x => !x.DeleteStatus
                && x.Status == "Processing"
                && x.ChangedDate < processingCutoff)
            .ToListAsync(cancellationToken);

        foreach (var session in staleProcessing)
        {
            var correlationId = session.CorrelationId ?? session.AudioCaseSessionId.ToString("N")[..12];
            await FailSessionAsync(
                session,
                correlationId,
                new InvalidOperationException(
                    $"Analysis did not complete within {processingStaleMinutes} minutes (stuck at {session.CurrentStep ?? "unknown"}). Please try again."),
                errorCode: "PROCESSING_STALE");
            recovered++;
        }

        var staleUploaded = await _context.AudioCaseSessions
            .Where(x => !x.DeleteStatus
                && x.Status == "Uploaded"
                && x.EnteredDate < uploadedCutoff)
            .ToListAsync(cancellationToken);

        foreach (var session in staleUploaded)
        {
            var correlationId = session.CorrelationId ?? session.AudioCaseSessionId.ToString("N")[..12];
            await FailSessionAsync(
                session,
                correlationId,
                new InvalidOperationException(
                    "Upload was never processed (server may have restarted). Please upload again."),
                errorCode: "UPLOAD_STALE");
            recovered++;
        }

        return recovered;
    }

    public async Task<int> RequeueOrphanedUploadedSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.RequeueOrphanedUploadedOnStartup) return 0;

        var uploadedStaleMinutes = Math.Max(5, _options.UploadedStaleMinutes);
        var freshCutoff = DateTime.UtcNow.AddMinutes(-uploadedStaleMinutes);

        var orphaned = await _context.AudioCaseSessions
            .Where(x => !x.DeleteStatus
                && x.Status == "Uploaded"
                && x.EnteredDate >= freshCutoff)
            .OrderBy(x => x.EnteredDate)
            .ToListAsync(cancellationToken);

        var requeued = 0;
        foreach (var session in orphaned)
        {
            if (string.IsNullOrWhiteSpace(session.AudioFilePath) || !File.Exists(session.AudioFilePath))
            {
                var correlationId = session.CorrelationId ?? session.AudioCaseSessionId.ToString("N")[..12];
                await FailSessionAsync(
                    session,
                    correlationId,
                    new InvalidOperationException("Audio file is missing. Please upload again."),
                    errorCode: "UPLOAD_FILE_MISSING");
                requeued++;
                continue;
            }

            await _queue.EnqueueAsync(new AudioCaseTakingJob
            {
                SessionId = session.AudioCaseSessionId,
                CorrelationId = session.CorrelationId ?? session.AudioCaseSessionId.ToString("N")[..12],
                JobType = "ProcessAudio",
            }, cancellationToken);
            requeued++;
        }

        return requeued;
    }

    public async Task FailSessionFromSystemAsync(
        Guid sessionId,
        string errorCode,
        string message,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.AudioCaseSessions
            .FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId && !x.DeleteStatus, cancellationToken);
        if (session == null) return;
        if (string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var correlationId = session.CorrelationId ?? sessionId.ToString("N")[..12];
        await FailSessionAsync(
            session,
            correlationId,
            new InvalidOperationException(message),
            errorCode: errorCode);
    }

    private CancellationTokenSource CreateProcessingTimeoutSource(CancellationToken cancellationToken)
    {
        var timeoutMinutes = Math.Max(1, _options.MaxProcessingMinutes);
        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(TimeSpan.FromMinutes(timeoutMinutes));
        return linked;
    }

    private string BuildProcessingTimeoutMessage(string? currentStep) =>
        $"Audio analysis exceeded the {_options.MaxProcessingMinutes}-minute limit"
        + (string.IsNullOrWhiteSpace(currentStep) ? "." : $" at step '{currentStep}'.")
        + " Please try again after the API has finished starting up.";

    private async Task FailSessionAsync(AudioCaseSession session, string correlationId, Exception ex, string? errorCode = null)
    {
        var detail = UnwrapDbError(ex);
        _logger.LogError(ex, "Audio case processing failed for SessionId={SessionId}. Detail={Detail}",
            session.AudioCaseSessionId, detail);
        session.Status = "Failed";
        session.CurrentStep = "Failed";
        session.ErrorCode = errorCode
            ?? (ex is TimeoutException ? "PROCESSING_TIMEOUT" : "PROCESSING_FAILED");
        session.ErrorMessage = TruncateForSession(detail, 2000);
        session.ChangedDate = DateTime.UtcNow;
        await LogEventAsync(session.AudioCaseSessionId, correlationId, "SessionFailed", "Failure", session.ErrorMessage);
        await _context.SaveChangesAsync();
    }

    private static string UnwrapDbError(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;

        // Prefer the deepest message (SQL truncation / FK / etc.) over the generic EF wrapper.
        if (!string.IsNullOrWhiteSpace(current.Message)
            && !string.Equals(current.Message, ex.Message, StringComparison.Ordinal))
        {
            return $"{ex.Message} | Inner: {current.Message}";
        }

        return ex.Message;
    }

    private static string TruncateForSession(string value, int maxLen) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLen
            ? value
            : value[..(maxLen - 3)] + "...";

    private async Task RunExtractionAndRubricsAsync(AudioCaseSession session, string correlationId, string transcript, CancellationToken cancellationToken)
    {
        var sessionId = session.AudioCaseSessionId;
        await UpdateSessionProgressAsync(session, "Extracting", "LlmExtractionStarted", 65);

        var extractStart = DateTime.UtcNow;
        var extractSw = Stopwatch.StartNew();
        var extractionResult = await _aiProcessor.ExtractCaseDataAsync(transcript, cancellationToken);
        extractSw.Stop();
        _pipelineTelemetry.IncrementLlmCalls();
        await LogAiRequestAsync(sessionId, correlationId, "OpenAI", "ChatCompletion", "gpt-4o",
            extractionResult.LatencyMs, extractionResult.RequestJson, extractionResult.ResponseJson,
            extractionResult.Success, extractionResult.Error, extractionResult.PromptTokens, extractionResult.CompletionTokens);

        await _pipelineTelemetry.RecordStageAsync(
            "GptExtraction",
            extractSw.ElapsedMilliseconds,
            extractStart,
            DateTime.UtcNow,
            status: extractionResult.Success ? "Success" : "Failure",
            message: extractionResult.Success ? "extraction_ok" : "extraction_failed",
            cancellationToken: cancellationToken);

        if (!extractionResult.Success || extractionResult.Result == null)
            throw new InvalidOperationException(extractionResult.Error ?? "Case extraction failed.");

        var extraction = extractionResult.Result;
        // TranscriptRaw already set from Whisper — do not replace with GPT echo.

        session.ConversationJson = JsonSerializer.Serialize(extraction.Conversation, JsonOptions);
        session.SummaryJson = JsonSerializer.Serialize(extraction.Summary, JsonOptions);
        session.ExtractedSymptomsJson = JsonSerializer.Serialize(extraction.Symptoms, JsonOptions);
        session.DetectedLanguage = extraction.DetectedLanguage ?? session.DetectedLanguage;

        DualLanguageMeaningContext? dualLanguage = null;
        if (_intelligenceOptions.DualLanguageForSensationSegments
            && !_intelligenceOptions.EnableFastClinicalRetrievalPipeline)
        {
            var dualStart = DateTime.UtcNow;
            var dualSw = Stopwatch.StartNew();
            dualLanguage = await TryBuildDualLanguageContextAsync(
                session,
                correlationId,
                transcript,
                extraction,
                cancellationToken);
            dualSw.Stop();
            await _pipelineTelemetry.RecordStageAsync(
                "DualLanguageWhisper",
                dualSw.ElapsedMilliseconds,
                dualStart,
                DateTime.UtcNow,
                message: dualLanguage == null ? "skipped_or_empty" : "built",
                cancellationToken: cancellationToken);
        }

        await UpdateSessionProgressAsync(session, "Extracting", "LlmExtractionCompleted", 80);
        await LogEventAsync(sessionId, correlationId, "LlmExtractionCompleted", "Success", "LLM extraction completed.");

        if (_intelligenceSettings.IsV3Active)
        {
            await UpdateSessionProgressAsync(session, "ConceptGraph", "ConceptGraphPipelineStarted", 85);
        }

        await UpdateSessionProgressAsync(session, "MatchingRubrics", "RubricMatchingStarted", 90);
        var matchSw = Stopwatch.StartNew();
        var matchStart = DateTime.UtcNow;
        var rubrics = await MatchRubricsWithIntelligenceAsync(
            session,
            correlationId,
            transcript,
            extraction.Symptoms,
            extraction.Summary,
            dualLanguage,
            cancellationToken);
        matchSw.Stop();
        await LogEventAsync(sessionId, correlationId, "LatencyGap3_MatchRubricsDone", "Success",
            $"MatchRubricsWithIntelligence elapsedMs={matchSw.ElapsedMilliseconds}; rubricCount={rubrics.Count}",
            details: new { matchSw.ElapsedMilliseconds, rubricCount = rubrics.Count });

        var dbBacked = rubrics.Count(r => r.SubSectionId > 0 && !r.IsAiSuggested);
        var aiOnly = rubrics.Count(r => r.IsAiSuggested || r.SubSectionId <= 0);
        _pipelineTelemetry.SetFinalRubricCounts(rubrics.Count, dbBacked, aiOnly);
        await _pipelineTelemetry.RecordStageAsync(
            "MatchRubricsWithIntelligence",
            matchSw.ElapsedMilliseconds,
            matchStart,
            DateTime.UtcNow,
            candidateCount: rubrics.Count,
            finalRubricCount: rubrics.Count,
            message: $"dbBacked={dbBacked}; aiOnly={aiOnly}",
            cancellationToken: cancellationToken);

        var finalizeSw = Stopwatch.StartNew();
        var finalizeStart = DateTime.UtcNow;
        session.SuggestedRubricsJson = JsonSerializer.Serialize(rubrics, JsonOptions);
        await LogEventAsync(sessionId, correlationId, "LatencyGap3_SuggestedRubricsJsonWritten", "Success",
            $"SuggestedRubricsJson length={session.SuggestedRubricsJson?.Length ?? 0}");

        session.Status = "Completed";
        session.CurrentStep = "Completed";
        session.CompletedAtUtc = DateTime.UtcNow;
        session.ChangedDate = DateTime.UtcNow;
        await LogEventAsync(sessionId, correlationId, "RubricMatchingCompleted", "Success", "Rubric matching completed.", details: new { rubricCount = rubrics.Count });
        await LogEventAsync(sessionId, correlationId, "SessionCompleted", "Success", "Audio case session completed.");
        await _context.SaveChangesAsync(cancellationToken);
        finalizeSw.Stop();
        await LogEventAsync(sessionId, correlationId, "LatencyGap3_StatusCompletedSaved", "Success",
            $"Status=Completed SaveChanges elapsedMs={finalizeSw.ElapsedMilliseconds}");
        await _pipelineTelemetry.RecordStageAsync(
            "Finalization",
            finalizeSw.ElapsedMilliseconds,
            finalizeStart,
            DateTime.UtcNow,
            finalRubricCount: rubrics.Count,
            cancellationToken: cancellationToken);
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> MatchRubricsWithIntelligenceAsync(
        AudioCaseSession session,
        string correlationId,
        string transcript,
        List<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary,
        DualLanguageMeaningContext? dualLanguage,
        CancellationToken cancellationToken)
    {
        var sessionId = session.AudioCaseSessionId;

        if (!_intelligenceSettings.IsV2Active)
        {
            var v1Only = await MatchRubricsV1Async(sessionId, correlationId, symptoms, summary, cancellationToken: cancellationToken);
            ApplyRubricMetadata(v1Only, "v1", requiresManualApproval: false);
            return await FinalizeRubricsForResponseAsync(sessionId, v1Only, null, cancellationToken);
        }

        // Stage C: explicit production fast path (rollback = set EnableFastClinicalRetrievalPipeline=false).
        if (_intelligenceOptions.EnableFastClinicalRetrievalPipeline)
        {
            return await MatchRubricsFastClinicalAsync(
                session, correlationId, transcript, symptoms, summary, cancellationToken);
        }

        var patientContext = await LoadPatientClinicalContextAsync(session.PatientId, cancellationToken);
        List<AudioCaseSuggestedRubricModel> v3SupplementRubrics = new();

        if (_intelligenceSettings.IsV3Active)
        {
            var v3Start = DateTime.UtcNow;
            var v3Sw = Stopwatch.StartNew();
            var v3Result = await _conceptGraphOrchestrator.AnalyzeAsync(
                sessionId,
                transcript,
                session.DetectedLanguage,
                patientContext,
                summary,
                dualLanguage,
                cancellationToken);
            v3Sw.Stop();
            _pipelineTelemetry.SetCandidateCount(v3Result.Rubrics?.Count ?? 0);
            await _pipelineTelemetry.RecordStageAsync(
                "ConceptGraphAnalyze",
                v3Sw.ElapsedMilliseconds,
                v3Start,
                DateTime.UtcNow,
                candidateCount: v3Result.Rubrics?.Count,
                finalRubricCount: v3Result.Rubrics?.Count,
                message: $"success={v3Result.Success}; rejected={v3Result.ValidationRejectedCount}; engine={v3Result.EngineVersion}",
                cancellationToken: cancellationToken);

            session.ConceptGraphEngineVersion = v3Result.EngineVersion ?? session.ConceptGraphEngineVersion;
            if (v3Result.Success && v3Result.TranscriptCoverageScore.HasValue)
            {
                session.TranscriptCoverageScore = v3Result.TranscriptCoverageScore;
                session.CaseCompletenessScore = v3Result.CaseCompletenessScore;
                session.RecallEngineVersion = v3Result.EngineVersion;
            }

            if (v3Result.Success && v3Result.Graph.HomeopathicConcepts.Count > 0)
                PersistV3ConceptsToSession(session, v3Result);

            // Task 1: StrictConceptGatedDiscovery — exclusive only when repertory recall is adequate.
            if (_intelligenceOptions.StrictConceptGatedDiscovery)
            {
                var preliminary = RubricResultMerger.SelectDiscoveryPath(
                    enableV3ConceptGraph: true,
                    strictConceptGatedDiscovery: true,
                    conceptGraphMinConfidence: _intelligenceOptions.ConceptGraphMinConfidence,
                    homeopathicConcepts: v3Result.Graph.HomeopathicConcepts,
                    conceptGraphRubrics: v3Result.Rubrics,
                    v1Rubrics: Array.Empty<AudioCaseSuggestedRubricModel>(),
                    v2Rubrics: Array.Empty<AudioCaseSuggestedRubricModel>(),
                    maxResults: 20,
                    minRepertoryCandidatesForExclusivePath: _intelligenceOptions.ConceptGraphMinCandidatesForExclusivePath);

                if (preliminary.Path == DiscoveryPathKind.ConceptGraphOnly)
                {
                    await LogDiscoveryPathSelectionAsync(
                        sessionId, correlationId, preliminary, cancellationToken);

                    var gatedRubrics = preliminary.Rubrics.ToList();

                    // ConceptGraphOnly skips V2 orchestrator — supplement with per-concept keyword discovery
                    // so SRP/mental/particular concepts (thirst, salt, sleep-talk, etc.) still get DB hits.
                    if (_intelligenceOptions.EnablePerConceptKeywordDiscovery)
                    {
                        var graphConcepts = ConceptGraphConceptMapper.FromGraph(v3Result.Graph);
                        var sessionConcepts = DeserializeClinicalConcepts(session.ClinicalConceptsJson);
                        var conceptsForKeyword = ConceptGraphConceptMapper.MergePreferringSearchTerms(
                            sessionConcepts, graphConcepts);

                        if (conceptsForKeyword.Count == 0)
                            conceptsForKeyword = graphConcepts;

                        var keywordStart = DateTime.UtcNow;
                        var keywordSw = Stopwatch.StartNew();
                        var keywordBatch = await _conceptKeywordDiscoveryEngine.DiscoverAsync(
                            sessionId,
                            correlationId,
                            conceptsForKeyword,
                            gatedRubrics,
                            cancellationToken);
                        keywordSw.Stop();
                        _pipelineTelemetry.IncrementSqlQueries(Math.Max(1, keywordBatch.Traces.Count));
                        await _pipelineTelemetry.RecordStageAsync(
                            "PerConceptKeywordDiscovery",
                            keywordSw.ElapsedMilliseconds,
                            keywordStart,
                            DateTime.UtcNow,
                            candidateCount: keywordBatch.Traces.Sum(t => t.RawCandidateCount),
                            finalRubricCount: keywordBatch.Rubrics.Count,
                            message: $"concepts={keywordBatch.Traces.Count}; withHits={keywordBatch.Traces.Count(t => t.KeptCandidateCount > 0)}",
                            cancellationToken: cancellationToken);

                        gatedRubrics = RubricResultMerger.Merge(
                            gatedRubrics, keywordBatch.Rubrics, maxResults: 40);

                        await _intelligenceRepository.SaveIntelligenceLogAsync(
                            sessionId,
                            correlationId,
                            stageName: "PerConceptKeywordDiscovery",
                            status: "Success",
                            message: $"ConceptGraphOnly supplement: attempted={keywordBatch.Traces.Count}, " +
                                     $"withHits={keywordBatch.Traces.Count(t => t.KeptCandidateCount > 0)}, " +
                                     $"mergedRubrics={gatedRubrics.Count}",
                            detailsJson: JsonSerializer.Serialize(new
                            {
                                path = "ConceptGraphOnlySupplement",
                                traces = keywordBatch.Traces.Select(t => new
                                {
                                    t.ConceptText,
                                    t.Category,
                                    t.IsSrp,
                                    t.SearchAttempted,
                                    t.RawCandidateCount,
                                    t.KeptCandidateCount,
                                    t.Outcome,
                                    t.TopRubricName,
                                    t.Error,
                                }),
                            }, JsonOptions),
                            latencyMs: keywordBatch.Traces.Sum(t => t.LatencyMs),
                            cancellationToken);
                    }

                    ApplyRubricMetadata(
                        gatedRubrics,
                        v3Result.EngineVersion ?? "v3-gated",
                        _intelligenceSettings.RequiresManualApproval);
                    var finalizeStart = DateTime.UtcNow;
                    var finalizeSw = Stopwatch.StartNew();
                    var gatedFinal = await FinalizeRubricsForResponseAsync(
                        sessionId, gatedRubrics, new List<CausationLinkModel>(), v3Result.Graph, cancellationToken);
                    finalizeSw.Stop();
                    await _pipelineTelemetry.RecordStageAsync(
                        "FinalizeRubricsForResponse",
                        finalizeSw.ElapsedMilliseconds,
                        finalizeStart,
                        DateTime.UtcNow,
                        finalRubricCount: gatedFinal.Count,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation(
                        "StrictConceptGatedDiscovery ConceptGraphOnly for session {SessionId}: concepts={Concepts}, maxConfidence={MaxConfidence}, repertory={Repertory}, rubrics={Count}",
                        sessionId,
                        preliminary.UsableConceptCount,
                        preliminary.MaxConceptConfidence,
                        preliminary.RepertoryCandidateCount,
                        gatedFinal.Count);

                    return gatedFinal;
                }

                await LogDiscoveryPathSelectionAsync(
                    sessionId, correlationId, preliminary, cancellationToken);

                _logger.LogWarning(
                    "StrictConceptGatedDiscovery falling back to legacy V1+V2 for session {SessionId}: {Reason}",
                    sessionId,
                    preliminary.Reason);

                v3SupplementRubrics = v3Result.Rubrics
                    .Where(RubricResultMerger.IsAuthoritativeRepertoryCandidate)
                    .ToList();
            }
            else if (!_intelligenceOptions.EnableV3ShadowMode
                && v3Result.Success
                && v3Result.Rubrics.Count > 0)
            {
                ApplyRubricMetadata(v3Result.Rubrics, v3Result.EngineVersion ?? "v4.0", _intelligenceSettings.RequiresManualApproval);
                var v3Final = await FinalizeRubricsForResponseAsync(
                    sessionId, v3Result.Rubrics, new List<CausationLinkModel>(), cancellationToken);

                _logger.LogInformation(
                    "V3 concept graph rubrics for session {SessionId}: accepted={Count}, rejected={Rejected}, engine={Engine}",
                    sessionId, v3Final.Count, v3Result.ValidationRejectedCount, v3Result.EngineVersion);

                return v3Final;
            }
            else
            {
                _logger.LogWarning(
                    "V3 pipeline returned no rubrics for session {SessionId}; falling back to V2. shadow={Shadow}, success={Success}, v3Rubrics={V3Count}, meanings={Meanings}, stages={Stages}",
                    sessionId,
                    _intelligenceOptions.EnableV3ShadowMode,
                    v3Result.Success,
                    v3Result.Rubrics.Count,
                    v3Result.Graph.Meanings.Count,
                    string.Join(',', v3Result.StagesCompleted));
            }
        }

        var v2Result = await _intelligenceOrchestrator.AnalyzeAsync(
            session,
            transcript,
            symptoms,
            summary,
            correlationId,
            patientContext,
            cancellationToken);

        PersistConceptsToSession(session, v2Result);

        var symptomsForMatch = v2Result.EnhancedSymptoms.Count > 0 ? v2Result.EnhancedSymptoms : symptoms;
        var v1Rubrics = await MatchRubricsV1Async(sessionId, correlationId, symptomsForMatch, summary, persistMatchLogs: false, cancellationToken);
        var merged = RubricResultMerger.Merge(v1Rubrics, v2Result.Rubrics);
        if (v3SupplementRubrics.Count > 0)
            merged = RubricResultMerger.Merge(v3SupplementRubrics, merged);

        foreach (var rubric in merged)
        {
            await _weightEngine.ApplyWeightToRubricAsync(rubric, v2Result.Concepts, cancellationToken);
        }

        if (_intelligenceOptions.RequiresStrictValidation)
        {
            var postMergeValidation = _clinicalValidationEngine.ValidateAndFilter(
                merged,
                new ClinicalValidationContext
                {
                    Patient = patientContext,
                    Transcript = transcript,
                    Concepts = v2Result.Concepts,
                    CausationLinks = v2Result.CausationLinks,
                    Symptoms = symptomsForMatch,
                    Summary = summary,
                    PrimarySymptom = v2Result.PrimarySymptom,
                });

            merged = _intelligenceOptions.EnableEnterpriseClinicalValidation
                ? postMergeValidation.AcceptedRubrics
                : RubricReviewFallbackHelper.ApplyIfEmpty(
                    postMergeValidation.AcceptedRubrics,
                    postMergeValidation.RejectedRubrics,
                    _intelligenceOptions,
                    20);

            if (merged.Count == 0 && _intelligenceOptions.EnableEnterpriseRubricReviewFallback
                && postMergeValidation.RejectedRubrics.Count > 0)
            {
                merged = RubricReviewFallbackHelper.ApplyIfEmpty(
                    merged,
                    postMergeValidation.RejectedRubrics,
                    _intelligenceOptions,
                    20);
            }
        }

        merged = merged
            .OrderByDescending(r => r.ConfidenceScore ?? r.MatchScore)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        var requiresManualApproval = v2Result.RequiresManualApproval || _intelligenceSettings.RequiresManualApproval;
        ApplyRubricMetadata(merged, v2Result.EngineVersion, requiresManualApproval);
        var mergedFinal = await FinalizeRubricsForResponseAsync(
            sessionId, merged, v2Result.CausationLinks, cancellationToken);

        _logger.LogInformation(
            "Rubric intelligence merge for session {SessionId}: concepts={ConceptCount}, enhancedSymptoms={SymptomCount}, v1Rubrics={V1Count}, merged={MergedCount}, manualApproval={ManualApproval}",
            sessionId,
            v2Result.Concepts.Count,
            v2Result.EnhancedSymptoms.Count,
            v1Rubrics.Count,
            mergedFinal.Count,
            requiresManualApproval);

        return mergedFinal;
    }

    /// <summary>
    /// Stage C fast path: parallel V1 + Keyword/FTS. Skips V3/V7/Enterprise/EnsureAll.
    /// </summary>
    private async Task<List<AudioCaseSuggestedRubricModel>> MatchRubricsFastClinicalAsync(
        AudioCaseSession session,
        string correlationId,
        string transcript,
        List<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken)
    {
        var sessionId = session.AudioCaseSessionId;
        var patientContext = await LoadPatientClinicalContextAsync(session.PatientId, cancellationToken);
        var messages = string.IsNullOrWhiteSpace(session.ConversationJson)
            ? new List<AudioCaseMessageModel>()
            : JsonSerializer.Deserialize<List<AudioCaseMessageModel>>(session.ConversationJson, JsonOptions)
              ?? new List<AudioCaseMessageModel>();

        var fast = await _fastClinicalRetrieval.DiscoverAsync(
            sessionId,
            correlationId,
            symptoms,
            summary,
            session.DetectedLanguage,
            ct => MatchRubricsV1Async(sessionId, correlationId, symptoms, summary, persistMatchLogs: false, ct),
            cancellationToken,
            new FastClinicalRetrievalContext
            {
                Patient = patientContext,
                Messages = messages,
                Transcript = transcript,
            });

        session.IntelligenceEngineVersion = fast.EngineVersion;
        session.ConceptGraphEngineVersion = fast.EngineVersion;
        session.RecallEngineVersion = fast.EngineVersion;
        _pipelineTelemetry.SetEngineVersion(fast.EngineVersion);
        session.ClinicalConceptsJson = JsonSerializer.Serialize(fast.Concepts, JsonOptions);

        var rubrics = fast.Rubrics;
        if (_intelligenceOptions.RequiresStrictValidation && rubrics.Count > 0)
        {
            var validation = _clinicalValidationEngine.ValidateAndFilter(
                rubrics,
                new ClinicalValidationContext
                {
                    Patient = patientContext,
                    Transcript = transcript,
                    Concepts = fast.Concepts,
                    CausationLinks = new List<CausationLinkModel>(),
                    Symptoms = symptoms,
                    Summary = summary,
                });

            rubrics = _intelligenceOptions.EnableEnterpriseClinicalValidation
                ? validation.AcceptedRubrics
                : RubricReviewFallbackHelper.ApplyIfEmpty(
                    validation.AcceptedRubrics,
                    validation.RejectedRubrics,
                    _intelligenceOptions,
                    _intelligenceOptions.FastPipelineMaxFinalRubrics);

            if (rubrics.Count == 0 && _intelligenceOptions.EnableEnterpriseRubricReviewFallback
                && validation.RejectedRubrics.Count > 0)
            {
                rubrics = RubricReviewFallbackHelper.ApplyIfEmpty(
                    rubrics,
                    validation.RejectedRubrics,
                    _intelligenceOptions,
                    _intelligenceOptions.FastPipelineMaxFinalRubrics);
            }
        }

        var maxFinal = Math.Clamp(_intelligenceOptions.FastPipelineMaxFinalRubrics, 5, 20);
        rubrics = rubrics
            .Where(r => r.SubSectionId > 0)
            .OrderByDescending(r => r.ConfidenceScore ?? r.MatchScore)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .Take(maxFinal)
            .ToList();

        ApplyRubricMetadata(rubrics, fast.EngineVersion, _intelligenceSettings.RequiresManualApproval);
        var finalized = await FinalizeRubricsForResponseAsync(
            sessionId,
            rubrics,
            new List<CausationLinkModel>(),
            v3Graph: null,
            cancellationToken,
            gateConcepts: fast.Concepts);

        _logger.LogInformation(
            "FastClinicalRetrieval finalized session {SessionId}: in={In} out={Out} engine={Engine} discoverMs={Ms}",
            sessionId,
            rubrics.Count,
            finalized.Count,
            fast.EngineVersion,
            fast.LatencyMs);

        return finalized;
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> FinalizeRubricsForResponseAsync(
        Guid sessionId,
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<CausationLinkModel>? causationLinks,
        ConceptGraphFullModel? v3Graph,
        CancellationToken cancellationToken,
        IReadOnlyList<ClinicalConceptModel>? gateConcepts = null)
    {
        var reconciled = await _aiRubricReconciler.ReconcileAsync(
            rubrics,
            _intelligenceOptions.AiReconciliationMinConfidence,
            _intelligenceOptions.AiReconciliationFuzzyMinConfidence,
            cancellationToken);

        // Shared correctness gate (Bugs A–D) — applies after every discovery path.
        var conceptsForGate = gateConcepts is { Count: > 0 }
            ? gateConcepts.ToList()
            : v3Graph != null
                ? ConceptGraphConceptMapper.FromGraph(v3Graph)
                : new List<ClinicalConceptModel>();
        var gated = RubricCandidateQualityGate.Apply(reconciled, conceptsForGate);

        var unified = RubricUnifiedContractHelper.ApplyUnifiedContract(gated);

        if (_intelligenceOptions.EnforceEvidenceChainCompleteGate
            && unified.Any(r => r.EvidenceChainComplete == true || r.EvidenceChain != null))
        {
            var completeOnly = unified.Where(r => r.EvidenceChainComplete == true).ToList();
            if (completeOnly.Count > 0)
                unified = RubricUnifiedContractHelper.ApplyUnifiedContract(completeOnly);
        }

        unified = await ApplyLiveSubSectionMasterAsync(unified, cancellationToken);

        await PersistRubricMatchLogsAsync(sessionId, unified, causationLinks, cancellationToken);

        unified = RubricModalityVariantGrouper.GroupForDisplay(unified);

        unified = unified
            .OrderByDescending(r => r.ConfidenceScore ?? r.MatchScore)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        if (_intelligenceSettings.IsV3Active)
        {
            var saveSw = System.Diagnostics.Stopwatch.StartNew();
            await _conceptGraphRepository.SaveDisplayedRubricsAsync(
                sessionId, unified, v3Graph, cancellationToken);
            saveSw.Stop();
            _logger.LogInformation(
                "LatencyGap3 Session {SessionId}: SaveDisplayedRubricsAsync elapsedMs={Ms} rubrics={Count} (PersistGraphAsync is deferred fire-and-forget when DeferGraphPersistence=true)",
                sessionId,
                saveSw.ElapsedMilliseconds,
                unified.Count);
            await _intelligenceRepository.SaveIntelligenceLogAsync(
                sessionId,
                sessionId.ToString("N")[..12],
                "LatencyGap3_SaveDisplayedRubrics",
                "Success",
                $"elapsedMs={saveSw.ElapsedMilliseconds}; rubricCount={unified.Count}",
                detailsJson: null,
                (int)saveSw.ElapsedMilliseconds,
                cancellationToken);
        }

        return unified;
    }

    private Task<List<AudioCaseSuggestedRubricModel>> FinalizeRubricsForResponseAsync(
        Guid sessionId,
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<CausationLinkModel>? causationLinks,
        CancellationToken cancellationToken) =>
        FinalizeRubricsForResponseAsync(sessionId, rubrics, causationLinks, null, cancellationToken);

    private async Task<DualLanguageMeaningContext?> TryBuildDualLanguageContextAsync(
        AudioCaseSession session,
        string correlationId,
        string englishTranscript,
        AudioCaseExtractionModel extraction,
        CancellationToken cancellationToken)
    {
        var sensationSymptoms = extraction.Symptoms
            .Where(s => s.IsSensationBearing && !string.IsNullOrWhiteSpace(s.Phrase))
            .ToList();

        if (sensationSymptoms.Count == 0)
            return null;

        var language = NormalizeSourceLanguage(
            extraction.DetectedLanguage ?? session.DetectedLanguage ?? session.LanguageOverride);
        if (language is "en" or null)
        {
            // Still pass English-only hints so meaning engine can mark sensation nodes.
            return new DualLanguageMeaningContext
            {
                EnglishTranscript = englishTranscript,
                LanguageCode = language ?? "en",
                SensationHints = sensationSymptoms.Select(s => new DualLanguageSensationHint
                {
                    EnglishPhrase = s.Phrase,
                    LanguageCode = language ?? "en",
                }).ToList(),
            };
        }

        // Cost control: at most ONE extra source-language Whisper call per case.
        if (string.IsNullOrWhiteSpace(session.AudioFilePath) || !File.Exists(session.AudioFilePath))
        {
            _logger.LogWarning(
                "DualLanguageForSensationSegments skipped — audio path missing for session {SessionId}",
                session.AudioCaseSessionId);
            return new DualLanguageMeaningContext
            {
                EnglishTranscript = englishTranscript,
                LanguageCode = language,
                SensationHints = sensationSymptoms.Select(s => new DualLanguageSensationHint
                {
                    EnglishPhrase = s.Phrase,
                    LanguageCode = language,
                }).ToList(),
            };
        }

        var transcription = await _aiProcessor.TranscribeAsync(session.AudioFilePath, language, cancellationToken);
        await LogAiRequestAsync(
            session.AudioCaseSessionId,
            correlationId,
            "OpenAI",
            "DualLanguageTranscription",
            "whisper-1",
            transcription.LatencyMs,
            transcription.RequestJson,
            transcription.ResponseJson,
            transcription.Success,
            transcription.Error);

        var original = transcription.Success ? transcription.Transcript : null;
        foreach (var symptom in sensationSymptoms)
        {
            symptom.LanguageCode = language;
            // Best-effort: keep English phrase; original transcript is passed as whole for M1.
            symptom.OriginalLanguageText = original;
        }

        session.ExtractedSymptomsJson = JsonSerializer.Serialize(extraction.Symptoms, JsonOptions);

        _logger.LogInformation(
            "DualLanguageForSensationSegments session {SessionId}: language={Lang}, sensationCount={Count}, originalCaptured={HasOriginal}",
            session.AudioCaseSessionId,
            language,
            sensationSymptoms.Count,
            !string.IsNullOrWhiteSpace(original));

        return new DualLanguageMeaningContext
        {
            EnglishTranscript = englishTranscript,
            OriginalLanguageTranscript = original,
            LanguageCode = language,
            SensationHints = sensationSymptoms.Select(s => new DualLanguageSensationHint
            {
                EnglishPhrase = s.Phrase,
                OriginalLanguageText = s.OriginalLanguageText,
                LanguageCode = language,
            }).ToList(),
        };
    }

    private static string? NormalizeSourceLanguage(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var value = code.Trim().ToLowerInvariant();
        return value switch
        {
            "marathi" or "mr" => "mr",
            "hindi" or "hi" => "hi",
            "english" or "en" => "en",
            _ => value.Length <= 10 ? value : null,
        };
    }

    private async Task LogDiscoveryPathSelectionAsync(
        Guid sessionId,
        string correlationId,
        DiscoveryPathSelectionResult selection,
        CancellationToken cancellationToken)
    {
        var detailsJson = JsonSerializer.Serialize(new
        {
            path = selection.Path.ToString(),
            usableConceptCount = selection.UsableConceptCount,
            maxConceptConfidence = selection.MaxConceptConfidence,
            repertoryCandidateCount = selection.RepertoryCandidateCount,
            rubricCount = selection.Rubrics.Count,
            reason = selection.Reason,
            conceptGraphMinConfidence = _intelligenceOptions.ConceptGraphMinConfidence,
            conceptGraphMinCandidatesForExclusivePath = _intelligenceOptions.ConceptGraphMinCandidatesForExclusivePath,
            strictConceptGatedDiscovery = _intelligenceOptions.StrictConceptGatedDiscovery,
        }, JsonOptions);

        await _intelligenceRepository.SaveIntelligenceLogAsync(
            sessionId,
            correlationId,
            stageName: "DiscoveryPathSelection",
            status: "Success",
            message: selection.Reason,
            detailsJson: detailsJson,
            latencyMs: null,
            cancellationToken);
    }

    private static List<ClinicalConceptModel> DeserializeClinicalConcepts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<ClinicalConceptModel>();

        try
        {
            return JsonSerializer.Deserialize<List<ClinicalConceptModel>>(json, JsonOptions)
                ?? new List<ClinicalConceptModel>();
        }
        catch (JsonException)
        {
            return new List<ClinicalConceptModel>();
        }
    }

    private static void PersistConceptsToSession(AudioCaseSession session, RubricIntelligenceAnalysisResult v2Result)
    {
        if (v2Result.Concepts.Count == 0 && v2Result.CausationLinks.Count == 0) return;

        if (v2Result.Concepts.Count > 0)
        {
            session.ClinicalConceptsJson = JsonSerializer.Serialize(v2Result.Concepts, JsonOptions);
        }

        if (v2Result.CausationLinks.Count > 0)
        {
            session.CausationLinksJson = JsonSerializer.Serialize(v2Result.CausationLinks, JsonOptions);
        }

        session.IntelligenceEngineVersion = v2Result.EngineVersion;
        session.ChangedDate = DateTime.UtcNow;
    }

    private static void PersistV3ConceptsToSession(AudioCaseSession session, ConceptGraphAnalysisResult v3Result)
    {
        var concepts = new List<ClinicalConceptModel>();
        var order = 1;

        foreach (var homeo in v3Result.Graph.HomeopathicConcepts)
        {
            var clinical = v3Result.Graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? v3Result.Graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            if (clinical == null) continue;

            var meaning = v3Result.Graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                ?? v3Result.Graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId);

            concepts.Add(new ClinicalConceptModel
            {
                ConceptId = Guid.NewGuid(),
                SequenceOrder = order++,
                RawStatement = meaning?.RawStatement ?? clinical.ConceptName,
                ClinicalMeaning = clinical.ConceptName,
                HomeopathicMeaning = homeo.ConceptName,
                Category = homeo.Category ?? clinical.SymptomCategory ?? clinical.Domain,
                IsSRP = homeo.IsSRP,
                Confidence = homeo.Confidence,
                HomeopathicWeight = homeo.Weight,
                ConceptTier = homeo.ConceptTier ?? clinical.ConceptTier,
            });
        }

        if (concepts.Count == 0)
        {
            concepts = v3Result.Graph.ClinicalConcepts
                .Select((c, i) => new ClinicalConceptModel
                {
                    ConceptId = Guid.NewGuid(),
                    SequenceOrder = i + 1,
                    RawStatement = v3Result.Graph.Meanings
                        .FirstOrDefault(m => m.PatientMeaningId == c.PatientMeaningId)?.RawStatement ?? c.ConceptName,
                    ClinicalMeaning = c.ConceptName,
                    HomeopathicMeaning = c.ConceptName,
                    Category = c.SymptomCategory ?? c.Domain,
                    Confidence = c.Confidence,
                    ConceptTier = c.ConceptTier,
                })
                .ToList();
        }

        if (concepts.Count == 0) return;

        session.ClinicalConceptsJson = JsonSerializer.Serialize(concepts, JsonOptions);
        session.IntelligenceEngineVersion = v3Result.EngineVersion ?? "v5";
        session.ChangedDate = DateTime.UtcNow;
    }

    public async Task<(bool Success, string Message, AudioCaseConceptsModel? Result)> GetConceptsAsync(
        int doctorUserId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOwnedSessionAsync(doctorUserId, sessionId, cancellationToken);
        if (session == null) return (false, "Session not found.", null);

        List<ClinicalConceptModel> concepts;
        if (!string.IsNullOrWhiteSpace(session.ClinicalConceptsJson))
        {
            concepts = JsonSerializer.Deserialize<List<ClinicalConceptModel>>(session.ClinicalConceptsJson, JsonOptions)
                ?? new List<ClinicalConceptModel>();
        }
        else
        {
            concepts = await _intelligenceRepository.GetConceptsAsync(sessionId, cancellationToken);
        }

        List<CausationLinkModel> causationLinks;
        if (!string.IsNullOrWhiteSpace(session.CausationLinksJson))
        {
            causationLinks = JsonSerializer.Deserialize<List<CausationLinkModel>>(session.CausationLinksJson, JsonOptions)
                ?? new List<CausationLinkModel>();
        }
        else
        {
            causationLinks = await _intelligenceRepository.GetCausationLinksAsync(sessionId, cancellationToken);
        }

        return (true, "Concepts fetched successfully.", new AudioCaseConceptsModel
        {
            SessionId = sessionId,
            EngineVersion = session.IntelligenceEngineVersion ?? (_intelligenceSettings.IsV2Active ? "v2" : "v1"),
            Concepts = concepts.OrderBy(c => c.SequenceOrder).ThenBy(c => c.Confidence, Comparer<decimal>.Create((a, b) => b.CompareTo(a))).ToList(),
            CausationLinks = causationLinks.OrderBy(l => l.SequenceOrder).ToList(),
            PrimaryConcepts = BuildTieredConcepts(concepts, ConceptTierLabels.Primary),
            SecondaryConcepts = BuildTieredConcepts(concepts, ConceptTierLabels.Secondary),
            SupportingConcepts = BuildTieredConcepts(concepts, ConceptTierLabels.Supporting),
        });
    }

    private static List<TieredConceptNodeModel> BuildTieredConcepts(
        IReadOnlyList<ClinicalConceptModel> concepts,
        string tier)
    {
        return concepts
            .Where(c => string.Equals(c.ConceptTier, tier, StringComparison.OrdinalIgnoreCase))
            .Select(c => new TieredConceptNodeModel
            {
                ConceptTier = tier,
                Category = c.Category ?? "General",
                ClinicalConceptName = c.ClinicalMeaning ?? string.Empty,
                HomeopathicConceptName = c.HomeopathicMeaning ?? string.Empty,
                Confidence = c.Confidence,
                Weight = c.HomeopathicWeight,
                IsSRP = c.IsSRP,
                EvidenceSpan = c.RawStatement,
            })
            .ToList();
    }

    private static void ApplyRubricMetadata(
        List<AudioCaseSuggestedRubricModel> rubrics,
        string engineVersion,
        bool requiresManualApproval)
    {
        foreach (var rubric in rubrics)
        {
            rubric.EngineVersion = engineVersion;
            rubric.RequiresManualApproval = requiresManualApproval;
        }
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> MatchRubricsV1Async(
        Guid sessionId,
        string correlationId,
        List<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary,
        bool persistMatchLogs = true,
        CancellationToken cancellationToken = default)
    {
        var candidates = new Dictionary<int, (AudioCaseSuggestedRubricModel Model, decimal Keyword, decimal Semantic)>();

        foreach (var symptom in symptoms.Where(s => !string.IsNullOrWhiteSpace(s.Phrase)).Take(12))
        {
            var conceptProxy = new ClinicalConceptModel
            {
                RawStatement = symptom.Phrase,
                ClinicalMeaning = symptom.Phrase,
                Category = symptom.Category,
                SearchTerms = symptom.SearchTerms?.ToList() ?? new List<string>(),
            };
            var domain = ConceptSearchTermBuilder.ResolveDomain(conceptProxy);

            foreach (var searchTerm in BuildSearchTerms(symptom))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var search = await _subSectionRepository.SearchSubSectionsByHotspotAsync(new SearchSubSectionByHotspotRequest
                {
                    HotspotName = searchTerm,
                    PageNumber = 1,
                    PageSize = 8,
                });

                foreach (var item in search.Items)
                {
                    var name = item.SubSectionName ?? string.Empty;
                    var domainScore = ConceptSearchTermBuilder.ScoreCandidate(
                        conceptProxy, domain, searchTerm, name);
                    if (domainScore < 0.35m)
                        continue;

                    var keywordScore = Math.Max(0.45m, domainScore);
                    var semanticScore = _options.EnableSemanticRubricMatch
                        ? AudioCaseAiProcessor.ComputeTextSimilarity(symptom.Phrase, name)
                        : 0m;
                    var finalScore = _options.EnableSemanticRubricMatch
                        ? (keywordScore * 0.6m) + (semanticScore * 0.4m)
                        : keywordScore;

                    if (candidates.TryGetValue(item.SubSectionId, out var existing))
                    {
                        if (finalScore > existing.Model.MatchScore)
                        {
                            candidates[item.SubSectionId] = (BuildRubric(item, symptom, finalScore), keywordScore, semanticScore);
                        }
                    }
                    else
                    {
                        candidates[item.SubSectionId] = (BuildRubric(item, symptom, finalScore), keywordScore, semanticScore);
                    }
                }
            }
        }

        var dbTop = candidates.Values
            .Select(x => x.Model)
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.SubSectionName)
            .Take(20)
            .ToList();

        var combined = new List<AudioCaseSuggestedRubricModel>(dbTop);

        if (AudioCaseFastPathGuards.ShouldSuggestAiRubrics(
                _options.EnableAiSuggestedRubrics,
                _intelligenceOptions.EnableFastClinicalRetrievalPipeline,
                dbTop.Count,
                _options.MaxAiSuggestedRubrics))
        {
            var aiSlots = Math.Min(_options.MaxAiSuggestedRubrics, Math.Max(0, AudioCaseFastPathGuards.V1CombinedCap - dbTop.Count));
            if (dbTop.Count == 0)
            {
                aiSlots = _options.MaxAiSuggestedRubrics;
            }

            if (aiSlots > 0)
            {
                var aiResult = await _aiProcessor.SuggestAiRubricsAsync(
                    symptoms,
                    summary,
                    dbTop.Select(r => r.SubSectionName).ToList(),
                    aiSlots,
                    cancellationToken);

                await LogAiRequestAsync(sessionId, correlationId, "OpenAI", "AiRubricSuggestion", "gpt-4o",
                    aiResult.LatencyMs, aiResult.RequestJson, aiResult.ResponseJson, aiResult.Success, aiResult.Error,
                    aiResult.PromptTokens, aiResult.CompletionTokens);

                if (aiResult.Success && aiResult.Rubrics.Count > 0)
                {
                    combined.AddRange(aiResult.Rubrics);
                }
            }
        }

        var top = combined.Take(20).ToList();

        if (persistMatchLogs)
        {
            await PersistRubricMatchLogsAsync(sessionId, top, null, cancellationToken);
        }

        return top;
    }

    private async Task PersistRubricMatchLogsAsync(
        Guid sessionId,
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<CausationLinkModel>? causationLinks,
        CancellationToken cancellationToken)
    {
        var oldLogs = await _context.AudioCaseRubricMatchLogs
            .Where(x => x.AudioCaseSessionId == sessionId)
            .ToListAsync(cancellationToken);
        if (oldLogs.Count > 0)
        {
            _context.AudioCaseRubricMatchLogs.RemoveRange(oldLogs);
        }

        for (var i = 0; i < rubrics.Count; i++)
        {
            var rubric = rubrics[i];
            var explainability = rubric.Explainability;
            var causationJson = explainability?.CausationChain.Count > 0
                ? JsonSerializer.Serialize(explainability.CausationChain, JsonOptions)
                : null;

            _context.AudioCaseRubricMatchLogs.Add(new AudioCaseRubricMatchLog
            {
                AudioCaseSessionId = sessionId,
                SymptomPhrase = rubric.MatchedFrom ?? explainability?.PatientStatement ?? string.Empty,
                SubSectionId = rubric.SubSectionId,
                SubSectionName = rubric.SubSectionName,
                KeywordScore = rubric.IsAiSuggested ? null : rubric.MatchScore,
                SemanticScore = rubric.ConfidenceScore,
                FinalScore = rubric.MatchScore,
                RankPosition = i + 1,
                IsSelectedForUi = true,
                SuggestedIntensityNo = rubric.SuggestedIntensityNo,
                MatchSource = rubric.MatchSource ?? (rubric.IsAiSuggested ? "AiGenerated" : "Combined"),
                ClinicalMeaning = explainability?.ClinicalMeaning,
                HomeopathicMeaning = explainability?.HomeopathicMeaning,
                WhySuggested = rubric.WhySuggested,
                ConfidenceScore = rubric.ConfidenceScore ?? rubric.MatchScore,
                HomeopathicWeight = rubric.HomeopathicWeight,
                RubricTier = rubric.RubricTier ?? explainability?.RubricTier,
                MatchLayer = rubric.MatchLayer ?? explainability?.MatchLayer,
                CausationJson = causationJson,
                ExplainabilityJson = explainability != null
                    ? JsonSerializer.Serialize(explainability, JsonOptions)
                    : null,
                UnifiedSource = rubric.Source ?? (rubric.IsAiSuggested ? "AiSuggested" : "Database"),
                FinalHybridScore = rubric.Scores?.FinalHybridScore ?? rubric.ConfidenceScore ?? rubric.MatchScore,
                EvidenceChainComplete = rubric.EvidenceChainComplete,
                GroundedInOntology = rubric.GroundedInOntology,
                EnteredDate = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<string> BuildSearchTerms(AudioCaseSymptomModel symptom)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(symptom.Phrase))
        {
            terms.Add(symptom.Phrase.Trim());
            foreach (var word in symptom.Phrase.Split(new[] { ' ', '-', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length >= 3)
                {
                    terms.Add(word.Trim());
                }
            }
        }

        foreach (var term in symptom.SearchTerms ?? [])
        {
            if (!string.IsNullOrWhiteSpace(term))
            {
                terms.Add(term.Trim());
            }
        }

        // Bare "desire"/"desires" pollutes MIND-DESIRES matches for thirst/food concepts.
        terms.Remove("desire");
        terms.Remove("desires");
        terms.Remove("amount");
        terms.Remove("same");
        terms.Remove("thing");

        return terms.Where(t => t.Length >= 3);
    }

    private static AudioCaseSuggestedRubricModel BuildRubric(SubSectionForPageModel item, AudioCaseSymptomModel symptom, decimal score) =>
        new()
        {
            SubSectionId = item.SubSectionId,
            SubSectionName = item.SubSectionName ?? string.Empty,
            MatchScore = Math.Round(score, 4),
            SuggestedIntensityNo = Math.Clamp(symptom.IntensityHint, 1, 4),
            MatchedFrom = symptom.Phrase,
            RemedyCountForSort = 0,
            IsAiSuggested = false,
            MatchSource = "Database",
        };


    private async Task LogAiRequestAsync(Guid sessionId, string? correlationId, string provider, string serviceType,
        string model, int latencyMs, string? requestJson, string? responseJson, bool success, string? error,
        int promptTokens = 0, int completionTokens = 0)
    {
        await _context.AudioCaseAiRequestLogs.AddAsync(new AudioCaseAiRequestLog
        {
            AudioCaseSessionId = sessionId,
            Provider = provider,
            ServiceType = serviceType,
            ModelName = model,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            LatencyMs = latencyMs,
            RequestPayloadJson = requestJson,
            ResponsePayloadJson = responseJson,
            IsSuccess = success,
            ErrorMessage = error,
            CorrelationId = correlationId,
            EnteredDate = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
    }

    private async Task<AudioCaseSession?> GetOwnedSessionAsync(int doctorUserId, Guid sessionId, CancellationToken cancellationToken) =>
        await _context.AudioCaseSessions.AsNoTracking().FirstOrDefaultAsync(
            x => x.AudioCaseSessionId == sessionId && x.DoctorUserId == doctorUserId && !x.DeleteStatus, cancellationToken);

    private async Task UpdateSessionProgressAsync(AudioCaseSession session, string status, string step, int percent)
    {
        session.Status = status;
        session.CurrentStep = step;
        session.ChangedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task LogEventAsync(Guid sessionId, string? correlationId, string eventType, string eventStatus,
        string message, int? userId = null, string? ipAddress = null, object? details = null)
    {
        _context.AudioCaseSessionEventLogs.Add(new AudioCaseSessionEventLog
        {
            AudioCaseSessionId = sessionId,
            EventType = eventType,
            EventStatus = eventStatus,
            Message = message,
            DetailsJson = details == null ? null : JsonSerializer.Serialize(details, JsonOptions),
            CorrelationId = correlationId,
            IpAddress = ipAddress,
            EnteredBy = userId,
            EnteredDate = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
    }

    private static int MapProgressPercent(string status, string? step)
    {
        if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)) return 100;
        if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)) return 0;
        return step switch
        {
            "Uploaded" => 10,
            "Processing" or "ProcessingStarted" => 20,
            "Transcribing" or "TranscriptionStarted" => 40,
            "TranscriptionCompleted" => 55,
            "Extracting" or "LlmExtractionStarted" => 70,
            "LlmExtractionCompleted" => 80,
            "ConceptGraph" or "ConceptGraphPipelineStarted" => 84,
            "CaseDecomposition" => 85,
            "PatientMeaningGraph" => 86,
            "CategoryDiscovery" or "MultiSymptomDiscovery" => 88,
            "MetaphorUnderstanding" => 89,
            "ClinicalConcept" or "HomeopathicConcept" => 90,
            "RecallExpansion" or "ConceptClustering" => 91,
            "MatchingRubrics" or "RubricMatchingStarted" => 92,
            "RubricDiscovery" => 93,
            "FinalizingResults" or "Finalizing" => 97,
            _ => 30,
        };
    }

    private static string MapStageLabel(string status, string? step)
    {
        if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            return "Analysis complete";
        if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
            return "Analysis failed";

        var key = (step ?? status ?? string.Empty).ToLowerInvariant();
        return key switch
        {
            "uploaded" => "Queued for analysis",
            "processing" or "processingstarted" => "Starting analysis",
            "transcribing" or "transcriptionstarted" => "Transcribing audio (usually the longest step)",
            "transcriptioncompleted" => "Transcript ready",
            "extracting" or "llmextractionstarted" => "Extracting clinical symptoms",
            "llmextractioncompleted" => "Symptoms extracted",
            "conceptgraph" or "conceptgraphpipelinestarted" => "Analyzing case concepts",
            "matchingrubrics" or "rubricmatchingstarted" or "rubricdiscovery" => "Matching database rubrics",
            "finalizingresults" or "finalizing" or "finishing" => "Preparing your results",
            "reanalysisrequested" => "Re-analysis queued",
            _ => "Processing audio case",
        };
    }

    private async Task<RubricPipelineTelemetrySummary?> TryLoadProcessingMetricsAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var row = await _context.AudioCaseIntelligenceLogs
                .AsNoTracking()
                .Where(x => x.AudioCaseSessionId == sessionId
                    && x.StageName == RubricPipelineTelemetryConstants.SummaryStageName)
                .OrderByDescending(x => x.EnteredDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (row?.DetailsJson == null)
                return null;

            return JsonSerializer.Deserialize<RubricPipelineTelemetrySummary>(row.DetailsJson, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load processing metrics for session {SessionId}", sessionId);
            return null;
        }
    }

    private static bool HasStoredAudioFile(AudioCaseSession session)
    {
        try
        {
            return session.AudioPurgedAtUtc == null
                && !string.IsNullOrWhiteSpace(session.AudioFilePath)
                && File.Exists(session.AudioFilePath);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static int CountJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return 0;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static string? ExtractChiefComplaintSnippet(string? summaryJson, int maxLength = 140)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
            return null;

        try
        {
            using var document = JsonDocument.Parse(summaryJson);
            var root = document.RootElement;
            string? text = null;
            if (root.TryGetProperty("chiefComplaint", out var chiefComplaint))
                text = chiefComplaint.GetString();
            else if (root.TryGetProperty("ChiefComplaint", out chiefComplaint))
                text = chiefComplaint.GetString();

            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();
            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength).TrimEnd() + "…";
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> ApplyLiveSubSectionMasterAsync(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        CancellationToken cancellationToken)
    {
        if (rubrics.Count == 0)
            return rubrics.ToList();

        var ids = rubrics
            .Where(r => r.SubSectionId > 0)
            .Select(r => r.SubSectionId)
            .Distinct()
            .ToList();

        var live = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            var rows = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => ids.Contains(s.SubSectionId) && !s.DeleteStatus)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            live = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.SubSectionName))
                .GroupBy(x => x.SubSectionId)
                .ToDictionary(g => g.Key, g => g.First().SubSectionName!.Trim());

            _pipelineTelemetry.IncrementSqlQueries();
        }

        var gated = LiveSubSectionMasterGate.Apply(rubrics, live, out var unresolved, out var renamed);
        if (unresolved > 0 || renamed > 0)
        {
            _logger.LogInformation(
                "Live SubSectionMaster check: unresolved={Unresolved} renamed={Renamed} queriedIds={Ids}",
                unresolved,
                renamed,
                ids.Count);
        }

        return gated;
    }

    private AudioCaseResultModel MapResult(AudioCaseSession session)
    {
        var messages = string.IsNullOrWhiteSpace(session.ConversationJson)
            ? new List<AudioCaseMessageModel>()
            : JsonSerializer.Deserialize<List<AudioCaseMessageModel>>(session.ConversationJson, JsonOptions) ?? new();
        var summary = string.IsNullOrWhiteSpace(session.SummaryJson)
            ? null : JsonSerializer.Deserialize<AudioCaseSummaryModel>(session.SummaryJson, JsonOptions);
        var rubrics = string.IsNullOrWhiteSpace(session.SuggestedRubricsJson)
            ? new List<AudioCaseSuggestedRubricModel>()
            : JsonSerializer.Deserialize<List<AudioCaseSuggestedRubricModel>>(session.SuggestedRubricsJson, JsonOptions) ?? new();

        var engineVersion = rubrics.FirstOrDefault()?.EngineVersion
            ?? (_intelligenceSettings.IsV2Active ? "v2" : "v1");
        var requireManualApproval = rubrics.Any(r => r.RequiresManualApproval)
            || _intelligenceSettings.RequiresManualApproval;

        return new AudioCaseResultModel
        {
            SessionId = session.AudioCaseSessionId,
            Transcript = session.TranscriptRaw,
            Messages = messages,
            Summary = summary,
            SuggestedRubrics = rubrics,
            RubricIntelligence = new RubricIntelligenceMetaModel
            {
                EngineVersion = engineVersion,
                RequireManualApprovalForSuggestedRubrics = requireManualApproval,
                V2Enabled = _intelligenceSettings.IsV2Active,
                RollbackToV1Only = _intelligenceSettings.RollbackToV1Only,
            },
        };
    }

    private async Task<PatientClinicalContext?> LoadPatientClinicalContextAsync(
        long patientId,
        CancellationToken cancellationToken)
    {
        if (patientId <= 0) return null;

        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId && p.DeleteStatus != true, cancellationToken);

        if (patient == null) return null;

        int? ageYears = patient.Age;
        if (!ageYears.HasValue && patient.DateOfBirth.HasValue)
        {
            ageYears = (int)Math.Floor((DateTime.UtcNow - patient.DateOfBirth.Value.ToUniversalTime()).TotalDays / 365.25);
        }

        return new PatientClinicalContext
        {
            PatientId = patient.PatientId,
            Gender = patient.Gender,
            AgeYears = ageYears,
            PatientName = patient.PatientName,
        };
    }
}

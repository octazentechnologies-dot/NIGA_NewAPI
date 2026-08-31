using System.Threading.Channels;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces;

public interface IAudioCaseTakingService
{
    Task<(bool Success, string Message, AudioCaseUploadResultModel? Result)> UploadAsync(
        int doctorUserId,
        AudioCaseUploadRequestModel request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, AudioCaseStatusModel? Result)> GetStatusAsync(
        int doctorUserId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, AudioCaseResultModel? Result)> GetResultAsync(
        int doctorUserId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, (byte[] Bytes, string FileName, string ContentType)? File)> DownloadAsync(
        int doctorUserId,
        Guid sessionId,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, AudioCaseUploadResultModel? Result)> ReAnalyzeFromTranscriptAsync(
        int doctorUserId,
        Guid sessionId,
        string transcript,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> LogDoctorActionAsync(
        int doctorUserId,
        Guid sessionId,
        AudioCaseDoctorActionRequestModel request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, AudioCaseLatestSessionModel? Result)> GetLatestSessionAsync(
        int doctorUserId,
        long patientId,
        long? caseId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, AudioCaseSessionListModel? Result)> GetSessionsAsync(
        int doctorUserId,
        long patientId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task ProcessSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task ProcessReAnalyzeAsync(Guid sessionId, string transcript, CancellationToken cancellationToken = default);

    Task<int> PurgeExpiredAudioFilesAsync(CancellationToken cancellationToken = default);

    Task<int> RecoverStaleProcessingSessionsAsync(CancellationToken cancellationToken = default);

    Task<int> RequeueOrphanedUploadedSessionsAsync(CancellationToken cancellationToken = default);

    Task FailSessionFromSystemAsync(
        Guid sessionId,
        string errorCode,
        string message,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, AudioCaseConceptsModel? Result)> GetConceptsAsync(
        int doctorUserId,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

public interface IAudioCaseTakingQueue
{
    ChannelReader<AudioCaseTakingJob> Reader { get; }

    ValueTask EnqueueAsync(AudioCaseTakingJob job, CancellationToken cancellationToken = default);
}

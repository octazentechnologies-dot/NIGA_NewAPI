using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.NewAPI.Controllers;

/// <summary>CLN-08 / CLN-09.01 — existing audio engine (accuracy, aliases, metaphors, benchmark). No new HTTP this sprint.</summary>
[Route("api/AudioCaseTaking")]
[ApiController]
[Authorize]
[DoctorOnly]
public class AudioCaseTakingController : ControllerBase
{
    private readonly IAudioCaseTakingService _audioCaseTakingService;
    private readonly IDoctorFeedbackLearningEngine _feedbackLearningEngine;
    private readonly ILogger<AudioCaseTakingController> _logger;

    private readonly Homeocentrum.Niga.NewAPI.Domain.Security.IPatientAccessGuard _patientAccess;
    private readonly Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext _context;

    public AudioCaseTakingController(
        IAudioCaseTakingService audioCaseTakingService,
        IDoctorFeedbackLearningEngine feedbackLearningEngine,
        ILogger<AudioCaseTakingController> logger,
        Homeocentrum.Niga.NewAPI.Domain.Security.IPatientAccessGuard patientAccess,
        Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext context)
    {
        _audioCaseTakingService = audioCaseTakingService;
        _feedbackLearningEngine = feedbackLearningEngine;
        _logger = logger;
        _patientAccess = patientAccess;
        _context = context;
    }

    /// <summary>Patient, case and appointment ids on an upload must all belong to a patient the caller may access.</summary>
    private async Task<bool> UploadTargetsAllowedAsync(AudioCaseUploadRequestModel request)
    {
        if (request.PatientId > 0 && !await _patientAccess.CanAccessPatientAsync(User, (int)request.PatientId))
            return false;
        if (request.CaseId is > 0)
        {
            var caseRow = await _context.CaseEntryDetails.AsNoTracking()
                .Where(c => c.CaseId == request.CaseId.Value)
                .Select(c => new { c.PatientId, c.DoctorId })
                .FirstOrDefaultAsync();
            if (caseRow == null
                || (request.PatientId > 0 && caseRow.PatientId != request.PatientId)
                || !await _patientAccess.CanAccessPatientAsync(User, caseRow.PatientId, caseRow.DoctorId))
                return false;
        }
        if (request.PatientAppId is > 0)
        {
            var appointment = await _context.PatientAppointments.AsNoTracking()
                .Where(a => a.PatientAppId == request.PatientAppId.Value)
                .Select(a => new { a.PatientId, a.DoctorId })
                .FirstOrDefaultAsync();
            if (appointment == null
                || (request.PatientId > 0 && appointment.PatientId != request.PatientId)
                || !Homeocentrum.Niga.NewAPI.Domain.Security.DoctorOwnership.EnsureDoctorOwns(User, appointment.DoctorId))
                return false;
        }
        return true;
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseUploadResultModel>), StatusCodes.Status200OK)]
    public async Task<object> Upload([FromForm] AudioCaseUploadRequestModel request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }
            if (!await UploadTargetsAllowedAsync(request))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this patient." });
            }

            var userId = User.GetUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            var (success, message, result) = await _audioCaseTakingService.UploadAsync(
                userId,
                request,
                ipAddress,
                userAgent);

            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking upload failed for UserId={UserId}", User.GetUserId());
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpGet("{sessionId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseStatusModel>), StatusCodes.Status200OK)]
    public async Task<object> Status(Guid sessionId)
    {
        try
        {
            var userId = User.GetUserId();
            var (success, message, result) = await _audioCaseTakingService.GetStatusAsync(userId, sessionId);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking status failed for SessionId={SessionId}", sessionId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpGet("{sessionId:guid}/result")]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseResultModel>), StatusCodes.Status200OK)]
    public async Task<object> Result(Guid sessionId)
    {
        try
        {
            var userId = User.GetUserId();
            var (success, message, result) = await _audioCaseTakingService.GetResultAsync(userId, sessionId);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking result failed for SessionId={SessionId}", sessionId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpPost("{sessionId:guid}/reanalyze")]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseUploadResultModel>), StatusCodes.Status200OK)]
    public async Task<object> ReAnalyze(Guid sessionId, [FromBody] AudioCaseReAnalyzeRequestModel request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }

            var userId = User.GetUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, message, result) = await _audioCaseTakingService.ReAnalyzeFromTranscriptAsync(
                userId,
                sessionId,
                request.Transcript,
                ipAddress);

            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking re-analyze failed for SessionId={SessionId}", sessionId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpPost("{sessionId:guid}/doctor-action")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<object> DoctorAction(Guid sessionId, [FromBody] AudioCaseDoctorActionRequestModel request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }

            var userId = User.GetUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, message) = await _audioCaseTakingService.LogDoctorActionAsync(
                userId,
                sessionId,
                request,
                ipAddress);

            if (!success)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(new { sessionId }, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking doctor action failed for SessionId={SessionId}", sessionId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseLatestSessionModel>), StatusCodes.Status200OK)]
    public async Task<object> Latest([FromQuery] long patientId, [FromQuery] long? caseId = null)
    {
        try
        {
            var userId = User.GetUserId();
            var (success, message, result) = await _audioCaseTakingService.GetLatestSessionAsync(
                userId,
                patientId,
                caseId);

            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking latest session failed for PatientId={PatientId}", patientId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseSessionListModel>), StatusCodes.Status200OK)]
    public async Task<object> Sessions(
        [FromQuery] long patientId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = User.GetUserId();
            var (success, message, result) = await _audioCaseTakingService.GetSessionsAsync(
                userId,
                patientId,
                pageNumber,
                pageSize);

            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking sessions failed for PatientId={PatientId}", patientId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpGet("{sessionId:guid}/concepts")]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseConceptsModel>), StatusCodes.Status200OK)]
    public async Task<object> Concepts(Guid sessionId)
    {
        try
        {
            var userId = User.GetUserId();
            var (success, message, result) = await _audioCaseTakingService.GetConceptsAsync(userId, sessionId);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking concepts failed for SessionId={SessionId}", sessionId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpPost("{sessionId:guid}/rubrics/feedback")]
    [ProducesResponseType(typeof(ApiResponse<AudioCaseRubricFeedbackResultModel>), StatusCodes.Status200OK)]
    public async Task<object> SubmitRubricFeedback(Guid sessionId, [FromBody] AudioCaseRubricFeedbackRequestModel request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }

            var userId = User.GetUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, message, result) = await _feedbackLearningEngine.ProcessFeedbackAsync(
                userId,
                sessionId,
                request,
                ipAddress);

            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking rubric feedback failed for SessionId={SessionId}", sessionId);
            return ThreeDBodyPartApiResponseHelper.Error(ex);
        }
    }

    [HttpGet("{sessionId:guid}/download")]
    public async Task<IActionResult> Download(Guid sessionId)
    {
        try
        {
            var userId = User.GetUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (success, message, file) = await _audioCaseTakingService.DownloadAsync(userId, sessionId, ipAddress);
            if (!success || file == null)
            {
                return BadRequest(new { success = false, message });
            }

            return File(file.Value.Bytes, file.Value.ContentType, file.Value.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioCaseTaking download failed for SessionId={SessionId}", sessionId);
            return this.ServerError(ex);
        }
    }

    private string GetValidationMessage()
    {
        var firstError = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstError) ? "Invalid request." : firstError;
    }
}

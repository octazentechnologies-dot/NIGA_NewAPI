using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Interfaces;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers;

/// <summary>
/// Save and restore in-progress Patient Board work. CLN-19.02 / CLN-02.02 —
/// treating doctor only (reception and patient 403). JWT-bound to doctor user id.
/// </summary>
[Route("api/PatientBoardBackup")]
[ApiController]
[Authorize]
[DoctorOnly]
public class PatientBoardBackupController : ControllerBase
{
    private readonly IPatientBoardBackupService _patientBoardBackupService;
    private readonly ILogger<PatientBoardBackupController> _logger;

    public PatientBoardBackupController(
        IPatientBoardBackupService patientBoardBackupService,
        ILogger<PatientBoardBackupController> logger)
    {
        _patientBoardBackupService = patientBoardBackupService;
        _logger = logger;
    }

    [HttpPost("Save")]
    [ProducesResponseType(typeof(ApiResponse<SavePatientBoardBackupResultModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<object> Save([FromBody] SavePatientBoardBackupRequest request)
    {
        try
        {
            // Authorized doctor with a missing payload must be 400, not HTTP 200 { success:false }.
            // Reception never reaches here: DoctorOnly IAuthorizationFilter returns 403 first.
            if (!ModelState.IsValid || request == null || string.IsNullOrWhiteSpace(request.BackupPayload))
            {
                return BadRequest(new { success = false, message = GetValidationMessage() });
            }

            var userId = ResolveDoctorUserId();
            var (success, message, result) = await _patientBoardBackupService.SaveLatestBackupAsync(userId, request);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save PatientBoardBackup failed for UserId={UserId}", User.GetUserId());
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("Summary")]
    [ProducesResponseType(typeof(ApiResponse<PatientBoardBackupSummaryModel>), StatusCodes.Status200OK)]
    public async Task<object> Summary()
    {
        try
        {
            var userId = ResolveDoctorUserId();
            var (success, message, result) = await _patientBoardBackupService.GetBackupSummaryAsync(userId);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Summary PatientBoardBackup failed for UserId={UserId}", User.GetUserId());
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("Latest")]
    [ProducesResponseType(typeof(ApiResponse<PatientBoardBackupDetailModel>), StatusCodes.Status200OK)]
    public async Task<object> Latest()
    {
        try
        {
            var userId = ResolveDoctorUserId();
            var (success, message, result) = await _patientBoardBackupService.GetLatestBackupAsync(userId);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Latest PatientBoardBackup failed for UserId={UserId}", User.GetUserId());
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("Delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<object> Delete()
    {
        try
        {
            var userId = ResolveDoctorUserId();
            var (success, message) = await _patientBoardBackupService.DeleteLatestBackupAsync(userId);
            if (!success)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(new { deleted = true }, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete PatientBoardBackup failed for UserId={UserId}", User.GetUserId());
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    /// <summary>
    /// JWT-bound: doctors use NameIdentifier (UserId). Reception with DoctorID claim still keys
    /// backup by their staff id unless DoctorUserId claim is present — never accept client doctorUserId.
    /// SEC-05.01 satisfied for backup via JWT userId binding (no client doctorUserId accepted).
    /// </summary>
    private int ResolveDoctorUserId()
    {
        var doctorUserClaim = User.FindFirst("DoctorUserID")?.Value
            ?? User.FindFirst("DoctorUserId")?.Value;
        if (int.TryParse(doctorUserClaim, out var doctorUserId) && doctorUserId > 0)
            return doctorUserId;
        return User.GetUserId();
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

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Interfaces;

namespace Niga_Domain.API.Controllers;

/// <summary>
/// Save and restore in-progress Patient Board work for doctors and reception staff.
/// </summary>
[Route("api/PatientBoardBackup")]
[ApiController]
[Authorize]
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
    public async Task<object> Save([FromBody] SavePatientBoardBackupRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }

            var userId = User.GetUserId();
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
            var userId = User.GetUserId();
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
            var userId = User.GetUserId();
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
            var userId = User.GetUserId();
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

    private string GetValidationMessage()
    {
        var firstError = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstError) ? "Invalid request." : firstError;
    }
}

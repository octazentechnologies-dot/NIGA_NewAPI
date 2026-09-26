using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Controllers;

[Route("api/AudioCaseIntelligence/admin")]
[ApiController]
[Authorize]
public class AudioCaseIntelligenceAdminController : ControllerBase
{
    private readonly IRubricIntelligenceAdminService _adminService;
    private readonly ILogger<AudioCaseIntelligenceAdminController> _logger;

    public AudioCaseIntelligenceAdminController(
        IRubricIntelligenceAdminService adminService,
        ILogger<AudioCaseIntelligenceAdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet("weights")]
    public async Task<object> GetWeights([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _adminService.GetWeightsAsync(pageNumber, pageSize);
        return ThreeDBodyPartApiResponseHelper.Success(result, "Weight rules fetched.");
    }

    [HttpGet("weights/{id:int}")]
    public async Task<object> GetWeight(int id)
    {
        var result = await _adminService.GetWeightByIdAsync(id);
        return result == null
            ? ThreeDBodyPartApiResponseHelper.Failure("Weight rule not found.")
            : ThreeDBodyPartApiResponseHelper.Success(result, "Weight rule fetched.");
    }

    [HttpPut("weights/{id:int}")]
    public async Task<object> UpdateWeight(int id, [FromBody] HomeopathicWeightRuleUpsertModel model)
    {
        var (success, message, result) = await _adminService.UpdateWeightAsync(
            User.GetUserId(), id, model, HttpContext.Connection.RemoteIpAddress?.ToString());
        return success && result != null
            ? ThreeDBodyPartApiResponseHelper.Success(result, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpGet("metaphors")]
    public async Task<object> GetMetaphors(
        [FromQuery] string? search,
        [FromQuery] string? language,
        [FromQuery] string? approvalStatus,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _adminService.GetMetaphorsAsync(search, language, approvalStatus, pageNumber, pageSize);
            return ThreeDBodyPartApiResponseHelper.Success(result, "Metaphors fetched.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get metaphors failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("metaphors/{id:long}")]
    public async Task<object> GetMetaphor(long id)
    {
        var result = await _adminService.GetMetaphorByIdAsync(id);
        if (result == null) return ThreeDBodyPartApiResponseHelper.Failure("Metaphor not found.");
        return ThreeDBodyPartApiResponseHelper.Success(result, "Metaphor fetched.");
    }

    [HttpPost("metaphors")]
    public async Task<object> CreateMetaphor([FromBody] RubricMetaphorUpsertModel model)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message, result) = await _adminService.CreateMetaphorAsync(userId, model, ip);
        return success && result != null
            ? ThreeDBodyPartApiResponseHelper.Success(result, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpPut("metaphors/{id:long}")]
    public async Task<object> UpdateMetaphor(long id, [FromBody] RubricMetaphorUpsertModel model)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message, result) = await _adminService.UpdateMetaphorAsync(userId, id, model, ip);
        return success && result != null
            ? ThreeDBodyPartApiResponseHelper.Success(result, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpDelete("metaphors/{id:long}")]
    public async Task<object> DeleteMetaphor(long id)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message) = await _adminService.DeleteMetaphorAsync(userId, id, ip);
        return success
            ? ThreeDBodyPartApiResponseHelper.Success(new { id }, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpDelete("metaphors")]
    public async Task<object> DeleteAllMetaphors()
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message, deletedCount) = await _adminService.DeleteAllMetaphorsAsync(userId, ip);
        return success
            ? ThreeDBodyPartApiResponseHelper.Success(new { deletedCount }, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpPost("metaphors/{id:long}/approve")]
    public async Task<object> ApproveMetaphor(long id)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message) = await _adminService.ApproveMetaphorAsync(userId, id, ip);
        return success
            ? ThreeDBodyPartApiResponseHelper.Success(new { id }, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpPost("metaphors/{id:long}/reject")]
    public async Task<object> RejectMetaphor(long id)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message) = await _adminService.RejectMetaphorAsync(userId, id, ip);
        return success
            ? ThreeDBodyPartApiResponseHelper.Success(new { id }, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpGet("aliases")]
    public async Task<object> GetAliases(
        [FromQuery] string? search,
        [FromQuery] string? language,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _adminService.GetAliasesAsync(search, language, pageNumber, pageSize);
            return ThreeDBodyPartApiResponseHelper.Success(result, "Aliases fetched.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get aliases failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("aliases/{id:long}")]
    public async Task<object> GetAlias(long id)
    {
        var result = await _adminService.GetAliasByIdAsync(id);
        if (result == null) return ThreeDBodyPartApiResponseHelper.Failure("Alias not found.");
        return ThreeDBodyPartApiResponseHelper.Success(result, "Alias fetched.");
    }

    [HttpPost("aliases")]
    public async Task<object> CreateAlias([FromBody] RubricAliasUpsertModel model)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message, result) = await _adminService.CreateAliasAsync(userId, model, ip);
        return success && result != null
            ? ThreeDBodyPartApiResponseHelper.Success(result, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpPut("aliases/{id:long}")]
    public async Task<object> UpdateAlias(long id, [FromBody] RubricAliasUpsertModel model)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message, result) = await _adminService.UpdateAliasAsync(userId, id, model, ip);
        return success && result != null
            ? ThreeDBodyPartApiResponseHelper.Success(result, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpDelete("aliases/{id:long}")]
    public async Task<object> DeleteAlias(long id)
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message) = await _adminService.DeleteAliasAsync(userId, id, ip);
        return success
            ? ThreeDBodyPartApiResponseHelper.Success(new { id }, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }

    [HttpDelete("aliases")]
    public async Task<object> DeleteAllAliases()
    {
        var userId = User.GetUserId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, message, deletedCount) = await _adminService.DeleteAllAliasesAsync(userId, ip);
        return success
            ? ThreeDBodyPartApiResponseHelper.Success(new { deletedCount }, message)
            : ThreeDBodyPartApiResponseHelper.Failure(message);
    }
}

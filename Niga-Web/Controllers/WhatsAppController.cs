using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.API.Controllers;

/// <summary>
/// WhatsApp Messaging APIs using Meta WhatsApp Cloud API.
/// </summary>
[Route("api/WhatsApp")]
[ApiController]
[Authorize]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(IWhatsAppService whatsAppService, ILogger<WhatsAppController> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    [HttpPost("SendIndividualMessage")]
    [ProducesResponseType(typeof(ApiResponse<SendWhatsAppMessageResultModel>), StatusCodes.Status200OK)]
    public async Task<object> SendIndividualMessage([FromBody] SendIndividualWhatsAppMessageRequest request)
    {
        return await ExecuteSendAsync(request, () => _whatsAppService.SendIndividualMessageAsync(request));
    }

    [HttpPost("SendBulkMessage")]
    [ProducesResponseType(typeof(ApiResponse<SendWhatsAppMessageResultModel>), StatusCodes.Status200OK)]
    public async Task<object> SendBulkMessage([FromBody] SendBulkWhatsAppMessageRequest request)
    {
        return await ExecuteSendAsync(request, () => _whatsAppService.SendBulkMessageAsync(request));
    }

    [HttpPost("SendHospitalServiceMessage")]
    [ProducesResponseType(typeof(ApiResponse<SendWhatsAppMessageResultModel>), StatusCodes.Status200OK)]
    public async Task<object> SendHospitalServiceMessage([FromBody] SendHospitalServiceMessageRequest request)
    {
        return await ExecuteSendAsync(request, () => _whatsAppService.SendHospitalServiceMessageAsync(request));
    }

    [HttpPost("SendOfferMessage")]
    [ProducesResponseType(typeof(ApiResponse<SendWhatsAppMessageResultModel>), StatusCodes.Status200OK)]
    public async Task<object> SendOfferMessage([FromBody] SendOfferMessageRequest request)
    {
        return await ExecuteSendAsync(request, () => _whatsAppService.SendOfferMessageAsync(request));
    }

    [HttpPost("SendHealthTipMessage")]
    [ProducesResponseType(typeof(ApiResponse<SendWhatsAppMessageResultModel>), StatusCodes.Status200OK)]
    public async Task<object> SendHealthTipMessage([FromBody] SendHealthTipMessageRequest request)
    {
        return await ExecuteSendAsync(request, () => _whatsAppService.SendHealthTipMessageAsync(request));
    }

    [HttpGet("GetMessageHistory")]
    [ProducesResponseType(typeof(PaginatedApiResponse<WhatsAppMessageHistoryItemModel>), StatusCodes.Status200OK)]
    public async Task<object> GetMessageHistory([FromQuery] GetWhatsAppMessageHistoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedFailure(GetValidationMessage());
            }

            var (success, message, result) = await _whatsAppService.GetMessageHistoryAsync(request);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedFailure(message);
            }

            return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMessageHistory failed.");
            return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
        }
    }

    [HttpGet("GetMessageById/{id}")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppMessageDetailModel>), StatusCodes.Status200OK)]
    public async Task<object> GetMessageById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return ThreeDBodyPartApiResponseHelper.Failure("WhatsApp message log ID is required.");
            }

            var result = await _whatsAppService.GetMessageByIdAsync(id);
            if (result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure("WhatsApp message log not found.");
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, "WhatsApp message details retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMessageById failed for ID={Id}", id);
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("GetCampaignHistory")]
    [ProducesResponseType(typeof(PaginatedApiResponse<WhatsAppCampaignHistoryItemModel>), StatusCodes.Status200OK)]
    public async Task<object> GetCampaignHistory([FromQuery] GetWhatsAppCampaignHistoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedFailure(GetValidationMessage());
            }

            var (success, message, result) = await _whatsAppService.GetCampaignHistoryAsync(request);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedFailure(message);
            }

            return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCampaignHistory failed.");
            return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
        }
    }

    [HttpGet("GetCampaignDetails/{campaignId}")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppCampaignDetailModel>), StatusCodes.Status200OK)]
    public async Task<object> GetCampaignDetails(int campaignId)
    {
        try
        {
            if (campaignId <= 0)
            {
                return ThreeDBodyPartApiResponseHelper.Failure("Campaign ID is required.");
            }

            var result = await _whatsAppService.GetCampaignDetailsAsync(campaignId);
            if (result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure("Campaign not found.");
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, "Campaign details retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCampaignDetails failed for CampaignID={CampaignId}", campaignId);
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get paginated WhatsApp message templates with optional category and active filters.
    /// </summary>
    [HttpGet("GetTemplates")]
    [ProducesResponseType(typeof(PaginatedApiResponse<WhatsAppTemplateListItemModel>), StatusCodes.Status200OK)]
    public async Task<object> GetTemplates([FromQuery] GetWhatsAppTemplatesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedFailure(GetValidationMessage());
            }

            var (success, message, result) = await _whatsAppService.GetTemplatesAsync(request);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedFailure(message);
            }

            return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTemplates failed.");
            return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
        }
    }

    /// <summary>
    /// Get WhatsApp template details by ID (includes TemplateBody).
    /// </summary>
    [HttpGet("GetTemplateById/{id}")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDetailModel>), StatusCodes.Status200OK)]
    public async Task<object> GetTemplateById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return ThreeDBodyPartApiResponseHelper.Failure("Template ID is required.");
            }

            var result = await _whatsAppService.GetTemplateByIdAsync(id);
            if (result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure("WhatsApp template not found.");
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, "WhatsApp template retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTemplateById failed for ID={Id}", id);
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    /// <summary>
    /// Create a new WhatsApp message template.
    /// </summary>
    [HttpPost("AddTemplate")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDetailModel>), StatusCodes.Status200OK)]
    public async Task<object> AddTemplate([FromBody] AddWhatsAppTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }

            var (success, message, result) = await _whatsAppService.AddTemplateAsync(request);
            if (!success)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result!, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddTemplate failed for TemplateName={TemplateName}", request.TemplateName);
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing WhatsApp message template.
    /// </summary>
    [HttpPost("UpdateTemplate")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppTemplateDetailModel>), StatusCodes.Status200OK)]
    public async Task<object> UpdateTemplate([FromBody] UpdateWhatsAppTemplateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }

            var (success, message, result) = await _whatsAppService.UpdateTemplateAsync(request);
            if (!success)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result!, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateTemplate failed for TemplateID={TemplateId}", request.TemplateID);
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("GetDashboard")]
    [ProducesResponseType(typeof(ApiResponse<WhatsAppDashboardModel>), StatusCodes.Status200OK)]
    public async Task<object> GetDashboard([FromQuery] GetWhatsAppDashboardRequest request)
    {
        try
        {
            var (success, message, result) = await _whatsAppService.GetDashboardAsync(request);
            if (!success || result == null)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(message);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDashboard failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    private async Task<object> ExecuteSendAsync<TRequest>(
        TRequest request,
        Func<Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)>> sendAction)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
            }

            var (success, message, result) = await sendAction();
            if (!success)
            {
                return ApiResponse<SendWhatsAppMessageResultModel>.CreateFailure(message, result);
            }

            return ThreeDBodyPartApiResponseHelper.Success(result!, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp send failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    private string GetValidationMessage()
    {
        return string.Join("; ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
    }
}

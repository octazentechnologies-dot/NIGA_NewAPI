using System.Threading;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IWhatsAppService
{
    Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendHospitalServiceMessageAsync(
        SendHospitalServiceMessageRequest request);

    Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendIndividualMessageAsync(
        SendIndividualWhatsAppMessageRequest request);

    Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendBulkMessageAsync(
        SendBulkWhatsAppMessageRequest request);

    Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendOfferMessageAsync(
        SendOfferMessageRequest request);

    Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendHealthTipMessageAsync(
        SendHealthTipMessageRequest request);

    Task ProcessBulkSendJobAsync(WhatsAppBulkSendJob job, int batchSize, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, PaginatedResult<WhatsAppMessageHistoryItemModel>? Result)> GetMessageHistoryAsync(
        GetWhatsAppMessageHistoryRequest request);

    Task<WhatsAppMessageDetailModel?> GetMessageByIdAsync(int whatsAppMessageLogId);

    Task<(bool Success, string Message, PaginatedResult<WhatsAppCampaignHistoryItemModel>? Result)> GetCampaignHistoryAsync(
        GetWhatsAppCampaignHistoryRequest request);

    Task<WhatsAppCampaignDetailModel?> GetCampaignDetailsAsync(int campaignId);

    Task<(bool Success, string Message, WhatsAppDashboardModel? Result)> GetDashboardAsync(GetWhatsAppDashboardRequest request);

    Task<(bool Success, string Message, PaginatedResult<WhatsAppTemplateListItemModel>? Result)> GetTemplatesAsync(
        GetWhatsAppTemplatesRequest request);

    Task<WhatsAppTemplateDetailModel?> GetTemplateByIdAsync(int templateId);

    Task<(bool Success, string Message, WhatsAppTemplateDetailModel? Result)> AddTemplateAsync(AddWhatsAppTemplateRequest request);

    Task<(bool Success, string Message, WhatsAppTemplateDetailModel? Result)> UpdateTemplateAsync(UpdateWhatsAppTemplateRequest request);
}

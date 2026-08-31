using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces;

public interface IWhatsAppRepository
{
    Task<Doctor?> GetActiveDoctorByIdAsync(int doctorId);

    Task<Patient?> GetActivePatientByIdAsync(int patientId);

    Task<bool> PatientHasWhatsAppConsentAsync(int patientId);

    Task<WhatsAppPatientRecipientModel?> GetActivePatientByMobileForDoctorAsync(
        int doctorId,
        string normalizedMobile);

    Task<WhatsAppPatientRecipientModel?> GetActivePatientByIdForDoctorAsync(int doctorId, int patientId);

    Task<List<WhatsAppPatientRecipientModel>> GetOptedInPatientsByDoctorIdAsync(int doctorId);

    Task<WhatsAppTemplateMaster?> GetActiveTemplateByIdAsync(int templateId);

    Task<WhatsAppTemplateMaster?> GetTemplateByIdAsync(int templateId);

    Task<WhatsAppTemplateDetailModel?> GetTemplateDetailByIdAsync(int templateId);

    Task<PaginatedResult<WhatsAppTemplateListItemModel>> GetTemplatesAsync(
        string? templateCategory,
        int? languageId,
        bool? isActive,
        PaginationRequestModel pagination);

    Task<bool> TemplateNameExistsAsync(
        string templateName,
        string templateCategory,
        int languageId,
        int? excludeTemplateId = null);

    Task<bool> LanguageExistsAsync(int languageId);

    Task<int> GetDefaultEnglishLanguageIdAsync();

    Task<string?> GetLanguageNameByIdAsync(int languageId);

    Task<WhatsAppTemplateMaster?> GetDefaultActiveTemplateByCategoryAndLanguageAsync(string category, int languageId);

    Task<int> CreateTemplateAsync(WhatsAppTemplateMaster template);

    Task<bool> UpdateTemplateAsync(WhatsAppTemplateMaster template);

    Task<WhatsAppCampaign?> GetActiveCampaignByIdAsync(int campaignId);

    Task<bool> CampaignNameExistsAsync(int doctorId, string campaignName, string category, int? excludeCampaignId = null);

    Task<int> CreateCampaignAsync(WhatsAppCampaign campaign);

    Task<WhatsAppMessageDetailModel?> GetMessageByIdAsync(int whatsAppMessageLogId);

    Task<PaginatedResult<WhatsAppMessageHistoryItemModel>> GetMessageHistoryAsync(
        int? doctorId,
        int? patientId,
        int? campaignId,
        string? messageCategory,
        bool? sendStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        PaginationRequestModel pagination);

    Task<PaginatedResult<WhatsAppCampaignHistoryItemModel>> GetCampaignHistoryAsync(
        int? doctorId,
        string? campaignCategory,
        DateTime? dateFrom,
        DateTime? dateTo,
        PaginationRequestModel pagination);

    Task<WhatsAppCampaignDetailModel?> GetCampaignDetailsAsync(int campaignId);

    Task<WhatsAppDashboardModel> GetDashboardAsync(int? doctorId, DateTime? dateFrom, DateTime? dateTo);

    void AddMessageLog(WhatsAppMessageLog log);

    Task<bool> SaveAllAsync();
}

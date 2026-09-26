using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

public class WhatsAppRepository : IWhatsAppRepository
{
    private readonly NIGACentrumContext _context;
    private readonly string _defaultCountryDialCode;

    public WhatsAppRepository(NIGACentrumContext context, IOptions<WhatsAppMetaOptions> metaOptions)
    {
        _context = context;
        _defaultCountryDialCode = metaOptions.Value.DefaultCountryDialCode;
    }

    public Task<Doctor?> GetActiveDoctorByIdAsync(int doctorId)
    {
        return _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.DeleteStatus);
    }

    public Task<Patient?> GetActivePatientByIdAsync(int patientId)
    {
        return _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId && p.DeleteStatus == false);
    }

    public async Task<bool> PatientHasWhatsAppConsentAsync(int patientId)
    {
        return await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.PatientId == patientId && p.DeleteStatus == false && p.IsWhatsAppOptIn);
    }

    public async Task<WhatsAppPatientRecipientModel?> GetActivePatientByMobileForDoctorAsync(
        int doctorId,
        string normalizedMobile)
    {
        var patients = await GetOptedInPatientsByDoctorIdAsync(doctorId);
        return patients.FirstOrDefault(p =>
            WhatsAppMessageTemplateEngine.MobileNumbersMatch(p.ContactNumber, normalizedMobile, _defaultCountryDialCode));
    }

    public async Task<WhatsAppPatientRecipientModel?> GetActivePatientByIdForDoctorAsync(int doctorId, int patientId)
    {
        var patients = await GetOptedInPatientsByDoctorIdAsync(doctorId);
        return patients.FirstOrDefault(p => p.PatientId == patientId);
    }

    public async Task<List<WhatsAppPatientRecipientModel>> GetOptedInPatientsByDoctorIdAsync(int doctorId)
    {
        var query =
            from caseEntry in _context.CaseEntryDetails.AsNoTracking()
            join patient in _context.Patients.AsNoTracking() on caseEntry.PatientId equals patient.PatientId
            where caseEntry.DoctorId == doctorId
                  && caseEntry.DeleteStatus == false
                  && patient.DeleteStatus == false
                  && patient.IsWhatsAppOptIn
                  && !string.IsNullOrWhiteSpace(patient.MobileNo)
            select new WhatsAppPatientRecipientModel
            {
                PatientId = patient.PatientId,
                PatientName = patient.PatientName,
                ContactNumber = patient.MobileNo,
                IsWhatsAppOptIn = patient.IsWhatsAppOptIn
            };

        var patients = await query.ToListAsync();

        return patients
            .GroupBy(p => p.PatientId)
            .Select(g => g.First())
            .OrderBy(p => p.PatientName)
            .ToList();
    }

    public Task<WhatsAppTemplateMaster?> GetActiveTemplateByIdAsync(int templateId)
    {
        return _context.WhatsAppTemplateMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.TemplateId == templateId
                && t.IsActive
                && !t.DeleteStatus);
    }

    public Task<WhatsAppTemplateMaster?> GetTemplateByIdAsync(int templateId)
    {
        return _context.WhatsAppTemplateMasters
            .FirstOrDefaultAsync(t => t.TemplateId == templateId && !t.DeleteStatus);
    }

    public async Task<WhatsAppTemplateDetailModel?> GetTemplateDetailByIdAsync(int templateId)
    {
        return await _context.WhatsAppTemplateMasters
            .AsNoTracking()
            .Where(t => t.TemplateId == templateId && !t.DeleteStatus)
            .Select(t => new WhatsAppTemplateDetailModel
            {
                TemplateID = t.TemplateId,
                TemplateName = t.TemplateName,
                TemplateCategory = t.TemplateCategory,
                MetaTemplateName = t.MetaTemplateName,
                TemplateBody = t.TemplateBody,
                LanguageId = t.LanguageId,
                LanguageName = t.Language.LanguageName,
                Description = t.Description,
                IsActive = t.IsActive,
                EnteredBy = t.EnteredBy,
                EnteredDate = t.EnteredDate,
                ChangedBy = t.ChangedBy,
                ChangedDate = t.ChangedDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PaginatedResult<WhatsAppTemplateListItemModel>> GetTemplatesAsync(
        string? templateCategory,
        int? languageId,
        bool? isActive,
        PaginationRequestModel pagination)
    {
        var query = _context.WhatsAppTemplateMasters
            .AsNoTracking()
            .Where(t => !t.DeleteStatus);

        if (!string.IsNullOrWhiteSpace(templateCategory))
        {
            query = query.Where(t => t.TemplateCategory == templateCategory);
        }

        if (languageId.HasValue)
        {
            query = query.Where(t => t.LanguageId == languageId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var projected = query
            .OrderByDescending(t => t.EnteredDate)
            .Select(t => new WhatsAppTemplateListItemModel
            {
                TemplateID = t.TemplateId,
                TemplateName = t.TemplateName,
                TemplateCategory = t.TemplateCategory,
                MetaTemplateName = t.MetaTemplateName,
                LanguageId = t.LanguageId,
                LanguageName = t.Language.LanguageName,
                Description = t.Description,
                IsActive = t.IsActive,
                EnteredDate = t.EnteredDate,
                ChangedDate = t.ChangedDate
            });

        return await projected.ToPaginatedResultAsync(pagination);
    }

    public Task<bool> TemplateNameExistsAsync(
        string templateName,
        string templateCategory,
        int languageId,
        int? excludeTemplateId = null)
    {
        var query = _context.WhatsAppTemplateMasters.AsNoTracking()
            .Where(t => !t.DeleteStatus
                        && t.TemplateName == templateName
                        && t.TemplateCategory == templateCategory
                        && t.LanguageId == languageId);

        if (excludeTemplateId.HasValue)
        {
            query = query.Where(t => t.TemplateId != excludeTemplateId.Value);
        }

        return query.AnyAsync();
    }

    public Task<bool> LanguageExistsAsync(int languageId)
    {
        return _context.LanguageMasters.AsNoTracking()
            .AnyAsync(l => l.LanguageId == languageId && (l.IsDeleted == false || l.IsDeleted == null));
    }

    public async Task<int> GetDefaultEnglishLanguageIdAsync()
    {
        var englishId = await _context.LanguageMasters.AsNoTracking()
            .Where(l => l.IsDeleted == false || l.IsDeleted == null)
            .Where(l => l.LanguageName != null
                        && (l.LanguageName.ToUpper() == "ENGLISH" || l.LanguageName.ToUpper() == "EN"))
            .OrderBy(l => l.LanguageId)
            .Select(l => l.LanguageId)
            .FirstOrDefaultAsync();

        return englishId > 0 ? englishId : WhatsAppLanguageHelper.FallbackEnglishLanguageId;
    }

    public Task<string?> GetLanguageNameByIdAsync(int languageId)
    {
        return _context.LanguageMasters.AsNoTracking()
            .Where(l => l.LanguageId == languageId)
            .Select(l => l.LanguageName)
            .FirstOrDefaultAsync();
    }

    public Task<WhatsAppTemplateMaster?> GetDefaultActiveTemplateByCategoryAndLanguageAsync(string category, int languageId)
    {
        return _context.WhatsAppTemplateMasters.AsNoTracking()
            .Where(t => !t.DeleteStatus
                        && t.IsActive
                        && t.TemplateCategory == category
                        && t.LanguageId == languageId)
            .OrderBy(t => t.TemplateId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateTemplateAsync(WhatsAppTemplateMaster template)
    {
        _context.WhatsAppTemplateMasters.Add(template);
        await _context.SaveChangesAsync();
        return template.TemplateId;
    }

    public async Task<bool> UpdateTemplateAsync(WhatsAppTemplateMaster template)
    {
        return await _context.SaveChangesAsync() > 0;
    }

    public Task<WhatsAppCampaign?> GetActiveCampaignByIdAsync(int campaignId)
    {
        return _context.WhatsAppCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId && !c.DeleteStatus);
    }

    public Task<bool> CampaignNameExistsAsync(int doctorId, string campaignName, string category, int? excludeCampaignId = null)
    {
        var query = _context.WhatsAppCampaigns.AsNoTracking()
            .Where(c => c.DoctorId == doctorId
                        && !c.DeleteStatus
                        && c.CampaignName == campaignName
                        && c.CampaignCategory == category);

        if (excludeCampaignId.HasValue)
        {
            query = query.Where(c => c.CampaignId != excludeCampaignId.Value);
        }

        return query.AnyAsync();
    }

    public async Task<int> CreateCampaignAsync(WhatsAppCampaign campaign)
    {
        _context.WhatsAppCampaigns.Add(campaign);
        await _context.SaveChangesAsync();
        return campaign.CampaignId;
    }

    public async Task<WhatsAppMessageDetailModel?> GetMessageByIdAsync(int whatsAppMessageLogId)
    {
        return await _context.WhatsAppMessageLogs
            .AsNoTracking()
            .Where(x => x.WhatsAppMessageLogId == whatsAppMessageLogId && !x.DeleteStatus)
            .Select(x => new WhatsAppMessageDetailModel
            {
                WhatsAppMessageLogID = x.WhatsAppMessageLogId,
                DoctorID = x.DoctorId,
                PatientID = x.PatientId,
                CampaignID = x.CampaignId,
                TemplateID = x.TemplateId,
                LanguageId = x.LanguageId,
                LanguageName = x.Language != null ? x.Language.LanguageName : null,
                PatientName = x.PatientName,
                MobileNumber = x.MobileNumber,
                MessageCategory = x.MessageCategory,
                MessageBody = x.MessageBody,
                TemplateMessage = x.TemplateMessage,
                FinalMessage = x.FinalMessage,
                MetaMessageId = x.MetaMessageId,
                MediaId = x.MediaId,
                IsBulk = x.IsBulk,
                SendStatus = x.SendStatus,
                ErrorMessage = x.ErrorMessage,
                CreatedDate = x.CreatedDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PaginatedResult<WhatsAppMessageHistoryItemModel>> GetMessageHistoryAsync(
        int? doctorId,
        int? patientId,
        int? campaignId,
        string? messageCategory,
        bool? sendStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        PaginationRequestModel pagination)
    {
        var query = _context.WhatsAppMessageLogs
            .AsNoTracking()
            .Where(x => !x.DeleteStatus);

        if (doctorId.HasValue)
        {
            query = query.Where(x => x.DoctorId == doctorId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(x => x.PatientId == patientId.Value);
        }

        if (campaignId.HasValue)
        {
            query = query.Where(x => x.CampaignId == campaignId.Value);
        }

        if (!string.IsNullOrWhiteSpace(messageCategory))
        {
            query = query.Where(x => x.MessageCategory == messageCategory);
        }

        if (sendStatus.HasValue)
        {
            query = query.Where(x => x.SendStatus == sendStatus.Value);
        }

        if (dateFrom.HasValue)
        {
            var fromDate = dateFrom.Value.Date;
            query = query.Where(x => x.CreatedDate >= fromDate);
        }

        if (dateTo.HasValue)
        {
            var toDate = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CreatedDate <= toDate);
        }

        var projected = query
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new WhatsAppMessageHistoryItemModel
            {
                WhatsAppMessageLogID = x.WhatsAppMessageLogId,
                DoctorID = x.DoctorId,
                PatientID = x.PatientId,
                CampaignID = x.CampaignId,
                TemplateID = x.TemplateId,
                LanguageId = x.LanguageId,
                LanguageName = x.Language != null ? x.Language.LanguageName : null,
                PatientName = x.PatientName,
                MobileNumber = x.MobileNumber,
                MessageCategory = x.MessageCategory,
                TemplateMessage = x.TemplateMessage,
                FinalMessage = x.FinalMessage,
                MetaMessageId = x.MetaMessageId,
                MediaId = x.MediaId,
                IsBulk = x.IsBulk,
                SendStatus = x.SendStatus,
                ErrorMessage = x.ErrorMessage,
                CreatedDate = x.CreatedDate
            });

        return await projected.ToPaginatedResultAsync(pagination);
    }

    public async Task<PaginatedResult<WhatsAppCampaignHistoryItemModel>> GetCampaignHistoryAsync(
        int? doctorId,
        string? campaignCategory,
        DateTime? dateFrom,
        DateTime? dateTo,
        PaginationRequestModel pagination)
    {
        var campaignQuery = _context.WhatsAppCampaigns.AsNoTracking().Where(c => !c.DeleteStatus);

        if (doctorId.HasValue)
        {
            campaignQuery = campaignQuery.Where(c => c.DoctorId == doctorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(campaignCategory))
        {
            campaignQuery = campaignQuery.Where(c => c.CampaignCategory == campaignCategory);
        }

        if (dateFrom.HasValue)
        {
            var fromDate = dateFrom.Value.Date;
            campaignQuery = campaignQuery.Where(c => c.EnteredDate >= fromDate);
        }

        if (dateTo.HasValue)
        {
            var toDate = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            campaignQuery = campaignQuery.Where(c => c.EnteredDate <= toDate);
        }

        var logStats = _context.WhatsAppMessageLogs.AsNoTracking().Where(l => !l.DeleteStatus);

        var projected = from campaign in campaignQuery
                        join stat in
                            from log in logStats
                            group log by log.CampaignId into g
                            select new
                            {
                                CampaignId = g.Key,
                                TotalMessages = g.Count(),
                                TotalDelivered = g.Count(x => x.SendStatus),
                                TotalFailed = g.Count(x => !x.SendStatus)
                            }
                            on campaign.CampaignId equals stat.CampaignId into stats
                        from stat in stats.DefaultIfEmpty()
                        orderby campaign.EnteredDate descending
                        select new WhatsAppCampaignHistoryItemModel
                        {
                            CampaignID = campaign.CampaignId,
                            CampaignName = campaign.CampaignName,
                            CampaignCategory = campaign.CampaignCategory,
                            DoctorID = campaign.DoctorId,
                            IsBulk = campaign.IsBulk,
                            EnteredDate = campaign.EnteredDate,
                            TotalMessages = stat != null ? stat.TotalMessages : 0,
                            TotalDelivered = stat != null ? stat.TotalDelivered : 0,
                            TotalFailed = stat != null ? stat.TotalFailed : 0
                        };

        return await projected.ToPaginatedResultAsync(pagination);
    }

    public async Task<WhatsAppCampaignDetailModel?> GetCampaignDetailsAsync(int campaignId)
    {
        var campaign = await _context.WhatsAppCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId && !c.DeleteStatus);

        if (campaign == null)
        {
            return null;
        }

        var logs = _context.WhatsAppMessageLogs.AsNoTracking()
            .Where(l => l.CampaignId == campaignId && !l.DeleteStatus);

        var totalMessages = await logs.CountAsync();
        var totalDelivered = await logs.CountAsync(l => l.SendStatus);
        var totalFailed = totalMessages - totalDelivered;

        var recentMessages = await logs
            .OrderByDescending(l => l.CreatedDate)
            .Take(20)
            .Select(l => new WhatsAppMessageHistoryItemModel
            {
                WhatsAppMessageLogID = l.WhatsAppMessageLogId,
                DoctorID = l.DoctorId,
                PatientID = l.PatientId,
                CampaignID = l.CampaignId,
                TemplateID = l.TemplateId,
                LanguageId = l.LanguageId,
                LanguageName = l.Language != null ? l.Language.LanguageName : null,
                PatientName = l.PatientName,
                MobileNumber = l.MobileNumber,
                MessageCategory = l.MessageCategory,
                TemplateMessage = l.TemplateMessage,
                FinalMessage = l.FinalMessage,
                MetaMessageId = l.MetaMessageId,
                MediaId = l.MediaId,
                IsBulk = l.IsBulk,
                SendStatus = l.SendStatus,
                ErrorMessage = l.ErrorMessage,
                CreatedDate = l.CreatedDate
            })
            .ToListAsync();

        return new WhatsAppCampaignDetailModel
        {
            CampaignID = campaign.CampaignId,
            CampaignName = campaign.CampaignName,
            CampaignCategory = campaign.CampaignCategory,
            DoctorID = campaign.DoctorId,
            MessageBody = campaign.MessageBody,
            ImageUrl = campaign.ImageUrl,
            IsBulk = campaign.IsBulk,
            EnteredDate = campaign.EnteredDate,
            TotalMessages = totalMessages,
            TotalDelivered = totalDelivered,
            TotalFailed = totalFailed,
            RecentMessages = recentMessages
        };
    }

    public async Task<WhatsAppDashboardModel> GetDashboardAsync(int? doctorId, DateTime? dateFrom, DateTime? dateTo)
    {
        var logQuery = _context.WhatsAppMessageLogs.AsNoTracking().Where(l => !l.DeleteStatus);
        var campaignQuery = _context.WhatsAppCampaigns.AsNoTracking().Where(c => !c.DeleteStatus);

        if (doctorId.HasValue)
        {
            logQuery = logQuery.Where(l => l.DoctorId == doctorId.Value);
            campaignQuery = campaignQuery.Where(c => c.DoctorId == doctorId.Value);
        }

        if (dateFrom.HasValue)
        {
            var fromDate = dateFrom.Value.Date;
            logQuery = logQuery.Where(l => l.CreatedDate >= fromDate);
            campaignQuery = campaignQuery.Where(c => c.EnteredDate >= fromDate);
        }

        if (dateTo.HasValue)
        {
            var toDate = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            logQuery = logQuery.Where(l => l.CreatedDate <= toDate);
            campaignQuery = campaignQuery.Where(c => c.EnteredDate <= toDate);
        }

        var totalMessages = await logQuery.CountAsync();
        var totalDelivered = await logQuery.CountAsync(l => l.SendStatus);
        var totalFailed = totalMessages - totalDelivered;
        var totalCampaigns = await campaignQuery.CountAsync();

        var messagesByCategory = await logQuery
            .GroupBy(l => l.MessageCategory ?? "Unknown")
            .Select(g => new WhatsAppCategoryCountModel
            {
                Category = g.Key,
                Count = g.Count(),
                Delivered = g.Count(x => x.SendStatus),
                Failed = g.Count(x => !x.SendStatus)
            })
            .ToListAsync();

        var messagesByDoctor = await logQuery
            .Where(l => l.DoctorId.HasValue)
            .GroupBy(l => l.DoctorId!.Value)
            .Select(g => new WhatsAppDoctorCountModel
            {
                DoctorID = g.Key,
                Count = g.Count(),
                Delivered = g.Count(x => x.SendStatus),
                Failed = g.Count(x => !x.SendStatus)
            })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync();

        var monthlyAnalytics = await logQuery
            .GroupBy(l => new { l.CreatedDate.Year, l.CreatedDate.Month })
            .Select(g => new WhatsAppMonthlyAnalyticsModel
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalMessages = g.Count(),
                Delivered = g.Count(x => x.SendStatus),
                Failed = g.Count(x => !x.SendStatus)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        return new WhatsAppDashboardModel
        {
            TotalMessages = totalMessages,
            TotalDelivered = totalDelivered,
            TotalFailed = totalFailed,
            TotalCampaigns = totalCampaigns,
            MessagesByCategory = messagesByCategory,
            MessagesByDoctor = messagesByDoctor,
            MonthlyAnalytics = monthlyAnalytics
        };
    }

    public void AddMessageLog(WhatsAppMessageLog log)
    {
        _context.WhatsAppMessageLogs.Add(log);
    }

    public async Task<bool> SaveAllAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}

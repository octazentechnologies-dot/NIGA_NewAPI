using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Enums;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories;

public class WhatsAppService : IWhatsAppService
{
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;

    private readonly IWhatsAppRepository _repository;
    private readonly IWhatsAppMetaApiClient _metaApiClient;
    private readonly IWhatsAppBulkSendQueue _bulkSendQueue;
    private readonly WhatsAppMetaOptions _metaOptions;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IWhatsAppRepository repository,
        IWhatsAppMetaApiClient metaApiClient,
        IWhatsAppBulkSendQueue bulkSendQueue,
        IOptions<WhatsAppMetaOptions> metaOptions,
        ILogger<WhatsAppService> logger)
    {
        _repository = repository;
        _metaApiClient = metaApiClient;
        _bulkSendQueue = bulkSendQueue;
        _metaOptions = metaOptions.Value;
        _logger = logger;
        // TODO S5: confirm WhatsApp Meta token in appsettings is still valid; S5 reuses this client.
    }

    public Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendHospitalServiceMessageAsync(
        SendHospitalServiceMessageRequest request)
    {
        return SendCategoryMessageAsync(
            WhatsAppMessageCategory.HospitalService,
            request.DoctorID,
            request.CampaignID,
            request.TemplateID,
            request.MessageBody,
            request.Message,
            request.ImageBase64,
            request.DoctorName,
            request.HospitalName,
            request.Date,
            request.Offer,
            request.HealthTip,
            request.AppointmentDate,
            request.AppointmentTime,
            request.Individual,
            request.Bulk,
            request.PatientName,
            null,
            request.PatientContactNumber,
            request.LanguageId);
    }

    public async Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendIndividualMessageAsync(
        SendIndividualWhatsAppMessageRequest request)
    {
        var languageContext = await ResolveLanguageContextAsync(request.LanguageId);
        if (languageContext.Error != null)
        {
            return (false, languageContext.Error, null);
        }

        var validation = await ValidateCommonAsync(request.DoctorID, request.TemplateID, languageContext.LanguageId);
        if (validation != null)
        {
            return (false, validation, null);
        }

        var recipient = await _repository.GetActivePatientByIdForDoctorAsync(request.DoctorID, request.PatientID);
        if (recipient == null)
        {
            return (false, "Patient not found, not linked to this doctor, or has not opted in to WhatsApp.", null);
        }

        if (!recipient.IsWhatsAppOptIn)
        {
            return (false, "Patient has not provided WhatsApp consent.", null);
        }

        var template = await ResolveTemplateAsync(
            request.TemplateID,
            WhatsAppMessageCategory.HospitalService,
            null,
            languageContext.LanguageId,
            languageContext.LanguageName);
        var sendContext = BuildSendContext(
            WhatsAppMessageCategory.HospitalService,
            request.DoctorID,
            null,
            request.TemplateID,
            languageContext.LanguageId,
            languageContext.LanguageName,
            template.Body,
            template.MetaTemplateName,
            request.Message,
            request.ImageBase64,
            request.DoctorName,
            request.HospitalName,
            request.Date,
            request.Offer,
            request.HealthTip,
            request.AppointmentDate,
            request.AppointmentTime,
            false,
            new List<WhatsAppPatientRecipientModel> { recipient });

        return await ExecuteSendAsync(sendContext);
    }

    public async Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendBulkMessageAsync(
        SendBulkWhatsAppMessageRequest request)
    {
        if (!WhatsAppMessageCategory.IsValid(request.MessageCategory))
        {
            return (false, "Invalid message category.", null);
        }

        var category = WhatsAppMessageCategory.Normalize(request.MessageCategory);
        var languageContext = await ResolveLanguageContextAsync(request.LanguageId);
        if (languageContext.Error != null)
        {
            return (false, languageContext.Error, null);
        }

        var validation = await ValidateCommonAsync(request.DoctorID, request.TemplateID, languageContext.LanguageId);
        if (validation != null)
        {
            return (false, validation, null);
        }

        if (await _repository.CampaignNameExistsAsync(request.DoctorID, request.CampaignName.Trim(), category))
        {
            return (false, "A campaign with the same name already exists for this doctor and category.", null);
        }

        var recipients = await _repository.GetOptedInPatientsByDoctorIdAsync(request.DoctorID);
        if (recipients.Count == 0)
        {
            return (false, "No opted-in patients with valid mobile numbers found for this doctor.", null);
        }

        var template = await ResolveTemplateAsync(
            request.TemplateID,
            category,
            null,
            languageContext.LanguageId,
            languageContext.LanguageName);
        var campaignId = await CreateCampaignRecordAsync(
            request.CampaignName.Trim(),
            category,
            request.DoctorID,
            template.Body ?? request.Message,
            true);

        var job = new WhatsAppBulkSendJob
        {
            JobId = Guid.NewGuid(),
            MessageCategory = category,
            DoctorId = request.DoctorID,
            CampaignId = campaignId,
            TemplateId = request.TemplateID,
            LanguageId = languageContext.LanguageId,
            LanguageName = languageContext.LanguageName,
            TemplateBody = template.Body,
            MetaTemplateName = template.MetaTemplateName,
            Message = request.Message,
            DoctorName = request.DoctorName,
            HospitalName = request.HospitalName,
            Date = request.Date,
            Offer = request.Offer,
            HealthTip = request.HealthTip,
            ImageBase64 = request.ImageBase64,
            Recipients = recipients
        };

        await _bulkSendQueue.EnqueueAsync(job);

        return (true, $"Bulk send queued for {recipients.Count} recipient(s).", new SendWhatsAppMessageResultModel
        {
            BulkJobId = job.JobId,
            CampaignID = campaignId,
            TotalQueued = recipients.Count
        });
    }

    public Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendOfferMessageAsync(
        SendOfferMessageRequest request)
    {
        return SendCategoryMessageAsync(
            WhatsAppMessageCategory.OffersDiscount,
            request.DoctorID,
            request.CampaignID,
            request.TemplateID,
            request.MessageBody,
            request.Message ?? request.Offer,
            request.ImageBase64,
            request.DoctorName,
            request.HospitalName,
            request.Date,
            request.Offer,
            request.HealthTip,
            request.AppointmentDate,
            request.AppointmentTime,
            request.Individual,
            request.Bulk,
            request.PatientName,
            request.PatientID,
            request.PatientContactNumber,
            request.LanguageId);
    }

    public Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendHealthTipMessageAsync(
        SendHealthTipMessageRequest request)
    {
        return SendCategoryMessageAsync(
            WhatsAppMessageCategory.HealthTips,
            request.DoctorID,
            request.CampaignID,
            request.TemplateID,
            request.MessageBody,
            request.Message ?? request.HealthTip,
            request.ImageBase64,
            request.DoctorName,
            request.HospitalName,
            request.Date,
            request.Offer,
            request.HealthTip,
            request.AppointmentDate,
            request.AppointmentTime,
            request.Individual,
            request.Bulk,
            request.PatientName,
            request.PatientID,
            request.PatientContactNumber,
            request.LanguageId);
    }

    public async Task ProcessBulkSendJobAsync(WhatsAppBulkSendJob job, int batchSize, CancellationToken cancellationToken = default)
    {
        var sendContext = BuildSendContext(
            job.MessageCategory,
            job.DoctorId,
            job.CampaignId,
            job.TemplateId,
            job.LanguageId,
            job.LanguageName,
            job.TemplateBody,
            job.MetaTemplateName,
            job.Message,
            job.ImageBase64,
            job.DoctorName,
            job.HospitalName,
            job.Date,
            job.Offer,
            job.HealthTip,
            job.AppointmentDate,
            job.AppointmentTime,
            true,
            job.Recipients);

        for (var i = 0; i < sendContext.Recipients.Count; i += batchSize)
        {
            var batch = sendContext.Recipients.Skip(i).Take(batchSize).ToList();
            sendContext.Recipients = batch;
            await ExecuteSendAsync(sendContext, cancellationToken);
        }
    }

    public async Task<(bool Success, string Message, PaginatedResult<WhatsAppMessageHistoryItemModel>? Result)> GetMessageHistoryAsync(
        GetWhatsAppMessageHistoryRequest request)
    {
        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom > request.DateTo)
        {
            return (false, "DateFrom cannot be greater than DateTo.", null);
        }

        if (request.DoctorID.HasValue && await _repository.GetActiveDoctorByIdAsync(request.DoctorID.Value) == null)
        {
            return (false, "Doctor not found or has been deleted.", null);
        }

        var pagination = new PaginationRequestModel
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = "CreatedDate",
            SortDirection = "desc"
        };

        var result = await _repository.GetMessageHistoryAsync(
            request.DoctorID,
            request.PatientID,
            request.CampaignID,
            request.MessageCategory,
            request.SendStatus,
            request.DateFrom,
            request.DateTo,
            pagination);

        return (true, result.TotalRecords == 0
            ? "No WhatsApp message history found."
            : "WhatsApp message history retrieved successfully.", result);
    }

    public Task<WhatsAppMessageDetailModel?> GetMessageByIdAsync(int whatsAppMessageLogId)
    {
        return _repository.GetMessageByIdAsync(whatsAppMessageLogId);
    }

    public async Task<(bool Success, string Message, PaginatedResult<WhatsAppCampaignHistoryItemModel>? Result)> GetCampaignHistoryAsync(
        GetWhatsAppCampaignHistoryRequest request)
    {
        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom > request.DateTo)
        {
            return (false, "DateFrom cannot be greater than DateTo.", null);
        }

        var pagination = new PaginationRequestModel
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = "EnteredDate",
            SortDirection = "desc"
        };

        var result = await _repository.GetCampaignHistoryAsync(
            request.DoctorID,
            request.CampaignCategory,
            request.DateFrom,
            request.DateTo,
            pagination);

        return (true, result.TotalRecords == 0
            ? "No campaigns found."
            : "Campaign history retrieved successfully.", result);
    }

    public Task<WhatsAppCampaignDetailModel?> GetCampaignDetailsAsync(int campaignId)
    {
        return _repository.GetCampaignDetailsAsync(campaignId);
    }

    public async Task<(bool Success, string Message, WhatsAppDashboardModel? Result)> GetDashboardAsync(GetWhatsAppDashboardRequest request)
    {
        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom > request.DateTo)
        {
            return (false, "DateFrom cannot be greater than DateTo.", null);
        }

        if (request.DoctorID.HasValue && await _repository.GetActiveDoctorByIdAsync(request.DoctorID.Value) == null)
        {
            return (false, "Doctor not found or has been deleted.", null);
        }

        var dashboard = await _repository.GetDashboardAsync(request.DoctorID, request.DateFrom, request.DateTo);
        return (true, "WhatsApp dashboard data retrieved successfully.", dashboard);
    }

    public async Task<(bool Success, string Message, PaginatedResult<WhatsAppTemplateListItemModel>? Result)> GetTemplatesAsync(
        GetWhatsAppTemplatesRequest request)
    {
        string? categoryFilter = null;
        if (!string.IsNullOrWhiteSpace(request.TemplateCategory))
        {
            if (!WhatsAppMessageCategory.IsValid(request.TemplateCategory))
            {
                return (false, "Invalid template category. Allowed: HospitalService, OffersDiscount, HealthTips.", null);
            }

            categoryFilter = WhatsAppMessageCategory.Normalize(request.TemplateCategory);
        }

        var pagination = new PaginationRequestModel
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = "EnteredDate",
            SortDirection = "desc"
        };

        if (request.LanguageId.HasValue && !await _repository.LanguageExistsAsync(request.LanguageId.Value))
        {
            return (false, "Invalid language ID.", null);
        }

        var result = await _repository.GetTemplatesAsync(categoryFilter, request.LanguageId, request.IsActive, pagination);
        return (true, result.TotalRecords == 0
            ? "No WhatsApp templates found."
            : "WhatsApp templates retrieved successfully.", result);
    }

    public Task<WhatsAppTemplateDetailModel?> GetTemplateByIdAsync(int templateId)
    {
        return _repository.GetTemplateDetailByIdAsync(templateId);
    }

    public async Task<(bool Success, string Message, WhatsAppTemplateDetailModel? Result)> AddTemplateAsync(
        AddWhatsAppTemplateRequest request)
    {
        var validation = ValidateTemplateRequest(request.TemplateCategory, request.TemplateName, request.TemplateBody);
        if (validation != null)
        {
            return (false, validation, null);
        }

        var languageContext = await ResolveLanguageContextAsync(request.LanguageId);
        if (languageContext.Error != null)
        {
            return (false, languageContext.Error, null);
        }

        var category = WhatsAppMessageCategory.Normalize(request.TemplateCategory);
        if (await _repository.TemplateNameExistsAsync(request.TemplateName.Trim(), category, languageContext.LanguageId))
        {
            return (false, "A template with the same name already exists for this category and language.", null);
        }

        var template = new WhatsAppTemplateMaster
        {
            TemplateName = request.TemplateName.Trim(),
            TemplateCategory = category,
            MetaTemplateName = string.IsNullOrWhiteSpace(request.MetaTemplateName) ? null : request.MetaTemplateName.Trim(),
            TemplateBody = request.TemplateBody.Trim(),
            LanguageId = languageContext.LanguageId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            EnteredBy = request.EnteredBy,
            EnteredDate = DateTime.Now,
            DeleteStatus = false
        };

        var templateId = await _repository.CreateTemplateAsync(template);
        var detail = await _repository.GetTemplateDetailByIdAsync(templateId);
        return (true, "WhatsApp template created successfully.", detail);
    }

    public async Task<(bool Success, string Message, WhatsAppTemplateDetailModel? Result)> UpdateTemplateAsync(
        UpdateWhatsAppTemplateRequest request)
    {
        var validation = ValidateTemplateRequest(request.TemplateCategory, request.TemplateName, request.TemplateBody);
        if (validation != null)
        {
            return (false, validation, null);
        }

        var entity = await _repository.GetTemplateByIdAsync(request.TemplateID);
        if (entity == null)
        {
            return (false, "WhatsApp template not found.", null);
        }

        var languageContext = await ResolveLanguageContextAsync(request.LanguageId);
        if (languageContext.Error != null)
        {
            return (false, languageContext.Error, null);
        }

        var category = WhatsAppMessageCategory.Normalize(request.TemplateCategory);
        if (await _repository.TemplateNameExistsAsync(
                request.TemplateName.Trim(),
                category,
                languageContext.LanguageId,
                request.TemplateID))
        {
            return (false, "A template with the same name already exists for this category and language.", null);
        }

        entity.TemplateName = request.TemplateName.Trim();
        entity.TemplateCategory = category;
        entity.MetaTemplateName = string.IsNullOrWhiteSpace(request.MetaTemplateName) ? null : request.MetaTemplateName.Trim();
        entity.TemplateBody = request.TemplateBody.Trim();
        entity.LanguageId = languageContext.LanguageId;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.IsActive = request.IsActive;
        entity.ChangedBy = request.ChangedBy;
        entity.ChangedDate = DateTime.Now;

        await _repository.UpdateTemplateAsync(entity);
        var detail = await _repository.GetTemplateDetailByIdAsync(request.TemplateID);
        return (true, "WhatsApp template updated successfully.", detail);
    }

    private static string? ValidateTemplateRequest(string templateCategory, string templateName, string templateBody)
    {
        if (!WhatsAppMessageCategory.IsValid(templateCategory))
        {
            return "Invalid template category. Allowed: HospitalService, OffersDiscount, HealthTips.";
        }

        if (string.IsNullOrWhiteSpace(templateName))
        {
            return "Template name is required.";
        }

        if (string.IsNullOrWhiteSpace(templateBody))
        {
            return "Template body is required.";
        }

        return null;
    }

    private async Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> SendCategoryMessageAsync(
        string category,
        int doctorId,
        int? campaignId,
        int? templateId,
        string? messageBody,
        string? message,
        string? imageBase64,
        string? doctorName,
        string? hospitalName,
        string? date,
        string? offer,
        string? healthTip,
        string? appointmentDate,
        string? appointmentTime,
        bool individual,
        bool bulk,
        string? patientName,
        int? patientId,
        string? patientContactNumber,
        int? languageId)
    {
        if (individual == bulk)
        {
            return (false, "Either Individual or Bulk must be true, but not both.", null);
        }

        var languageContext = await ResolveLanguageContextAsync(languageId);
        if (languageContext.Error != null)
        {
            return (false, languageContext.Error, null);
        }

        var validation = await ValidateCommonAsync(doctorId, templateId, languageContext.LanguageId);
        if (validation != null)
        {
            return (false, validation, null);
        }

        var template = await ResolveTemplateAsync(
            templateId,
            category,
            messageBody,
            languageContext.LanguageId,
            languageContext.LanguageName);
        List<WhatsAppPatientRecipientModel> recipients;

        if (bulk)
        {
            recipients = await _repository.GetOptedInPatientsByDoctorIdAsync(doctorId);
            if (recipients.Count == 0)
            {
                return (false, "No opted-in patients with valid mobile numbers found for this doctor.", null);
            }

            if (!campaignId.HasValue)
            {
                campaignId = await CreateCampaignRecordAsync(
                    $"{category}_{DateTime.Now:yyyyMMddHHmmss}",
                    category,
                    doctorId,
                    template.Body ?? message,
                    true);
            }

            var job = new WhatsAppBulkSendJob
            {
                JobId = Guid.NewGuid(),
                MessageCategory = category,
                DoctorId = doctorId,
                CampaignId = campaignId,
                TemplateId = templateId,
                LanguageId = languageContext.LanguageId,
                LanguageName = languageContext.LanguageName,
                TemplateBody = template.Body,
                MetaTemplateName = template.MetaTemplateName,
                Message = message,
                DoctorName = doctorName,
                HospitalName = hospitalName,
                Date = date,
                Offer = offer,
                HealthTip = healthTip,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                ImageBase64 = imageBase64,
                Recipients = recipients
            };

            await _bulkSendQueue.EnqueueAsync(job);

            return (true, $"Bulk {category} messages queued for {recipients.Count} recipient(s).", new SendWhatsAppMessageResultModel
            {
                BulkJobId = job.JobId,
                CampaignID = campaignId,
                TotalQueued = recipients.Count
            });
        }

        if (patientId.HasValue)
        {
            var byId = await _repository.GetActivePatientByIdForDoctorAsync(doctorId, patientId.Value);
            if (byId == null)
            {
                return (false, "Patient not found, not linked to this doctor, or has not opted in to WhatsApp.", null);
            }

            recipients = new List<WhatsAppPatientRecipientModel> { byId };
        }
        else
        {
            if (string.IsNullOrWhiteSpace(patientContactNumber))
            {
                return (false, "PatientContactNumber or PatientID is required for individual messages.", null);
            }

            var normalizedMobile = WhatsAppMessageTemplateEngine.NormalizeMobileNumber(patientContactNumber);
            if (normalizedMobile.Length < 10)
            {
                return (false, "PatientContactNumber is invalid.", null);
            }

            var patient = await _repository.GetActivePatientByMobileForDoctorAsync(doctorId, normalizedMobile);
            if (patient == null)
            {
                return (false, "Patient not found, not linked to this doctor, or has not opted in to WhatsApp.", null);
            }

            recipients = new List<WhatsAppPatientRecipientModel>
            {
                new()
                {
                    PatientId = patient.PatientId,
                    PatientName = string.IsNullOrWhiteSpace(patientName) ? patient.PatientName : patientName.Trim(),
                    ContactNumber = normalizedMobile,
                    IsWhatsAppOptIn = patient.IsWhatsAppOptIn
                }
            };
        }

        var sendContext = BuildSendContext(
            category,
            doctorId,
            campaignId,
            templateId,
            languageContext.LanguageId,
            languageContext.LanguageName,
            template.Body,
            template.MetaTemplateName,
            message,
            imageBase64,
            doctorName,
            hospitalName,
            date,
            offer,
            healthTip,
            appointmentDate,
            appointmentTime,
            false,
            recipients);

        return await ExecuteSendAsync(sendContext);
    }

    private async Task<(bool Success, string Message, SendWhatsAppMessageResultModel? Result)> ExecuteSendAsync(
        WhatsAppSendContext context,
        CancellationToken cancellationToken = default)
    {
        byte[]? imageBytes = null;
        string? imageMimeType = null;
        string? uploadedMediaId = null;

        if (!string.IsNullOrWhiteSpace(context.ImageBase64))
        {
            if (!WhatsAppMessageTemplateEngine.TryDecodeBase64Image(
                    context.ImageBase64,
                    out var decodedBytes,
                    out imageMimeType,
                    out var imageError))
            {
                return (false, imageError ?? "Invalid image data.", null);
            }

            if (decodedBytes.Length > MaxImageSizeBytes)
            {
                return (false, "Image size exceeds the maximum allowed limit of 5 MB.", null);
            }

            imageBytes = decodedBytes;
            var (uploadSuccess, mediaId, uploadError) = await _metaApiClient.UploadImageAsync(
                imageBytes,
                imageMimeType!,
                cancellationToken);

            if (!uploadSuccess)
            {
                return (false, uploadError ?? "Media upload failed.", null);
            }

            uploadedMediaId = mediaId;
        }

        var doctor = await _repository.GetActiveDoctorByIdAsync(context.DoctorId);
        var doctorDisplayName = string.IsNullOrWhiteSpace(context.DoctorName)
            ? BuildDoctorName(doctor)
            : context.DoctorName.Trim();

        var defaultTemplate = WhatsAppLanguageHelper.GetDefaultTemplateBody(
            context.MessageCategory,
            context.LanguageId,
            context.LanguageName);
        var result = new SendWhatsAppMessageResultModel { CampaignID = context.CampaignId };
        string? lastSuccessfulMessageId = null;

        foreach (var recipient in context.Recipients)
        {
            var placeholderContext = BuildPlaceholderContext(context, recipient, doctorDisplayName);
            string resolvedMessage;
            try
            {
                resolvedMessage = WhatsAppMessageTemplateEngine.ResolveTemplate(
                    context.TemplateBody,
                    placeholderContext,
                    defaultTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Template replacement failed for PatientID={PatientId}", recipient.PatientId);
                var templateError = $"Template replacement failed: {ex.Message}";
                await PersistLogAsync(context, recipient, context.TemplateBody, null, uploadedMediaId, false, null, templateError);
                result.TotalFailed++;
                result.Results.Add(BuildItemResult(recipient, false, null, templateError));
                continue;
            }

            var mobileNumber = WhatsAppMessageTemplateEngine.FormatForWhatsAppApi(
                recipient.ContactNumber,
                _metaOptions.DefaultCountryDialCode);

            if (string.IsNullOrWhiteSpace(mobileNumber))
            {
                const string numberError = "Contact number is missing.";
                await PersistLogAsync(context, recipient, context.TemplateBody, null, uploadedMediaId, false, null, numberError);
                result.TotalFailed++;
                result.Results.Add(BuildItemResult(recipient, false, null, numberError));
                continue;
            }

            var (sendSuccess, messageId, sendError) = await SendToMetaAsync(
                mobileNumber,
                resolvedMessage,
                uploadedMediaId,
                context.MetaTemplateName,
                context.MetaLanguageCode,
                placeholderContext,
                cancellationToken);

            await PersistLogAsync(
                context,
                recipient,
                context.TemplateBody,
                resolvedMessage,
                uploadedMediaId,
                sendSuccess,
                messageId,
                sendError);

            if (sendSuccess)
            {
                result.TotalSent++;
                lastSuccessfulMessageId = messageId;
                result.Results.Add(BuildItemResult(recipient, true, messageId, null));
            }
            else
            {
                result.TotalFailed++;
                result.Results.Add(BuildItemResult(recipient, false, null, sendError));
            }
        }

        result.MessageId = lastSuccessfulMessageId;

        if (result.TotalSent == 0)
        {
            var firstError = result.Results.FirstOrDefault()?.ErrorMessage;
            var failureMessage = string.IsNullOrWhiteSpace(firstError)
                ? "Failed to send WhatsApp message to all recipients."
                : $"Failed to send WhatsApp message: {firstError}";
            return (false, failureMessage, result);
        }

        if (result.TotalFailed > 0)
        {
            return (true, $"WhatsApp messages sent to {result.TotalSent} recipient(s). {result.TotalFailed} failed.", result);
        }

        return (true, "WhatsApp message sent successfully.", result);
    }

    private async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendToMetaAsync(
        string mobileNumber,
        string resolvedMessage,
        string? mediaId,
        string? metaTemplateName,
        string metaLanguageCode,
        WhatsAppPlaceholderContext placeholderContext,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(metaTemplateName))
        {
            var parameters = new List<string>
            {
                placeholderContext.PatientName,
                placeholderContext.HospitalName,
                placeholderContext.DoctorName,
                placeholderContext.Date,
                placeholderContext.Message
            };

            return await _metaApiClient.SendTemplateMessageAsync(
                mobileNumber,
                metaTemplateName,
                metaLanguageCode,
                parameters,
                cancellationToken);
        }

        if (mediaId == null)
        {
            return await _metaApiClient.SendTextMessageAsync(mobileNumber, resolvedMessage, cancellationToken);
        }

        return await _metaApiClient.SendImageMessageAsync(mobileNumber, mediaId, resolvedMessage, cancellationToken);
    }

    private async Task<string?> ValidateCommonAsync(int doctorId, int? templateId, int languageId)
    {
        if (!_metaOptions.IsConfigured())
        {
            return "WhatsApp Meta API configuration is missing or incomplete.";
        }

        if (await _repository.GetActiveDoctorByIdAsync(doctorId) == null)
        {
            return "Doctor not found or has been deleted.";
        }

        if (templateId.HasValue)
        {
            var template = await _repository.GetActiveTemplateByIdAsync(templateId.Value);
            if (template == null)
            {
                return "Template not found or is inactive.";
            }

            if (template.LanguageId != languageId)
            {
                return "Selected template does not match the chosen language.";
            }
        }

        return null;
    }

    private async Task<(string? Body, string? MetaTemplateName)> ResolveTemplateAsync(
        int? templateId,
        string category,
        string? messageBody,
        int languageId,
        string? languageName)
    {
        if (templateId.HasValue)
        {
            var template = await _repository.GetActiveTemplateByIdAsync(templateId.Value);
            if (template != null)
            {
                return (template.TemplateBody, template.MetaTemplateName);
            }
        }

        if (!string.IsNullOrWhiteSpace(messageBody))
        {
            return (messageBody, null);
        }

        var dbDefault = await _repository.GetDefaultActiveTemplateByCategoryAndLanguageAsync(category, languageId);
        if (dbDefault != null)
        {
            return (dbDefault.TemplateBody, dbDefault.MetaTemplateName);
        }

        return (WhatsAppLanguageHelper.GetDefaultTemplateBody(category, languageId, languageName), null);
    }

    private async Task<(int LanguageId, string? LanguageName, string? Error)> ResolveLanguageContextAsync(int? languageId)
    {
        if (languageId.HasValue && languageId.Value > 0)
        {
            if (!await _repository.LanguageExistsAsync(languageId.Value))
            {
                return (0, null, "Invalid language ID.");
            }

            var languageName = await _repository.GetLanguageNameByIdAsync(languageId.Value);
            return (languageId.Value, languageName, null);
        }

        var englishLanguageId = await _repository.GetDefaultEnglishLanguageIdAsync();
        var englishLanguageName = await _repository.GetLanguageNameByIdAsync(englishLanguageId);
        return (englishLanguageId, englishLanguageName, null);
    }

    private async Task<int> CreateCampaignRecordAsync(
        string campaignName,
        string category,
        int doctorId,
        string? messageBody,
        bool isBulk)
    {
        var campaign = new WhatsAppCampaign
        {
            CampaignName = campaignName,
            CampaignCategory = category,
            DoctorId = doctorId,
            MessageBody = messageBody,
            IsBulk = isBulk,
            EnteredDate = DateTime.Now,
            DeleteStatus = false
        };

        return await _repository.CreateCampaignAsync(campaign);
    }

    private static WhatsAppSendContext BuildSendContext(
        string messageCategory,
        int doctorId,
        int? campaignId,
        int? templateId,
        int languageId,
        string? languageName,
        string? templateBody,
        string? metaTemplateName,
        string? message,
        string? imageBase64,
        string? doctorName,
        string? hospitalName,
        string? date,
        string? offer,
        string? healthTip,
        string? appointmentDate,
        string? appointmentTime,
        bool isBulk,
        List<WhatsAppPatientRecipientModel> recipients)
    {
        return new WhatsAppSendContext
        {
            MessageCategory = messageCategory,
            DoctorId = doctorId,
            CampaignId = campaignId,
            TemplateId = templateId,
            LanguageId = languageId,
            LanguageName = languageName,
            MetaLanguageCode = WhatsAppLanguageHelper.GetMetaLanguageCode(languageId, languageName),
            TemplateBody = templateBody,
            MetaTemplateName = metaTemplateName,
            Message = message,
            ImageBase64 = imageBase64,
            DoctorName = doctorName,
            HospitalName = hospitalName,
            Date = date,
            Offer = offer,
            HealthTip = healthTip,
            AppointmentDate = appointmentDate,
            AppointmentTime = appointmentTime,
            IsBulk = isBulk,
            Recipients = recipients
        };
    }

    private static WhatsAppPlaceholderContext BuildPlaceholderContext(
        WhatsAppSendContext context,
        WhatsAppPatientRecipientModel recipient,
        string doctorDisplayName)
    {
        return new WhatsAppPlaceholderContext
        {
            PatientName = string.IsNullOrWhiteSpace(recipient.PatientName) ? "Patient" : recipient.PatientName!,
            HospitalName = string.IsNullOrWhiteSpace(context.HospitalName) ? "Homeo Centrum" : context.HospitalName.Trim(),
            DoctorName = doctorDisplayName,
            Date = WhatsAppMessageTemplateEngine.FormatDisplayDate(context.Date),
            Message = context.Message?.Trim() ?? string.Empty,
            Offer = context.Offer?.Trim() ?? string.Empty,
            HealthTip = context.HealthTip?.Trim() ?? string.Empty,
            AppointmentDate = WhatsAppMessageTemplateEngine.FormatDisplayDate(context.AppointmentDate),
            AppointmentTime = context.AppointmentTime?.Trim() ?? string.Empty
        };
    }

    private async Task PersistLogAsync(
        WhatsAppSendContext context,
        WhatsAppPatientRecipientModel recipient,
        string? templateMessage,
        string? finalMessage,
        string? mediaId,
        bool sendStatus,
        string? metaMessageId,
        string? errorMessage)
    {
        var log = new WhatsAppMessageLog
        {
            DoctorId = context.DoctorId,
            PatientId = recipient.PatientId,
            CampaignId = context.CampaignId,
            TemplateId = context.TemplateId,
            LanguageId = context.LanguageId,
            PatientName = recipient.PatientName,
            MobileNumber = recipient.ContactNumber,
            MessageCategory = context.MessageCategory,
            MessageBody = context.Message,
            TemplateMessage = templateMessage,
            FinalMessage = finalMessage,
            MetaMessageId = metaMessageId,
            MediaId = mediaId,
            IsBulk = context.IsBulk,
            SendStatus = sendStatus,
            ErrorMessage = errorMessage,
            CreatedDate = DateTime.Now,
            DeleteStatus = false
        };

        _repository.AddMessageLog(log);
        await _repository.SaveAllAsync();
    }

    private static SendWhatsAppMessageItemResultModel BuildItemResult(
        WhatsAppPatientRecipientModel recipient,
        bool success,
        string? messageId,
        string? errorMessage)
    {
        return new SendWhatsAppMessageItemResultModel
        {
            PatientID = recipient.PatientId,
            PatientName = recipient.PatientName,
            MobileNumber = recipient.ContactNumber,
            Success = success,
            MessageId = messageId,
            ErrorMessage = errorMessage
        };
    }

    private static string BuildDoctorName(Doctor? doctor)
    {
        if (doctor == null)
        {
            return "Doctor";
        }

        if (string.IsNullOrWhiteSpace(doctor.MiddleName))
        {
            return $"{doctor.FirstName} {doctor.LastName}".Trim();
        }

        return $"{doctor.FirstName} {doctor.MiddleName} {doctor.LastName}".Trim();
    }

    private sealed class WhatsAppSendContext
    {
        public string MessageCategory { get; set; } = null!;

        public int DoctorId { get; set; }

        public int? CampaignId { get; set; }

        public int? TemplateId { get; set; }

        public int LanguageId { get; set; }

        public string? LanguageName { get; set; }

        public string MetaLanguageCode { get; set; } = "en";

        public string? TemplateBody { get; set; }

        public string? MetaTemplateName { get; set; }

        public string? Message { get; set; }

        public string? ImageBase64 { get; set; }

        public string? DoctorName { get; set; }

        public string? HospitalName { get; set; }

        public string? Date { get; set; }

        public string? Offer { get; set; }

        public string? HealthTip { get; set; }

        public string? AppointmentDate { get; set; }

        public string? AppointmentTime { get; set; }

        public bool IsBulk { get; set; }

        public List<WhatsAppPatientRecipientModel> Recipients { get; set; } = new();
    }
}

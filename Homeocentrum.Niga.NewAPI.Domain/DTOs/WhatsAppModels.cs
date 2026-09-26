using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public class SendHospitalServiceMessageRequest : SendWhatsAppCategoryMessageRequest
{
    public bool Individual { get; set; }

    public bool Bulk { get; set; }

    public string? PatientContactNumber { get; set; }
}

public class SendIndividualWhatsAppMessageRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int DoctorID { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int PatientID { get; set; }

    public int? TemplateID { get; set; }

    public int? LanguageId { get; set; }

    public string? Message { get; set; }

    public string? ImageBase64 { get; set; }

    public string? DoctorName { get; set; }

    public string? HospitalName { get; set; }

    public string? Date { get; set; }

    public string? Offer { get; set; }

    public string? HealthTip { get; set; }

    public string? AppointmentDate { get; set; }

    public string? AppointmentTime { get; set; }
}

public class SendBulkWhatsAppMessageRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int DoctorID { get; set; }

    public int? CampaignID { get; set; }

    [Required]
    [MaxLength(200)]
    public string CampaignName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string MessageCategory { get; set; } = null!;

    public int? TemplateID { get; set; }

    public int? LanguageId { get; set; }

    public string? Message { get; set; }

    public string? ImageBase64 { get; set; }

    public string? DoctorName { get; set; }

    public string? HospitalName { get; set; }

    public string? Date { get; set; }

    public string? Offer { get; set; }

    public string? HealthTip { get; set; }
}

public class SendOfferMessageRequest : SendWhatsAppCategoryMessageRequest
{
    public bool Individual { get; set; }

    public bool Bulk { get; set; }

    public int? PatientID { get; set; }

    public string? PatientContactNumber { get; set; }

    public string? Offer { get; set; }
}

public class SendHealthTipMessageRequest : SendWhatsAppCategoryMessageRequest
{
    public bool Individual { get; set; }

    public bool Bulk { get; set; }

    public int? PatientID { get; set; }

    public string? PatientContactNumber { get; set; }

    public string? HealthTip { get; set; }
}

public class SendWhatsAppCategoryMessageRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "DoctorID is required.")]
    public int DoctorID { get; set; }

    public int? CampaignID { get; set; }

    public int? TemplateID { get; set; }

    public int? LanguageId { get; set; }

    public string? DoctorName { get; set; }

    public string? HospitalName { get; set; }

    public string? Date { get; set; }

    public string? PatientName { get; set; }

    public string? MessageBody { get; set; }

    public string? Message { get; set; }

    public string? ImageBase64 { get; set; }

    public string? Offer { get; set; }

    public string? HealthTip { get; set; }

    public string? AppointmentDate { get; set; }

    public string? AppointmentTime { get; set; }
}

public class SendWhatsAppMessageResultModel
{
    public string? MessageId { get; set; }

    public Guid? BulkJobId { get; set; }

    public int? CampaignID { get; set; }

    public int TotalQueued { get; set; }

    public int TotalSent { get; set; }

    public int TotalFailed { get; set; }

    public List<SendWhatsAppMessageItemResultModel> Results { get; set; } = new();
}

public class SendWhatsAppMessageItemResultModel
{
    public int? PatientID { get; set; }

    public string? PatientName { get; set; }

    public string? MobileNumber { get; set; }

    public bool Success { get; set; }

    public string? MessageId { get; set; }

    public string? ErrorMessage { get; set; }
}

public class GetWhatsAppMessageHistoryRequest
{
    public int? DoctorID { get; set; }

    public int? PatientID { get; set; }

    public int? CampaignID { get; set; }

    public string? MessageCategory { get; set; }

    public bool? SendStatus { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, PaginationRequestModel.MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;
}

public class GetWhatsAppCampaignHistoryRequest
{
    public int? DoctorID { get; set; }

    public string? CampaignCategory { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, PaginationRequestModel.MaxPageSize)]
    public int PageSize { get; set; } = 10;
}

public class GetWhatsAppDashboardRequest
{
    public int? DoctorID { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}

public class WhatsAppMessageHistoryItemModel
{
    public int WhatsAppMessageLogID { get; set; }

    public int? DoctorID { get; set; }

    public int? PatientID { get; set; }

    public int? CampaignID { get; set; }

    public int? TemplateID { get; set; }

    public int? LanguageId { get; set; }

    public string? LanguageName { get; set; }

    public string? PatientName { get; set; }

    public string? MobileNumber { get; set; }

    public string? MessageCategory { get; set; }

    public string? TemplateMessage { get; set; }

    public string? FinalMessage { get; set; }

    public string? MetaMessageId { get; set; }

    public string? MediaId { get; set; }

    public bool IsBulk { get; set; }

    public bool SendStatus { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class WhatsAppMessageDetailModel
{
    public int WhatsAppMessageLogID { get; set; }

    public int? DoctorID { get; set; }

    public int? PatientID { get; set; }

    public int? CampaignID { get; set; }

    public int? TemplateID { get; set; }

    public int? LanguageId { get; set; }

    public string? LanguageName { get; set; }

    public string? PatientName { get; set; }

    public string? MobileNumber { get; set; }

    public string? MessageCategory { get; set; }

    public string? MessageBody { get; set; }

    public string? TemplateMessage { get; set; }

    public string? FinalMessage { get; set; }

    public string? MetaMessageId { get; set; }

    public string? MediaId { get; set; }

    public bool IsBulk { get; set; }

    public bool SendStatus { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class WhatsAppCampaignHistoryItemModel
{
    public int CampaignID { get; set; }

    public string CampaignName { get; set; } = null!;

    public string CampaignCategory { get; set; } = null!;

    public int DoctorID { get; set; }

    public bool IsBulk { get; set; }

    public DateTime EnteredDate { get; set; }

    public int TotalMessages { get; set; }

    public int TotalDelivered { get; set; }

    public int TotalFailed { get; set; }
}

public class WhatsAppCampaignDetailModel
{
    public int CampaignID { get; set; }

    public string CampaignName { get; set; } = null!;

    public string CampaignCategory { get; set; } = null!;

    public int DoctorID { get; set; }

    public string? MessageBody { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsBulk { get; set; }

    public DateTime EnteredDate { get; set; }

    public int TotalMessages { get; set; }

    public int TotalDelivered { get; set; }

    public int TotalFailed { get; set; }

    public List<WhatsAppMessageHistoryItemModel> RecentMessages { get; set; } = new();
}

public class WhatsAppDashboardModel
{
    public int TotalMessages { get; set; }

    public int TotalDelivered { get; set; }

    public int TotalFailed { get; set; }

    public int TotalCampaigns { get; set; }

    public List<WhatsAppCategoryCountModel> MessagesByCategory { get; set; } = new();

    public List<WhatsAppDoctorCountModel> MessagesByDoctor { get; set; } = new();

    public List<WhatsAppMonthlyAnalyticsModel> MonthlyAnalytics { get; set; } = new();
}

public class WhatsAppCategoryCountModel
{
    public string Category { get; set; } = null!;

    public int Count { get; set; }

    public int Delivered { get; set; }

    public int Failed { get; set; }
}

public class WhatsAppDoctorCountModel
{
    public int DoctorID { get; set; }

    public int Count { get; set; }

    public int Delivered { get; set; }

    public int Failed { get; set; }
}

public class WhatsAppMonthlyAnalyticsModel
{
    public int Year { get; set; }

    public int Month { get; set; }

    public int TotalMessages { get; set; }

    public int Delivered { get; set; }

    public int Failed { get; set; }
}

public class WhatsAppPatientRecipientModel
{
    public int PatientId { get; set; }

    public string? PatientName { get; set; }

    public string? ContactNumber { get; set; }

    public bool IsWhatsAppOptIn { get; set; }
}

public class WhatsAppPlaceholderContext
{
    public string PatientName { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public string DoctorName { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Offer { get; set; } = string.Empty;

    public string HealthTip { get; set; } = string.Empty;

    public string AppointmentDate { get; set; } = string.Empty;

    public string AppointmentTime { get; set; } = string.Empty;
}

public class GetWhatsAppTemplatesRequest
{
    public string? TemplateCategory { get; set; }

    public int? LanguageId { get; set; }

    public bool? IsActive { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, PaginationRequestModel.MaxPageSize)]
    public int PageSize { get; set; } = 10;
}

public class WhatsAppTemplateListItemModel
{
    public int TemplateID { get; set; }

    public string TemplateName { get; set; } = null!;

    public string TemplateCategory { get; set; } = null!;

    public string? MetaTemplateName { get; set; }

    public int LanguageId { get; set; }

    public string? LanguageName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime EnteredDate { get; set; }

    public DateTime? ChangedDate { get; set; }
}

public class WhatsAppTemplateDetailModel
{
    public int TemplateID { get; set; }

    public string TemplateName { get; set; } = null!;

    public string TemplateCategory { get; set; } = null!;

    public string? MetaTemplateName { get; set; }

    public string TemplateBody { get; set; } = null!;

    public int LanguageId { get; set; }

    public string? LanguageName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }
}

public class AddWhatsAppTemplateRequest
{
    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string TemplateCategory { get; set; } = null!;

    [MaxLength(200)]
    public string? MetaTemplateName { get; set; }

    [Required]
    public string TemplateBody { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int? LanguageId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(50)]
    public string? EnteredBy { get; set; }
}

public class UpdateWhatsAppTemplateRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int TemplateID { get; set; }

    [Required]
    [MaxLength(200)]
    public string TemplateName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string TemplateCategory { get; set; } = null!;

    [MaxLength(200)]
    public string? MetaTemplateName { get; set; }

    [Required]
    public string TemplateBody { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int? LanguageId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(50)]
    public string? ChangedBy { get; set; }
}

public class WhatsAppBulkSendJob
{
    public Guid JobId { get; set; }

    public string MessageCategory { get; set; } = null!;

    public int DoctorId { get; set; }

    public int? CampaignId { get; set; }

    public int? TemplateId { get; set; }

    public int LanguageId { get; set; }

    public string? LanguageName { get; set; }

    public string? TemplateBody { get; set; }

    public string? MetaTemplateName { get; set; }

    public string? Message { get; set; }

    public string? DoctorName { get; set; }

    public string? HospitalName { get; set; }

    public string? Date { get; set; }

    public string? Offer { get; set; }

    public string? HealthTip { get; set; }

    public string? AppointmentDate { get; set; }

    public string? AppointmentTime { get; set; }

    public string? ImageBase64 { get; set; }

    public List<WhatsAppPatientRecipientModel> Recipients { get; set; } = new();
}

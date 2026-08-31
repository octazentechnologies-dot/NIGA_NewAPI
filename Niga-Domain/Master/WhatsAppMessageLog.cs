using System;

namespace Niga_Domain.Master;

public partial class WhatsAppMessageLog
{
    public int WhatsAppMessageLogId { get; set; }

    public int? DoctorId { get; set; }

    public int? PatientId { get; set; }

    public int? CampaignId { get; set; }

    public int? TemplateId { get; set; }

    public int? LanguageId { get; set; }

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

    public bool DeleteStatus { get; set; }

    public virtual LanguageMaster? Language { get; set; }
}

using System;

namespace Niga_Domain.Master;

public partial class WhatsAppTemplateMaster
{
    public int TemplateId { get; set; }

    public string TemplateName { get; set; } = null!;

    public string TemplateCategory { get; set; } = null!;

    public string? MetaTemplateName { get; set; }

    public string TemplateBody { get; set; } = null!;

    public int LanguageId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual LanguageMaster Language { get; set; } = null!;
}

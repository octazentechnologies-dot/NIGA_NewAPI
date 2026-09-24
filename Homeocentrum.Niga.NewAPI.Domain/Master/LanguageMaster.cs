using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class LanguageMaster
{
    public int LanguageId { get; set; }

    public string? LanguageName { get; set; }

    public string? Description { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<DemoDatum> DemoData { get; set; } = new List<DemoDatum>();

    public virtual ICollection<SubSectionLanguageDetail> SubSectionLanguageDetails { get; set; } = new List<SubSectionLanguageDetail>();

    public virtual ICollection<WhatsAppTemplateMaster> WhatsAppTemplateMasters { get; set; } = new List<WhatsAppTemplateMaster>();
}

using System;
using System.Collections.Generic;
using Niga_Domain.Entities;

namespace Niga_Domain.Master;

public partial class SubSectionMaster:AuditableEntities
{
    public int SubSectionId { get; set; }

    public int? SectionId { get; set; }

    public string? SubSectionName { get; set; }

    public string? SubSectionNameAlias { get; set; }

    public string? Description { get; set; }

    public bool DeleteStatus { get; set; }

    public int? ParentSubSectionId { get; set; }

    public int? BodyPartId { get; set; }

    public int? PartLocationId { get; set; }
    public bool? MainParentSubsection { get; set; }

    public virtual ICollection<CaseDetail> CaseDetails { get; set; } = new List<CaseDetail>();

    public virtual ICollection<ClinicalQueRubric> ClinicalQueRubrics { get; set; } = new List<ClinicalQueRubric>();

    public virtual ICollection<ClipboardRubric> ClipboardRubrics { get; set; } = new List<ClipboardRubric>();

    public virtual ICollection<DemoDatum> DemoData { get; set; } = new List<DemoDatum>();

    public virtual ICollection<DiagnosisDetail> DiagnosisDetails { get; set; } = new List<DiagnosisDetail>();

    public virtual ICollection<ReferenceRubricDetail> ReferenceRubricDetailRefSubSections { get; set; } = new List<ReferenceRubricDetail>();

    public virtual ICollection<ReferenceRubricDetail> ReferenceRubricDetailSubSections { get; set; } = new List<ReferenceRubricDetail>();

    public virtual ICollection<RubricRemedyDetail> RubricRemedyDetails { get; set; } = new List<RubricRemedyDetail>();

    public virtual SectionMaster? Section { get; set; }

    public virtual ICollection<SubSectionLanguageDetail> SubSectionLanguageDetails { get; set; } = new List<SubSectionLanguageDetail>();
}

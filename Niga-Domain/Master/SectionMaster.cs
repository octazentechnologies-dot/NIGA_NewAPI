using System;
using System.Collections.Generic;
using Niga_Domain.Entities;

namespace Niga_Domain.Master;

public partial class SectionMaster:AuditableEntities
{
    public int SectionId { get; set; }

    public int? BodyPartSectionId { get; set; }

    public string SectionName { get; set; } = null!;

    public string? SectionAlias { get; set; }

    public string? Description { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<BodyPartMaster> BodyPartMasters { get; set; } = new List<BodyPartMaster>();

    public virtual BodyPartSectionMaster? BodyPartSection { get; set; }

    public virtual ICollection<QuestionGroupMaster> QuestionGroupMasters { get; set; } = new List<QuestionGroupMaster>();

    public virtual ICollection<SubSectionMaster> SubSectionMasters { get; set; } = new List<SubSectionMaster>();

    public virtual ICollection<TypeofSymptomsMaster> TypeofSymptomsMasters { get; set; } = new List<TypeofSymptomsMaster>();
}

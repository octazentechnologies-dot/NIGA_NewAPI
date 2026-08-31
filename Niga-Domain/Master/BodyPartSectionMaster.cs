using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class BodyPartSectionMaster
{
    public int BodyPartSectionId { get; set; }

    public string BodyPartSectionName { get; set; } = null!;

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<SectionMaster> SectionMasters { get; set; } = new List<SectionMaster>();
}

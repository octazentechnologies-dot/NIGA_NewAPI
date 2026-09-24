using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class SectionGroupMaster
{
    public int SectionGroupId { get; set; }

    public string SectionGroupName { get; set; } = null!;

    public string? Description { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<TypeofSymptomsMaster> TypeofSymptomsMasters { get; set; } = new List<TypeofSymptomsMaster>();
}

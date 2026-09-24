using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class TypeofSymptomsGroupMaster
{
    public int TypeofSymptomsGroupId { get; set; }

    public string TypeofSymptomsGroupName { get; set; } = null!;

    public string? Description { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<TypeofSymptomsMaster> TypeofSymptomsMasters { get; set; } = new List<TypeofSymptomsMaster>();
}

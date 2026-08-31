using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DrugSystemMaster
{
    public int DrugSystemId { get; set; }

    public string? DrugSystemName { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual ICollection<DrugGroupMaster> DrugGroupMasters { get; set; } = new List<DrugGroupMaster>();
}

using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DrugGroupMaster
{
    public int DrugGroupId { get; set; }

    public int DrugSystemId { get; set; }

    public string DrugGroupName { get; set; } = null!;

    public bool? DeleteStatus { get; set; }

    public virtual ICollection<AllopathicDrugMaster> AllopathicDrugMasters { get; set; } = new List<AllopathicDrugMaster>();

    public virtual DrugSystemMaster DrugSystem { get; set; } = null!;
}

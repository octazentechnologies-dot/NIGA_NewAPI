using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class AllopathicDrugMaster
{
    public int AllopathicDrugId { get; set; }

    public int DrugGroupId { get; set; }

    public string AllopathicDrugName { get; set; } = null!;

    public bool? DeleteStatus { get; set; }

    public virtual ICollection<AdverseReactionMaster> AdverseReactionMasters { get; set; } = new List<AdverseReactionMaster>();

    public virtual DrugGroupMaster DrugGroup { get; set; } = null!;

    public virtual ICollection<OtherSideEffectMaster> OtherSideEffectMasters { get; set; } = new List<OtherSideEffectMaster>();

    public virtual ICollection<SeriousSideEffectMaster> SeriousSideEffectMasters { get; set; } = new List<SeriousSideEffectMaster>();
}

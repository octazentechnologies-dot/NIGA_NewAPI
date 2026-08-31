using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class OtherSideEffectMaster
{
    public int OtherSideEffectId { get; set; }

    public string OtherSideEffectName { get; set; } = null!;

    public int AllopathicDrugId { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual AllopathicDrugMaster AllopathicDrug { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class SeriousSideEffectMaster
{
    public int SeriousSideEffectId { get; set; }

    public string SeriousSideEffectName { get; set; } = null!;

    public int AllopathicDrugId { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual AllopathicDrugMaster AllopathicDrug { get; set; } = null!;
}

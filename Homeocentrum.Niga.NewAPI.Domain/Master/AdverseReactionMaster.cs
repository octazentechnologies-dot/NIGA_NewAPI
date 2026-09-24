using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AdverseReactionMaster
{
    public int AdverseReactionId { get; set; }

    public string AdverseReactionName { get; set; } = null!;

    public int AllopathicDrugId { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual AllopathicDrugMaster AllopathicDrug { get; set; } = null!;
}

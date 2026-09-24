using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class LocationExtentionRubricDetail
{
    public int LocationExtentionRubricDetailsId { get; set; }

    public int LocationExtentionDetailsId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual LocationExtentionDetail LocationExtentionDetails { get; set; } = null!;
}

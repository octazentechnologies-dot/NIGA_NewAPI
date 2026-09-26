using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class BeforeAfterDuringRubricDetail
{
    public int BeforeAfterDuringRubricDetailsId { get; set; }

    public int BeforeAfterDuringDetailsId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual BeforeAfterDuringDetail BeforeAfterDuringDetails { get; set; } = null!;
}

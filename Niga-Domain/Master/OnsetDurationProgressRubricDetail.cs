using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class OnsetDurationProgressRubricDetail
{
    public int OnsetRubricId { get; set; }

    public int OnsetDetailId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual OnsetDurationProgressDetail OnsetDetail { get; set; } = null!;
}

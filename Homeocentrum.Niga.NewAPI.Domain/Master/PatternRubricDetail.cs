using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PatternRubricDetail
{
    public int PatternRubricDetailsId { get; set; }

    public int PatternDetailsId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual PatternsDetail PatternDetails { get; set; } = null!;
}

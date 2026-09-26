using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class ObservationsRubricDetail
{
    public int ObservationsRubricDetailsId { get; set; }

    public int ObservationsDetailsId { get; set; }

    public int Subsection { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual ObservationsDetail ObservationsDetails { get; set; } = null!;
}

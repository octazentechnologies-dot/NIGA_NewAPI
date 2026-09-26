using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisCausationRubricDetail
{
    public int CausationRubricDetailsId { get; set; }

    public int CausationId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisCausation Causation { get; set; } = null!;
}

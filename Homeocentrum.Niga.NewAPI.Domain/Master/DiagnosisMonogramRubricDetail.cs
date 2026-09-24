using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisMonogramRubricDetail
{
    public int DiagnosisMonogramRubricDetailsId { get; set; }

    public int DiagnosisMonogramDetailsId { get; set; }

    public int Subsections { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMonogramDetail DiagnosisMonogramDetails { get; set; } = null!;
}

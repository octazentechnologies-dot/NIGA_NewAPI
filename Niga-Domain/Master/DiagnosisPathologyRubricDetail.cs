using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisPathologyRubricDetail
{
    public int DiagnosisPathologyRubricDetailsId { get; set; }

    public int DiagnosisPathologyDetailsId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisPathologyDetail DiagnosisPathologyDetails { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisPathologyDetail
{
    public int DiagnosisPathologyDetailsId { get; set; }

    public string DiagnosisPathologyKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<DiagnosisPathologyRubricDetail> DiagnosisPathologyRubricDetails { get; set; } = new List<DiagnosisPathologyRubricDetail>();
}

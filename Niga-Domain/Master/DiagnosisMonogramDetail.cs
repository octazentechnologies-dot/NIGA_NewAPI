using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisMonogramDetail
{
    public int DiagnosisMonogramDetailsId { get; set; }

    public string DiagnosisMonogramKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<DiagnosisMonogramRubricDetail> DiagnosisMonogramRubricDetails { get; set; } = new List<DiagnosisMonogramRubricDetail>();
}

using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisCausation
{
    public int CausationId { get; set; }

    public int? DiagnosisId { get; set; }

    public string? CausationName { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster? Diagnosis { get; set; }

    public virtual ICollection<DiagnosisCausationRubricDetail> DiagnosisCausationRubricDetails { get; set; } = new List<DiagnosisCausationRubricDetail>();
}

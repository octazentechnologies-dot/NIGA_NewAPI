using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class PatternsDetail
{
    public int PatternDetailsId { get; set; }

    public string PatternsKeywords { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<PatternRubricDetail> PatternRubricDetails { get; set; } = new List<PatternRubricDetail>();
}

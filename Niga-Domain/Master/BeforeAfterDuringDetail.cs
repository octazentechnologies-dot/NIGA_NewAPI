using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class BeforeAfterDuringDetail
{
    public int BeforeAfterDuringDetailsId { get; set; }

    public string BeforeAfterDuringDetailsKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual ICollection<BeforeAfterDuringRubricDetail> BeforeAfterDuringRubricDetails { get; set; } = new List<BeforeAfterDuringRubricDetail>();

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;
}

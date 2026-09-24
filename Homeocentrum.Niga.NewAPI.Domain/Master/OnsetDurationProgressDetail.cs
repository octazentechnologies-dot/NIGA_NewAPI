using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class OnsetDurationProgressDetail
{
    public int OnsetDetailId { get; set; }

    public string OnsetKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<OnsetDurationProgressRubricDetail> OnsetDurationProgressRubricDetails { get; set; } = new List<OnsetDurationProgressRubricDetail>();
}

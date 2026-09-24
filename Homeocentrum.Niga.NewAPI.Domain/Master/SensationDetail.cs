using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class SensationDetail
{
    public int SensationDetailsId { get; set; }

    public string SensationDetailsKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<SensationRubricDetail> SensationRubricDetails { get; set; } = new List<SensationRubricDetail>();
}

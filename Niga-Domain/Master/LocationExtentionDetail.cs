using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class LocationExtentionDetail
{
    public int LocationExtentionDetailsId { get; set; }

    public string LocationExtentionDetailsKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<LocationExtentionRubricDetail> LocationExtentionRubricDetails { get; set; } = new List<LocationExtentionRubricDetail>();
}

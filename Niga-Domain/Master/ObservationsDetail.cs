using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ObservationsDetail
{
    public int ObservationsDetailsId { get; set; }

    public string ObservationsDetailsKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<ObservationsRubricDetail> ObservationsRubricDetails { get; set; } = new List<ObservationsRubricDetail>();
}

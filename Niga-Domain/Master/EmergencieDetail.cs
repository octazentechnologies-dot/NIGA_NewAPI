using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class EmergencieDetail
{
    public int EmergencieId { get; set; }

    public string? EmergencieKeyword { get; set; }

    public int? DiagnosisId { get; set; }

    public bool DeletedStatus { get; set; }

    public virtual DiagnosisMaster? Diagnosis { get; set; }

    public virtual ICollection<EmergencieRubricDetail> EmergencieRubricDetails { get; set; } = new List<EmergencieRubricDetail>();
}

using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AccompaniedDetail
{
    public int AccompaniedDetailsId { get; set; }

    public string AccompaniedDetailsSystem { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual ICollection<AccompaniedRubricDetail> AccompaniedRubricDetails { get; set; } = new List<AccompaniedRubricDetail>();

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;
}

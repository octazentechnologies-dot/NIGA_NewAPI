using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisMonogram
{
    public int DiagnosisMonogramId { get; set; }

    public int? MonogramId { get; set; }

    public int? DiagnosisId { get; set; }

    public virtual DiagnosisMaster? Diagnosis { get; set; }

    public virtual Monogram? Monogram { get; set; }
}

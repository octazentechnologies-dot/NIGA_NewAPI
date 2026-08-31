using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisSystemDetail
{
    public int DiagnosisSystemDetailId { get; set; }

    public int DiagnosisId { get; set; }

    public int DiagnosisSystemId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual DiagnosisSystem DiagnosisSystem { get; set; } = null!;
}

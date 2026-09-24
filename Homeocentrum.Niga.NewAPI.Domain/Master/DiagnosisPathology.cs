using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisPathology
{
    public int DiagnosisPathologyId { get; set; }

    public int? DiagnosisId { get; set; }

    public int? PathologyId { get; set; }

    public virtual DiagnosisMaster? Diagnosis { get; set; }

    public virtual Pathology? Pathology { get; set; }
}

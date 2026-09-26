using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisDetail
{
    public int DiagnosisDetailId { get; set; }

    public int? DiagnosisId { get; set; }

    public int? SubSectionId { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual DiagnosisMaster? Diagnosis { get; set; }

    public virtual SubSectionMaster? SubSection { get; set; }
}

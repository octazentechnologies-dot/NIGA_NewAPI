using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class CaseDetail
{
    public int CaseDetailId { get; set; }

    public int? CaseId { get; set; }

    public int? SubsectionId { get; set; }

    public int? IntensityId { get; set; }

    public int? RemedyCount { get; set; }

    public virtual IntensityMaster? Intensity { get; set; }

    public virtual SubSectionMaster? Subsection { get; set; }
}

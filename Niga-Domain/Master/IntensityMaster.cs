using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class IntensityMaster
{
    public int IntensityId { get; set; }

    public int IntensityNo { get; set; }

    public string? Description { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<CaseDetail> CaseDetails { get; set; } = new List<CaseDetail>();
}

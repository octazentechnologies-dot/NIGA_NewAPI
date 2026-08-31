using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class YearMaster
{
    public int YearId { get; set; }

    public int FirmId { get; set; }

    public string DisplayYear { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? YearType { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual FirmDetail Firm { get; set; } = null!;
}

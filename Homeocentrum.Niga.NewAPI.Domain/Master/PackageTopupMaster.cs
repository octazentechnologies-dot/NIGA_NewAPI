using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PackageTopupMaster
{
    public int PackageTopupId { get; set; }

    public string PackageTopupName { get; set; } = null!;

    public int CaseCount { get; set; }

    public decimal Amount { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }
}

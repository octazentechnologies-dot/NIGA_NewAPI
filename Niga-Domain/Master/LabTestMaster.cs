using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class LabTestMaster
{
    public int TestId { get; set; }

    public string? TestName { get; set; }

    public string? TestAlias { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }
}

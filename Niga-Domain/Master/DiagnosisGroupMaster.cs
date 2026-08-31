using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisGroupMaster
{
    public int DiagnosisGroupId { get; set; }

    public string DiagnosisGroupName { get; set; } = null!;

    public string? Description { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }
}

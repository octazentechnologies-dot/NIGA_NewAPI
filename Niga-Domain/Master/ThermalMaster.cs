using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ThermalMaster
{
    public int ThermalId { get; set; }

    public string? ThermalName { get; set; }

    public string? Color { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual ICollection<RemedyMaster> RemedyMasters { get; set; } = new List<RemedyMaster>();
}

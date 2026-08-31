using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ModuleMaster
{
    public int ModuleId { get; set; }

    public string ModuleName { get; set; } = null!;

    public string ModuleMarathiName { get; set; } = null!;

    public string? ModuleIcon { get; set; }

    public string? ModuleAreaName { get; set; }

    public int Seqno { get; set; }

    public bool IsDirectNode { get; set; }

    public string? ActionName { get; set; }

    public string? ControllerName { get; set; }

    public string? ModuleUrl { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<MenuMaster> MenuMasters { get; set; } = new List<MenuMaster>();
}

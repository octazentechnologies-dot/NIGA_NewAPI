using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class MenuMaster
{
    public int MenuId { get; set; }

    public int ModuleId { get; set; }

    public string MenuName { get; set; } = null!;

    public string MenuNameMarathi { get; set; } = null!;

    public string MenuType { get; set; } = null!;

    public int? ParentMenuId { get; set; }

    public string? MenuUrl { get; set; }

    public string? Description { get; set; }

    public string MenuIcon { get; set; } = null!;

    public string? ActionName { get; set; }

    public string? ControllerName { get; set; }

    public bool IsLeaf { get; set; }

    public bool ShowInMainMenu { get; set; }

    public int? SeqNo { get; set; }

    public string FirmIds { get; set; } = null!;

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ModuleMaster Module { get; set; } = null!;

    public virtual ICollection<ReportSetting> ReportSettings { get; set; } = new List<ReportSetting>();

    public virtual ICollection<RoleDetail> RoleDetails { get; set; } = new List<RoleDetail>();

    public virtual ICollection<SearchPageSetting> SearchPageSettings { get; set; } = new List<SearchPageSetting>();

    public virtual ICollection<UserDetail> UserDetails { get; set; } = new List<UserDetail>();
}

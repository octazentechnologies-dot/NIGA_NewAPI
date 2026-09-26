using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RoleDetail
{
    public int RecordId { get; set; }

    public int RoleId { get; set; }

    public int MenuId { get; set; }

    public bool IsView { get; set; }

    public bool IsAdd { get; set; }

    public bool IsModify { get; set; }

    public bool IsDelete { get; set; }

    public virtual MenuMaster Menu { get; set; } = null!;

    public virtual RoleMaster Role { get; set; } = null!;
}

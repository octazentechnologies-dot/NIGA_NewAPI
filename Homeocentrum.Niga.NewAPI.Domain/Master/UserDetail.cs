using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class UserDetail
{
    public int RecordId { get; set; }

    public long UserId { get; set; }

    public int MenuId { get; set; }

    public bool? IsView { get; set; }

    public bool? IsAdd { get; set; }

    public bool? IsModify { get; set; }

    public bool? IsDelete { get; set; }

    public int? FirmId { get; set; }

    public virtual FirmDetail? Firm { get; set; }

    public virtual MenuMaster Menu { get; set; } = null!;
}

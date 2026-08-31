using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class UserLoginStatus
{
    public int LoginId { get; set; }

    public DateTime LogDate { get; set; }

    public long UserId { get; set; }

    public string MachineNo { get; set; } = null!;

    public DateTime InTime { get; set; }

    public DateTime? OutTime { get; set; }

    public bool Satus { get; set; }
}

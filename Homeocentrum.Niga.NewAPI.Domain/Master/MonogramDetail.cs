using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class MonogramDetail
{
    public int MonogramDetailId { get; set; }

    public int? MonogramId { get; set; }

    public int? SubsectionId { get; set; }

    public bool? IsDelete { get; set; }

    public virtual Monogram? Monogram { get; set; }
}

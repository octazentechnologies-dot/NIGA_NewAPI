using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class AccompaniedRubricDetail
{
    public int AccompaniedRubricDetailsId { get; set; }

    public int AccompaniedDetailsId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual AccompaniedDetail AccompaniedDetails { get; set; } = null!;
}

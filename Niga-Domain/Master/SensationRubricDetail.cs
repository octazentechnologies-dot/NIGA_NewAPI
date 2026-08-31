using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class SensationRubricDetail
{
    public int SensationRubricDetailsId { get; set; }

    public int SensationDetailsId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual SensationDetail SensationDetails { get; set; } = null!;
}

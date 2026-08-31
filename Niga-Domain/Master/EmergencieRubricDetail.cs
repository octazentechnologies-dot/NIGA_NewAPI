using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class EmergencieRubricDetail
{
    public int EmergencieRubricId { get; set; }

    public int EmergencieId { get; set; }

    public int? SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual EmergencieDetail Emergencie { get; set; } = null!;
}

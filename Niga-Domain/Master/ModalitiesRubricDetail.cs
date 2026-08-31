using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ModalitiesRubricDetail
{
    public int ModalitiesRubricDetailsId { get; set; }

    public int ModalitiesDetailsId { get; set; }

    public int? SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual ModalitiesDetail ModalitiesDetails { get; set; } = null!;
}

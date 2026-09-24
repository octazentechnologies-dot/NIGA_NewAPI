using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class ModalitiesDetail
{
    public int ModalitiesDetailsId { get; set; }

    public string ModalitiesDetailsKeyword { get; set; } = null!;

    public int DiagnosisId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;

    public virtual ICollection<ModalitiesRubricDetail> ModalitiesRubricDetails { get; set; } = new List<ModalitiesRubricDetail>();
}

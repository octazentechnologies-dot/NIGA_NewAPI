using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class ClinicalQueRubric
{
    public int ClinicalQueRubricId { get; set; }

    public int? SubsectionId { get; set; }

    public bool? IsDeleted { get; set; }

    public int? ClinicalQuestionBodyPartId { get; set; }

    public int? ClinicalQueKeywordId { get; set; }

    public virtual SubSectionMaster? Subsection { get; set; }
}

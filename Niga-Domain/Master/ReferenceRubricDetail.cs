using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ReferenceRubricDetail
{
    public int ReferenceRubricId { get; set; }

    public int? SubSectionId { get; set; }

    public int? RefSubSectionId { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual SubSectionMaster? RefSubSection { get; set; }

    public virtual SubSectionMaster? SubSection { get; set; }
}

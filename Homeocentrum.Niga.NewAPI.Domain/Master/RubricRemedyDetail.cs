using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RubricRemedyDetail
{
    public int RubricRemedyId { get; set; }

    public int? SubSectionId { get; set; }

    public int? RemedyId { get; set; }

    public int? GradeId { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public bool? DeletedStatus { get; set; }

    public bool? IsConfirmationRubric { get; set; }

    public bool? IsSmallRubric { get; set; }

    public virtual RemedyGradeMaster? Grade { get; set; }

    public virtual RemedyMaster? Remedy { get; set; }

    public virtual ICollection<RemedyRubricAuthorDetail> RemedyRubricAuthorDetails { get; set; } = new List<RemedyRubricAuthorDetail>();

    public virtual SubSectionMaster? SubSection { get; set; }
}

using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RemedyGradeMaster:AuditableEntities
{
    public int GradeId { get; set; }

    public int GradeNo { get; set; }

    public string? Description { get; set; }

    public string FontName { get; set; } = null!;

    public string FontStyle { get; set; } = null!;

    public string FontColor { get; set; } = null!;

    public bool DeleteStatus { get; set; }

    public virtual ICollection<RubricRemedyDetail> RubricRemedyDetails { get; set; } = new List<RubricRemedyDetail>();
}

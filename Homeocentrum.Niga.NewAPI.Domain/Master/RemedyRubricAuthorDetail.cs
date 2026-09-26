using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RemedyRubricAuthorDetail
{
    public int RemedyRubricAuthorId { get; set; }

    public int? RubricRemedyId { get; set; }

    public int? AuthorId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual AuthorMaster? Author { get; set; }

    public virtual RubricRemedyDetail? RubricRemedy { get; set; }
}

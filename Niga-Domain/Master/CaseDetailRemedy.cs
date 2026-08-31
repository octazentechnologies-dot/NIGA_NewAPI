using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class CaseDetailRemedy
{
    public int CaseDetailRemedyId { get; set; }

    public int? CaseId { get; set; }

    public int? RemedyId { get; set; }

    public int? RemedyIndex { get; set; }

    public virtual CaseEntryDetail? Case { get; set; }
}

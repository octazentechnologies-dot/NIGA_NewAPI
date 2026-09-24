using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class CaseEntryChiefComplaint
{
    public int CaseChiefComplaintId { get; set; }

    public int? CaseId { get; set; }

    public string? ChiefComplaintName { get; set; }

    public string? CreatedByRole { get; set; }

    public virtual CaseEntryDetail? Case { get; set; }
}

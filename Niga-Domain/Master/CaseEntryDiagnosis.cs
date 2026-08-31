using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class CaseEntryDiagnosis
{
    public int CaseDiagnosisId { get; set; }

    public int CaseId { get; set; }

    public int DiagnosisId { get; set; }

    public virtual CaseEntryDetail Case { get; set; } = null!;

    public virtual DiagnosisMaster Diagnosis { get; set; } = null!;
}

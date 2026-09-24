using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisSymptomRubric
{
    public int DiagnosisSymptomRubricId { get; set; }

    public int DiagnosisSymptomId { get; set; }

    public int SubsectionId { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisSymptom DiagnosisSymptom { get; set; } = null!;
}

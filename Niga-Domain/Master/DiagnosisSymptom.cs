using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DiagnosisSymptom
{
    public int DiagnosisSymptomId { get; set; }

    public int? DiagnosisId { get; set; }

    public string? Symptom { get; set; }

    public int? EnteredBy { get; set; }

    public bool? DeletedStatus { get; set; }

    public virtual DiagnosisMaster? Diagnosis { get; set; }

    public virtual ICollection<DiagnosisSymptomRubric> DiagnosisSymptomRubrics { get; set; } = new List<DiagnosisSymptomRubric>();
}

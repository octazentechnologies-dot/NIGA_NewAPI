using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisSymptomsModel
    {
        public DiagnosisSymptomsModel()
        {
            DiagnosisSymptomRubric = new List<DiagnosisSymptomRubricModel>();
        }
        public int DiagnosisSymptomId { get; set; }
        public int? DiagnosisId { get; set; }
        public string Symptom { get; set; }
        public int? EnteredBy { get; set; }
        public List<DiagnosisSymptomRubricModel> DiagnosisSymptomRubric { get; set; }
    }
}

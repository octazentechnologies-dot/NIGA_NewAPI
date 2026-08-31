using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisCausationModel
    {
        public DiagnosisCausationModel()
        {
            DiagnosisCausationRubricDetails = new List<DiagnosisCausationRubricDetailsModel>();
        }

        public int CausationId { get; set; }
        public int? DiagnosisId { get; set; }
        public string CausationName { get; set; }

        public List<DiagnosisCausationRubricDetailsModel> DiagnosisCausationRubricDetails { get; set; }
    }
}

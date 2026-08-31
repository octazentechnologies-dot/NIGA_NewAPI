using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class EmergencieDetailsModel
    {
        public EmergencieDetailsModel()
        {
            EmergencieRubricDetails = new List<EmergencieRubricDetailsModel>();
        }
        public int EmergencieId { get; set; }
        public string EmergencieKeyword { get; set; }
        public int? DiagnosisId { get; set; }
        public bool DeletedStatus { get; set; }

        public List<EmergencieRubricDetailsModel> EmergencieRubricDetails { get; set; }
    }
}

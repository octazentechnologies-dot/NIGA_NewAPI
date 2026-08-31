using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class SensationDetailsModel
    {
        public SensationDetailsModel()
        {
            SensationRubricDetails = new List<SensationRubricDetailsModel>();
        }

        public int SensationDetailsId { get; set; }
        public string SensationDetailsKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }
      
        public virtual List<SensationRubricDetailsModel> SensationRubricDetails { get; set; }
    }
}

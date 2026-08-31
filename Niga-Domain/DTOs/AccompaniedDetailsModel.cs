using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class AccompaniedDetailsModel
    {
        public AccompaniedDetailsModel()
        {
            AccompaniedRubricDetails = new List<AccompaniedRubricDetailsModel>();
        }

        public int AccompaniedDetailsId { get; set; }
        public string AccompaniedDetailsSystem { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public  List<AccompaniedRubricDetailsModel> AccompaniedRubricDetails { get; set; }
    }
}

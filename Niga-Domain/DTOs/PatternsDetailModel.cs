using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class PatternsDetailModel
    {
        public PatternsDetailModel()
        {
            PatternRubricDetails = new List<PatternRubricDetailsModel>();
        }

        public int PatternDetailsId { get; set; }
        public string PatternsKeywords { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public virtual List<PatternRubricDetailsModel> PatternRubricDetails { get; set; }
    }
}

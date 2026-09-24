using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class OnsetDurationProgressDetailsModel
    {
        public OnsetDurationProgressDetailsModel() {
            OnsetDurationProgressRubricDetails = new List<OnsetDurationProgressRubricDetailsModel>();
        }
        public int OnsetDetailId { get; set; }
        public string OnsetKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public List<OnsetDurationProgressRubricDetailsModel> OnsetDurationProgressRubricDetails { get; set; }

    }
}

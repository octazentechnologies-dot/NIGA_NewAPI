using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class BeforeAfterDuringDetailsModel
    {
        public BeforeAfterDuringDetailsModel()
        {
            BeforeAfterDuringRubricDetails = new List<BeforeAfterDuringRubricDetailsModel>();
        }

        public int BeforeAfterDuringDetailsId { get; set; }
        public string BeforeAfterDuringDetailsKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public List<BeforeAfterDuringRubricDetailsModel> BeforeAfterDuringRubricDetails { get; set; }
        public List<int>? SectionIds { get; set; }
        public List<SectionViewModel>? Sections { get; set; }
    }
}

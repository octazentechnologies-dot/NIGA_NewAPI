using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class DiagnosisPathologyDetailsModel
    {
        public DiagnosisPathologyDetailsModel()
        {
            DiagnosisPathologyRubricDetails = new List<DiagnosisPathologyRubricDetailsModel>();
        }

        public int DiagnosisPathologyDetailsId { get; set; }
        public string DiagnosisPathologyKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public List<DiagnosisPathologyRubricDetailsModel> DiagnosisPathologyRubricDetails { get; set; }
        public List<int>? SectionIds { get; set; }
        public List<SectionViewModel>? Sections { get; set; }
    }
}

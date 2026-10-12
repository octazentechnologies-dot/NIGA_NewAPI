using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class DiagnosisMonogramDetailsModel
    {
        public DiagnosisMonogramDetailsModel()
        {
            DiagnosisMonogramRubricDetails = new List<DiagnosisMonogramRubricDetailsModel>();
        }

        public int DiagnosisMonogramDetailsId { get; set; }
        public string DiagnosisMonogramKeyword { get; set; }
        public int DiagnosisId { get; set; }
        public bool? DeletedStatus { get; set; }

        public List<DiagnosisMonogramRubricDetailsModel> DiagnosisMonogramRubricDetails { get; set; }
        public List<int>? SectionIds { get; set; }
        public List<SectionViewModel>? Sections { get; set; }
    }
}

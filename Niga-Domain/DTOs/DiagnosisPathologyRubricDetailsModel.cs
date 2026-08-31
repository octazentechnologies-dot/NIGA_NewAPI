using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisPathologyRubricDetailsModel
    {
        public int DiagnosisPathologyRubricDetailsId { get; set; }
        public int DiagnosisPathologyDetailsId { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public int SubsectionId { get; set; }
        public string SubsectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}

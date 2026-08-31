using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class PatternRubricDetailsModel
    {
        public int PatternRubricDetailsId { get; set; }
        public int PatternDetailsId { get; set; }
        public int SubsectionId { get; set; }
        public string SubsectionName { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}

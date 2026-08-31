using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class RubricModel
    {
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int RubricRemedyId { get; set; }
        public int SubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public int Grade { get; set; }
       

        public List<SubSectionRemedy> subSectionRemedies { get; set; }
    }

    public class SubSectionRemedy
    {
        public int RemedyId { get; set; }
        public string RemedyName { get; set; }
    }
}

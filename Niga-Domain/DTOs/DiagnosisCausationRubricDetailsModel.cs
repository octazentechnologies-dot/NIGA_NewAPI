using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisCausationRubricDetailsModel
    {
        public int CausationRubricDetailsId { get; set; }
        public int CausationId { get; set; }
        public int SectionId { get; set; }
        public int SubsectionId { get; set; }
        public string SectionName { get; set; }
        public string SubsectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}

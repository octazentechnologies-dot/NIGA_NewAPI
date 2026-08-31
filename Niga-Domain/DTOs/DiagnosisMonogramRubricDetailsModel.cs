using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisMonogramRubricDetailsModel
    {
        public int DiagnosisMonogramRubricDetailsId { get; set; }
        public int DiagnosisMonogramDetailsId { get; set; }
        public int SectionsId{ get; set; }
        public int Subsections { get; set; }
        public string SectionsName{ get; set; }
        public string SubsectionName { get; set; }
        public bool? DeletedStatus { get; set; }
    }
}

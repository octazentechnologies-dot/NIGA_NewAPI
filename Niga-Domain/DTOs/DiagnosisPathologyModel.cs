using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisPathologyModel
    {
        public int DiagnosisPathologyId { get; set; }
        public int DiagnosisId { get; set; }
        public int PathologyId { get; set; }
        public string PathologyName { get; set; }
        public string Description { get; set; }
    }
}

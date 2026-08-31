using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DianosisDetailModel
    {
        public int DiagnosisDetailId { get; set; }
        public int? DiagnosisId { get; set; }
        public int? SubSectionId { get; set; }
        public bool? DeleteStatus { get; set; }

    }
}

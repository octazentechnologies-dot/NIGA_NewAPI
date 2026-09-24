using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class DiagnosisTherapeuticsDetailModel
    {
        public int DiagnosisTherapeuticsDetailId { get; set; }
        public int DiagnosisId { get; set; }
        public string DiagnosisName { get; set; }
        public string DiagnosisTherapeuticsDetail1 { get; set; }
        public bool? DeletedStatus { get; set; }

    }
}

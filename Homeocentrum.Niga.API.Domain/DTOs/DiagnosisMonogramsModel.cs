using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class DiagnosisMonogramsModel
    {
        public int DiagnosisMonogramId { get; set; }
        public int? MonogramId { get; set; }
        public int? DiagnosisId { get; set; }
        public string Monogram { get; set; }
    }
}

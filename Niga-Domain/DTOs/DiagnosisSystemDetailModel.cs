using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisSystemDetailModel
    {
        public int DiagnosisSystemDetailId { get; set; }
        public int DiagnosisId { get; set; }
        public int DiagnosisSystemId { get; set; }
        public bool? DeletedStatus { get; set; }=false;

        public string DiagnosisSystemName { get; set; }
        public string Description { get; set; }
    }
}

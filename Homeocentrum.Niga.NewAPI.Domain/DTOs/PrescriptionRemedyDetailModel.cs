using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class PrescriptionRemedyDetailModel
    {
        public int PrescriptionRemedyId { get; set; } = 0;
        public int RemedyId { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
    }

    public class PrescriptionRemedyDetailViewModel
    {
        public int PrescriptionRemedyId { get; set; }= 0;
        public int AppointmentId { get; set; } = 0;
        public int RemedyId { get; set; } = 0;
        public string RemedyName { get; set; }=string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string? CreatedDate { get; set; }
    }

    public class PrescriptionRemedyViewModel
    {
        public int RemedyId { get; set; } = 0;
        public string RemedyName { get; set; } = string.Empty;
       
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
public class PatientLabEntryModel
    {
        public int PatientLabId { get; set; }

        [Required(ErrorMessage = "Please select patient")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Please select test")]
        public int PatientLabTestId { get; set; }
        public string PatientLabTestName { get; set; }
        public DateTime? LabDate { get; set; }
        public string ParameterName { get; set; }
        public string ParameterValue { get; set; }
        public int EnteredBy { get; set; }
        public int EnteredDate { get; set; }
        public int ChangedBy { get; set; }
        public int ChangedDate { get; set; }
        public int DeleteStatus { get; set; }


    }
}

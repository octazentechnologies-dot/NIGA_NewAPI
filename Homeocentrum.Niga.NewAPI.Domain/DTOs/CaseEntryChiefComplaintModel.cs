using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class CaseEntryChiefComplaintModel
    {
        public int CaseChiefComplaintId { get; set; }
        public int? CaseId { get; set; }
        [Required(ErrorMessage = "Chief Complaint Name is required")]
        public string ChiefComplaintName { get; set; }
    }
}

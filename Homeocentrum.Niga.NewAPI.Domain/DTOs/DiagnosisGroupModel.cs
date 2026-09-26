using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
   public class DiagnosisGroupModel
    {
        public int DiagnosisGroupId { get; set; }
        [Required(ErrorMessage = "Diagnosis Group Name is required")]
        public string DiagnosisGroupName { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }

    public class DiagnosisGroupListViewModel
    {
        public int DiagnosisGroupId { get; set; }
        public string DiagnosisGroupName { get; set; }
        public string Description { get; set; }
    }
}

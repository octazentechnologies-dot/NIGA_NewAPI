using System;
using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
   public class QualificationModel
    {
        public int QualificationId { get; set; }

        [Required(ErrorMessage = "Qualification Name is required")]
        public string QualificationName { get; set; } = null!;

        [Required(ErrorMessage = "Qualification Alias is required")]
        public string QualificationAlias { get; set; } = null!;

        public string? Description { get; set; }
        public string? DegreeLevel { get; set; }
        public string? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }

    public class QualificationViewModel
    {
        public int QualificationId { get; set; }
        public string? QualificationName { get; set; }
        public string? QualificationAlias { get; set; }
        public string? Description { get; set; }
        public string? DegreeLevel { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
   public class RemedyGradeModel
    {
        public int GradeId { get; set; }
       // [Required(ErrorMessage = "Grade No is required")]
        public int GradeNo { get; set; }
        public string Description { get; set; }
       // [Required(ErrorMessage = "Font Name is required")]
        public string FontName { get; set; }
      //  [Required(ErrorMessage = "Font Style is required")]
        public string FontStyle { get; set; }
      //  [Required(ErrorMessage = "Font Color is required")]
        public string FontColor { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }
}

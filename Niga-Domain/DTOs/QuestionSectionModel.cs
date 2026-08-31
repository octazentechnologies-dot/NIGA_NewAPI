using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
   public class QuestionSectionModel
    {
        public int QuestionSectionId { get; set; }
        [Required(ErrorMessage = "QuestionSection Name is required")]
        public string QuestionSectionName { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }

    public class QuestionSectionModelDDL
    {
        public int QuestionSectionId { get; set; }
        public string QuestionSectionName { get; set; }
    }

    public class QuestionSectionViewModel
    {
        public int QuestionSectionId { get; set; }
        public string QuestionSectionName { get; set; }
        public string Description { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
   public class QuestionGroupModel
    {
        public int QuestionGroupId { get; set; }
        [Required(ErrorMessage = "Question Group Name is required")]
        public string QuestionGroupName { get; set; }
       public int? QuestionSectionId { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public int? SectionId { get; set; }
    }
  
public class QuestionGroupModel1
    {
        public int QuestionGroupId { get; set; }
        [Required(ErrorMessage = "Question Group Name is required")] 
        public int? QuestionSectionId { get; set; }
        public string QuestionGroupName { get; set; }
        public string QuestionSectionName { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public int? SectionId { get; set; }

    }

    public class QuestionGroupModelDDL
    {
        public int QuestionGroupId { get; set; }
        public string QuestionGroupName { get; set; }
    }

    public class QuestionGroupViewModel
    {
        public int QuestionGroupId { get; set; }
        public string QuestionGroupName { get; set; }
        public int? QuestionSectionId { get; set; }
        public string QuestionSectionName { get; set; }
        public string Description { get; set; }
      
        public int? SectionId { get; set; }

    }

    
}

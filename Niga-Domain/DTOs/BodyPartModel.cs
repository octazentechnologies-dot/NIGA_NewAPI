using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
   public class BodyPartModel
    {
        public int BodyPartId { get; set; }
        public int SectionId { get; set; }
        [Required(ErrorMessage = "Body Part Name is required")]
        public string BodyPartName { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }

        
    }

    public class BodyPartViewModel
    {
        public int BodyPartId { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public string BodyPartName { get; set; }
        public string Description { get; set; }


    }

    public class BodyPartDDLModel
    {
        public int BodyPartId { get; set; }
        public string BodyPartName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class SectionModel
    {
        public SectionModel()
        {
            listSubSectionModel = new List<SubSectionModel>();
        }
        public int? SectionId { get; set; }
        [Required(ErrorMessage = "Section Name is required")]
        public string SectionName { get; set; }
        [Required(ErrorMessage = "Section Alias is required")]
        public string SectionAlias { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public int? ParentSubSectionID { get; set; }
        public List<SubSectionModel> listSubSectionModel { get; set; }
    }

     public class SectionList
    {
    
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public string SectionAlias { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public int? ParentSubSectionID { get; set; }
    }

    public class SectionViewModel
    {  
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public string SectionAlias { get; set; }
        public string Description { get; set; }
    }

    public class SectionMasterDto
    {
        public long SectionId { get; set; }
        public string SectionName { get; set; }
        public string SectionAlias { get; set; }
        public string Description { get; set; }
        public bool DeleteStatus { get; set; }
        public int? BodyPartSectionId { get; set; }

     }

}

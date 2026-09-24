using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    /// <summary>
    /// Model for RubricRemedyDetails.
    /// </summary>
    public class RubricRemedyDetailsModel
    {
        public RubricRemedyDetailsModel()
        {
            this.Authors = new List<RemedyRubricAuthorDetailsModel>();
        }
        public int RubricRemedyId { get; set; }
        [Required(ErrorMessage = "Please select Sub Section")]
        public int SubSectionId { get; set; }
        [Required(ErrorMessage = "Please add remedies")]
        public int RemedyId { get; set; }
        [Required(ErrorMessage = "Please select grade")]
        public int GradeId { get; set; }
        public string RemedyIds { get; set; }
        public int? SectionId { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public List<RemedyRubricAuthorDetailsModel> Authors { get; set; }

    }


    public class RemedyRubricAuthorDetailsModel
    {

        
        public int? RemedyRubricAuthorId { get; set; }
        public int? RubricRemedyId { get; set; }
        public int? AuthorId { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class RubricRemedyDetailModel
    {
        public RubricRemedyDetailModel()
        {
            this.RubricRemedyAuthorList = new List<RubricRemedyAuthorModel>();
        }
       
        public int SubSectionId { get; set; }
        public int SectionId { get; set; }
        public int GradeId { get; set; }

        public List<RubricRemedyAuthorModel> RubricRemedyAuthorList { get; set; }
    }

    public class RubricRemedyAuthorModel
    {
        public RubricRemedyAuthorModel()
        {
            this.RubricAuthorList = new List<RubricAuthorModel>();
        }
        public int RubricRemedyId { get; set; }
        public int? RemedyId { get; set; }
        public string RemedyName { get; set; }
        public List<RubricAuthorModel> RubricAuthorList { get; set; }
    }


    public class RubricAuthorModel
    {
        public int RemedyRubricAuthorId { get; set; } = 0;
        public int? AuthorId { get; set; } = 0;
        public string AuthorName { get; set; } = string.Empty;
    }


}

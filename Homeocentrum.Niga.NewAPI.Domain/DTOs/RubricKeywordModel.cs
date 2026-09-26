using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class RubricKeywordModel
    {
        public RubricKeywordModel() {
            tabRubricRemedyData=new List<TabRubricRemedyData> ();
        }
        public int KeywordID { get; set; } = 0;
        public int SectionID { get; set; } = 0;
        public int SubSectionID { get; set; } = 0;

        public string SectionName { get; set; } = string.Empty;
        public string SectionNameAlias { get; set; } = string.Empty;
        public string SubSectionName { get; set; } = string.Empty;
        public string SubSectionNameAlias { get; set; } = string.Empty;

        public List<TabRubricRemedyData> tabRubricRemedyData { get; set; }
       


    }

    public class TabRubricRemedyData
    {
        public TabRubricRemedyData()
        {
            rubricRemedyModel=new List<RubricRemedyModel> ();
        }
        public int GradeID { get; set; } = 0;
        public int GradeNumber { get; set; } = 0;
        public int SubSectionId { get; set; } = 0;
        public int authorId { get; set; } = 0;
        public int remedyId { get; set; } = 0;
        public string SubSectionName { get; set; } = string.Empty;
        public string FontName { get; set; } = string.Empty;
        public string FontStyle { get; set; } = string.Empty;
        public string FontColor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RubricRemedyModel> rubricRemedyModel { get; set; }
    }

    public class RubricRemedyModel
    {
        public int RemedyId { get; set; } = 0;
        public int AuthorId { get; set; } = 0;
        public string RemedyName { get; set; } = string.Empty;
        public string RemedyAlias { get; set; } = string.Empty;
        public string AuthorAlias { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;

    }
}

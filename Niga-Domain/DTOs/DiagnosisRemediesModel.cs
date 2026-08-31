using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DiagnosisRemediesModel
    {

        public DiagnosisRemediesModel()
        {
           diagnosisRemedyModel = new List<DiagnosisRemedyModel>();
        }
        public int GradeID { get; set; } = 0;
        public int GradeNo { get; set; } = 0;
        public int SubSectionId { get; set; } = 0;
        public int authorId { get; set; } = 0;
        public int remedyId { get; set; } = 0;
        public string SubSectionName { get; set; } = string.Empty;
        public string FontName { get; set; } = string.Empty;
        public string FontStyle { get; set; } = string.Empty;
        public string FontColor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

       public List<DiagnosisRemedyModel> diagnosisRemedyModel { get; set; }
    }

    public class DiagnosisRemedyModel
    {
        public int remedyId { get; set; } = 0;
        public int authorId { get; set; } = 0;
        public string remedyName { get; set; } = string.Empty;
        public string remedyAlias { get; set; } = string.Empty;
        public string authorAlias { get; set; } = string.Empty;
        public string authorName { get; set; } = string.Empty;

    }
}

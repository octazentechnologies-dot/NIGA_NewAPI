using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class RubricRemedyViewModel
    {
        public int? RemedyId { get; set; }
        public int? SectionId { get; set; }
        public int? SubSectionId { get; set; }
        public int? RubricRemedyId { get; set; }

        public string SubSectionName { get; set; }
        public int? GradeId { get; set; }
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }

        public string FontName { get; set; }
        public string FontColor { get; set; }
        public string FontStyle { get; set; }

        public int RemedyCount { get; set; } = 0;
        public bool? IsConformationRubric { get; set; } =false;
        public bool? IsSmallRubric { get; set; } = false;
    }
    public class RemedyRubricViewModel
    {

        public RemedyRubricViewModel()
        {
             this.RubricRemedyViewsList = new List<RubricRemedyViewModel>();
        }

        public int? RemedyID { get; set; } = 0;
        public string RemedyName { get; set; } = string.Empty;
        public string ThemesOrCharacteristics { get; set; } = string.Empty;
        public string Generals { get; set; } = string.Empty;
        public string Modalities { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;

        public List<RubricRemedyViewModel> RubricRemedyViewsList { get; set; }

    }

    public class RubricRemedyViewModel1
    {
        public int SubSectionId { get; set; }
        public int? SectionId { get; set; }
        public string SubSectionName { get; set; }
        public bool DeleteStatus { get; set; }
    }

    
}

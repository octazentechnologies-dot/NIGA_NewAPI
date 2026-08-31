using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
   public class RemedyModel: GradeDisplayProperties
    {
        public int RemedyId { get; set; }
        public string RemedyName { get; set; }
        public string RemedyAlias { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; } 
        
        public int? ThermalId { get; set; }
        public bool? CommonOrUncommon { get; set; }
        public string ThemesOrCharacteristics { get; set; }
        public string Generals { get; set; }
        public string Particulars { get; set; }
        public string Modalities { get; set; }

    }

    public class SearchRemedyModel
    {
        public int RemedyId { get; set; }
        public string RemedyName { get; set; }
        public string Description { get; set; }
        public bool DeleteStatus { get; set; }
        public string RemedyAlias { get; set; }


    }

    public class GradeDisplayProperties
    {
        public string FontName { get; set; }
        public string FontStyle { get; set; }
        public string FontColor { get; set; }
        public int GradeNo { get; set; }
    }

    public class RemedyViewModel
    {
        public int RemedyId { get; set; }
        public string RemedyName { get; set; }
        public string Description { get; set; }
        public string RemedyAlias { get; set; }
    }
}

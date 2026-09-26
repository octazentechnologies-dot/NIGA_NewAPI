using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ClipboardRubricsModel
    {
        public ClipboardRubricsModel()
        {
            this.remedyModels = new List<RemedyModel>();
        }
        public int ClipboardRubricsId { get; set; }
        public int? PatientId { get; set; }
        public int? SubSectionId { get; set; }
        public string Intensity { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public string SubSectionName { get; set; }
        public int? RemedyCount { get; set; }

        public List<RemedyModel> remedyModels { get; set; }
    }

    public class ClipboardRubricsModel1
    {
        public int? SubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public int? RemedyId { get; set; }
        public string RemedyName { get; set; }
        public int Intensity { get; set; }
        public int Rubriccount { get; set; }
        public int GradeSum { get; set; }
        public int intensitysum { get; set; }
        public int DegreeMultiplies { get; set; }
    }

    public class ClipboardRubricsRemedyModel
    {
        public int SubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public int? RemedyId { get; set; }
        public int? ThermalId { get; set; }
        public bool? CommonUncommon { get; set; }
        public string RemedyName { get; set; }
        public int Intensity { get; set; }
        public int Rubriccount { get; set; }
        public int GradeSum { get; set; }
        public int intensitysum { get; set; }
        public int DegreeMultiplies { get; set; }

        public string ThemesOrCharacteristics { get; set; } = string.Empty;
        public string Generals { get; set; } = string.Empty;
        public string Modalities { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;

        public string score { get; set; } = string.Empty;
        public int maxIndex { get; set; } = 0;
    }

    public class ClipboardRemedyModel
    {
        public ClipboardRemedyModel()
        {
            this.CommonRemedyList = new List<ClipboardRubricsRemedyModel>();
            this.UnCommonRemedyList = new List<ClipboardRubricsRemedyModel>();
        }

        public List<ClipboardRubricsRemedyModel> CommonRemedyList { get; set; }
        public List<ClipboardRubricsRemedyModel> UnCommonRemedyList { get; set; }
    }

    public class RepertorizarionRemedyModel
    {

        public int? RubricRemedyId { get; set; } = 0;
        public int? SectionId { get; set; } = 0;
        public int? SubSectionId { get; set; } = 0;
        public string SubSectionName { get; set; } = string.Empty;
        public int? RemedyID { get; set; } = 0;
        public string RemedyName { get; set; } = string.Empty;
        public string ThemesOrCharacteristics { get; set; } = string.Empty;
        public string Generals { get; set; } = string.Empty;
        public string Modalities { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;
        public int? GradeId { get; set; } = 0;
        public string FontName { get; set; } = string.Empty;
        public string FontColor { get; set; } = string.Empty;
        public string FontStyle { get; set; } = string.Empty;



    }

    public class RepertorizarionRemedyInputModel
    {
        public int? RemedyID { get; set; } = 0;
        public string RequiredType { get; set; } = string.Empty;
    }


    public class ClipboardRUbricModel
    {
        public ClipboardRUbricModel()
        {
            this.WithoutEliminateRubric = new List<ClipboardRubricsNonEliminateModel>();
            this.WithEliminateRubric = new List<ClipboardRubricsEliminateModel>();
        }

        public List<ClipboardRubricsNonEliminateModel> WithoutEliminateRubric { get; set; }
        public List<ClipboardRubricsEliminateModel> WithEliminateRubric { get; set; }
    }

    public class ClipboardRubricsNonEliminateModel
    {
        public int? SubSectionId { get; set; }
        public int Intensity { get; set; }
    }

    public class ClipboardRubricsEliminateModel
    {
        public int SubSectionId { get; set; }
        public int Intensity { get; set; }
        public int Rubriccount { get; set; }
    }

    public class RemedyArrayModel
    {
        public int? RemedyId { get; set; }
        public string RemedyName { get; set; }
        public string RemedyAlies { get; set; }
        public int Intensity { get; set; }
        public int IntensitySum { get; set; }
        public int Count { get; set; }
        public int Grade { get; set; }
        public int final { get; set; }

        public int? ThermalId { get; set; }
        public bool? CommonUncommon { get; set; }

        public string ThemesOrCharacteristics { get; set; } = string.Empty;
        public string Generals { get; set; } = string.Empty;
        public string Modalities { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;

        public string score { get; set; } = string.Empty;
        public int scoreCount { get; set; } = 0;
        public List<int?> PresentSubSection { get; set; }
        public int SmallRubric { get; set; } = 0;
    }

    public class SortedRemedyArrayModel
    {
        public int MaxIndex { get; set; }
        public int progressBar { get; set; }
        public int? RemedyId { get; set; }
        public string RemedyName { get; set; }
        public string RemedyAlies { get; set; }
        public int Intensity { get; set; }
        public int IntensitySum { get; set; }
        public int Count { get; set; }
        public int Grade { get; set; }
        public int final { get; set; }

        public int? ThermalId { get; set; }
        public bool? CommonUncommon { get; set; }

        public string ThemesOrCharacteristics { get; set; } = string.Empty;
        public string Generals { get; set; } = string.Empty;
        public string Modalities { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;

        public string score { get; set; } = string.Empty;
        public int scoreCount { get; set; } = 0;
        public List<int?> PresentSubSection { get; set; }
        public int SmallRubric { get; set; } = 0;

    }


    public class ClipboardRemedyNewModel
    {
        public ClipboardRemedyNewModel()
        {
            this.CommonRemedyList = new List<SortedRemedyArrayModel>();
            this.UnCommonRemedyList = new List<SortedRemedyArrayModel>();
        }

        public List<SortedRemedyArrayModel> CommonRemedyList { get; set; }
        public List<SortedRemedyArrayModel> UnCommonRemedyList { get; set; }
    }



    public class EliminateModel 
    {
        //public int? SubSectionId { get; set; }
        //public string SubSectionName { get; set; }
        public int? RemedyId { get; set; }
        //public string RemedyName { get; set; }
        //public string RemedyAlias { get; set; }
        //public int GradeNo { get; set; }
        //public string FontName { get; set; }
        //public string FontStyle { get; set; }
        //public string FontColor { get; set; }
        //public int eliminateIntensity { get; set; }
        //public int total { get; set; }
        //public int? ThermalId { get; set; }
        //public bool? CommonUncommon { get; set; }
        //public string ThemesOrCharacteristics { get; set; } = string.Empty;
        //public string Generals { get; set; } = string.Empty;
        //public string Modalities { get; set; } = string.Empty;
        //public string Particulars { get; set; } = string.Empty;
        //public int score { get; set; } = 0;

    }

    public class ClipboardRubricsRemedyViewModel
    {
        public int SubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public int? RemedyId { get; set; }
        public string RemedyAlias { get; set; }
        public string RemedyName { get; set; }
        public int GradeNo { get; set; }
        public string FontName { get; set; }
        public string FontStyle { get; set; }
        public string FontColor { get; set; }

        public int Intensity { get; set; }

        public int total { get; set; }

        public int? ThermalId { get; set; }
        public bool? CommonOrUncommon { get; set; }

        public string ThemesOrCharacteristics { get; set; } = string.Empty;
        public string Generals { get; set; } = string.Empty;
        public string Modalities { get; set; } = string.Empty;
        public string Particulars { get; set; } = string.Empty;

        public int score { get; set; } =0;
        public int SmallRubric { get; set; } =0;

    }

    public class ClipboardRubricsRemedyDataViewModel
    {
        public int? SubSectionId { get; set; }
        public string SubSectionName { get; set; }
        public int? RemedyId { get; set; }
        public string RemedyName { get; set; }
        public string RemedyAlias { get; set; }
        public int GradeNo { get; set; }
        public int SelectedIntensity { get; set; }
        public int RubricCount { get; set; }
        public int DegreesSum { get; set; }
        public int SumRubricDegree { get; set; }
        public int MultIntensityDegree { get; set; }
        public int MultIntensityDegreeAddIntensity { get; set; }
        public int intensityMatch { get; set; }
        public int intensityMatchCount { get; set; }
        public int MaxIndex { get; set; }
        public int SmallRubricCount { get; set; }

    }


    public class ClipboardRubricsRemedyDataViewModelW
    {
        public int? SubSectionId { get; set; }
        public int? RemedyId { get; set; }
        public string RemedyName { get; set; }
        public int SmallRubricCount { get; set; }
        public int SelectedIntensity { get; set; }

        public int RubricCount { get; set; }
        public int DegreesSum { get; set; }
       
        public string RemedyAlias { get; set; }
        public int GradeNo { get; set; }
        public int intensityMatchCount { get; set; }


        public int SumRubricDegree { get; set; }
        public int MultIntensityDegree { get; set; }
        public int MultIntensityDegreeAddIntensity { get; set; }
        public int intensityMatch { get; set; }
        public int MaxIndex { get; set; }
        
    }

    public class ClipboardRubricsRemedyInput
    {
        public int SubsectionID { get; set; } = 0;
        public int Intensity { get; set; } = 0;
        public int RemedyCount { get; set; } = 0;
    }

}

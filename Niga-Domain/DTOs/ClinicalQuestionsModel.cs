using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
   public class ClinicalQuestionsModel
    {

        public ClinicalQuestionsModel()
        {
            this.ModelEx = new List<ClinicalQueKeywordModel>();
            this.Model1 = new List<ClinicalQueRubricModel>();
        }

        

        public int QuestionsId { get; set; }
        [Required(ErrorMessage = "QuestionGroupId is required")]
        public int? QuestionGroupId { get; set; }
        //[Required(ErrorMessage = "Questions is required")]
        public int? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool? DeleteStatus { get; set; }
        public int? QuestionSectionId { get; set; }
        public int? QuestionSubgroupId { get; set; }
        public int? BodyPartId { get; set; }

        public List<ClinicalQueKeywordModel> ModelEx { get; set; }
        public List<ClinicalQueRubricModel> Model1 { get; set; }
        
    }
    public class ClinicalQueKeywordModel
    {
       
        public int? QuestionsId { get; set; }
        public string KeywordQuestion { get; set; }
        public bool? IsDeleted { get; set; }
        public int? SubsectionId { get; set; }
        public string SubSectionName { get; set; }
        public int? QuestionSubgroupId { get; set; }
        public int? BodyPartId { get; set; }
        public string QuestionSubgroup1 { get; set; }
        public string BodyPartName { get; set; }
    }

    public class ClinicalQueRubricModel
    {
        public int? QuestionsId { get; set; }
        public int? SubsectionId { get; set; }
        public bool? IsDeleted { get; set; }
    }

    public class ClinicalQuestionsBodyPartModel
    {

        public ClinicalQuestionsBodyPartModel()
        {
            this.ClinicalQuestionList = new List<ClinicalQuestionModel>();
            this.ClinicalBodyPartList = new List<ClinicalBodyPartModel>();
        }
        public int QuestionsId { get; set; }
        public int QuestionSectionID { get; set; }
        public int? QuestionGroupId { get; set; }
        public int? QuestionSubGroupID { get; set; }
        public int QBType { get; set; }


        public List<ClinicalQuestionModel> ClinicalQuestionList { get; set; }
        public List<ClinicalBodyPartModel> ClinicalBodyPartList { get; set; }

    }

    public class ClinicalQuestionModel
    {
        public ClinicalQuestionModel()
        {
            this.ClinicalQuestionRubricList = new List<ClinicalQuestionRubricModel>();
        }
        public int ClinicalQuestionKeywordID { get; set; }
        public int? QuestionID { get; set; }
        public string KeyWords { get; set; }

        public List<ClinicalQuestionRubricModel> ClinicalQuestionRubricList { get; set; }

    }

    public class ClinicalBodyPartModel
    {
        public ClinicalBodyPartModel()
        {
            this.ClinicalBodyPartRubricList = new List<ClinicalBodyPartRubricModel>();
        }

        public int ClinicalQuestionBodyPartID { get; set; }
        public int QuestionID { get; set; }
        public int? BodypartID { get; set; }
        public List<ClinicalBodyPartRubricModel> ClinicalBodyPartRubricList { get; set; }
    }

    public class ClinicalQuestionRubricModel
    {
        public int ClinicalQuestionRubricID { get; set; }
        public int ClinicalQuestionKeywordID { get; set; }
        public int? SubsectionID { get; set; }
    }

    public class ClinicalBodyPartRubricModel
    {
        public int ClinicalQuestionRubricID { get; set; }
        public int ClinicalQuestionBodyPartID { get; set; } = 0;
        public int? SubsectionID { get; set; } = 0;
    }

    public class QuestionKeyWordBodyPartInputModel
    {
        public int QuestionSectionID { get; set; } = 0;
        public int QuestionGroupId { get; set; } = 0;
        public int QuestionSubGroupId { get; set; } = 0;

        public string requestType { get; set; } = string.Empty; //Question/Bodypart
    }

    public class QuestionKeyWordBodyPartOutputModel
    {
        public int QuestionKeyWordBodyPartID { get; set; } = 0;
        public string QuestionKeyWordBodyPart { get; set; } = string.Empty;
        public int BodyPartID { get; set; } = 0;
    }

    public class QuestionKeyWordBodyPartRubricInputModel
    {
        public int QuestionKeyWordBodyPartID { get; set; } = 0;
        public string RequestType { get; set; } = string.Empty; //Question/Bodypart
    }

    public class QuestionKeyWordBodyPartRubricOutputModel
    {
        public int? SubsectionId { get; set; } = 0;
        public string SubsectionName { get; set; } = string.Empty;
    }

    public class ClinicalQuestionViewModel
    {
        public int QuestionsId { get; set; }
        public int? QuestionGroupId { get; set; }
        public string QuestionGroupName { get; set; }
        public int? QuestionSectionId { get; set; }
        public string QuestionSectionName { get; set; }
        public int? QuestionSubgroupId { get; set; }
        public string QuestionSubgroupName { get; set; }
    }

    public class ClinicalQuestionBodyViewModel
    {
        public int QuestionsId { get; set; }
        public int? QuestionGroupId { get; set; }
        public int? QuestionSectionId { get; set; }
        public int? QuestionSubgroupId { get; set; }
        public ClinicalQuestionBodyViewModel()
        {
            this.ClinicalQuestionBodyPartViewList = new List<ClinicalQuestionBodyPartRubricViewModel>();
        }
       public  List<ClinicalQuestionBodyPartRubricViewModel> ClinicalQuestionBodyPartViewList { get; set; }
    }

    public class ClinicalQuestionBodyPartRubricViewModel
    {
        public ClinicalQuestionBodyPartRubricViewModel()
        {
            this.ClinicalRubricViewList = new List<ClinicalRubricViewModel>();
        }
        public int QuestionsBodyPartId { get; set; }
        public string QuestionsBodyPartName { get; set; }

        public int SectionId { get; set; }
        public int? BodyPartId { get; set; }
        public int? QBType { get; set; }

        public int? ClinicalQueKeywordId { get; set; }

        public string KeywordQuestion { get; set; }
        public string BodyPartName { get; set; }

        public int? ClinicalQuestionBodyPartId { get; set; }

        public List<ClinicalRubricViewModel> ClinicalRubricViewList { get; set; }
    }

    public class ClinicalRubricViewModel
    {
        public int ClinicalRubricID { get; set; } = 0;
        public int? SubsectionID { get; set; } = 0;
        public string SubsectionName { get; set; } = string.Empty;

        public int? ClinicalQuestionBodyPartID { get; set; } = 0;
        public int? ClinicalQuestionKeywordID { get; set; } = 0;
        public int ClinicalQuestionRubricID { get; set; } = 0;

        

    }

}

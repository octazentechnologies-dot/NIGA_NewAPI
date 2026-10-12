#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    public class ClinicalQuestionsService : IClinicalQuestionsService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public ClinicalQuestionsService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Methood to get clinical questions by questionsId
        /// </summary>
        /// <param name="questionsId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public ClinicalQuestionsModel GetClinicalQuestionsById(long questionsId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var clinicalquestionsEntity = context.ClinicalQuestions.FirstOrDefault(x => x.QuestionsId == questionsId && x.DeleteStatus==false);
            if (clinicalquestionsEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Clinical Questions not found";
            }
            return new ClinicalQuestionsModel
            {
                QuestionsId = clinicalquestionsEntity.QuestionsId,
                QuestionGroupId = clinicalquestionsEntity.QuestionGroupId,
                QuestionSectionId = clinicalquestionsEntity.QuestionSectionId,
                QuestionSubgroupId = clinicalquestionsEntity.QuestionSubgroupId,
                BodyPartId = clinicalquestionsEntity.BodyPartId,
                EnteredDate = clinicalquestionsEntity.EnteredDate,
                EnteredBy = clinicalquestionsEntity.EnteredBy,
                ChangedBy = clinicalquestionsEntity.ChangedBy,
                ChangedDate = clinicalquestionsEntity.ChangedDate,
                DeleteStatus = clinicalquestionsEntity.DeleteStatus,
            };
        }

        /// <summary>
        /// Method implementation for save and update Clinical Questions
        /// </summary>
        /// <param name="clinicalQuestionsBodyPart"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string AddEditClinicalQuestionsBodyPart(ClinicalQuestionsBodyPartModel clinicalQuestionsBodyPart, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            // QBType ==1 i.e Questions & QBType == 2 i.e. BodyParts

            if (clinicalQuestionsBodyPart.QuestionsId == 0)
            {
                int questionsId = AddClinicalQuestions(clinicalQuestionsBodyPart.QuestionSectionID, clinicalQuestionsBodyPart.QuestionGroupId, clinicalQuestionsBodyPart.QuestionSubGroupID);
                if (clinicalQuestionsBodyPart.QBType == 1)
                {
                    foreach (var questionItem in clinicalQuestionsBodyPart.ClinicalQuestionList)
                    {
                        int clinicalQueKeywordId = AddClinicalQuestionKeyword(questionItem, questionsId);

                        foreach (var questionRubricItem in questionItem.ClinicalQuestionRubricList)
                        {
                            AddRubricForQuestion(questionRubricItem, clinicalQueKeywordId);
                        }
                    }

                    Message = "Clinical Questions Saved Successfully";
                }
                else
                {
                    foreach (var bodyPartItem in clinicalQuestionsBodyPart.ClinicalBodyPartList)
                    {
                        int clinicalQuestionBodypartId= AddClinicalQuestionBodyPart(bodyPartItem, questionsId);

                        foreach (var bodyPartRubricItem in bodyPartItem.ClinicalBodyPartRubricList)
                        {
                            AddRubricForBodyPart(bodyPartRubricItem, clinicalQuestionBodypartId);
                        }
                    }

                    Message = "Clinical Body Part Saved Successfully";
                }
            }
            else
            {

                if (clinicalQuestionsBodyPart.QBType == 1)
                {
                    foreach (var questionItem in clinicalQuestionsBodyPart.ClinicalQuestionList)
                    {
                        int clinicalQueKeywordId= AddClinicalQuestionKeyword(questionItem, clinicalQuestionsBodyPart.QuestionsId);

                        foreach (var questionRubricItem in questionItem.ClinicalQuestionRubricList)
                        {
                            AddRubricForQuestion(questionRubricItem, clinicalQueKeywordId);
                        }
                    }
                    Message = "Clinical Questions Update Successfully";
                }
                else
                {
                    foreach (var bodyPartItem in clinicalQuestionsBodyPart.ClinicalBodyPartList)
                    {
                        int clinicalQuestionBodypartId = AddClinicalQuestionBodyPart(bodyPartItem, clinicalQuestionsBodyPart.QuestionsId);
                        foreach (var bodyPartRubricItem in bodyPartItem.ClinicalBodyPartRubricList)
                        {
                            AddRubricForBodyPart(bodyPartRubricItem, clinicalQuestionBodypartId);
                        }
                    }

                    Message = "Clinical Bodypart Update Successfully";
                }
            }

            return Message;
        }

        private int AddClinicalQuestionBodyPart(ClinicalBodyPartModel bodyPartItem, int questionsId)
        {
            int resultId = 0;
            if (bodyPartItem.ClinicalQuestionBodyPartID == 0)
            {
                var clinicalBodyPart = new ClinicalQuestionBodyPart();
                clinicalBodyPart.QuestionId = questionsId;
                clinicalBodyPart.BodyPartId = bodyPartItem.BodypartID;
                clinicalBodyPart.DeletedStatus = false;
                context.ClinicalQuestionBodyParts.Add(clinicalBodyPart);
                context.SaveChanges();
                resultId = clinicalBodyPart.ClinicalQuestionBodyPartId;
            }
            else
            {
                var bodypartEntity = context.ClinicalQuestionBodyParts.FirstOrDefault(x => x.ClinicalQuestionBodyPartId == bodyPartItem.ClinicalQuestionBodyPartID);
                if (bodypartEntity != null)
                {
                    bodypartEntity.QuestionId = bodyPartItem.QuestionID;
                    bodypartEntity.DeletedStatus = false;   
                    bodypartEntity.BodyPartId=bodyPartItem.BodypartID;
                    context.SaveChanges();
                    resultId = bodypartEntity.ClinicalQuestionBodyPartId;
                }
            }
            return resultId;
        }

        private int AddClinicalQuestionKeyword(ClinicalQuestionModel questionItem, int questionsId)
        {
            int resultId = 0;
            
            if (questionItem.ClinicalQuestionKeywordID == 0)
            {
                var clinicalQuestionKeyword = new ClinicalQueKeyword();
                clinicalQuestionKeyword.QuestionsId = questionsId;
                clinicalQuestionKeyword.KeywordQuestion = questionItem.KeyWords;
                clinicalQuestionKeyword.IsDeleted = false;
                context.ClinicalQueKeywords.Add(clinicalQuestionKeyword);
                context.SaveChanges();
                resultId = clinicalQuestionKeyword.ClinicalQueKeywordId;
            }
            else
            {
                var clinicalQuestionEntity = context.ClinicalQueKeywords.FirstOrDefault(x => x.ClinicalQueKeywordId == questionItem.ClinicalQuestionKeywordID);
                if (clinicalQuestionEntity != null)
                {
                    clinicalQuestionEntity.QuestionsId = questionItem.QuestionID;
                    clinicalQuestionEntity.KeywordQuestion = questionItem.KeyWords;
                    clinicalQuestionEntity.IsDeleted = false;
                    context.SaveChanges();
                    resultId = clinicalQuestionEntity.ClinicalQueKeywordId;
                }
            }
            return resultId;
        }

        private void AddRubricForQuestion(ClinicalQuestionRubricModel clinicalQuestionBodyPartRubric, int? KeywordBodyPartId)
        {
            if (clinicalQuestionBodyPartRubric.ClinicalQuestionRubricID == 0)
            {
                var clinicalRubrics = new ClinicalQueRubric();
                clinicalRubrics.SubsectionId = clinicalQuestionBodyPartRubric.SubsectionID;
                clinicalRubrics.ClinicalQueKeywordId = KeywordBodyPartId==0? clinicalQuestionBodyPartRubric.ClinicalQuestionKeywordID: KeywordBodyPartId;
                clinicalRubrics.IsDeleted = false;
                context.ClinicalQueRubrics.Add(clinicalRubrics);
                context.SaveChanges();
            }
            else
            {
                var clinicalQuestionRubricEntity = context.ClinicalQueRubrics.FirstOrDefault(x => x.ClinicalQueRubricId == clinicalQuestionBodyPartRubric.ClinicalQuestionRubricID);
                if (clinicalQuestionRubricEntity != null)
                {
                    clinicalQuestionRubricEntity.SubsectionId = clinicalQuestionBodyPartRubric.SubsectionID;
                    clinicalQuestionRubricEntity.IsDeleted = false;
                    clinicalQuestionRubricEntity.ClinicalQueKeywordId = KeywordBodyPartId;
                    context.SaveChanges();
                }
            }
        }

        private void AddRubricForBodyPart(ClinicalBodyPartRubricModel clinicalQuestionBodyPartRubric, int? KeywordBodyPartId)
        {
            if (clinicalQuestionBodyPartRubric.ClinicalQuestionRubricID == 0)
            {
                var clinicalRubrics = new ClinicalQueRubric();
                clinicalRubrics.SubsectionId = clinicalQuestionBodyPartRubric.SubsectionID;
                clinicalRubrics.ClinicalQuestionBodyPartId = KeywordBodyPartId == 0 ? clinicalQuestionBodyPartRubric.ClinicalQuestionBodyPartID : KeywordBodyPartId;
                clinicalRubrics.IsDeleted = false;
                context.ClinicalQueRubrics.Add(clinicalRubrics);
                context.SaveChanges();
            }
            else
            {
                var clinicalBodypartRubricEntity = context.ClinicalQueRubrics.FirstOrDefault(x => x.ClinicalQueRubricId == clinicalQuestionBodyPartRubric.ClinicalQuestionRubricID);
                if (clinicalBodypartRubricEntity != null)
                {
                    clinicalBodypartRubricEntity.SubsectionId = clinicalQuestionBodyPartRubric.SubsectionID;
                    clinicalBodypartRubricEntity.IsDeleted = false;
                    clinicalBodypartRubricEntity.ClinicalQuestionBodyPartId = KeywordBodyPartId;
                    context.SaveChanges();
                }
            }
        }

        private int AddClinicalQuestions(int questionSectionID, int? questionGroupId, int? questionSubGroupID)
        {
            var clinicalQuestion = new ClinicalQuestion();
            clinicalQuestion.QuestionSectionId = questionSectionID;
            clinicalQuestion.QuestionGroupId = questionGroupId;
            clinicalQuestion.QuestionSubgroupId = questionSubGroupID;
            clinicalQuestion.DeleteStatus = false;
            context.ClinicalQuestions.Add(clinicalQuestion);
            context.SaveChanges();

            return clinicalQuestion.QuestionsId;
        }

        public ClinicalQuestionBodyViewModel GetClinicalQuestionBodyPartDataById(int quetionId, int QBType, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var result = new ClinicalQuestionBodyViewModel();

            var clinicalQuestionBodyPart=context.ClinicalQuestions.FirstOrDefault(x=>x.QuestionsId == quetionId);
            if (clinicalQuestionBodyPart == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Clinical question not found";
                return null;
            }

            result.QuestionsId = clinicalQuestionBodyPart.QuestionsId; 
            result.QuestionGroupId = clinicalQuestionBodyPart.QuestionGroupId;
            result.QuestionSectionId = clinicalQuestionBodyPart.QuestionSectionId;
            result.QuestionSubgroupId = clinicalQuestionBodyPart.QuestionSubgroupId;

            if (QBType == 1)
            {
                var clinicalQuestion = (from clinicalQuestionKey in context.ClinicalQueKeywords
                                        where clinicalQuestionKey.QuestionsId == quetionId && clinicalQuestionKey.IsDeleted==false
                                        select new ClinicalQuestionBodyPartRubricViewModel
                                        {
                                            ClinicalQueKeywordId = clinicalQuestionKey.ClinicalQueKeywordId,
                                            KeywordQuestion = clinicalQuestionKey.KeywordQuestion,
                                            ClinicalRubricViewList= ( from cliniclRubric in context.ClinicalQueRubrics
                                                                      join subsection in context.SubSectionMasters on cliniclRubric.SubsectionId equals subsection.SubSectionId
                                                                      where cliniclRubric.ClinicalQueKeywordId == clinicalQuestionKey.ClinicalQueKeywordId && cliniclRubric.IsDeleted==false
                                                                      select new ClinicalRubricViewModel
                                                                      {
                                                                          ClinicalQuestionRubricID = cliniclRubric.ClinicalQueRubricId,
                                                                          ClinicalQuestionKeywordID = cliniclRubric.ClinicalQueKeywordId,
                                                                          SubsectionID =cliniclRubric.SubsectionId,
                                                                          SubsectionName = subsection.SubSectionName,
                                                                      }).ToList()
                                        }).ToList();
                result.ClinicalQuestionBodyPartViewList = clinicalQuestion;
            }
            else
            {
            
                var clinicalBodyPart = (from clinicalQuestionBodyPart_ in context.ClinicalQuestionBodyParts
                                        join bodyPart in context.BodyPartMasters on clinicalQuestionBodyPart_.BodyPartId equals bodyPart.BodyPartId  
                                        where clinicalQuestionBodyPart_.QuestionId == quetionId && clinicalQuestionBodyPart_.DeletedStatus == false
                                        select new ClinicalQuestionBodyPartRubricViewModel
                                        {
                                            ClinicalQuestionBodyPartId = clinicalQuestionBodyPart_.ClinicalQuestionBodyPartId,
                                            BodyPartId = clinicalQuestionBodyPart_.BodyPartId,
                                            SectionId = bodyPart.SectionId,
                                            BodyPartName = bodyPart.BodyPartName,
                                            ClinicalRubricViewList = (from cliniclRubric in context.ClinicalQueRubrics
                                                                      join subsection in context.SubSectionMasters on cliniclRubric.SubsectionId equals subsection.SubSectionId
                                                                      where cliniclRubric.ClinicalQuestionBodyPartId == clinicalQuestionBodyPart_.ClinicalQuestionBodyPartId && cliniclRubric.IsDeleted == false
                                                                      select new ClinicalRubricViewModel
                                                                      {
                                                                          ClinicalQuestionRubricID = cliniclRubric.ClinicalQueRubricId,
                                                                          ClinicalQuestionBodyPartID = cliniclRubric.ClinicalQuestionBodyPartId,
                                                                          SubsectionID = cliniclRubric.SubsectionId,
                                                                          SubsectionName = subsection.SubSectionName,
                                                                      }).ToList()
                                        }).ToList();
                result.ClinicalQuestionBodyPartViewList = clinicalBodyPart;

            }

            return result;
        }

        /// <summary>
        /// Method is used for delete Clinical Questions.
        /// </summary>
        /// <param name="clinicalquestionsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteClinicalQuestionBodyPart(int questionId,int userId, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var clinicalquestionsEntity = context.ClinicalQuestions.FirstOrDefault(x => x.QuestionsId == questionId);
            if (clinicalquestionsEntity != null)
            {
                clinicalquestionsEntity.DeleteStatus = true;
               // clinicalquestionsEntity.ChangedBy = userId;
                clinicalquestionsEntity.ChangedDate = DateTime.Now;
                context.SaveChanges();
                Message = "Clinical Questions Deleted Successfully";
            }
            return Message;
        }

        //Doctor Side

        public List<QuestionKeyWordBodyPartOutputModel> GetClinicalQuestionsKeyWordBodyPart(QuestionKeyWordBodyPartInputModel questionKeyWordBodyPartInput, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var questionKeyWordBodyPartOutputList = new List<QuestionKeyWordBodyPartOutputModel>();

            if (questionKeyWordBodyPartInput.requestType.Equals("Question"))
            {
                questionKeyWordBodyPartOutputList = (from clinicalQuestion in context.ClinicalQuestions
                                                     join questionKeyword in context.ClinicalQueKeywords on clinicalQuestion.QuestionsId equals questionKeyword.QuestionsId
                                                     where clinicalQuestion.QuestionSectionId == questionKeyWordBodyPartInput.QuestionSectionID &&
                                                     clinicalQuestion.QuestionGroupId == questionKeyWordBodyPartInput.QuestionGroupId &&
                                                     clinicalQuestion.QuestionSubgroupId == questionKeyWordBodyPartInput.QuestionSubGroupId &&
                                                     clinicalQuestion.DeleteStatus == false && questionKeyword.IsDeleted == false
                                                     select new QuestionKeyWordBodyPartOutputModel
                                                     {
                                                         QuestionKeyWordBodyPartID = questionKeyword.ClinicalQueKeywordId,
                                                         QuestionKeyWordBodyPart = questionKeyword.KeywordQuestion
                                                     }

                                                   ).ToList();
            }
            else if (questionKeyWordBodyPartInput.requestType.Equals("Bodypart"))
            {
                questionKeyWordBodyPartOutputList = (from clinicalQuestion in context.ClinicalQuestions
                                                     join clinicalQuestionBodyPart in context.ClinicalQuestionBodyParts on clinicalQuestion.QuestionsId equals clinicalQuestionBodyPart.QuestionId
                                                     join bodyPart in context.BodyPartMasters on clinicalQuestionBodyPart.BodyPartId equals bodyPart.BodyPartId
                                                     where clinicalQuestion.QuestionSectionId == questionKeyWordBodyPartInput.QuestionSectionID &&
                                                     clinicalQuestion.QuestionGroupId == questionKeyWordBodyPartInput.QuestionGroupId &&
                                                     clinicalQuestion.QuestionSubgroupId == questionKeyWordBodyPartInput.QuestionSubGroupId &&
                                                     clinicalQuestion.DeleteStatus == false && clinicalQuestionBodyPart.DeletedStatus == false
                                                     select new QuestionKeyWordBodyPartOutputModel
                                                     {
                                                         QuestionKeyWordBodyPartID = clinicalQuestionBodyPart.ClinicalQuestionBodyPartId,
                                                         BodyPartID = bodyPart.BodyPartId,
                                                         QuestionKeyWordBodyPart = bodyPart.BodyPartName
                                                     }

                                                   ).ToList();
            }

            if (questionKeyWordBodyPartOutputList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = questionKeyWordBodyPartInput.requestType.Equals("Question") ? "Clinical Questions not found" : "Clinical Body Part not found";
            }
            return questionKeyWordBodyPartOutputList;
        }

        public List<QuestionKeyWordBodyPartRubricOutputModel> GetClinicalRubricData(QuestionKeyWordBodyPartRubricInputModel questionKeyWordBodyPartRubricInput, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var questionKeyRubricList = new List<QuestionKeyWordBodyPartRubricOutputModel>();

            if (questionKeyWordBodyPartRubricInput.RequestType.Equals("Question"))
            {
                questionKeyRubricList = (from clinicalRubric in context.ClinicalQueRubrics
                                         join subSection in context.SubSectionMasters on clinicalRubric.SubsectionId equals subSection.SubSectionId
                                         where clinicalRubric.ClinicalQueKeywordId == questionKeyWordBodyPartRubricInput.QuestionKeyWordBodyPartID &&
                                         clinicalRubric.IsDeleted == false && subSection.DeleteStatus == false
                                         select new QuestionKeyWordBodyPartRubricOutputModel
                                         {
                                             SubsectionId = clinicalRubric.SubsectionId,
                                             SubsectionName = subSection.SubSectionName
                                         }

                                                   ).ToList();
            }
            else if (questionKeyWordBodyPartRubricInput.RequestType.Equals("Bodypart"))
            {
                questionKeyRubricList = (from clinicalRubric in context.ClinicalQueRubrics
                                         join subSection in context.SubSectionMasters on clinicalRubric.SubsectionId equals subSection.SubSectionId
                                         where clinicalRubric.ClinicalQuestionBodyPartId == questionKeyWordBodyPartRubricInput.QuestionKeyWordBodyPartID &&
                                         clinicalRubric.IsDeleted == false && subSection.DeleteStatus == false
                                         select new QuestionKeyWordBodyPartRubricOutputModel
                                         {
                                             SubsectionId = clinicalRubric.SubsectionId,
                                             SubsectionName = subSection.SubSectionName
                                         }

                                                   ).ToList();
            }

            if (questionKeyRubricList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = questionKeyWordBodyPartRubricInput.RequestType.Equals("Question") ? "Clinical Questions not found" : "Clinical Body Part not found";
            }
            return questionKeyRubricList;
        }
    }
}

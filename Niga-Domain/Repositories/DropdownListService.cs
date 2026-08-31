using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace Niga_Domain.Business.Implementation
{
    public class DropdownListService:IDropdownListService
    {

        NIGACentrumContext context;
        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public DropdownListService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }
        public List<ThermalModel> GetAllThermalDDL(string? Desc)
        {
            //List<ThermalModel> thermalsDDL = new List<ThermalModel>();

            var thermalsDDL = (from thermalMaster in context.ThermalMasters
                           where thermalMaster.DeleteStatus==false
                           select new ThermalModel
                           {
                               ThermalId = thermalMaster.ThermalId,
                               ThermalName = thermalMaster.ThermalName
                           }).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                thermalsDDL = thermalsDDL.Where(s => s.ThermalName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            thermalsDDL = thermalsDDL.Take(20).ToList();

            return thermalsDDL;
        }

        public List<ThermalModel> GetHeadingbyAuthorDDL(int authorId, string? Desc = null)
        {
            var thermalsDDL = (from thermalMaster in context.ThermalMasters
                               where thermalMaster.DeleteStatus == false
                               select new ThermalModel
                               {
                                   ThermalId = thermalMaster.ThermalId,
                                   ThermalName = thermalMaster.ThermalName
                               }
                            ).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                thermalsDDL = thermalsDDL.Where(s => s.ThermalName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            if (authorId > 0)
            {
                thermalsDDL= thermalsDDL.Where(a => a.ThermalId == authorId).ToList();
            }
            thermalsDDL = thermalsDDL.Take(20).ToList();
            return thermalsDDL;
        }

        public List<AuthorMasterModel> GetAuthorforMateriaMedica(string? Desc = null)
        {
            var authorModelList = new List<AuthorMasterModel>();
            var authorEntityList = context.AuthorMasters.Where(x => x.IsDeleted == false && x.IsForRepertory == false).ToList();

            authorEntityList.ForEach(item =>
            {
                authorModelList.Add(new AuthorMasterModel
                {
                    AuthorId = item.AuthorId,
                    AuthorName = item.AuthorName,
                    AuthorAlias = item.AuthorAlias,
                });
            });
            if (!string.IsNullOrEmpty(Desc))
            {
                authorModelList = authorModelList.Where(a => a.AuthorName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            authorModelList = authorModelList.Take(20).ToList();
            return authorModelList;
        }

        public List<PatientLabTestModel> GetPatientLabTestDDl(string? Desc = null)
        {
            var patientLabTestDDl = (from patientLabTest in context.PatientLabTestMasters
                           where patientLabTest.DeleteStatus == false
                           select new PatientLabTestModel
                           {
                               PatientLabTestId = patientLabTest.PatientLabTestId,
                               LabTestName = patientLabTest.LabTestName,
                               Description = patientLabTest.Description,
                           }
                            ).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                patientLabTestDDl = patientLabTestDDl.Where(p => p.LabTestName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            patientLabTestDDl = patientLabTestDDl.Take(20).ToList();
            return patientLabTestDDl;
        }

        public List<QuestionGroupModelDDL> GetQuestionGroupDDL(string? Desc = null)
        {
           var  errorResponseModel = new ErrorResponseModel();
            var questionGroupList = (from questionGroup in context.QuestionGroupMasters
                                     where questionGroup.DeleteStatus == false
                                     select new QuestionGroupModelDDL
                                     {
                                         QuestionGroupId = questionGroup.QuestionGroupId,
                                         QuestionGroupName = questionGroup.QuestionGroupName,
                                     }
                               ).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                questionGroupList = questionGroupList.Where(q => q.QuestionGroupName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            questionGroupList = questionGroupList.Take(20).ToList();
            if (questionGroupList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Question Group not found";
            }
            return questionGroupList;
        }

        public List<QuestionSectionModelDDL> GetQuestionSectionsDDL( string? Desc = null)
        {
            var errorResponseModel = new ErrorResponseModel();
            var questionSectionList = (from questionSection in context.QuestionSectionMasters
                                       where questionSection.DeleteStatus == false
                                       select new QuestionSectionModelDDL
                                       {
                                           QuestionSectionId = questionSection.QuestionSectionId,
                                           QuestionSectionName = questionSection.QuestionSectionName,
                                       }
                               ).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                questionSectionList = questionSectionList.Where(q => q.QuestionSectionName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            questionSectionList = questionSectionList.Take(20).ToList();
            if (questionSectionList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Question Section not found";
            }
            return questionSectionList;
        }

        public List<QuestionSubGroupModelDDL> GetQuestionSubGroupDDL( string? Desc = null)
        {
            var errorResponseModel = new ErrorResponseModel();
            var questionSectionList = (from questionSubGroup in context.QuestionSubgroups
                                       where questionSubGroup.DeleteStatus == false
                                       select new QuestionSubGroupModelDDL
                                       {
                                           QuestionSubgroupId = questionSubGroup.QuestionSubgroupId,
                                           QuestionSubgroup1 = questionSubGroup.QuestionSubgroup1,
                                       }
                              ).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                questionSectionList = questionSectionList.Where(q => q.QuestionSubgroup1.ToLower().Contains(Desc.ToLower())).ToList();
            }
            questionSectionList = questionSectionList.Take(20).ToList();
            if (questionSectionList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "QuestionSubGroup Not Found";
            }
            return questionSectionList;
        }

        public List<BodyPartDDLModel> GetBodyPartDDL(int sectionId,  string? Desc = null)
        {
            var errorResponseModel = new ErrorResponseModel();
            var bodyPartList = (from bodyPart in context.BodyPartMasters
                                where bodyPart.DeleteStatus == false && bodyPart.SectionId == sectionId
                                select new BodyPartDDLModel
                                {
                                    BodyPartId = bodyPart.BodyPartId,
                                    BodyPartName = bodyPart.BodyPartName,
                                }
                              ).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                bodyPartList = bodyPartList.Where(b => b.BodyPartName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            bodyPartList = bodyPartList.Take(20).ToList();
            if (bodyPartList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Body Part Not Found";
            }
            return bodyPartList;
        }

        public List<QuestionSubGroupModelDDL> GetSubQuestionGroupByQGIDQSIDDDL(int questionGroupId, int questionSectionId,  string? Desc = null)
        {
            var errorResponseModel = new ErrorResponseModel();
            var questionSubGroupList = (from questionSection in context.QuestionSectionMasters
                                        join questionGroup in context.QuestionGroupMasters on questionSection.QuestionSectionId equals questionGroup.QuestionSectionId
                                        join questionSubGroup in context.QuestionSubgroups on questionGroup.QuestionGroupId equals questionSubGroup.QuestionGroupId
                                        where questionSection.QuestionSectionId == questionSectionId
                                        && questionGroup.QuestionGroupId == questionGroupId
                                        && questionSubGroup.DeleteStatus == false
                                        && questionSection.DeleteStatus == false && questionGroup.DeleteStatus == false
                                        select new QuestionSubGroupModelDDL
                                        {
                                            QuestionSubgroupId = questionSubGroup.QuestionSubgroupId,
                                            QuestionSubgroup1 = questionSubGroup.QuestionSubgroup1,
                                        }
                              ).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                questionSubGroupList = questionSubGroupList.Where(q => q.QuestionSubgroup1.ToLower().Contains(Desc.ToLower())).ToList();
            }
            if (questionGroupId > 0)
            {
                questionSubGroupList = questionSubGroupList.Where(q => q.QuestionSubgroupId == questionGroupId).ToList();
            }
            questionSubGroupList = questionSubGroupList.Take(20).ToList();
            if (questionSubGroupList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Question SubGroup Not Found";
            }
            return questionSubGroupList;
        }

        /// <summary>
        /// Method to get all the subsections
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<SubSectionDDLModel> GetSubsectionBySection(long sectionId,  string? Desc = null)
        {
            var errorResponseModel = new ErrorResponseModel();
            var subsectionEntityList =(from subSection in context.SubSectionMasters
                                       where subSection.SectionId==sectionId && subSection.DeleteStatus==false
                                       orderby subSection.SubSectionName
                                       select new SubSectionDDLModel
                                       {
                                           SubSectionId = subSection.SubSectionId,
                                           SubSectionName = subSection.SubSectionName,
                                       }).ToList();
            if (!string.IsNullOrEmpty(Desc))
            {
                subsectionEntityList = subsectionEntityList.Where(s => s.SubSectionName.ToLower().Contains(Desc.ToLower())).ToList();
            }
            subsectionEntityList = subsectionEntityList.Take(20).ToList();
            if (subsectionEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "SubSection not found";
            }
            return subsectionEntityList;
        }

       
        
    }
}

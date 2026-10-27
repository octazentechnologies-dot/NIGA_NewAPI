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
using Microsoft.AspNetCore.Http;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    public class PaginationService : IPaginationService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public PaginationService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        string Normalize(string input)
        {
            // Remove spaces, hyphens, commas, and periods, and convert to lowercase
            return input.Replace(" ", "")
                        .Replace("-", "")
                        .Replace(",", "")
                        .Replace(".", "")
                        .ToLower();
        }

        public PaginationResult GetSubSectionBySectionIdAndQueryString(int sectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
            var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            //var subsectionModelList = (from subSection in context.SubSectionMasters
            //                           where subSection.SectionId == sectionId
            //                           && (string.IsNullOrEmpty(queryString) || subSection.SubSectionName.ToLower().Contains(queryString.ToLower()))
            //                           && subSection.DeleteStatus == false
            //                           orderby subSection.SubSectionName
            //                           select new SubSectionViewModel
            //                           {
            //                               SubSectionId = subSection.SubSectionId,
            //                               SectionId = subSection.SectionId,
            //                               ParentSubSectionId = subSection.ParentSubSectionId,
            //                               SubSectionName = subSection.SubSectionName,
            //                               SubSectionNameAlias = subSection.SubSectionNameAlias,
            //                               Description = subSection.Description,
            //                           }).ToList();

            var subsectionModelList =
(
    from subSection in context.SubSectionMasters
    join parentSubSection in context.SubSectionMasters
        on subSection.ParentSubSectionId equals parentSubSection.SubSectionId
        into parentJoin
    from parent in parentJoin.DefaultIfEmpty() // LEFT JOIN
    where subSection.SectionId == sectionId

          && subSection.DeleteStatus == false
    orderby subSection.SubSectionName
    select new SubSectionViewModel
    {
        SubSectionId = subSection.SubSectionId,
        SectionId = subSection.SectionId,
        ParentSubSectionId = subSection.ParentSubSectionId,
        ParentSubSectionName = parent != null ? parent.SubSectionName : null,
        SubSectionName = subSection.SubSectionName,
        SubSectionNameAlias = subSection.SubSectionNameAlias,
        Description = subSection.Description,
        MainParentSubsection = subSection.MainParentSubsection
    }
).ToList();





            //var subsectionModelList = (from subSection in context.SubSectionMasters
            //                           where subSection.SectionId == sectionId
            //                           && (string.IsNullOrEmpty(queryString) || Normalize(subSection.SubSectionName).Contains(Normalize(queryString)))
            //                           && subSection.DeleteStatus == false
            //                           orderby subSection.SubSectionName
            //                           select new SubSectionViewModel
            //                           {
            //                               SubSectionId = subSection.SubSectionId,
            //                               SectionId = subSection.SectionId,
            //                               ParentSubSectionId = subSection.ParentSubSectionId,
            //                               SubSectionName = subSection.SubSectionName,
            //                               SubSectionNameAlias = subSection.SubSectionNameAlias,
            //                               Description = subSection.Description,
            //                           }).ToList();



            if (subsectionModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Sub section Not Found";
            }
            subsectionModelList = AdminListFieldSearch.Filter(subsectionModelList, queryString, ignorePunctuation: true);
            totalRecords = subsectionModelList.Count;
            totalPages = Math.Ceiling((double)totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = subsectionModelList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// Method to get all the DrugSystem
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetDrugSystem(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            var drugSystemModelList = new List<DrugSystemModel>();
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            drugSystemModelList = (from drugSystem in context.DrugSystemMasters
                                       where drugSystem.DeleteStatus == false
                                       select new DrugSystemModel
                                       {
                                           DrugSystemId = drugSystem.DrugSystemId,
                                           DrugSystemName = drugSystem.DrugSystemName,
                                           DeleteStatus = drugSystem.DeleteStatus,
                                       }).ToList();
          

            if (drugSystemModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug data Not Found";
            }
            drugSystemModelList = AdminListFieldSearch.Filter(drugSystemModelList, queryString);
            totalRecords = drugSystemModelList.Count;
            totalPages = Math.Ceiling((double)totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = drugSystemModelList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method to get all the DrugGroup
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetDrugGroup(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            var drugGroupModelList = new List<DrugGroupModel>();
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            drugGroupModelList = (from drugGroup in context.DrugGroupMasters
                                   join drugSystem in context.DrugSystemMasters on drugGroup.DrugSystemId equals drugSystem.DrugSystemId
                                   where drugGroup.DeleteStatus == false
                                   select new DrugGroupModel
                                   {
                                       DrugSystemId = drugSystem.DrugSystemId,
                                       DrugSystemName = drugSystem.DrugSystemName,
                                       DrugGroupId = drugGroup.DrugGroupId,
                                       DrugGroupName = drugGroup.DrugGroupName,
                                       DeleteStatus = drugGroup.DeleteStatus,
                                   }).ToList();


            if (drugGroupModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug data Not Found";
            }
            drugGroupModelList = AdminListFieldSearch.Filter(drugGroupModelList, queryString);
            totalRecords = drugGroupModelList.Count;
            totalPages = Math.Ceiling((double)totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = drugGroupModelList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// Method to get all the AllopathicDrug
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetAllopathicDrug(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            var allopathicDrugList = new List<AllopathicDrugViewModel>();
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            allopathicDrugList = (from allopathicDrug in context.AllopathicDrugMasters
                                  join drugGroup in context.DrugGroupMasters on allopathicDrug.DrugGroupId equals drugGroup.DrugGroupId
                                  where allopathicDrug.DeleteStatus == false
                                  select new AllopathicDrugViewModel
                                  {
                                      AllopathicDrugId = allopathicDrug.AllopathicDrugId,
                                      AllopathicDrugName = allopathicDrug.AllopathicDrugName,
                                      DrugGroupId = drugGroup.DrugGroupId,
                                      DrugGroupName = drugGroup.DrugGroupName,
                                  }).ToList();


            if (allopathicDrugList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Allopathic Drug data Not Found";
            }
            allopathicDrugList = AdminListFieldSearch.Filter(allopathicDrugList, queryString);
            totalRecords = allopathicDrugList.Count;
            totalPages = Math.Ceiling((double)totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = allopathicDrugList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// Method to get all the languages
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetLanguage(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

           var languageList = (from language in context.LanguageMasters
                                  where language.IsDeleted == false
                                  select new LanguageMasterModel
                                  {
                                      LanguageId = language.LanguageId,
                                      LanguageName = language.LanguageName,
                                      Description = language.Description,
                                      IsDeleted = language.IsDeleted,
                                  }).ToList();


            if (languageList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug data Not Found";
            }
            languageList = AdminListFieldSearch.Filter(languageList, queryString);
            totalRecords = languageList.Count;
            totalPages = Math.Ceiling((double)totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = languageList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// Method for getting all the questionsections
        /// </summary>
        ///  <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetQuestionSections(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var questionSectionList = (from questionSecton in context.QuestionSectionMasters
                                       where !questionSecton.DeleteStatus
                                       select new QuestionSectionViewModel
                                       {
                                            QuestionSectionId = questionSecton.QuestionSectionId,
                                            QuestionSectionName = questionSecton.QuestionSectionName,
                                            Description = questionSecton.Desciption,
                                        }).ToList();



            if (questionSectionList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug data Not Found";
            }
            questionSectionList = AdminListFieldSearch.Filter(questionSectionList, queryString);
            totalRecords = questionSectionList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = questionSectionList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method for getting all the question group
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetQuestionGroupExistance(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel, int? questionGroupId = null, int? questionSectionId = null)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var questionGroupList = (from questionGroup in context.QuestionGroupMasters
                                      join questionSection in context.QuestionSectionMasters on questionGroup.QuestionSectionId equals questionSection.QuestionSectionId
                                      where !questionGroup.DeleteStatus
                                      select new QuestionGroupViewModel
                                      {
                                          QuestionGroupId = questionGroup.QuestionGroupId,
                                          QuestionGroupName = questionGroup.QuestionGroupName,
                                          QuestionSectionId = questionGroup.QuestionSectionId,
                                          Description = questionGroup.Description,
                                          QuestionSectionName = questionSection.QuestionSectionName,
                                          SectionId = questionGroup.SectionId
                                      }).ToList();

            if (questionGroupId.GetValueOrDefault() > 0)
            {
                questionGroupList = questionGroupList.Where(x => x.QuestionGroupId == questionGroupId).ToList();
            }
            if (questionSectionId.GetValueOrDefault() > 0)
            {
                questionGroupList = questionGroupList.Where(x => x.QuestionSectionId == questionSectionId).ToList();
            }


            if (questionGroupList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug data Not Found";
            }
            questionGroupList = AdminListFieldSearch.Filter(questionGroupList, queryString);
            totalRecords = questionGroupList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = questionGroupList.Skip(skip).Take(pageSize);
            return result;
          
        }

        /// <summary>
        /// Method to get all the QuestionSubGroup
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        public PaginationResult GetQuestionSubGroup(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel, int? questionSubgroupId = null, int? questionGroupId = null, int? questionSectionId = null)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var questionSubGroupList = (from questionSubGroup in context.QuestionSubgroups
                                   join questionGroup in context.QuestionGroupMasters on questionSubGroup.QuestionGroupId equals questionGroup.QuestionGroupId
                                   where questionSubGroup.DeleteStatus == false
                                   select new QuestionSubGroupModel
                                   {
                                       QuestionGroupId = questionSubGroup.QuestionGroupId,
                                       QuestionGroupName = questionGroup.QuestionGroupName,
                                       QuestionSubgroupId = questionSubGroup.QuestionSubgroupId,
                                       QuestionSubGroupName = questionSubGroup.QuestionSubgroup1,
                                       Description = questionSubGroup.Description,
                                       DeleteStatus = questionSubGroup.DeleteStatus,
                                   }
                              ).ToList();

            AttachQuestionSubGroupSections(questionSubGroupList);
            AttachQuestionSubGroupExistance(questionSubGroupList);

            if (questionSubgroupId.GetValueOrDefault() > 0)
            {
                questionSubGroupList = questionSubGroupList.Where(x => x.QuestionSubgroupId == questionSubgroupId).ToList();
            }
            if (questionGroupId.GetValueOrDefault() > 0)
            {
                questionSubGroupList = questionSubGroupList.Where(x => x.QuestionGroupId == questionGroupId).ToList();
            }
            if (questionSectionId.GetValueOrDefault() > 0)
            {
                questionSubGroupList = questionSubGroupList.Where(x => x.QuestionSectionId == questionSectionId).ToList();
            }


            if (questionSubGroupList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug data Not Found";
            }
            questionSubGroupList = AdminListFieldSearch.Filter(questionSubGroupList, queryString);
            totalRecords = questionSubGroupList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = questionSubGroupList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method for getting all the authors
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetAuthor(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var authorList = (from author in context.AuthorMasters
                                        where author.IsDeleted==false
                              select new AuthorMasterModel
                                        {
                                            AuthorId = author.AuthorId,
                                            AuthorName = author.AuthorName,
                                            Description = author.Description,
                                            AuthorAlias = author.AuthorAlias,
                                            IsDeleted = author.IsDeleted,
                                            IsForRepertory = author.IsForRepertory,
                                        }).ToList();

            if (authorList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Drug data Not Found";
            }
            authorList = AdminListFieldSearch.Filter(authorList, queryString);
            totalRecords = authorList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = authorList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method to get all the remedies
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetRemedies(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

           var remedyList = (from remedy in context.RemedyMasters
                                  where !remedy.DeleteStatus
                                  select new RemedyViewModel
                                  {
                                      RemedyId = remedy.RemedyId,
                                      RemedyName = remedy.RemedyName,
                                      Description = remedy.Description,
                                      RemedyAlias = string.IsNullOrEmpty(remedy.RemedyAlias) ? "Not Available" : remedy.RemedyAlias
                                  }).ToList();


            if (remedyList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Allopathic Drug data Not Found";
            }
            remedyList = AdminListFieldSearch.Filter(remedyList, queryString);
            totalRecords = remedyList.Count;
            totalPages = Math.Ceiling((double)totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = remedyList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// Method to get all the MateriaMedicaHead
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetMateriaMedica(int authorId, int remedyId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var materiaMedicaList = (from materiaMedica in context.MateriaMedicaMasters
                                           join author in context.AuthorMasters on materiaMedica.AuthorId equals author.AuthorId
                                           join remedy in context.RemedyMasters on materiaMedica.RemedyId equals remedy.RemedyId
                                           join meteriaMedicaHead in context.MateriaMedicaHeadMasters
                                           on materiaMedica.MateriaMedicaHeadId equals meteriaMedicaHead.MateriaMedicaHeadId
                                           where (authorId==0 || author.AuthorId==authorId) && (remedyId==0 || remedy.RemedyId==remedyId) && materiaMedica.IsDeleted == false
                                           select new MateriaMedicaModel
                                           {
                                               MateriaMedicaId = materiaMedica.MateriaMedicaId,
                                               AuthorId = materiaMedica.AuthorId,
                                               RemedyId = materiaMedica.RemedyId,
                                               MateriaMedicaHeadId = materiaMedica.MateriaMedicaHeadId,
                                               AuthorName = author.AuthorName,
                                               RemedyName = remedy.RemedyName,
                                               MateriaMedicaHeadName = meteriaMedicaHead.MateriaMedicaHeadName,
                                           }).ToList();

            if (materiaMedicaList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "MateriaMedica Not Found";
            }

            materiaMedicaList = AdminListFieldSearch.Filter(materiaMedicaList, queryString);
            totalRecords = materiaMedicaList.Count;
            totalPages = Math.Ceiling((double)totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = materiaMedicaList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method to get all the MateriaMedicaHead
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        public PaginationResult GetMateriaMedicaHead(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var materiaMedicaheadList = (from materiaMedicaHead in context.MateriaMedicaHeadMasters
                                               join auth in context.AuthorMasters on materiaMedicaHead.AuthorId equals auth.AuthorId
                                               where materiaMedicaHead.IsDeleted == false
                                               select new MateriaMedicaHeadMasterModel1
                                               {
                                                   MateriaMedicaHeadId = materiaMedicaHead.MateriaMedicaHeadId,
                                                   AuthorId = materiaMedicaHead.AuthorId,
                                                   MateriaMedicaHeadName = materiaMedicaHead.MateriaMedicaHeadName,
                                                   Description = materiaMedicaHead.Description,
                                                   IsSection = materiaMedicaHead.IsSection,
                                                   SeqNo = materiaMedicaHead.SeqNo,
                                                   AuthorName = auth.AuthorName,
                                                   DifferentialMM = materiaMedicaHead.DifferentialMm,
                                                   IsDeleted = materiaMedicaHead.IsDeleted
                                               }).ToList();
            if (materiaMedicaheadList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "MateriaMedicaHead Not Found";
            }

            materiaMedicaheadList = AdminListFieldSearch.Filter(materiaMedicaheadList, queryString);
            totalRecords = materiaMedicaheadList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = materiaMedicaheadList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// Method for getting all the Intensities
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetIntensities(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var intensityList = context.IntensityMasters.Where(x => !x.DeleteStatus).Select(x=> new IntensityModel
            {
                IntensityId = x.IntensityId,
                IntensityNo = x.IntensityNo,
                Description = x.Description,
            }).ToList();

            if (intensityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Intensity Not Found";
            }

            intensityList = AdminListFieldSearch.Filter(intensityList, queryString);
            totalRecords = intensityList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = intensityList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method to get all the sections
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetSections(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;
            var sectionList = context.SectionMasters.Where(x => !x.DeleteStatus ).Select(x=> new SectionViewModel
            {
                SectionId = x.SectionId,
                SectionName = x.SectionName,
                SectionAlias = x.SectionAlias,
                Description = x.Description,
            }).ToList();
            if (sectionList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Section Not Found";
            }

           
            sectionList = AdminListFieldSearch.Filter(sectionList, queryString);
            totalRecords = sectionList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = sectionList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method to get all the diagnosis system
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetDiagnosisSystem(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var diagnosisSystemList = context.DiagnosisSystems.Where(x => x.IsActive == false).Select(x=> new DiagnosisSystemModel
            {
                DiagnosisSystemId = x.DiagnosisSystemId,
                DiagnosisSystemName = x.DiagnosisSystemName,
                Description = x.Description,

            }).ToList();
            if (diagnosisSystemList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "DrugSystem Not Found";
            }

            diagnosisSystemList = AdminListFieldSearch.Filter(diagnosisSystemList, queryString);
            totalRecords = diagnosisSystemList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = diagnosisSystemList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method for getting all the bodyparts
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetBodyParts(int sectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var bodyPartList = (from bodyPart in context.BodyPartMasters
                                join section in context.SectionMasters on bodyPart.SectionId equals section.SectionId
                                where (sectionId == 0 || bodyPart.SectionId == sectionId) && !bodyPart.DeleteStatus
                                select new BodyPartViewModel
                                {
                                    BodyPartId = bodyPart.BodyPartId,
                                    SectionId = bodyPart.SectionId,
                                    BodyPartName = bodyPart.BodyPartName,
                                    Description = bodyPart.Description,
                                    SectionName = section.SectionName,
                                }).ToList();

           

            if (bodyPartList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Body Part Not Found";
            }

            bodyPartList = AdminListFieldSearch.Filter(bodyPartList, queryString);
            totalRecords = bodyPartList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = bodyPartList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// Method for getting all the clinical questions
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetClinicalQuestionBodyPartList(int questionGroupId, int questionSubgroupId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

          var clinicalQuestionViewList = (from clinicalQuestion in context.ClinicalQuestions
                                        join questionGroup in context.QuestionGroupMasters on clinicalQuestion.QuestionGroupId equals questionGroup.QuestionGroupId
                                        join questionSection in context.QuestionSectionMasters on clinicalQuestion.QuestionSectionId equals questionSection.QuestionSectionId
                                        join questionSubgroup in context.QuestionSubgroups on clinicalQuestion.QuestionSubgroupId equals questionSubgroup.QuestionSubgroupId
                                        where clinicalQuestion.DeleteStatus == false &&
                                        (questionGroupId == 0 || clinicalQuestion.QuestionGroupId == questionGroupId) && 
                                        (questionSubgroupId == 0 || clinicalQuestion.QuestionSubgroupId == questionSubgroupId)
                                          select new ClinicalQuestionViewModel
                                        {
                                            QuestionsId = clinicalQuestion.QuestionsId,
                                            QuestionGroupId = clinicalQuestion.QuestionGroupId,
                                            QuestionSectionId = clinicalQuestion.QuestionSectionId,
                                            QuestionSubgroupId = clinicalQuestion.QuestionSubgroupId,
                                            QuestionGroupName = questionGroup.QuestionGroupName,
                                            QuestionSectionName = questionSection.QuestionSectionName,
                                            QuestionSubgroupName = questionSubgroup.QuestionSubgroup1,
                                        }).ToList();


            if (clinicalQuestionViewList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Clinical Questions Not Found";
            }

            clinicalQuestionViewList = AdminListFieldSearch.Filter(clinicalQuestionViewList, queryString);
            totalRecords = clinicalQuestionViewList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = clinicalQuestionViewList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method for getting all the Diagnosis Therapeutics Details
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns>PaginationResult</returns>
        public PaginationResult GetDiagnosisTherapeuticsDetails(int diagonosisId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;


            errorResponseModel = new ErrorResponseModel();
            var diagnosisTherapeuticsDetailList = (from dtd in context.DiagnosisTherapeuticsDetails
                                                         join ds in context.DiagnosisMasters on dtd.DiagnosisId equals ds.DiagnosisId
                                                         where (diagonosisId == 0 || dtd.DiagnosisId==diagonosisId)
                                                         select new DiagnosisTherapeuticsDetailModel
                                                         {
                                                             DiagnosisTherapeuticsDetailId = dtd.DiagnosisTherapeuticsDetailId,
                                                             DiagnosisId = dtd.DiagnosisId,
                                                             DiagnosisTherapeuticsDetail1 = dtd.DiagnosisTherapeuticsDetail1,
                                                             DeletedStatus = (bool)dtd.DeletedStatus,
                                                             DiagnosisName = ds.DiagnosisName
                                                         }
                                                     ).ToList();
            if (diagnosisTherapeuticsDetailList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "DrugSystem Not Found";
            }

            diagnosisTherapeuticsDetailList = AdminListFieldSearch.Filter(diagnosisTherapeuticsDetailList, queryString);
            totalRecords = diagnosisTherapeuticsDetailList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = diagnosisTherapeuticsDetailList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method for getting all the Diagnosis 
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns>PaginationResult</returns>
        public PaginationResult GetDiagnosis(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;
         
           var diagnosisList = context.DiagnosisMasters.Where(x => !x.DeleteStatus)
                                                        .Select(item=> new DignosisViewModel
                                                        {
                                                            DiagnosisId = item.DiagnosisId,
                                                            DiagnosisName = item.DiagnosisName,
                                                            DiagnosisNameAlias = item.DiagnosisNameAlias,   
                                                        }).ToList();
            if (diagnosisList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Diagnosis Not Found";
            }

            diagnosisList = AdminListFieldSearch.Filter(diagnosisList, queryString);
            totalRecords = diagnosisList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;


            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = diagnosisList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method for getting all the patient lab test list 
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns>PaginationResult</returns>
        public PaginationResult GetPatientLabTests(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var labTestMasterEntities = (from patientLabTest in context.PatientLabTestMasters
                                         where patientLabTest.DeleteStatus == false
                                         select new PatientLabTestModel
                                         {
                                             PatientLabTestId = patientLabTest.PatientLabTestId,
                                             LabTestName = patientLabTest.LabTestName,
                                             Description = patientLabTest.Description,
                                         }
                                         ).ToList();

            if (labTestMasterEntities.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "records Not Found";
            }

            labTestMasterEntities = AdminListFieldSearch.Filter(labTestMasterEntities, queryString);
            totalRecords = labTestMasterEntities.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;

            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = labTestMasterEntities.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// Method to get all the user
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetUser(string queryString,NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var userList = (from user in context.UserMasters
                                  join role in context.RoleMasters on user.RoleId equals role.RoleId
                                  where !user.DeleteStatus
                                  select new UserViewModel
                                  {
                                      UserId = user.UserId,
                                      UserName = user.UserName,
                                      UserStatus = user.UserStatus == true ? "Active" : "InActive",
                                      EmailId = user.EmailId,
                                      MobileNo = user.MobileNo,
                                      CreatedDate = user.EnteredDate,
                                      FirstName = user.FirstName,
                                      LastName = user.LastName,
                                      RoleId = user.RoleId,
                                      Role = role.RoleName,
                                  }).ToList();

            if (userList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "User Not Found";
            }

            userList = AdminListFieldSearch.Filter(userList, queryString);
            totalRecords = userList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;

            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = userList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// interface for getting all the newsdetails
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetAllNewsDetails(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var newsList = (from news in context.NewsDetails
                                  join category in context.NewsCategories on news.NewsCategoryId equals category.NewsCategoryId
                                  where news.IsActive == true
                                  select new NewsModel
                                  {
                                    NewsId = news.NewsId,
                                    NewsDate = news.NewsDate.HasValue ? news.NewsDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                                    NewsHeading = news.NewsHeading,
                                    NewsSubHeading = news.NewsSubHeading,
                                    NewsCategory = category.NewsCategory1,
                                  }).ToList();

            if (newsList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "News Details Not Found";
            }

            newsList = AdminListFieldSearch.Filter(newsList, queryString);
            totalRecords = newsList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;

            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = newsList.Skip(skip).Take(pageSize);
            return result;

        }

        /// <summary>
        /// interface for getting all the Blogdetail
        /// </summary>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetAllBlogDetail(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
             var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var blogList = context.BlogDetails.Where(x => x.IsActive == true).Select(item => new BlogDetailModel
            {
                BlogId = item.BlogId,
                BlogHead = item.BlogHead,
                BlogSubHead = item.BlogSubHead,
                BlogDate = item.BlogDate.HasValue ? item.BlogDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                IsActive = item.IsActive,
            }).ToList();

            if (blogList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Blog Details Not Found";
            }

            blogList = AdminListFieldSearch.Filter(blogList, queryString);
            totalRecords = blogList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;

            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = blogList.Skip(skip).Take(pageSize);
            return result;
        }

        /// <summary>
        /// GetRubricList
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PaginationResult GetSubSectionForRubric(int SectionId,string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
            var pageSize = nigaParameters.PageSize;
            var totalRecords = 0.0;
            var totalPages = 0.0;
            var skip = 0;

            var subSectionList = (from subSection in context.SubSectionMasters
                                     where !subSection.DeleteStatus && subSection.SectionId == SectionId
                                  orderby subSection.SubSectionName
                                  select new SubSectionForPageModel
                                     {
                                         SubSectionId=subSection.SubSectionId,
                                         SubSectionName=subSection.SubSectionName
                                     }).ToList();


            if (subSectionList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "SubSection Not Found";
            }

            subSectionList = AdminListFieldSearch.Filter(subSectionList, queryString);
            totalRecords = subSectionList.Count;
            totalPages = Math.Ceiling(totalRecords / pageSize);
            skip = (pageNumber - 1) * pageSize;

            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = subSectionList.Skip(skip).Take(pageSize);

            return result;

        }

        public PaginationResult GetRepertorizarionRemedyForAccordion(
            int remedyID,
            string RequiredType,
            NigaParameters nigaParameters,
            ref ErrorResponseModel errorResponseModel,
            int[] sectionIds = null,
            int? gradeNo = null)
        {
            errorResponseModel = new ErrorResponseModel();

            var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
            var pageSize = nigaParameters.PageSize;
            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            var repertorizarionRemedyList = (from remedyDetails in context.RubricRemedyDetails
                                             join remedy in context.RemedyMasters on remedyDetails.RemedyId equals remedy.RemedyId
                                             join subSection in context.SubSectionMasters on remedyDetails.SubSectionId equals subSection.SubSectionId
                                             join section in context.SectionMasters on subSection.SectionId equals section.SectionId
                                             join gradeMaster in context.RemedyGradeMaster on remedyDetails.GradeId equals gradeMaster.GradeId
                                             where remedyDetails.RemedyId == remedyID
                                                   && remedyDetails.DeletedStatus == false
                                                   && (RequiredType == "SmallRubric"
                                                        ? remedyDetails.IsSmallRubric == true
                                                        : remedyDetails.IsConfirmationRubric == true)
                                             select new RepertorizarionRemedyModel
                                             {
                                                 RubricRemedyId = remedyDetails.RubricRemedyId,
                                                 SectionId = subSection.SectionId,
                                                 SectionName = section.SectionName,
                                                 SubSectionId = subSection.SubSectionId,
                                                 SubSectionName = subSection.SubSectionName,
                                                 GradeId = gradeMaster.GradeId,
                                                 GradeNo = gradeMaster.GradeNo,
                                                 FontName = gradeMaster.FontName,
                                                 FontColor = gradeMaster.FontColor,
                                                 FontStyle = gradeMaster.FontStyle,

                                             }).OrderBy(x => x.SectionId).ThenBy(x => x.SubSectionName).ToList();

            IEnumerable<RepertorizarionRemedyModel> filtered = repertorizarionRemedyList;

            if (sectionIds != null && sectionIds.Length > 0)
            {
                var sectionIdSet = new HashSet<int>(sectionIds.Where(id => id > 0));
                if (sectionIdSet.Count > 0)
                {
                    filtered = filtered.Where(x =>
                        x.SectionId.HasValue && sectionIdSet.Contains(x.SectionId.Value));
                }
            }

            if (gradeNo.HasValue && gradeNo.Value > 0)
            {
                var normalizedGradeNo = gradeNo.Value;
                filtered = filtered.Where(x => x.GradeNo == normalizedGradeNo);
            }

            var filteredList = filtered.ToList();

            if (filteredList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy Not Found";
            }

            var totalRecords = (double)filteredList.Count;
            var totalPages = pageSize > 0 ? Math.Ceiling(totalRecords / pageSize) : 0;
            var skip = (pageNumber - 1) * pageSize;

            var result = new PaginationResult();
            result.TotalPageCount = totalPages;
            result.TotalCount = totalRecords;
            result.ResultObject = filteredList.Skip(skip).Take(pageSize);
            return result;
        }

        private void AttachQuestionSubGroupExistance(List<QuestionSubGroupModel> questionSubGroups)
        {
            if (questionSubGroups == null || questionSubGroups.Count == 0)
            {
                return;
            }

            var groupIds = questionSubGroups
                .Where(x => x.QuestionGroupId.HasValue)
                .Select(x => x.QuestionGroupId.Value)
                .Distinct()
                .ToList();

            var sections = (from questionGroup in context.QuestionGroupMasters
                            join section in context.QuestionSectionMasters
                                on questionGroup.QuestionSectionId equals section.QuestionSectionId into sectionJoin
                            from section in sectionJoin.DefaultIfEmpty()
                            where groupIds.Contains(questionGroup.QuestionGroupId)
                            select new
                            {
                                questionGroup.QuestionGroupId,
                                questionGroup.QuestionSectionId,
                                QuestionSectionName = section != null ? section.QuestionSectionName : null
                            }).ToList();

            var byGroup = sections.ToDictionary(x => x.QuestionGroupId);
            foreach (var row in questionSubGroups)
            {
                if (!row.QuestionGroupId.HasValue || !byGroup.TryGetValue(row.QuestionGroupId.Value, out var section))
                {
                    continue;
                }
                row.QuestionSectionId = section.QuestionSectionId;
                row.QuestionSectionName = section.QuestionSectionName;
            }
        }

        private void AttachQuestionSubGroupSections(List<QuestionSubGroupModel> questionSubGroups)
        {
            if (questionSubGroups == null || questionSubGroups.Count == 0)
            {
                return;
            }

            var subGroupIds = questionSubGroups.Select(x => x.QuestionSubgroupId).ToList();

            var sectionLinks = (from map in context.QuestionSubgroupSections
                                join section in context.SectionMasters on map.SectionId equals section.SectionId
                                where subGroupIds.Contains(map.QuestionSubgroupId)
                                      && !map.DeleteStatus
                                      && !section.DeleteStatus
                                select new
                                {
                                    map.QuestionSubgroupId,
                                    Section = new SectionViewModel
                                    {
                                        SectionId = section.SectionId,
                                        SectionName = section.SectionName,
                                        SectionAlias = section.SectionAlias,
                                        Description = section.Description
                                    }
                                }).ToList();

            foreach (var subGroup in questionSubGroups)
            {
                var sections = sectionLinks
                    .Where(x => x.QuestionSubgroupId == subGroup.QuestionSubgroupId)
                    .Select(x => x.Section)
                    .ToList();

                subGroup.Sections = sections;
                subGroup.SectionIds = sections
                    .Where(x => x.SectionId.HasValue)
                    .Select(x => x.SectionId.Value)
                    .ToList();
            }
        }
    }
}

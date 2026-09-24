using API.Helpers;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using System.Net;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.Implementation
{
    /// <summary>
    /// This is implementation  for the Question Group operations 
    /// </summary>
    public class QuestionGroupService : IQuestionGroupService
    {
        NIGACentrumContext context;
        public QuestionGroupService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        public async Task<QuestionGroupMaster> GetQuestionById(long questiongroupId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var questiongroupEntity = await context.QuestionGroupMasters.FirstOrDefaultAsync(x => x.QuestionGroupId == questiongroupId && !x.DeleteStatus);
            if (questiongroupEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Question Group not found";
            }
            return questiongroupEntity;
        }

        /// <summary>
        /// Method to get all the Question Group
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<PagedList<QuestionGroupModel1>> GetQuestionGroupList(ParameterParams parameter)
        {
            var questiongroupModelList = (from x in context.QuestionGroupMasters
                                         join qs in context.QuestionSectionMasters 
                                         on x.QuestionSectionId equals qs.QuestionSectionId        
                                          where x.DeleteStatus == false
                                          select new QuestionGroupModel1
                                          {
                                              QuestionGroupId = x.QuestionGroupId,
                                              QuestionSectionId = x.QuestionSectionId,
                                              QuestionGroupName = x.QuestionGroupName,
                                              SectionId = x.SectionId,
                                              Description = x.Description,
                                              EnteredDate = x.EnteredDate,
                                              EnteredBy = x.EnteredBy,
                                              ChangedBy = x.ChangedBy,
                                              ChangedDate = x.ChangedDate,
                                              DeleteStatus = x.DeleteStatus,
                                             QuestionSectionName = qs.QuestionSectionName
                                          }).AsQueryable();
            if (!string.IsNullOrEmpty(parameter.search))
            {
                questiongroupModelList = questiongroupModelList.Where(x => x.QuestionGroupName.ToLower().Contains(parameter.search.ToLower()));

            }
            return await PagedList<QuestionGroupModel1>.CreateAsync(questiongroupModelList.AsNoTracking(), parameter.PageNumber,
                parameter.PageSize);

          
        }


        // public List<QuestionGroupModel1> GetQuestionGroupExistance(ref ErrorResponseModel errorResponseModel)
        // {
        //     var MateriaMedicaHeadList = new List<QuestionGroupModel1>();
        //     errorResponseModel = new ErrorResponseModel();
        //     var materiaMedicaheadEntityList = (from m in context.QuestionGroupMaster
        //                                        join auth in context.QuestionSectionMaster on m.QuestionSectionId equals auth.QuestionSectionId
        //                                        where m.DeleteStatus == false
        //                                        select new
        //                                        {
        //                                            m.QuestionGroupId,
        //                                            m.QuestionSectionId,
        //                                            m.QuestionGroupName,
        //                                            m.Description,
        //                                            m.EnteredDate,
        //                                            m.EnteredBy,
        //                                            m.ChangedDate,
        //                                            m.ChangedBy,
        //                                            m.DeleteStatus,
        //                                            auth.QuestionSectionName,
        //                                            m.SectionId
        //                                        }).ToList();
        //     if (materiaMedicaheadEntityList.Count == 0)
        //     {
        //         errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //         errorResponseModel.Message = "MateriaMedicaHead not found";
        //     }



        //     materiaMedicaheadEntityList.ForEach(item =>
        //     {
        //         MateriaMedicaHeadList.Add(new QuestionGroupModel1
        //         {
        //             QuestionGroupId = item.QuestionGroupId,
        //             QuestionGroupName = item.QuestionGroupName,
        //             QuestionSectionId = item.QuestionSectionId,
        //             Description = item.Description,
        //             EnteredBy = item.EnteredBy,
        //             EnteredDate = item.EnteredDate,
        //             QuestionSectionName = item.QuestionSectionName,
        //             ChangedBy = item.ChangedBy,
        //             ChangedDate = item.ChangedDate,
        //             DeleteStatus = item.DeleteStatus,
        //             SectionId = item.SectionId
        //         });
        //     });
        //     return MateriaMedicaHeadList;
        // }

        public async Task<List<QuestionGroupModel1>> GetQuestionGroupByExistanceId(long QuestionSectionId)
        {
           var errorResponseModel = new ErrorResponseModel();
            var questiongroupEntity = await (from q in context.QuestionGroupMasters
                                      where q.QuestionSectionId == QuestionSectionId && q.DeleteStatus == false
                                      select new QuestionGroupModel1{
                                        QuestionGroupId = q.QuestionGroupId,    
                                        QuestionGroupName = q.QuestionGroupName,
                                        QuestionSectionId = q.QuestionSectionId,
                                        Description = q.Description,
                                        EnteredBy = q.EnteredBy,
                                        EnteredDate = q.EnteredDate,
                                        ChangedBy = q.ChangedBy,
                                        ChangedDate = q.ChangedDate,
                                        DeleteStatus = q.DeleteStatus,
                                      }).ToListAsync();
            
            if (questiongroupEntity.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Question Group not found";
            }
           
            return questiongroupEntity;
        }


        public async Task<QuestionGroupModel> GetQuestionDetailsById(long questiongroupId)
        {
            var questiongroupEntity = await (from q in context.QuestionGroupMasters
                                      where q.QuestionGroupId == questiongroupId && q.DeleteStatus == false
                                      select new QuestionGroupModel
                                      {
                                        QuestionGroupId = q.QuestionGroupId,    
                                        QuestionGroupName = q.QuestionGroupName,
                                        QuestionSectionId = q.QuestionSectionId,
                                        Description = q.Description,
                                        EnteredBy = q.EnteredBy,
                                        EnteredDate = q.EnteredDate,
                                        ChangedBy = q.ChangedBy,
                                        ChangedDate = q.ChangedDate,
                                        DeleteStatus = q.DeleteStatus,
                                      }).FirstOrDefaultAsync();
             return questiongroupEntity;
        }

       
        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }


        public void SaveQuestionGroup(QuestionGroupMaster questionGroup)
        {
            context.Entry(questionGroup).State = EntityState.Added;
        }

        public void UpdateQuestionGroup(QuestionGroupMaster questionGroup)
        {
            context.Entry(questionGroup).State = EntityState.Modified;
        }

        public void DeleteQuestionGroup(QuestionGroupMaster questionGroup)
        {
            questionGroup.DeleteStatus = true;
            context.Entry(questionGroup).State = EntityState.Modified;

        }

        // public Task<QuestionGroupMaster> GetQuestionById(long questiongroupId, ref ErrorResponseModel errorResponseModel)
        // {
        //     throw new NotImplementedException();
        // }

        // public List<QuestionGroupModel1> GetQuestionGroupExistance(ref ErrorResponseModel errorResponseModel)
        // {
        //     throw new NotImplementedException();
        // }
    }
}

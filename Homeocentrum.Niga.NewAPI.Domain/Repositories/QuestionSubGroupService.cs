using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using System.Net;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using API.Helpers;
using AutoMapper;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
{
    public class QuestionSubGroupService : IQuestionSubGroupService
    {
        private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;

        public QuestionSubGroupService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<QuestionSubgroup> GetQuestionSubGroupById(long questionSubGroupId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var questionSubGroupEntity = await _context.QuestionSubgroups.FirstOrDefaultAsync(x => x.QuestionSubgroupId == questionSubGroupId && x.DeleteStatus == false);
            if (questionSubGroupEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Question sub group not found";
            }
            return questionSubGroupEntity;
        }

        public async Task<PagedList<QuestionSubGroupModel>> GetAllQuestionSubGroups(ParameterParams parameter)
        {
            var questionSubGroupModelQuery = (from x in _context.QuestionSubgroups
                                              join g in _context.QuestionGroupMasters
                                              on x.QuestionGroupId equals g.QuestionGroupId
                                              select new QuestionSubGroupModel
                                              {
                                                  QuestionSubgroupId = x.QuestionSubgroupId,
                                                  QuestionGroupId = x.QuestionGroupId,
                                                  QuestionSubGroupName = x.QuestionSubgroup1,
                                                  QuestionGroupName = g.QuestionGroupName,
                                                  Description = x.Description,
                                                  DeleteStatus = x.DeleteStatus
                                              }).AsQueryable();

            if (!string.IsNullOrEmpty(parameter.search))
            {
                questionSubGroupModelQuery = questionSubGroupModelQuery.Where(x => x.QuestionSubGroupName.ToLower().Contains(parameter.search.ToLower()));
            }

            return await PagedList<QuestionSubGroupModel>.CreateAsync(questionSubGroupModelQuery.AsNoTracking(), parameter.PageNumber, parameter.PageSize);
        }


        public void SaveQuestionSubGroup(QuestionSubgroup questionSubGroup)
        {
            _context.Entry(questionSubGroup).State = EntityState.Added;
        }

        public void UpdateQuestionSubGroup(QuestionSubgroup questionSubGroup)
        {
            _context.Entry(questionSubGroup).State = EntityState.Modified;
        }

        public void DeleteQuestionSubGroup(QuestionSubgroup questionSubGroup)
        {
            questionSubGroup.DeleteStatus = true;
            _context.Entry(questionSubGroup).State = EntityState.Modified;
        }

        public async Task<QuestionSubGroupModel> GetQuestionSubGroupDetailsById(long questionSubGroupId)
        {
            var questionSubGroupDetails = await (from q in _context.QuestionSubgroups
                                                 where q.QuestionSubgroupId == questionSubGroupId && q.DeleteStatus == false
                                                 select new QuestionSubGroupModel
                                                 {
                                                     QuestionGroupId = q.QuestionGroupId,
                                                     QuestionSubGroupName = q.QuestionSubgroup1,
                                                     QuestionSubgroupId = q.QuestionSubgroupId,
                                                     Description = q.Description,
                                                     DeleteStatus = q.DeleteStatus
                                                 })
                                             .FirstOrDefaultAsync();
            return questionSubGroupDetails;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<QuestionSubGroupModel>> GetQuestionSubGroupDD(string search)
        {
            var questionSubGroupModelQuery = await (from x in _context.QuestionSubgroups
                                                    select new QuestionSubGroupModel
                                                    {
                                                        QuestionSubgroupId = x.QuestionSubgroupId,
                                                        QuestionSubGroupName = x.QuestionSubgroup1,
                                                        Description = x.Description,
                                                        DeleteStatus = x.DeleteStatus,
                                                        QuestionGroupId = x.QuestionGroupId
                                                    }).ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                questionSubGroupModelQuery = questionSubGroupModelQuery.Where(x => x.QuestionSubGroupName.ToLower().Contains(search.ToLower())).ToList();
            }

            return questionSubGroupModelQuery;
        }

       
    }
}
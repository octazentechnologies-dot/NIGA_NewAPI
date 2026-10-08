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
                                                 join g in _context.QuestionGroupMasters
                                                     on q.QuestionGroupId equals (int?)g.QuestionGroupId into groupJoin
                                                 from g in groupJoin.DefaultIfEmpty()
                                                 select new QuestionSubGroupModel
                                                 {
                                                     QuestionGroupId = q.QuestionGroupId,
                                                     QuestionGroupName = g != null ? g.QuestionGroupName : null,
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
            var questionSubGroupModelQuery = await (from x in _context.QuestionSubgroups.AsNoTracking()
                                                    join g in _context.QuestionGroupMasters.AsNoTracking()
                                                        on x.QuestionGroupId equals (int?)g.QuestionGroupId into groupJoin
                                                    from g in groupJoin.DefaultIfEmpty()
                                                    select new QuestionSubGroupModel
                                                    {
                                                        QuestionSubgroupId = x.QuestionSubgroupId,
                                                        QuestionSubGroupName = x.QuestionSubgroup1,
                                                        QuestionGroupName = g != null ? g.QuestionGroupName : null,
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


        #region Old API compatible overloads
#nullable disable

        /// <summary>
        /// Method is used for delete QuestionSubGroup.
        /// </summary>
        /// <param name="questionSubGroupModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        public string DeleteQuestionSubGroup(QuestionSubGroupModel questionSubGroupModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var questionsubEntity = _context.QuestionSubgroups.FirstOrDefault(x => x.QuestionSubgroupId == questionSubGroupModel.QuestionSubgroupId);
            if (questionsubEntity != null)
            {
                questionsubEntity.DeleteStatus = true;

                var mappingRows = _context.QuestionSubgroupSections
                    .Where(x => x.QuestionSubgroupId == questionsubEntity.QuestionSubgroupId && !x.DeleteStatus)
                    .ToList();
                foreach (var row in mappingRows)
                {
                    row.DeleteStatus = true;
                    row.ChangedDate = DateTime.Now;
                }

                _context.SaveChanges();
                Message = "QuestionSubGroup Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Method implementation for saving new QuestionSubGroup
        /// </summary>
        /// <param name="questionSubGroupModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        public string SaveQuestionSubGroup(QuestionSubGroupModel questionSubGroupModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (questionSubGroupModel.QuestionSubgroupId == 0)
            {
                QuestionSubgroup questionsubEntity = new QuestionSubgroup();
                questionsubEntity.QuestionSubgroupId = questionSubGroupModel.QuestionSubgroupId;
                questionsubEntity.QuestionGroupId = questionSubGroupModel.QuestionGroupId;
                questionsubEntity.QuestionSubgroup1 = questionSubGroupModel.QuestionSubGroupName;
                questionsubEntity.Description = questionSubGroupModel.Description;
                questionsubEntity.DeleteStatus = false;
                _context.QuestionSubgroups.Add(questionsubEntity);
                _context.SaveChanges();

                SyncSections(questionsubEntity.QuestionSubgroupId, questionSubGroupModel.SectionIds);
                Message = "QuestionSubGroup Saved Successfully";
            }
            else
            {
                var questionsubEntity = _context.QuestionSubgroups.FirstOrDefault(x => x.QuestionSubgroupId == questionSubGroupModel.QuestionSubgroupId);
                if (questionsubEntity != null)
                {
                    questionsubEntity.QuestionSubgroupId = questionSubGroupModel.QuestionSubgroupId;
                    questionsubEntity.QuestionGroupId = questionSubGroupModel.QuestionGroupId;
                    questionsubEntity.QuestionSubgroup1 = questionSubGroupModel.QuestionSubGroupName;
                    questionsubEntity.Description = questionSubGroupModel.Description;
                    questionsubEntity.DeleteStatus = false;
                    _context.SaveChanges();

                    SyncSections(questionsubEntity.QuestionSubgroupId, questionSubGroupModel.SectionIds);
                    Message = "QuestionSubGroup Updated Successfully";
                }
            }
            return Message;
        }

        private void SyncSections(int questionSubgroupId, List<int> sectionIds)
        {
            var existingRows = _context.QuestionSubgroupSections
                .Where(x => x.QuestionSubgroupId == questionSubgroupId)
                .ToList();

            if (existingRows.Any())
            {
                _context.QuestionSubgroupSections.RemoveRange(existingRows);
                _context.SaveChanges();
            }

            var distinctSectionIds = (sectionIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            foreach (var sectionId in distinctSectionIds)
            {
                _context.QuestionSubgroupSections.Add(new QuestionSubgroupSection
                {
                    QuestionSubgroupId = questionSubgroupId,
                    SectionId = sectionId,
                    DeleteStatus = false,
                    EnteredDate = DateTime.Now
                });
            }

            if (distinctSectionIds.Any())
            {
                _context.SaveChanges();
            }
        }

#nullable restore
        #endregion
    }
}
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Interfaces;
using Niga_Domain.DTOs;
using System.Net;
using Niga_Domain.Data;
using Niga_Domain.Master;
using Niga_Domain.Helpers;
using API.Helpers;
using AutoMapper;

namespace Niga_Domain.Repositories
{
    public class QuestionSectionService : IQuestionSectionService
    {
        private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;

        public QuestionSectionService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<QuestionSectionMaster> GetQuestionSectionById(long questionSectionId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var questionSectionEntity = await _context.QuestionSectionMasters.FirstOrDefaultAsync(x => x.QuestionSectionId == questionSectionId && !x.DeleteStatus);
            if (questionSectionEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Question section not found";
            }
            return questionSectionEntity;
        }

        public async Task<PagedList<QuestionSectionModel>> GetAllQuestionSections(ParameterParams parameter)
        {
            var questionSectionModelQuery = (from x in _context.QuestionSectionMasters
                                           select new QuestionSectionModel
                                           {
                                               QuestionSectionId = x.QuestionSectionId,
                                               QuestionSectionName = x.QuestionSectionName,
                                               Description = x.Desciption,
                                               EnteredBy = x.EnteredBy,
                                               EnteredDate = x.EnteredDate,
                                               ChangedBy = x.ChangedBy,
                                               ChangedDate = x.ChangedDate,
                                               DeleteStatus = x.DeleteStatus
                                           }).AsQueryable();

            if (!string.IsNullOrEmpty(parameter.search))
            {
                questionSectionModelQuery = questionSectionModelQuery.Where(x => x.QuestionSectionName.ToLower().Contains(parameter.search.ToLower()));
            }

            return await PagedList<QuestionSectionModel>.CreateAsync(questionSectionModelQuery.AsNoTracking(), parameter.PageNumber, parameter.PageSize);
        }

       
        public void SaveQuestionSection(QuestionSectionMaster questionSection)
        {
            _context.Entry(questionSection).State = EntityState.Added;
        }

        public void UpdateQuestionSection(QuestionSectionMaster questionSection)
        {
            _context.Entry(questionSection).State = EntityState.Modified;
        }

        public void DeleteQuestionSection(QuestionSectionMaster questionSection)
        {
            questionSection.DeleteStatus = true;
            _context.Entry(questionSection).State = EntityState.Modified;
        }

        public async Task<QuestionSectionModel> GetQuestionSectionDetailsById(long questionSectionId)
        {
            var questionSectionDetails = await (from q in _context.QuestionSectionMasters
                                              where q.QuestionSectionId == questionSectionId && q.DeleteStatus == false
                                              select new QuestionSectionModel
                                              {
                                                  QuestionSectionId = q.QuestionSectionId,
                                                  QuestionSectionName = q.QuestionSectionName,
                                                  Description = q.Desciption,
                                                  DeleteStatus = q.DeleteStatus
                                              })
                                            .FirstOrDefaultAsync();
            return questionSectionDetails;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<QuestionSectionModel>> GetQuestionSectionDD(string? search)
        {
            var questionSectionModelQuery = await (from x in _context.QuestionSectionMasters
                                                 select new QuestionSectionModel
                                                 {
                                                     QuestionSectionId = x.QuestionSectionId,
                                                     QuestionSectionName = x.QuestionSectionName,
                                                     Description = x.Desciption,
                                                     DeleteStatus = x.DeleteStatus
                                                 }).ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                questionSectionModelQuery = questionSectionModelQuery.Where(x => x.QuestionSectionName.ToLower().Contains(search.ToLower())).ToList();
            }

            return questionSectionModelQuery;
        }
    }
}
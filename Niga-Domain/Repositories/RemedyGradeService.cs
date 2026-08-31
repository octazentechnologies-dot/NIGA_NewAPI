using Microsoft.EntityFrameworkCore;
using Niga_Domain.Interfaces;
using Niga_Domain.DTOs;
using System.Net;
using Niga_Domain.Data;
using Niga_Domain.Master;
using Niga_Domain.Helpers;
using API.Helpers;
using AutoMapper;

namespace Niga_Domain.Services
{
    /// <summary>
    /// Service implementation for remedy grade related operations
    /// </summary>
    public class RemedyGradeService : IRemedyGradeRepository
    {
        private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor for RemedyGradeService
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="mapper">AutoMapper instance</param>
        public RemedyGradeService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets a remedy grade by its ID
        /// </summary>
        /// <param name="remedyGradeId">ID of the remedy grade to retrieve</param>
        /// <returns>Remedy grade model if found, null otherwise</returns>
        public async Task<RemedyGradeMaster> GetRemedyGradeById(long remedyGradeId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var remedyGradeEntity = await _context.RemedyGradeMaster.FirstOrDefaultAsync(x => x.GradeId == remedyGradeId && x.DeleteStatus==false);
            if (remedyGradeEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy grade not found";
            }
            return remedyGradeEntity;
        }

        /// <summary>
        /// Gets all remedy grades
        /// </summary>
        /// <param name="parameter">Parameter for filtering and paging</param>
        /// <returns>Paged list of remedy grade models</returns>
        public async Task<PagedList<RemedyGradeModel>> GetAllRemedyGrades(ParameterParams parameter)
        {
             //var remedy= _context.RemedyGradeMaster.ToList();
            var remedyGradeModelQuery = (from x in _context.RemedyGradeMaster
                                        where x.DeleteStatus == false
                                       select new RemedyGradeModel
                                       {
                                           GradeId = x.GradeId,
                                           GradeNo = x.GradeNo,
                                           Description = x.Description!=null ?x.Description:"NA",
                                           EnteredBy = x.EnteredBy,
                                           EnteredDate = x.EnteredDate,
                                           ChangedBy = x.ChangedBy,
                                           ChangedDate = x.ChangedDate,
                                           DeleteStatus = x.DeleteStatus
                                       }).AsQueryable();

            if (!string.IsNullOrEmpty(parameter.search))
            {
                remedyGradeModelQuery = remedyGradeModelQuery.Where(x => x.GradeNo.ToString().Contains(parameter.search));
            }

            return await PagedList<RemedyGradeModel>.CreateAsync(remedyGradeModelQuery.AsNoTracking(), parameter.PageNumber, parameter.PageSize);
        }

        /// <summary>
        /// Gets all remedy grades by filter
        /// </summary>
        /// <param name="search">Search string</param>
        /// <param name="errorResponseModel">Error response model to be populated if any error occurs</param>
        /// <returns>List of remedy grade models</returns>
      
        /// <summary>
        /// Saves or updates a remedy grade
        /// </summary>
        /// <param name="remedyGrade">Remedy grade entity to save or update</param>
        public void SaveRemedyGrade(RemedyGradeMaster remedyGrade)
        {
            _context.Entry(remedyGrade).State = EntityState.Added;
        }

        /// <summary>
        /// Updates a remedy grade
        /// </summary>
        /// <param name="remedyGrade">Remedy grade entity to update</param>
        public void UpdateRemedyGrade(RemedyGradeMaster remedyGrade)
        {
            _context.Entry(remedyGrade).State = EntityState.Modified;
        }

        /// <summary>
        /// Deactivates a remedy grade
        /// </summary>
        /// <param name="remedyGrade">Remedy grade entity to delete</param>
        public void DeleteRemedyGrade(RemedyGradeMaster remedyGrade)
        {
            remedyGrade.DeleteStatus = true;
            _context.Entry(remedyGrade).State = EntityState.Modified;
        }

        /// <summary>
        /// Gets the details of a remedy grade
        /// </summary>
        /// <param name="remedyGradeId">ID of the remedy grade to retrieve</param>
        /// <returns>Remedy grade model if found, null otherwise</returns>
        public async Task<RemedyGradeModel> GetRemedyGradeDetailsById(long remedyGradeId)
        {
            var remedyGradeDetails = await (from r in _context.RemedyGradeMaster
                                          where r.GradeId == remedyGradeId && r.DeleteStatus == false
                                          select new RemedyGradeModel
                                          {
                                              GradeId = r.GradeId,
                                              GradeNo = r.GradeNo,
                                              Description = r.Description,
                                              DeleteStatus = r.DeleteStatus
                                          })
                                        .FirstOrDefaultAsync();
            return remedyGradeDetails;
        }

        /// <summary>
        /// Saves all changes to the database
        /// </summary>
        /// <returns>True if changes are saved successfully, false otherwise</returns>
        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
} 
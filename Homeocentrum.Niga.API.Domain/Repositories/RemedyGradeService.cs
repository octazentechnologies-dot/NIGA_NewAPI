using Homeocentrum.Niga.API.Domain.Business.Interface;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.DTOs;
using System.Net;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Helpers;
using API.Helpers;

namespace Homeocentrum.Niga.API.Domain.Services
{
    /// <summary>
    /// Service implementation for remedy grade related operations
    /// </summary>
    public class RemedyGradeService : IRemedyGradeRepository, IRemedyGradeService
    {
        private readonly NIGACentrumContext _context;

        /// <summary>
        /// Constructor for RemedyGradeService
        /// </summary>
        /// <param name="context">Database context</param>
        public RemedyGradeService(NIGACentrumContext context)
        {
            _context = context;
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
                                           FontName = x.FontName,
                                           FontStyle = x.FontStyle,
                                           FontColor = x.FontColor,
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
                                              FontName = r.FontName,
                                              FontStyle = r.FontStyle,
                                              FontColor = r.FontColor,
                                              EnteredBy = r.EnteredBy,
                                              EnteredDate = r.EnteredDate,
                                              ChangedBy = r.ChangedBy,
                                              ChangedDate = r.ChangedDate,
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


        #region Old API compatible methods
#nullable disable

        /// <summary>
        /// Method is used for delete Remedy Grade.
        /// </summary>
        /// <param name="remedyGradeModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteRemedyGrade(RemedyGradeModel remedyGradeModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var remedyGradeEntity = _context.RemedyGradeMaster.FirstOrDefault(x => x.GradeId == remedyGradeModel.GradeId);
            if (remedyGradeEntity != null)
            {
                remedyGradeEntity.DeleteStatus = remedyGradeModel.DeleteStatus;
                remedyGradeEntity.ChangedBy = remedyGradeModel.EnteredBy;
                remedyGradeEntity.ChangedDate = DateTime.Now;
                _context.SaveChanges();
                Message = "Remedy Grade Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Method implementation for saving new Remedy Grade
        /// </summary>
        /// <param name="remedyGradeModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveRemedyGrade(RemedyGradeModel remedyGradeModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (remedyGradeModel.GradeId == 0)
            {
                RemedyGradeMaster remedyGradeEntity = new RemedyGradeMaster();
                remedyGradeEntity.GradeNo = remedyGradeModel.GradeNo;
                remedyGradeEntity.Description = remedyGradeModel.Description;
                remedyGradeEntity.FontName = remedyGradeModel.FontName;
                remedyGradeEntity.FontStyle = remedyGradeModel.FontStyle;
                remedyGradeEntity.FontColor = remedyGradeModel.FontColor;
                remedyGradeEntity.EnteredBy = remedyGradeModel.EnteredBy;
                remedyGradeEntity.EnteredDate = DateTime.Now;
                _context.RemedyGradeMaster.Add(remedyGradeEntity);
                _context.SaveChanges();
                Message = "Remedy Grade Saved Successfully";
            }
            else
            {
                var remedyGradeEntity = _context.RemedyGradeMaster.FirstOrDefault(x => x.GradeId == remedyGradeModel.GradeId);
                if (remedyGradeEntity !=  null)
                {

                    remedyGradeEntity.GradeNo = remedyGradeModel.GradeNo;
                    remedyGradeEntity.Description = remedyGradeModel.Description;
                    remedyGradeEntity.FontName = remedyGradeModel.FontName;
                    remedyGradeEntity.FontStyle = remedyGradeModel.FontStyle;
                    remedyGradeEntity.FontColor = remedyGradeModel.FontColor;
                    remedyGradeEntity.ChangedBy = remedyGradeModel.EnteredBy;
                    remedyGradeEntity.ChangedDate = DateTime.Now;
                    _context.SaveChanges();
                    Message = "Remedy Grade Updated Successfully";
                }
            }
            return Message;
        }

#nullable restore
        #endregion
    }
} 
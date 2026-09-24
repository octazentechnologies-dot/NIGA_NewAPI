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
    public class QualificationService : IQualificationService
    {
         private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;

        public QualificationService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }        public async Task<QualificationMaster> GetQualificationById(long qualificationId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var qualificationEntity = await _context.QualificationMasters.FirstOrDefaultAsync(x => x.QualificationId == qualificationId && !x.DeleteStatus);
            if (qualificationEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Qualification not found";
            }
            return qualificationEntity;
        }

        public async Task<PagedList<QualificationModel>> GetAllQualifications(ParameterParams parameter)
        {
            var qualificationModelQuery = (from x in _context.QualificationMasters
                                           where !x.DeleteStatus
                                           select new QualificationModel
                                           {
                                               QualificationId = x.QualificationId,
                                               QualificationName = x.QualificationName,
                                               QualificationAlias = x.QualificationAlias,
                                               Description = x.Description,
                                               DegreeLevel = x.DegreeLevel,
                                               EnteredBy = x.EnteredBy,
                                               EnteredDate = x.EnteredDate,
                                               ChangedBy = x.ChangedBy,
                                               ChangedDate = x.ChangedDate,
                                               DeleteStatus = x.DeleteStatus
                                           }).AsQueryable();

            if (!string.IsNullOrEmpty(parameter.search))
            {
                qualificationModelQuery = qualificationModelQuery.Where(x =>
                    x.QualificationName.ToLower().Contains(parameter.search.ToLower()) ||
                    (x.QualificationAlias != null && x.QualificationAlias.ToLower().Contains(parameter.search.ToLower())));
            }

            return await PagedList<QualificationModel>.CreateAsync(qualificationModelQuery.AsNoTracking(), parameter.PageNumber, parameter.PageSize);
        }

        public List<QualificationModel> GetAllQualificationsByFilter(string search, ref ErrorResponseModel errorResponseModel)
        {
            var qualificationModelQuery = (from x in _context.QualificationMasters
                                           where !x.DeleteStatus
                                           select new QualificationModel
                                           {
                                               QualificationId = x.QualificationId,
                                               QualificationName = x.QualificationName,
                                               QualificationAlias = x.QualificationAlias,
                                               Description = x.Description,
                                               DegreeLevel = x.DegreeLevel,
                                               EnteredBy = x.EnteredBy,
                                               EnteredDate = x.EnteredDate,
                                               ChangedBy = x.ChangedBy,
                                               ChangedDate = x.ChangedDate,
                                               DeleteStatus = x.DeleteStatus
                                           }).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                qualificationModelQuery = qualificationModelQuery.Where(x =>
                    x.QualificationName.ToLower().Contains(search.ToLower()) ||
                    (x.QualificationAlias != null && x.QualificationAlias.ToLower().Contains(search.ToLower()))).ToList();
            }

            return qualificationModelQuery;
        }

        public void SaveQualification(QualificationMaster qualification)
        {
            if (qualification.EnteredDate == null)
            {
                qualification.EnteredDate = DateTime.Now;
            }
            qualification.DeleteStatus = false;
            _context.Entry(qualification).State = EntityState.Added;
        }

        public void UpdateQualification(QualificationMaster qualification)
        {
            qualification.ChangedDate = DateTime.Now;
            _context.Entry(qualification).State = EntityState.Modified;
        }

        public void DeleteQualification(QualificationMaster qualification)
        {
            qualification.DeleteStatus = true;
            _context.Entry(qualification).State = EntityState.Modified;
        }

        public async Task<QualificationModel> GetQualificationDetailsById(long qualificationId)
        {
            var qualificationDetails = await (from q in _context.QualificationMasters
                                              where q.QualificationId == qualificationId && q.DeleteStatus == false
                                              select new QualificationModel
                                              {
                                                  QualificationId = q.QualificationId,
                                                  QualificationName = q.QualificationName,
                                                  Description = q.Description,
                                                  DeleteStatus = q.DeleteStatus
                                              })
                                          .FirstOrDefaultAsync();
            return qualificationDetails;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

    }
}
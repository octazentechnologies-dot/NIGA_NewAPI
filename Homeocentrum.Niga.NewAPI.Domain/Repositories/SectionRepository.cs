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

    public class SectionService : ISectionRepository
    {
        private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;

        public SectionService(NIGACentrumContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<SectionMaster> GetSectionById(long sectionId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var sectionEntity = await _context.SectionMasters.FirstOrDefaultAsync(x => x.SectionId == sectionId && !x.DeleteStatus);
            if (sectionEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Section not found";
            }
            return sectionEntity;
        }

        public async Task<PagedList<SectionList>> getAllSections(ParameterParams parameter)
        {
            var sectionModelQuery = (from x in _context.SectionMasters
                                     select new SectionList
                                     {
                                         SectionId = x.SectionId,
                                         SectionName = x.SectionName,
                                         SectionAlias = x.SectionAlias,
                                         Description = x.Description,
                                         EnteredBy = x.EnteredBy,
                                         EnteredDate = x.EnteredDate,
                                         ChangedBy = x.ChangedBy,
                                         ChangedDate = x.ChangedDate,
                                         DeleteStatus = x.DeleteStatus,
                                         //  listSubSectionModel = (from sub in _context.SubSectionMaster
                                         //                         where sub.SectionId == x.SectionId && sub.DeleteStatus == false
                                         //                         select new SubSectionModel
                                         //                         {
                                         //                             SubSectionId = sub.SubSectionId,
                                         //                             ParentSubSectionId = sub.ParentSubSectionId,
                                         //                             SubSectionName = sub.SubSectionName
                                         //                         }).ToList()
                                     }).AsQueryable();
            if (!string.IsNullOrEmpty(parameter.search))
            {
                sectionModelQuery = sectionModelQuery.Where(x => x.SectionName.ToLower().Contains(parameter.search.ToLower()));

            }
            return await PagedList<SectionList>.CreateAsync(sectionModelQuery.AsNoTracking(), parameter.PageNumber,
                parameter.PageSize);

        }

        public List<SectionModel> getAllRemedyByFilter(string search, int SectionId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var sectionModelList = new List<SectionModel>();
            //---if sectionId>0 filter by section
            var listSectionModelEntity = _context.SectionMasters.Include(x => x.SubSectionMasters)
                .Where(x => x.DeleteStatus == false && (SectionId > 0 ? x.SectionId == SectionId : true)).ToList();


            if (listSectionModelEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Section not found";
            }
            foreach (var item in listSectionModelEntity)
            {
                SectionModel sectionModel = new SectionModel();
                sectionModel.SectionId = item.SectionId;
                sectionModel.SectionName = item.SectionName;
                if (!string.IsNullOrEmpty(search))
                {
                    item.SubSectionMasters = item.SubSectionMasters.Where(s => (!string.IsNullOrEmpty(search) ? s.SubSectionName.ToLower().Contains(search.ToLower()) : true)).ToList();
                    foreach (var subSectionItem in item.SubSectionMasters)
                    {


                        SubSectionModel subSectionModel = new SubSectionModel();
                        subSectionModel.SubSectionId = subSectionItem.SubSectionId;
                        subSectionModel.ParentSubSectionId = subSectionItem.ParentSubSectionId;
                        subSectionModel.SubSectionName = subSectionItem.SubSectionName;
                        sectionModel.listSubSectionModel.Add(subSectionModel);
                    }

                }
                else
                {
                    foreach (var subSectionItem in item.SubSectionMasters)
                    {
                        SubSectionModel subSectionModel = new SubSectionModel();
                        subSectionModel.SubSectionId = subSectionItem.SubSectionId;
                        subSectionModel.ParentSubSectionId = subSectionItem.ParentSubSectionId;
                        subSectionModel.SubSectionName = subSectionItem.SubSectionName;
                        sectionModel.listSubSectionModel.Add(subSectionModel);
                    }

                }
                sectionModelList.Add(sectionModel);

            }

            return sectionModelList;

        }


        public void SaveSection(SectionMaster section)
        {
             _context.Entry(section).State=EntityState.Added;
        }

        public void UpdateSection(SectionMaster section)
        {
            _context.Entry(section).State = EntityState.Modified;

        }

        public void DeleteSection(SectionMaster section)
        {
            section.DeleteStatus = true;
            _context.Entry(section).State = EntityState.Modified;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<SectionMasterDto> GetSectionDetailsById(long sectionId)
        {
             var user= _context.UserMasters.FirstOrDefault();
            var sectionDetails = await (from s in _context.SectionMasters
                                       where s.SectionId == sectionId && s.DeleteStatus == false
                                       select new SectionMasterDto
                                       {

                                           SectionId = s.SectionId,
                                           SectionName = s.SectionName,
                                           SectionAlias = s.SectionAlias,
                                           Description = s.Description,
                                           DeleteStatus = s.DeleteStatus
                                       })
                                     .FirstOrDefaultAsync();
            return sectionDetails;
        }

        public async Task<List<SectionMasterDto>> GetSectionDD(string? Search)
        {
            var sectionModelQuery = await (from x in _context.SectionMasters
                                           select new SectionMasterDto
                                           {
                                               SectionId = x.SectionId,
                                               SectionName = x.SectionName,
                                               SectionAlias = x.SectionAlias,
                                               Description = x.Description,
                                               DeleteStatus = x.DeleteStatus,
                                           }).ToListAsync();
            if (!string.IsNullOrEmpty(Search))
            {
                sectionModelQuery = sectionModelQuery.Where(x => x.SectionName.ToLower().Contains(Search.ToLower())).ToList();
            }
            return sectionModelQuery;
        }
    }
}


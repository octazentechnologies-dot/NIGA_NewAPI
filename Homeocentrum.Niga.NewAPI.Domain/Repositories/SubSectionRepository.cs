using System.IO;
using System.Net;
using API.Helpers;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
{
    /// <summary>
    /// This is implementation  for the subsection operations 
    /// </summary>
    public class SubSectionService : ISubSectionRepository
    {
        private readonly NIGACentrumContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public SubSectionService(NIGACentrumContext centrumContext, IMapper mapper, IWebHostEnvironment env)
        {
            _context = centrumContext;
            _mapper = mapper;
            _env = env;
        }

        public void DeleteLanguageDetails(SubSectionLanguageDetail languageDetails)
        {
            languageDetails.DeleteStatus = true;
            _context.Entry(languageDetails).State = EntityState.Modified;

        }

        public void DeleteLanguageDetails(SubSectionMaster subSection)
        {
            throw new NotImplementedException();
        }

        public void DeleterubricDetails(ReferenceRubricDetail rubricDetails)
        {
            rubricDetails.DeleteStatus = true;
            _context.Entry(rubricDetails).State = EntityState.Modified;
        }



        public void DeleteSubSection(SubSectionMaster subSection)
        {
            subSection.DeleteStatus = true;
            _context.Entry(subSection).State = EntityState.Modified;
        }

        public async Task<ReferenceRubricDetail> GetReferenceRubricById(int ReferenceRubricId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var ReferenceRubric = await _context.ReferenceRubricDetails.FirstOrDefaultAsync(x => x.ReferenceRubricId == ReferenceRubricId && x.DeleteStatus == false);
            if (ReferenceRubric == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Reference Rubric not found";
            }
            return ReferenceRubric;
        }

        public async Task<SubSectionLanguageDetail> GetSubLanguageById(int SubSectionLanguageId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var SubSectionLanguageDetails = await _context.SubSectionLanguageDetails.FirstOrDefaultAsync(x => x.SubSectionLanguageId == SubSectionLanguageId && x.DeleteStatus == false);
            if (SubSectionLanguageDetails == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Sub section language details not found";
            }
            return SubSectionLanguageDetails;
        }

        public async Task<SubSectionMaster> GetSubSectionById(int SubSectionId)
        {
            var errorResponseModel = new ErrorResponseModel();
            var subsectionEntity = await _context.SubSectionMasters.FirstOrDefaultAsync(x => x.SubSectionId == SubSectionId && !x.DeleteStatus);
            if (subsectionEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Subsection not found";
            }
            return subsectionEntity;
        }



        /// <summary>
        /// Methood to get subsection by SubSectionId
        /// </summary>
        /// <param name="subsectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        // public SubSectionModel GetSubSectionById(long subsectionId, ref ErrorResponseModel errorResponseModel)
        // {
        //     var listSubSectionModel = new SubSectionModel();
        //     errorResponseModel = new ErrorResponseModel();
        //     if (subsectionId == 0)
        //     {
        //         var listSubsectionEntity = context.SectionMaster.Where(x => x.DeleteStatus == false).ToList();
        //         if (listSubsectionEntity == null)
        //         {
        //             errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //             errorResponseModel.Message = "Section not found";
        //         }


        //         listSubSectionModel.SubSectionId = 0;
        //         listSubSectionModel.SubSectionName = listSubSectionModel.SectionName;
        //         listSubSectionModel.SectionId = listSubSectionModel.SectionId;
        //         listSubSectionModel.ParentSubSectionId = listSubSectionModel.ParentSubSectionId;


        //     }

        //     else
        //     {
        //         var listSubsectionEntity = context.SubSectionMaster.Where(x => x.DeleteStatus == false).Where((x => x.SubSectionId == subsectionId)).FirstOrDefault();
        //         List<ReferenceRubricDetailsModel> lstMatMedicaDetails = new List<ReferenceRubricDetailsModel>();
        //         var materiamedicaremediesEntity = (from sub in context.SubSectionMaster
        //                                            join refrub in context.ReferenceRubricDetails
        //                                            on sub.SubSectionId equals refrub.RefSubSectionId
        //                                            join sect in context.SectionMaster
        //                                           on sub.SectionId equals sect.SectionId
        //                                            where refrub.SubSectionId == subsectionId && refrub.DeleteStatus == false
        //                                            select new ReferenceRubricDetailsModel
        //                                            {
        //                                                 ReferenceRubricId= (int)refrub.ReferenceRubricId,
        //                                                 SubSectionId=refrub.SubSectionId,
        //                                                 RefSubSectionId=refrub.RefSubSectionId,
        //                                                 RefSubSectionName=sub.SubSectionName,
        //                                                 SectionId=sub.SectionId,
        //                                                 SectionName=sect.SectionName,
        //                                                EnteredBy= refrub.EnteredBy,
        //                                                EnteredDate=refrub.EnteredDate,
        //                                                ChangedBy = refrub.ChangedBy,
        //                                                ChangedDate = refrub.ChangedDate
        //                                            }).ToList();






        //         var subsectionlanguageEntity = (from sub in context.SubSectionMaster
        //                                            join sublag in context.SubSectionLanguageDetails
        //                                            on sub.SubSectionId equals sublag.SubSectionId
        //                                             join lagmst in context.LanguageMaster
        //                                            on sublag.LanguageId equals lagmst.LanguageId


        //                                         where sublag.SubSectionId == subsectionId && sublag.DeleteStatus == false
        //                                            select new SubSectionLanguageDetailsModel
        //                                            {
        //                                               SubSectionId =sublag.SubSectionId,
        //                                                SectionName=sub.SubSectionName,
        //                                                LanguageId =sublag.LanguageId,
        //                                                SubSectionDetails=sublag.SubSectionDetails,
        //                                                LanguageName=lagmst.LanguageName,
        //                                                SubSectionLanguageId=sublag.SubSectionLanguageId,
        //                                                LanguageDescription =lagmst.Description

        //                                            }).ToList();

        //         if (listSubsectionEntity == null)
        //         {
        //             errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //             errorResponseModel.Message = "Section not found";
        //         }
        //         else
        //         {
        //             listSubSectionModel.SubSectionId = listSubsectionEntity.SubSectionId;
        //             listSubSectionModel.Description = listSubsectionEntity.Description;
        //             listSubSectionModel.SubSectionNameAlias = listSubsectionEntity.SubSectionNameAlias;
        //             listSubSectionModel.SubSectionName = listSubsectionEntity.SubSectionName;
        //             listSubSectionModel.SectionId = listSubsectionEntity.SectionId;
        //             listSubSectionModel.ParentSubSectionId = listSubsectionEntity.ParentSubSectionId;
        //             listSubSectionModel.ParentSubSectionName = context.SubSectionMaster.Where(x=>x.SubSectionId== listSubsectionEntity.ParentSubSectionId).Select(x=>x.SubSectionName).FirstOrDefault();
        //             listSubSectionModel.Referencerubric = materiamedicaremediesEntity;
        //             listSubSectionModel.SubSectionLanguageDetails = subsectionlanguageEntity;


        //         }
        //     }

        //     return listSubSectionModel;
        // }

        // /// <summary>
        /// Method to get all the subsections
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>


        public async Task<PagedList<SubSectionList>> GetSubSectionList(ParameterParams parameter)
        {
            var query = (from sub in _context.SubSectionMasters
                         where sub.DeleteStatus == false
                         select new SubSectionList
                         {
                             SubSectionId = sub.SubSectionId,
                             SectionId = sub.SectionId,
                             SubSectionName = sub.SubSectionName,
                             SubSectionNameAlias = sub.SubSectionNameAlias,
                             ParentSubSectionId = sub.ParentSubSectionId,
                             ParentSubSectionName = sub.ParentSubSectionId != null ? _context.SubSectionMasters.Where(s => s.SubSectionId == sub.ParentSubSectionId).FirstOrDefault().SubSectionName : null
                         }).OrderByDescending(c => c.SubSectionId).AsQueryable();
            if (parameter.search != null)
            {
                query = query.Where(x => x.SubSectionName.ToLower().Contains(parameter.search.ToLower()));
            }
            if (parameter.SectionId > 0)
            {
                query = query.Where(x => x.SectionId == parameter.SectionId);
            }
            return await PagedList<SubSectionList>.CreateAsync(query.ProjectTo<SubSectionList>(_mapper.ConfigurationProvider)
                                   .AsNoTracking(), parameter.PageNumber, parameter.PageSize);
        }

        public async Task<RubricKeywordSearchPagedResponse> SearchRubricsByKeywordAsync(
            string keyword,
            int pageNumber,
            int pageSize,
            List<int> sectionIds = null)
        {
            var normalizedKeyword = keyword?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                return new RubricKeywordSearchPagedResponse
                {
                    PageNumber = pageNumber > 0 ? pageNumber : 1,
                    PageSize = pageSize > 0 ? pageSize : 10,
                };
            }

            var searchLower = normalizedKeyword.ToLower();
            var safePageNumber = pageNumber > 0 ? pageNumber : 1;
            var safePageSize = pageSize > 0 ? pageSize : 10;
            var filteredSectionIds = sectionIds?.Where(id => id > 0).Distinct().ToList();

            var query =
                from sub in _context.SubSectionMasters.AsNoTracking()
                join sec in _context.SectionMasters.AsNoTracking() on sub.SectionId equals sec.SectionId
                where sub.DeleteStatus == false
                    && sec.DeleteStatus == false
                    && sub.SectionId != null
                    && (
                        (sub.SubSectionName != null && sub.SubSectionName.ToLower().Contains(searchLower))
                        || (sec.SectionName != null && sec.SectionName.ToLower().Contains(searchLower))
                    )
                    && (filteredSectionIds == null || filteredSectionIds.Count == 0 || filteredSectionIds.Contains((int)sub.SectionId))
                orderby sub.SubSectionName
                select new RubricKeywordModel
                {
                    SubSectionID = sub.SubSectionId,
                    SubSectionName = sub.SubSectionName ?? string.Empty,
                    SubSectionNameAlias = sub.SubSectionNameAlias ?? string.Empty,
                    SectionID = sec.SectionId,
                    SectionName = sec.SectionName ?? string.Empty,
                    SectionNameAlias = sec.SectionAlias ?? string.Empty,
                };

            var pagedList = await PagedList<RubricKeywordModel>.CreateAsync(
                query,
                safePageNumber,
                safePageSize);

            return new RubricKeywordSearchPagedResponse
            {
                Items = pagedList,
                PageNumber = pagedList.CurrentPage,
                PageSize = pagedList.PageSize,
                TotalCount = pagedList.TotalCount,
                HasMore = pagedList.CurrentPage < pagedList.TotalPages,
            };
        }

        /// <summary>
        /// Searches non-deleted subsections by hotspot name.
        /// Prefers FULLTEXT CONTAINS (script 725) on SubSectionName; falls back to Contains + in-memory word filter.
        /// </summary>
        public async Task<PaginatedResult<SubSectionForPageModel>> SearchSubSectionsByHotspotAsync(
            SearchSubSectionByHotspotRequest request)
        {
            var hotspot = (request.HotspotName ?? string.Empty).Trim();
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : Math.Min(request.PageSize, 40);
            var fetchSize = Math.Max(pageSize * 4, 32);

            List<SubSectionForPageModel> fetched;
            if (!string.IsNullOrWhiteSpace(hotspot) && TryBuildFullTextQuery(hotspot, out var ftsQuery))
            {
                try
                {
                    // Uses dbo full-text index from 725_FullText_SubSectionMaster_SubSectionName.sql
                    fetched = await _context.SubSectionMasters
                        .FromSqlRaw(
                            @"SELECT s.*
                              FROM dbo.SubSectionMaster AS s
                              WHERE s.DeleteStatus = 0
                                AND s.SubSectionName IS NOT NULL
                                AND CONTAINS(s.SubSectionName, {0})",
                            ftsQuery)
                        .AsNoTracking()
                        .OrderBy(x => x.SubSectionName!.Length)
                        .ThenBy(x => x.SubSectionName)
                        .Take(fetchSize)
                        .Select(x => new SubSectionForPageModel
                        {
                            SubSectionId = x.SubSectionId,
                            SubSectionName = x.SubSectionName!,
                        })
                        .ToListAsync();
                }
                catch
                {
                    // Full-text unavailable / query syntax rejected — fall back to Contains scan.
                    fetched = await FetchByContainsAsync(hotspot, fetchSize);
                }
            }
            else
            {
                fetched = await FetchByContainsAsync(hotspot, fetchSize);
            }

            var filtered = string.IsNullOrWhiteSpace(hotspot)
                ? fetched
                : fetched.Where(x =>
                {
                    if (WordBoundaryMatcher.Matches(x.SubSectionName, hotspot))
                        return true;
                    if (hotspot.Contains(' ') || hotspot.Length > 6)
                        return true;
                    return !WordBoundaryMatcher.IsSubstringCollision(x.SubSectionName, hotspot);
                }).ToList();

            var totalCount = filtered.Count;
            var items = filtered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedResult<SubSectionForPageModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            };
        }

        private async Task<List<SubSectionForPageModel>> FetchByContainsAsync(string hotspot, int fetchSize) =>
            await _context.SubSectionMasters
                .AsNoTracking()
                .ApplySubSectionByHotspotSearch(hotspot)
                .Take(fetchSize)
                .Select(x => new SubSectionForPageModel
                {
                    SubSectionId = x.SubSectionId,
                    SubSectionName = x.SubSectionName!,
                })
                .ToListAsync();

        /// <summary>
        /// Builds a CONTAINS predicate: single token → "term*"; multi-token → "a*" AND "b*".
        /// </summary>
        private static bool TryBuildFullTextQuery(string hotspot, out string ftsQuery)
        {
            ftsQuery = string.Empty;
            var tokens = hotspot
                .Split(new[] { ' ', '-', '/', ',', ';', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length >= 3)
                .Select(t => t.Replace("\"", string.Empty).Replace("'", string.Empty))
                .Where(t => t.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();

            if (tokens.Count == 0)
                return false;

            ftsQuery = tokens.Count == 1
                ? $"\"{tokens[0]}*\""
                : string.Join(" AND ", tokens.Select(t => $"\"{t}*\""));
            return true;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public void SaveReferenceRubric(ReferenceRubricDetail rubricDetails)
        {
            _context.Entry(rubricDetails).State = EntityState.Added;
        }

        public void SaveSubSection(SubSectionMaster subSection)
        {
            _context.Entry(subSection).State = EntityState.Added;
        }

        public void SaveSubsectionlanguage(SubSectionLanguageDetail languageDetails)
        {
            _context.Entry(languageDetails).State = EntityState.Added;
        }

        public void UpdateReferenceRubric(ReferenceRubricDetail rubricDetails)
        {
            _context.Entry(rubricDetails).State = EntityState.Modified;
        }

        public void UpdateSubSection(SubSectionMaster subSection)
        {
            _context.Entry(subSection).State = EntityState.Modified;
        }

        public void UpdateSubsectionlanguage(SubSectionLanguageDetail languageDetails)
        {
            _context.Entry(languageDetails).State = EntityState.Modified;
        }

        /// <summary>
        /// Method implementation for saving new SubSection
        /// </summary>
        /// <param name="subSectionModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        // public string SaveSubSection(List<SubSectionModel> subSectionModel, ref ErrorResponseModel errorResponseModel)
        // {
        //     string Message = "";
        //     foreach (var items in subSectionModel)
        //     {
        //         if (items.SubSectionId == 0)
        //         {
        //             foreach (var item in subSectionModel)
        //             {
        //                 SubSectionMaster subSectionEntity = new SubSectionMaster();
        //                 if (item.SubSectionId == 0)
        //                 {
        //                     subSectionEntity.SectionId = item.SectionId;
        //                     subSectionEntity.ParentSubSectionId = item.ParentSubSectionId;
        //                     subSectionEntity.SubSectionName = item.SubSectionName;
        //                     subSectionEntity.SubSectionNameAlias = item.SubSectionNameAlias;
        //                     subSectionEntity.Description = item.Description;
        //                     subSectionEntity.EnteredBy = item.EnteredBy;
        //                     subSectionEntity.EnteredDate = DateTime.Now;
        //                     subSectionEntity.DeleteStatus = false;
        //                     context.SubSectionMaster.Add(subSectionEntity);
        //                     context.SaveChanges();
        //                 }

        //                 foreach (var item1 in item.Referencerubric)
        //                 {
        //                     var modeldetails = new ReferenceRubricDetails();
        //                     modeldetails.SubSectionId = subSectionEntity.SubSectionId;
        //                     modeldetails.RefSubSectionId = item1.RefSubSectionId;
        //                     modeldetails.EnteredBy = item1.EnteredBy;
        //                     modeldetails.EnteredDate = DateTime.Now;
        //                     modeldetails.ChangedBy = item1.ChangedBy;
        //                     modeldetails.ChangedDate = item1.ChangedDate;
        //                     modeldetails.DeleteStatus = false;
        //                     context.ReferenceRubricDetails.Add(modeldetails);
        //                     context.SaveChanges();

        //                 }



        //                 foreach (var item1 in item.SubSectionLanguageDetails)
        //                 {
        //                     var languagemodeldetails = new SubSectionLanguageDetails();
        //                     languagemodeldetails.SubSectionId = subSectionEntity.SubSectionId;
        //                     languagemodeldetails.LanguageId = item1.LanguageId;
        //                     languagemodeldetails.SubSectionDetails = item1.SubSectionDetails;
        //                     languagemodeldetails.DeleteStatus = false;
        //                     context.SubSectionLanguageDetails.Add(languagemodeldetails);
        //                     context.SaveChanges();

        //                 }

        //                 Message = "Rubric Remedy Details Saved Successfully";
        //             }

        //         }
        //         else
        //         {
        //             foreach (var item in subSectionModel)
        //             {
        //                 SubSectionMaster subSectionEntity = new SubSectionMaster();
        //                 if (item.SubSectionId > 0)
        //                 {
        //                     var subSectionUpdateEntity = context.SubSectionMaster.FirstOrDefault(x => x.SubSectionId == item.SubSectionId);
        //                     if (subSectionUpdateEntity != null)
        //                     {
        //                         subSectionUpdateEntity.SectionId = item.SectionId;
        //                         subSectionUpdateEntity.ParentSubSectionId = item.ParentSubSectionId;
        //                         subSectionUpdateEntity.SubSectionName = item.SubSectionName;
        //                         subSectionUpdateEntity.SubSectionNameAlias = item.SubSectionNameAlias;
        //                         subSectionUpdateEntity.Description = item.Description;
        //                         subSectionUpdateEntity.EnteredBy = item.EnteredBy;
        //                         subSectionUpdateEntity.ChangedBy = item.ChangedBy;
        //                         subSectionUpdateEntity.ChangedDate = DateTime.Now;
        //                         subSectionUpdateEntity.EnteredDate = DateTime.Now;
        //                         context.SaveChanges();
        //                     }
        //                 }


        //                 foreach (var item1 in item.Referencerubric)
        //                 {
        //                     var referencerubricUpdateEntity = context.ReferenceRubricDetails.FirstOrDefault(x => x.ReferenceRubricId == item1.ReferenceRubricId && x.DeleteStatus == false);
        //                     if (referencerubricUpdateEntity != null)
        //                     {
        //                         referencerubricUpdateEntity.SubSectionId = item1.SubSectionId;
        //                         referencerubricUpdateEntity.RefSubSectionId = item1.RefSubSectionId;
        //                         referencerubricUpdateEntity.EnteredDate = item1.EnteredDate;
        //                         referencerubricUpdateEntity.EnteredBy = Convert.ToInt32(item.EnteredBy);
        //                         referencerubricUpdateEntity.ChangedBy = Convert.ToInt32(item.ChangedBy);
        //                         referencerubricUpdateEntity.ChangedDate = DateTime.Now;
        //                         referencerubricUpdateEntity.EnteredDate = item1.EnteredDate;
        //                         referencerubricUpdateEntity.DeleteStatus = false;
        //                         context.SaveChanges();
        //                     }
        //                     else
        //                     {
        //                         var modeldetails = new ReferenceRubricDetails();
        //                         modeldetails.SubSectionId = item.SubSectionId;
        //                         modeldetails.RefSubSectionId = item1.RefSubSectionId;
        //                         modeldetails.EnteredDate = DateTime.Now;
        //                         modeldetails.EnteredBy = Convert.ToInt32(item.EnteredBy);
        //                         modeldetails.ChangedBy = Convert.ToInt32(item.ChangedBy);
        //                         modeldetails.DeleteStatus = false;
        //                         context.ReferenceRubricDetails.Add(modeldetails);
        //                         context.SaveChanges();

        //                     }
        //                 }



        //                 foreach (var item1 in item.SubSectionLanguageDetails)
        //                 {
        //                     var subSectionLanguageDetailsEntity = context.SubSectionLanguageDetails.FirstOrDefault(x => x.SubSectionLanguageId == item1.SubSectionLanguageId && x.DeleteStatus == false);
        //                     if (subSectionLanguageDetailsEntity != null)
        //                     {
        //                         subSectionLanguageDetailsEntity.SubSectionId = item1.SubSectionId;
        //                         subSectionLanguageDetailsEntity.LanguageId = item1.LanguageId;
        //                         subSectionLanguageDetailsEntity.SubSectionDetails = item1.SubSectionDetails;
        //                         subSectionLanguageDetailsEntity.DeleteStatus = false;

        //                         context.SaveChanges();
        //                     }
        //                     else
        //                     {
        //                         var languagemodeldetails = new SubSectionLanguageDetails();
        //                         languagemodeldetails.SubSectionId = item.SubSectionId;
        //                         languagemodeldetails.LanguageId = item1.LanguageId;
        //                         languagemodeldetails.SubSectionDetails = item1.SubSectionDetails;
        //                         languagemodeldetails.DeleteStatus = false;
        //                         context.SubSectionLanguageDetails.Add(languagemodeldetails);
        //                         context.SaveChanges();

        //                     }
        //                 }

        //                 Message = "Rubric Remedy Details Update Successfully";
        //             }
        //         }
        //     }


        public List<SubSection> GetSubSectionsByDate(int userId, ref ErrorResponseModel errorResponseModel)
        {
            var subsectionModelList = new List<SubSection>();
            errorResponseModel = new ErrorResponseModel();

            var subsectionEntityList = (
                from rubric in _context.RubricRemedyDetails
                join subsec in _context.SubSectionMasters on rubric.SubSectionId equals subsec.SubSectionId
                where rubric.EnteredBy == userId &&
                      rubric.DeletedStatus == false &&
                      rubric.EnteredDate.HasValue && // Check if EnteredDate has value
                      rubric.EnteredDate.Value.Date == DateTime.Now.Date // Access the Value and compare Date
                group subsec by new { subsec.SubSectionId, subsec.SubSectionName } into g
                select new SubSection
                {
                    SubSectionId = g.Key.SubSectionId,
                    SubSectionName = g.Key.SubSectionName,
                    RemedyCount = g.Count() // Count of entered remedies for this subsection
                }
            ).ToList();

            if (subsectionEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "SubSection not found";
            }

            return subsectionEntityList;
        }

        public async Task<AddSubSectionModel> GetSubSectionById(long subsectionId)
        {
            var subsectiondetails = await (from sub in _context.SubSectionMasters
                         where sub.DeleteStatus == false && subsectionId==sub.SubSectionId
                         select new AddSubSectionModel
                         {
                             SubSectionId = sub.SubSectionId,
                             SectionId = sub.SectionId,
                             SubSectionName = sub.SubSectionName,
                             SubSectionNameAlias = sub.SubSectionNameAlias,
                             ParentSubSectionId = sub.ParentSubSectionId,
                             DeleteStatus=sub.DeleteStatus,
                             Referencerubric= (from r in _context.ReferenceRubricDetails
                                              where r.SubSectionId==subsectionId
                                              select new AddReferenceRubricDetails
                                              {
                                                SubSectionId=r.SubSectionId,
                                                RefSubSectionId= _context.ReferenceRubricDetails.Where(s=>s.SubSectionId==subsectionId).Select(s=>s.RefSubSectionId).ToList()
                                              }).ToList(),
                            SubLanguageDetail= (from l in _context.SubSectionLanguageDetails
                                                        where l.SubSectionId==subsectionId
                                              select new AddSubSectionLanguage
                                              {
                                                SubSectionId=l.SubSectionId,
                                                LanguageId=l.LanguageId,
                                                SubSectionLanguageId=l.SubSectionLanguageId
                                              }).ToList()
                         }).FirstOrDefaultAsync();
                        return subsectiondetails;
        }

        public async Task<(
            bool Success,
            string Message,
            int TotalRows,
            int SuccessRows,
            int FailedRows
        )> ImportSubSectionsFromExcel(IFormFile file)
        {
            int successCount = 0;
            int failedCount = 0;
            List<string> errors = new List<string>();
            int secId = 0;
            int subSecId = 0;
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var workbook = new XLWorkbook(stream);
            var mainSheet = workbook.Worksheets.FirstOrDefault();
            if (mainSheet is null)
            {
                throw new Exception("Main worksheet not found in the Excel file.");
            }

            var languageSheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Languages");
            var rubricSheet = workbook.Worksheets.FirstOrDefault(ws =>
                ws.Name == "ReferenceRubrics"
            );
            var rowCount = mainSheet.RowsUsed().Count();

            // Skip header row
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var sectionName = mainSheet.Cell(row, 1).Value.ToString();
                    var section = await _context.SectionMasters.FirstOrDefaultAsync(s =>
                        s.SectionName == sectionName && !s.DeleteStatus
                    );
                    if (section == null)
                    {
                        var Sectiondata = new SectionMaster
                        {
                            SectionName = sectionName,
                            SectionAlias = sectionName,
                            DeleteStatus = false,
                        };
                        _context.Add(Sectiondata);
                        _context.SaveChanges();
                        secId = Sectiondata.SectionId;
                    }
                    var parentname = !string.IsNullOrEmpty(mainSheet.Cell(row, 5).Value.ToString())
                        ? (mainSheet.Cell(row, 5).Value.ToString())
                        : null;
                    var parentSubSection = await _context.SubSectionMasters.FirstOrDefaultAsync(s =>
                        s.SubSectionName == parentname && !s.DeleteStatus
                    );
                    var subsectionDto = new SubSectionExcelImportDto
                    {
                        SectionId = section.SectionId,
                        SubSectionName = mainSheet.Cell(row, 2).Value.ToString(),
                        SubSectionNameAlias = mainSheet.Cell(row, 3).Value.ToString(),
                        Description = mainSheet.Cell(row, 4).Value.ToString(),
                        ParentSubSectionId = parentSubSection.SubSectionId,
                        LanguageName = mainSheet.Cell(row, 6).Value.ToString(),
                        SubSectionDetails = mainSheet.Cell(row, 7).Value.ToString(),
                    };

                    // Check for duplicate SubSectionName
                    var duplicateName = await _context.SubSectionMasters.FirstOrDefaultAsync(s =>
                        s.SubSectionName == subsectionDto.SubSectionName && !s.DeleteStatus
                    );
                    if (duplicateName == null)
                    {
                        var subSection = new SubSectionMaster
                        {
                            SectionId = subsectionDto.SectionId,
                            SubSectionName = subsectionDto.SubSectionName,
                            SubSectionNameAlias = subsectionDto.SubSectionNameAlias,
                            Description = subsectionDto.Description,
                            ParentSubSectionId = subsectionDto.ParentSubSectionId,
                            DeleteStatus = false,
                        };

                        // Save main subsection
                        _context.SubSectionMasters.Add(subSection);
                        await _context.SaveChangesAsync();
                        subSecId = subSection.SubSectionId;
                    }
                    else
                    {
                        subSecId = duplicateName.SubSectionId;
                    }
                    var language = await _context.LanguageMasters.FirstOrDefaultAsync(l =>
                        l.LanguageName == subsectionDto.LanguageName && l.IsDeleted == false
                    );
                    if (language == null)
                    {
                        errors.Add(
                            $"Row {row}: Language with name '{subsectionDto.LanguageName}' does not exist."
                        );
                        failedCount++;
                        continue;
                    }
                    var existingLanguageDetail =
                        await _context.SubSectionLanguageDetails.FirstOrDefaultAsync(s =>
                            s.SubSectionId == subSecId && s.LanguageId == language.LanguageId
                        );
                    if (existingLanguageDetail != null)
                    {
                        var languageDetail = new SubSectionLanguageDetail
                        {
                            SubSectionId = subSecId,
                            LanguageId = language.LanguageId,
                            SubSectionDetails = subsectionDto.SubSectionDetails,
                            DeleteStatus = false,
                        };
                        _context.SubSectionLanguageDetails.Add(languageDetail);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var languageDetail = new SubSectionLanguageDetail
                        {
                            SubSectionId = existingLanguageDetail.SubSectionId,
                            LanguageId = existingLanguageDetail.LanguageId,
                            SubSectionDetails = subsectionDto.SubSectionDetails,
                            DeleteStatus = false,
                        };
                        _context.SubSectionLanguageDetails.Update(languageDetail);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {row}: {ex.Message}");
                    failedCount++;
                }
            }

            int subsecId = 0;
            int rubsubId = 0;
            // Process reference rubrics if present
            if (rubricSheet != null)
            {
                var rubricRows = rubricSheet.RowsUsed().Count();
                for (int rubricRow = 2; rubricRow <= rubricRows; rubricRow++)
                {
                    var subsection = await _context.SubSectionMasters.FirstOrDefaultAsync(s =>
                        s.SubSectionName == rubricSheet.Cell(rubricRow, 1).Value.ToString()
                        && !s.DeleteStatus
                    );
                    if (subsection == null)
                    {
                        var sectionid = rubricSheet.Cell(rubricRow, 1).Value.ToString();
                        var sectionName1 = "";
                        if (!sectionid.Contains("-"))
                        {
                            sectionName1 = sectionid;
                        }
                        else
                        {
                            sectionName1 = sectionid.Split('-')[0];
                        }
                        var sectionEntity =
                            await _context.SectionMasters.FirstOrDefaultAsync(s =>
                                s.SectionName == sectionName1 && !s.DeleteStatus
                            ) ?? null;
                        int secId1 = 0;
                        if (sectionEntity == null)
                        {
                            var Sectiondata = new SectionMaster
                            {
                                SectionName = sectionName1,
                                SectionAlias = sectionName1,
                                DeleteStatus = false,
                                EnteredBy = "1",
                                EnteredDate = DateTime.Now,
                            };
                            _context.Add(Sectiondata);
                            _context.SaveChanges();
                            secId1 = Sectiondata.SectionId;
                        }
                        else
                        {
                            secId1 = sectionEntity.SectionId;
                        }
                        var subSection = await _context.SubSectionMasters.FirstOrDefaultAsync(s =>
                            s.SubSectionName == sectionid && !s.DeleteStatus
                        );
                        if (subSection == null)
                        {
                            var subSectiondata = new SubSectionMaster
                            {
                                SectionId = secId1,
                                SubSectionName = sectionid,
                                SubSectionNameAlias = sectionid,
                                Description = sectionid,
                                DeleteStatus = false,
                                EnteredBy = "1",
                                EnteredDate = DateTime.Now,
                            };
                            _context.Add(subSectiondata);
                            _context.SaveChanges();
                            subSecId = subSectiondata.SubSectionId;
                        }
                        else
                        {
                            subsecId = subSection.SubSectionId;
                        }
                    }
                    var refSubSectionName = rubricSheet.Cell(rubricRow, 2).Value.ToString();
                    var refSubSection = await _context.SubSectionMasters.FirstOrDefaultAsync(s =>
                        s.SubSectionName == refSubSectionName && !s.DeleteStatus
                    );

                    if (refSubSection == null)
                    {
                        var sectionValue1 = rubricSheet.Cell(rubricRow, 2).Value.ToString();
                        var rubsectionName = "";
                        if (!sectionValue1.Contains("-"))
                        {
                            rubsectionName = sectionValue1;
                        }
                        else
                        {
                            rubsectionName = sectionValue1.Split('-')[0];
                        }
                        var sectionEntity = await _context.SectionMasters.FirstOrDefaultAsync(s =>
                            s.SectionName == rubsectionName && !s.DeleteStatus
                        );
                        if (sectionEntity == null)
                        {
                            var Section1data = new SectionMaster
                            {
                                SectionName = rubsectionName,
                                SectionAlias = rubsectionName,
                                DeleteStatus = false,
                                EnteredBy = "1",
                                EnteredDate = DateTime.Now,
                            };
                            _context.Add(Section1data);
                            _context.SaveChanges();
                            secId = Section1data.SectionId;
                        }
                        else
                        {
                            secId = sectionEntity.SectionId;
                        }
                        var subSection = await _context.SubSectionMasters.FirstOrDefaultAsync(s =>
                            s.SubSectionName == sectionValue1 && !s.DeleteStatus
                        );
                        if (subSection == null)
                        {
                            var subSectiondata = new SubSectionMaster
                            {
                                SectionId = secId,
                                SubSectionName = sectionValue1,
                                SubSectionNameAlias = sectionValue1,
                                Description = sectionValue1,
                                DeleteStatus = false,
                                EnteredBy = "1",
                                EnteredDate = DateTime.Now,
                            };
                            _context.Add(subSectiondata);
                            _context.SaveChanges();
                            rubsubId = subSectiondata.SubSectionId;
                        }
                        else
                        {
                            rubsubId = subSection.SubSectionId;
                        }
                    }
                    else
                    {
                        rubsubId = refSubSection.SubSectionId;
                    }
                    var rubricDetail = new ReferenceRubricDetail
                    {
                        SubSectionId = subSecId,
                        RefSubSectionId = rubsubId,
                        DeleteStatus = false,
                        EnteredDate = DateTime.Now,
                        EnteredBy = 1,
                    };

                    var existingRubricDetail =
                        await _context.ReferenceRubricDetails.FirstOrDefaultAsync(r =>
                            r.SubSectionId == subSecId && r.RefSubSectionId == rubsubId
                        );
                    if (existingRubricDetail == null)
                    {
                        _context.ReferenceRubricDetails.Add(rubricDetail);
                        _context.SaveChanges();
                    }
                }
            }
            return (
                true,
                "Subsections imported successfully.",
                rowCount - 1,
                successCount,
                failedCount
            );
        }

        public async Task<byte[]> ExportSubSectionsToExcel(int sectionId)
        {
            using (var workbook = new XLWorkbook())
            {
                // === Sheet 1: SubSections ===
                var mainSheet = workbook.Worksheets.Add("SubSections");

                // Header Row
                mainSheet.Cell(1, 1).Value = "SectionId";
                mainSheet.Cell(1, 2).Value = "SectionName";
                mainSheet.Cell(1, 3).Value = "SubsectionId";
                mainSheet.Cell(1, 4).Value = "SubsectionName";
                mainSheet.Cell(1, 5).Value = "Alias";
                mainSheet.Cell(1, 6).Value = "Description";
                mainSheet.Cell(1, 7).Value = "ParentSubSectionId";
                mainSheet.Cell(1, 8).Value = "ParentSubSectionName";
                mainSheet.Cell(1, 9).Value = "DeletedStatus";

                // Fetch subsections
                var subSections = await _context
                    .SubSectionMasters.Where(x => x.SectionId == sectionId && !x.DeleteStatus).OrderBy(s=>s.SubSectionName)
                    .ToListAsync();

                int row = 2;
                foreach (var sub in subSections)
                {
                    var section = await _context.SectionMasters.FirstOrDefaultAsync(s =>
                        s.SectionId == sub.SectionId
                    );

                    var parentName = await _context
                        .SubSectionMasters.Where(s => s.SubSectionId == sub.ParentSubSectionId)
                        .Select(s => s.SubSectionName)
                        .FirstOrDefaultAsync();

                    // Write subsection row (without language details)
                    mainSheet.Cell(row, 1).Value = section?.SectionId;
                    mainSheet.Cell(row, 2).Value = section?.SectionName;
                    mainSheet.Cell(row, 3).Value = sub.SubSectionId;
                    mainSheet.Cell(row, 4).Value = sub.SubSectionName;
                    mainSheet.Cell(row, 5).Value = sub.SubSectionNameAlias;
                    mainSheet.Cell(row, 6).Value = sub.Description;
                    mainSheet.Cell(row, 7).Value = sub.ParentSubSectionId;
                    mainSheet.Cell(row, 8).Value = parentName;
                    mainSheet.Cell(row, 9).Value = sub.DeleteStatus;
                    row++;
                }

                // === Sheet 2: SubSectionLanguages ===
                var langSheet = workbook.Worksheets.Add("SubSectionLanguages");

                langSheet.Cell(1, 1).Value = "SubSectionLanguageId";
                langSheet.Cell(1, 2).Value = "SubSectionId";
                langSheet.Cell(1, 3).Value = "LanguageId";
                langSheet.Cell(1, 4).Value = "Language";
                langSheet.Cell(1, 5).Value = "SubSectionDetails";
                langSheet.Cell(1, 6).Value = "DeletedStatus";

                int langRow = 2;
                var allLanguageDetails = await _context
                    .SubSectionLanguageDetails.Where(ld =>
                        ld.DeleteStatus == false
                        && subSections.Select(s => s.SubSectionId).Contains(ld.SubSectionId)
                    )
                    .Include(ld => ld.Language)
                    .ToListAsync();

                foreach (var lang in allLanguageDetails)
                {
                    langSheet.Cell(langRow, 1).Value = lang.SubSectionLanguageId;
                    langSheet.Cell(langRow, 2).Value = lang.SubSectionId;
                    langSheet.Cell(langRow, 3).Value = lang.LanguageId;
                    langSheet.Cell(langRow, 4).Value = lang.Language?.LanguageName;
                    langSheet.Cell(langRow, 5).Value = lang.SubSectionDetails;
                    langSheet.Cell(langRow, 6).Value = Convert.ToBoolean(lang.DeleteStatus);

                    langRow++;
                }

                // === Sheet 3: ReferenceRubrics ===
                var rubricSheet = workbook.Worksheets.Add("ReferenceRubrics");

                rubricSheet.Cell(1, 1).Value = "ReferenceRubricId";
                rubricSheet.Cell(1, 2).Value = "SubSectionId";
                rubricSheet.Cell(1, 3).Value = "RefSubSectionId";
                rubricSheet.Cell(1, 4).Value = "SubSectionName";
                rubricSheet.Cell(1, 5).Value = "RefSubSectionName";
                rubricSheet.Cell(1, 6).Value = "DeletedStatus";
                

                var rubricDetails = await (
                    from r in _context.ReferenceRubricDetails
                    join sub in _context.SubSectionMasters on r.SubSectionId equals sub.SubSectionId
                    join refSub in _context.SubSectionMasters
                        on r.RefSubSectionId equals refSub.SubSectionId
                    where r.DeleteStatus == false && sub.SectionId == sectionId
                    select new
                    {
                        r,
                        sub,
                        refSub,
                    }
                ).ToListAsync();

                int rubricRow = 2;
                foreach (var rubric in rubricDetails)
                {
                    rubricSheet.Cell(rubricRow, 1).Value = rubric.r.ReferenceRubricId;
                    rubricSheet.Cell(rubricRow, 2).Value = rubric.sub.SubSectionId;
                    rubricSheet.Cell(rubricRow, 3).Value = rubric.r.RefSubSectionId;
                    rubricSheet.Cell(rubricRow, 4).Value = rubric.sub.SubSectionName;
                    rubricSheet.Cell(rubricRow, 5).Value = rubric.refSub.SubSectionName;
                    rubricSheet.Cell(rubricRow, 6).Value = rubric.refSub.DeleteStatus;
                    
                    rubricRow++;
                }

                // Save workbook
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<(
            bool Success,
            string Message,
            int TotalRows,
            int SuccessRows,
            int FailedRows
        )> UpdateSubSectionsFromExcel(IFormFile file)
        {
            int totalRows = 0,
                successRows = 0,
                failedRows = 0;
            string message = "";

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        // === SHEET 1: SubSections ===
                        var subSheet = workbook.Worksheet("SubSections");
                        int subRowCount = subSheet.LastRowUsed().RowNumber();

                        for (int row = 2; row <= subRowCount; row++)
                        {
                            totalRows++;
                            try
                            {
                                int subSectionId = int.Parse(subSheet.Cell(row, 3).GetString());
                                var subSection =
                                    await _context.SubSectionMasters.FirstOrDefaultAsync(x =>
                                        x.SubSectionId == subSectionId && !x.DeleteStatus
                                    );

                                if (subSection != null)
                                {
                                    subSection.SectionId = int.Parse(
                                        subSheet.Cell(row, 1).GetString()
                                    );
                                    subSection.SubSectionName = subSheet.Cell(row, 4).GetString();
                                    subSection.SubSectionNameAlias = subSheet
                                        .Cell(row, 5)
                                        .GetString();
                                    subSection.Description = subSheet.Cell(row, 6).GetString();
                                    var parentId = subSheet.Cell(row, 7).GetString();
                                    subSection.ParentSubSectionId = string.IsNullOrEmpty(parentId)
                                        ? (int?)null
                                        : int.Parse(parentId);
                                    subSection.DeleteStatus = subSheet.Cell(row, 8).GetBoolean();
                                    subSection.ChangedDate = DateTime.Now;
                                    _context.SubSectionMasters.Update(subSection);
                                    successRows++;
                                }
                                else
                                {
                                    failedRows++;
                                }
                            }
                            catch
                            {
                                failedRows++;
                            }
                        }

                        // === SHEET 2: SubSectionLanguages ===
                        var langSheet = workbook.Worksheet("SubSectionLanguages");
                        int langRowCount = langSheet.LastRowUsed().RowNumber();

                        for (int row = 2; row <= langRowCount; row++)
                        {
                            totalRows++;
                            try
                            {
                                int langId = int.Parse(langSheet.Cell(row, 1).GetString());
                                int subSectionId = int.Parse(langSheet.Cell(row, 2).GetString());

                                var langDetail =
                                    await _context.SubSectionLanguageDetails.FirstOrDefaultAsync(
                                        ld =>
                                            ld.SubSectionLanguageId == langId
                                            && ld.DeleteStatus == false
                                    );

                                if (langDetail != null)
                                {
                                    // Update existing
                                    langDetail.SubSectionId = subSectionId;
                                    langDetail.LanguageId = int.Parse(
                                        langSheet.Cell(row, 3).GetString()
                                    );
                                    langDetail.SubSectionDetails = langSheet
                                        .Cell(row, 5)
                                        .GetString();
                                    _context.SubSectionLanguageDetails.Update(langDetail);

                                    successRows++;
                                }
                            }
                            catch
                            {
                                failedRows++;
                            }
                        }

                        // === SHEET 3: ReferenceRubrics ===
                        var rubricSheet = workbook.Worksheet("ReferenceRubrics");
                        int rubricRowCount = rubricSheet.LastRowUsed().RowNumber();

                        for (int row = 2; row <= rubricRowCount; row++)
                        {
                            totalRows++;
                            try
                            {
                                int rubricId = int.Parse(rubricSheet.Cell(row, 1).GetString());

                                var rubric =
                                    await _context.ReferenceRubricDetails.FirstOrDefaultAsync(r =>
                                        r.ReferenceRubricId == rubricId && r.DeleteStatus == false
                                    );

                                if (rubric != null)
                                {
                                    rubric.SubSectionId = int.Parse(
                                        rubricSheet.Cell(row, 2).GetString()
                                    );
                                    rubric.RefSubSectionId = int.Parse(
                                        rubricSheet.Cell(row, 3).GetString()
                                    );
                                    _context.ReferenceRubricDetails.Update(rubric);

                                    successRows++;
                                }
                            }
                            catch
                            {
                                failedRows++;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                message =
                    $"Processed {totalRows} rows. Success: {successRows}, Failed: {failedRows}";
                return (true, message, totalRows, successRows, failedRows);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return (false, message, totalRows, successRows, failedRows);
            }
        }

       


        //New API
        public async Task<(bool Success, string Message, int TotalRows, int SuccessRows, int FailedRows)>
        ImportSubSectionsFromExcelPC(IFormFile file)
        {
            int totalRows = 0, successRows = 0, failedRows = 0;

            var skippedRows = new List<IXLRow>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            //  PRELOAD (CRITICAL FOR PERFORMANCE)
            var sections = await _context.SectionMasters
                .ToDictionaryAsync(x => x.SectionName.ToUpper(), x => x.SectionId);

            var subSections = await _context.SubSectionMasters
    .Where(x => x.DeleteStatus == false || x.DeleteStatus == null)
    .ToDictionaryAsync(
        x => x.SubSectionName.ToUpper(),
        x => x.SubSectionId
    );


            var languages = await _context.LanguageMasters
                .ToDictionaryAsync(x => x.LanguageName.ToUpper(), x => x.LanguageId);

            var existingRefs = await _context.ReferenceRubricDetails
                .Select(x => new { x.SubSectionId, x.RefSubSectionId })
                .ToListAsync();

            int lastRow = sheet.LastRowUsed().RowNumber();

            for (int row = 2; row <= lastRow; row++)
            {
                totalRows++;
                var r = sheet.Row(row);

                string sectionName = r.Cell(1).GetString().Trim().ToUpper();
                string mainRubric = r.Cell(2).GetString().Trim().ToUpper();
                string mainParent = r.Cell(3).GetString().Trim().ToUpper();
                string parentRubric = r.Cell(4).GetString().Trim().ToUpper();
                string crossRef = r.Cell(5).GetString().Trim().ToUpper();
                string enText = r.Cell(6).GetString().Trim();
                string mrText = r.Cell(7).GetString().Trim();

                //  VALIDATION
                if (!sections.ContainsKey(sectionName)
                    || !subSections.ContainsKey(mainRubric)
                    || (!string.IsNullOrEmpty(parentRubric) && !subSections.ContainsKey(parentRubric))
                    || (!string.IsNullOrEmpty(crossRef) && !subSections.ContainsKey(crossRef)))
                {
                    skippedRows.Add(r);
                    failedRows++;
                    continue;
                }

                int subSectionId = subSections[mainRubric];

                //  UPDATE MAIN SUBSECTION
                var sub = await _context.SubSectionMasters.FindAsync(subSectionId);
                sub.SectionId = sections[sectionName];
                sub.MainParentSubsection = mainParent == "Y";
                sub.ParentSubSectionId = string.IsNullOrEmpty(parentRubric)
                    ? null
                    : subSections[parentRubric];

                //  CROSS REFERENCE (DUPLICATE SAFE)
                if (!string.IsNullOrEmpty(crossRef))
                {
                    int refId = subSections[crossRef];
                    if (!existingRefs.Any(x => x.SubSectionId == subSectionId && x.RefSubSectionId == refId))
                    {
                        _context.ReferenceRubricDetails.Add(new ReferenceRubricDetail
                        {
                            SubSectionId = subSectionId,
                            RefSubSectionId = refId,
                            DeleteStatus = false,
                            EnteredDate = DateTime.Now, 
                        });
                    }
                }

                //// LANGUAGE (EN)
                //if (!string.IsNullOrEmpty(enText))
                //{
                //    int langId = languages["ENGLISH"];
                //    if (!_context.SubSectionLanguageDetails.Any(x =>
                //            x.SubSectionId == subSectionId && x.LanguageId == langId))
                //    {
                //        _context.SubSectionLanguageDetails.Add(new SubSectionLanguageDetail
                //        {
                //            SubSectionId = subSectionId,
                //            LanguageId = langId,
                //            SubSectionDetails = enText,
                //            DeleteStatus = false
                //        });
                //    }
                //}

                //// LANGUAGE (MR)
                //if (!string.IsNullOrEmpty(mrText))
                //{
                //    int langId = languages["MARATHI"];
                //    if (!_context.SubSectionLanguageDetails.Any(x =>
                //            x.SubSectionId == subSectionId && x.LanguageId == langId))
                //    {
                //        _context.SubSectionLanguageDetails.Add(new SubSectionLanguageDetail
                //        {
                //            SubSectionId = subSectionId,
                //            LanguageId = langId,
                //            SubSectionDetails = mrText,
                //            DeleteStatus = false
                //        });
                //    }
                //}
                // LANGUAGE (EN)
                if (!string.IsNullOrEmpty(enText))
                {
                    if (languages.TryGetValue("ENGLISH", out int enLangId))
                    {
                        if (!_context.SubSectionLanguageDetails.Any(x =>
                                x.SubSectionId == subSectionId && x.LanguageId == enLangId))
                        {
                            _context.SubSectionLanguageDetails.Add(new SubSectionLanguageDetail
                            {
                                SubSectionId = subSectionId,
                                LanguageId = enLangId,
                                SubSectionDetails = enText,
                                DeleteStatus = false
                            });
                        }
                    }
                }

                // LANGUAGE (MR)
                if (!string.IsNullOrEmpty(mrText))
                {
                    if (languages.TryGetValue("MARATHI", out int mrLangId))
                    {
                        if (!_context.SubSectionLanguageDetails.Any(x =>
                                x.SubSectionId == subSectionId && x.LanguageId == mrLangId))
                        {
                            _context.SubSectionLanguageDetails.Add(new SubSectionLanguageDetail
                            {
                                SubSectionId = subSectionId,
                                LanguageId = mrLangId,
                                SubSectionDetails = mrText,
                                DeleteStatus = false
                            });
                        }
                    }
                }


                successRows++;
            }

            await _context.SaveChangesAsync();

            //  WRITE SKIPPED ROWS
            // WRITE SKIPPED ROWS
            if (skippedRows.Any())
            {
                using var skippedWb = new XLWorkbook();
                var skippedSheet = skippedWb.AddWorksheet("SkippedRows");

                sheet.Row(1).CopyTo(skippedSheet.Row(1));
                int sr = 2;
                foreach (var row in skippedRows)
                    row.CopyTo(skippedSheet.Row(sr++));

                // DATA/ParentChild folder
                string dataFolder = Path.Combine(_env.ContentRootPath, "Data", "ParentChild");

                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                }

                string filePath = Path.Combine(dataFolder, "Skipped_SubSectionsWithParentChild.xlsx");
                skippedWb.SaveAs(filePath);
            }


            return (true,
                "Import completed successfully",
                totalRows,
                successRows,
                failedRows);
        }

        public byte[] GetReferenceRubricsImportTemplate(string format)
        {
            return string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
                ? ReferenceRubricImportFileHelper.BuildTemplateCsv()
                : ReferenceRubricImportFileHelper.BuildTemplateExcel();
        }

        public async Task<ReferenceRubricImportResultModel> ImportReferenceRubricsAsync(IFormFile file)
        {
            var result = new ReferenceRubricImportResultModel();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var parsedRows = await ReferenceRubricImportFileHelper.ParseImportFileAsync(file);
            result.TotalRows = parsedRows.Count;

            if (parsedRows.Count == 0)
            {
                return result;
            }

            var subSectionRows = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => s.DeleteStatus != true)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync();

            var subSectionNameMap = subSectionRows
                .Where(s => !string.IsNullOrWhiteSpace(s.SubSectionName))
                .GroupBy(s => s.SubSectionName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.SubSectionId).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var existingLinks = await _context.ReferenceRubricDetails.AsNoTracking()
                .Where(r => r.DeleteStatus != true && r.SubSectionId.HasValue && r.RefSubSectionId.HasValue)
                .Select(r => new
                {
                    SubSectionId = r.SubSectionId!.Value,
                    RefSubSectionId = r.RefSubSectionId!.Value,
                })
                .ToListAsync();

            var existingSet = new HashSet<(int SubSectionId, int RefSubSectionId)>(
                existingLinks.Select(x => (x.SubSectionId, x.RefSubSectionId)));

            var skippedRows = new List<ReferenceRubricImportRowModel>();
            var pendingPairs = new HashSet<(int SubSectionId, int RefSubSectionId)>();
            var toInsert = new List<ReferenceRubricDetail>();
            const int batchSize = 500;
            var now = DateTime.Now;

            foreach (var row in parsedRows)
            {
                if (!TryResolveSubSectionId(row.SubSectionName, subSectionNameMap, out var subSectionId, out var subSectionError))
                {
                    SkipReferenceRubricRow(row, string.IsNullOrWhiteSpace(row.SubSectionName) ? "SubSectionName is required." : subSectionError, skippedRows, result);
                    continue;
                }

                if (!TryResolveSubSectionId(row.RefSubSectionName, subSectionNameMap, out var refSubSectionId, out var refSubSectionError))
                {
                    SkipReferenceRubricRow(row, string.IsNullOrWhiteSpace(row.RefSubSectionName) ? "RefSubSectionName is required." : refSubSectionError, skippedRows, result);
                    continue;
                }

                if (subSectionId == refSubSectionId)
                {
                    SkipReferenceRubricRow(row, "Subsection cannot reference itself.", skippedRows, result);
                    continue;
                }

                var pair = (subSectionId, refSubSectionId);
                if (existingSet.Contains(pair))
                {
                    SkipReferenceRubricRow(row, "Reference rubric link already exists.", skippedRows, result);
                    continue;
                }

                if (!pendingPairs.Add(pair))
                {
                    SkipReferenceRubricRow(row, "Duplicate row in import file.", skippedRows, result);
                    continue;
                }

                toInsert.Add(new ReferenceRubricDetail
                {
                    SubSectionId = subSectionId,
                    RefSubSectionId = refSubSectionId,
                    DeleteStatus = false,
                    EnteredDate = now,
                    EnteredBy = 1,
                });
            }

            for (var offset = 0; offset < toInsert.Count; offset += batchSize)
            {
                var batch = toInsert.Skip(offset).Take(batchSize).ToList();
                _context.ReferenceRubricDetails.AddRange(batch);
                await _context.SaveChangesAsync();
                result.InsertedCount += batch.Count;

                foreach (var item in batch)
                {
                    existingSet.Add((item.SubSectionId!.Value, item.RefSubSectionId!.Value));
                }
            }

            result.SkippedCount = skippedRows.Count;

            if (skippedRows.Count > 0)
            {
                var skippedBytes = ReferenceRubricImportFileHelper.BuildSkippedRowsFile(skippedRows, extension);
                result.SkippedFile = new ReferenceRubricImportSkippedFileModel
                {
                    FileName = ReferenceRubricImportFileHelper.BuildSkippedFileName(extension),
                    ContentType = ReferenceRubricImportFileHelper.GetSkippedFileContentType(extension),
                    ContentBase64 = Convert.ToBase64String(skippedBytes),
                };
            }

            return result;
        }

        private static bool TryResolveSubSectionId(
            string? name,
            IReadOnlyDictionary<string, List<int>> subSectionNameMap,
            out int subSectionId,
            out string error)
        {
            subSectionId = 0;
            error = string.Empty;
            var text = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Name is required.";
                return false;
            }

            if (!subSectionNameMap.TryGetValue(text, out var ids))
            {
                error = $"Subsection not found: {text}";
                return false;
            }

            if (ids.Count > 1)
            {
                error = $"Ambiguous subsection name (multiple matches): {text}";
                return false;
            }

            subSectionId = ids[0];
            return true;
        }

        private static void SkipReferenceRubricRow(
            ReferenceRubricImportRowModel row,
            string reason,
            List<ReferenceRubricImportRowModel> skippedRows,
            ReferenceRubricImportResultModel result)
        {
            row.SkipReason = reason;
            skippedRows.Add(row);
            result.Errors.Add(new ReferenceRubricImportErrorModel
            {
                RowNumber = row.RowNumber,
                Message = reason,
            });
        }

    }
}

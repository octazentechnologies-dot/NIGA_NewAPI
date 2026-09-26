using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using static System.Collections.Specialized.BitVector32;

/// <summary>
/// Created Date    :   10-March-2020
/// Purpose         :   Class for RubricRemedyDetails
/// </summary>
namespace Homeocentrum.Niga.NewAPI.Domain.Implementation
{
    public class RubricRemedyDetailsService : IRubricRemedyDetailsService
    {
        private static readonly MemoryCacheEntryOptions RubricDetailsCacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
        };

        NIGACentrumContext context;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// Initilize class constructior
        /// </summary>
        /// <param name="centrumContext"></param>
        public RubricRemedyDetailsService(NIGACentrumContext centrumContext, IMemoryCache cache)
        {
            context = centrumContext;
            _cache = cache;
        }

        /// <summary>
        /// Method implementation for saving rubric remedy details.
        /// </summary>
        /// <param name="rubricRemedyDetailsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveRubricRemedyDetails(
            List<RubricRemedyDetailsModel> rubricRemedyDetailsModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string Message = "";

            ////Delete existing ones//
            //var existingDetails = context.RubricRemedyDetails.Where(x => x.SubSectionId == rubricRemedyDetailsModel.SubSectionId
            //                                                    && x.GradeId == rubricRemedyDetailsModel.GradeId ).ToList();
            //context.RubricRemedyDetails.RemoveRange(existingDetails);
            //context.SaveChanges();

            //var remedyIds = rubricRemedyDetailsModel.RemedyIds.Split(',');
            //foreach (var item in remedyIds)
            //{
            //        var rubricRemedyDetailsEntity = new RubricRemedyDetails();
            //        rubricRemedyDetailsEntity.SubSectionId = rubricRemedyDetailsModel.SubSectionId;
            //         rubricRemedyDetailsEntity.RemedyId = Convert.ToInt32(item);
            //      //  rubricRemedyDetailsEntity.RemedyId = rubricRemedyDetailsModel.RemedyId;
            //        rubricRemedyDetailsEntity.GradeId = rubricRemedyDetailsModel.GradeId;
            //        rubricRemedyDetailsEntity.EnteredDate = rubricRemedyDetailsModel.EnteredDate;
            //        rubricRemedyDetailsEntity.EnteredBy = rubricRemedyDetailsModel.EnteredBy;
            //        context.RubricRemedyDetails.Add(rubricRemedyDetailsEntity);
            //        context.SaveChanges();

            //    }
            //            if(existingDetails.Count > 0)
            //                {
            //                    Message = "Rubric Remedy Details Updated Successfully";
            //                }
            //            else
            //                {
            //                    Message = "Rubric Remedy Details Saved Successfully";
            //                }

            foreach (var item in rubricRemedyDetailsModel)
            {
                var rubricRemedyDetailsEntity = new RubricRemedyDetail();

                if (item.RubricRemedyId == 0)
                {
                    rubricRemedyDetailsEntity.RubricRemedyId = item.RubricRemedyId;
                    rubricRemedyDetailsEntity.SubSectionId = item.SubSectionId;
                    rubricRemedyDetailsEntity.RemedyId = item.RemedyId;
                    rubricRemedyDetailsEntity.GradeId = item.GradeId;
                    rubricRemedyDetailsEntity.EnteredDate = item.EnteredDate;
                    rubricRemedyDetailsEntity.EnteredBy = item.EnteredBy;
                    rubricRemedyDetailsEntity.DeletedStatus = false;
                    context.RubricRemedyDetails.Add(rubricRemedyDetailsEntity);
                    context.SaveChanges();

                    foreach (var item1 in item.Authors)
                    {
                        var modeldetails = new RemedyRubricAuthorDetail();
                        modeldetails.RubricRemedyId = rubricRemedyDetailsEntity.RubricRemedyId;
                        modeldetails.AuthorId = item1.AuthorId;
                        context.RemedyRubricAuthorDetails.Add(modeldetails);
                        context.SaveChanges();
                    }
                }
                else
                {
                    var rubricRemedyDetails = context.RubricRemedyDetails.FirstOrDefault(x =>
                        x.RubricRemedyId == item.RubricRemedyId
                    );

                    if (rubricRemedyDetails != null)
                    {
                        rubricRemedyDetails.RubricRemedyId = item.RubricRemedyId;
                        rubricRemedyDetails.SubSectionId = item.SubSectionId;
                        rubricRemedyDetails.RemedyId = item.RemedyId;
                        rubricRemedyDetails.GradeId = item.GradeId;
                        rubricRemedyDetails.EnteredDate = item.EnteredDate;
                        rubricRemedyDetails.EnteredBy = item.EnteredBy;
                        rubricRemedyDetails.DeletedStatus = false;
                        context.SaveChanges();

                        foreach (var item1 in item.Authors)
                        {
                            if (item1.RemedyRubricAuthorId == 0)
                            {
                                var modeldetails = new RemedyRubricAuthorDetail();
                                modeldetails.RubricRemedyId = rubricRemedyDetails.RubricRemedyId;
                                modeldetails.AuthorId = item1.AuthorId;
                                context.RemedyRubricAuthorDetails.Add(modeldetails);
                                context.SaveChanges();
                            }
                            else
                            {
                                var modeldetails = context.RemedyRubricAuthorDetails.FirstOrDefault(
                                    x => x.RemedyRubricAuthorId == item1.RemedyRubricAuthorId
                                );
                                if (modeldetails != null)
                                {
                                    modeldetails.RubricRemedyId =
                                        rubricRemedyDetails.RubricRemedyId;
                                    modeldetails.AuthorId = item1.AuthorId;
                                    context.SaveChanges();
                                }
                            }
                        }
                    }
                }

                Message = "Rubric Remedy Details Saved Successfully";
            }

            return Message;
        }

        /// <summary>
        /// Method implementation for getting rubric remedy details.
        /// </summary>
        /// <param name="rubricRemedyDetailsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public RemedyRubricViewModel GetRubricRemedyDetails(
            long RemedyId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var remadyInfo = context
                .RemedyMasters.Where(x => x.RemedyId == RemedyId)
                .FirstOrDefault();
            var remedyRubricViewModel = new RemedyRubricViewModel();
            remedyRubricViewModel.RemedyID = remadyInfo.RemedyId;
            remedyRubricViewModel.RemedyName = remadyInfo.RemedyName;
            remedyRubricViewModel.ThemesOrCharacteristics = remadyInfo.ThemesOrCharacteristics;
            remedyRubricViewModel.Generals = remadyInfo.Generals;
            remedyRubricViewModel.Particulars = remadyInfo.Particulars;
            remedyRubricViewModel.Modalities = remadyInfo.Modalities;

            errorResponseModel = new ErrorResponseModel();
            var remedyEntities = (
                from remedyDetails in context.RubricRemedyDetails
                join subSection in context.SubSectionMasters
                    on remedyDetails.SubSectionId equals subSection.SubSectionId
                join gradeMaster in context.RemedyGradeMaster
                    on remedyDetails.GradeId equals gradeMaster.GradeId
                where
                    remedyDetails.RemedyId == RemedyId
                    && subSection.DeleteStatus == false
                    && remedyDetails.DeletedStatus == false
                select new RubricRemedyViewModel
                {
                    RubricRemedyId = remedyDetails.RubricRemedyId,
                    SectionId = subSection.SectionId,
                    SubSectionId = subSection.SubSectionId,
                    SubSectionName = subSection.SubSectionName,
                    RemedyId = remedyDetails.RemedyId,
                    GradeId = gradeMaster.GradeId,
                    EnteredBy = remedyDetails.EnteredBy,
                    EnteredDate = remedyDetails.EnteredDate,
                    FontName = gradeMaster.FontName,
                    FontColor = gradeMaster.FontColor,
                    FontStyle = gradeMaster.FontStyle,
                    RemedyCount = context
                        .RubricRemedyDetails.Where(x => x.SubSectionId == subSection.SubSectionId)
                        .Count(),
                    IsSmallRubric = remedyDetails.IsSmallRubric,
                    IsConformationRubric = remedyDetails.IsConfirmationRubric,
                }
            ).OrderBy(x => x.SectionId).ToList();
            if (remedyEntities.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy Not Found";
            }
            remedyRubricViewModel.RubricRemedyViewsList = remedyEntities;
            return remedyRubricViewModel;
        }

        /// <summary>
        /// GetRubricRemedyDetails
        /// </summary>
        /// <param name="subSectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public RemedyCountsModel GetRemedyCounts(
            int subSectionId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var remedyCount = context
                .RubricRemedyDetails.Where(x =>
                    x.SubSectionId == subSectionId
                    && x.DeletedStatus == false
                    && x.Remedy != null
                    && x.Remedy.DeleteStatus == false
                    && x.Grade != null
                    && x.RemedyId != null
                )
                .Select(x => x.RemedyId.Value)
                .Distinct()
                .Count();
            var remedyCountModel = new RemedyCountsModel();
            errorResponseModel = new ErrorResponseModel();
            if (remedyCount == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy not found";
            }
            remedyCountModel.SubSectionId = subSectionId;
            remedyCountModel.RemedyCount = remedyCount;
            return remedyCountModel;
        }

        /// <summary>
        /// GetRubricList
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<RubricModel> GetRubricList(
            int SectionId,
            NigaParameters nigaParameters,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var runricModelList = new List<RubricModel>();
            var rubricRemedyGroup = context
                .RubricRemedyDetails.Include(x => x.SubSection)
                .Include(x => x.Grade)
                .Include(x => x.Remedy)
                .Where(x =>
                    x.SubSection.DeleteStatus.Equals(false) && x.SubSection.SectionId == SectionId
                )
                .GroupBy(x => new { x.SubSectionId, x.GradeId })
                .Skip((nigaParameters.PageNumber - 1) * nigaParameters.PageSize)
                .Take(nigaParameters.PageSize)
                .ToList();

            if (rubricRemedyGroup.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy not found";
            }
            var list = rubricRemedyGroup
                .Select(x => new RubricModel
                {
                    RubricRemedyId = x.Max(item => item.RubricRemedyId),
                    SubSectionId = Convert.ToInt32(x.Key.SubSectionId),
                    Grade = Convert.ToInt32(x.Key.GradeId),
                    SectionId = x.Select(subsection => subsection.SubSection.SectionId)
                        .FirstOrDefault(),
                    SectionName = context
                        .SectionMasters.Where(s => s.SectionId == SectionId)
                        .Select(s => s.SectionName)
                        .FirstOrDefault(),
                    //SectionName=x.Select(section=>section.Section.SectionName).FirstOrDefault(),
                    SubSectionName = x.Select(subsection => subsection.SubSection.SubSectionName)
                        .FirstOrDefault(),
                })
                .Where(x => x.SectionId == SectionId)
                .GroupBy(x => new { x.SubSectionId })
                .Select(g => g.First())
                .ToList();
            return list.OrderBy(x => x.SubSectionName).ToList();
        }

        /// <summary>
        /// Get Grade remedies from subsection
        /// </summary>
        /// <param name="subSectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<GradeRemediesModel> GetGradeRemedies(
            int subSectionId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var gradeGroup = context
                .RubricRemedyDetails.Where(x =>
                    x.SubSectionId == subSectionId && x.DeletedStatus == false
                )
                .Include(x => x.Grade)
                .Include(x => x.Remedy)
                .Include(x => x.RemedyRubricAuthorDetails)
                .GroupBy(x => x.GradeId)
                .ToList();
            errorResponseModel = new ErrorResponseModel();
            if (gradeGroup.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Grade details not found";
                return new List<GradeRemediesModel>();
            }

            return gradeGroup
                .Select(grade => new GradeRemediesModel
                {
                    GradeId = Convert.ToInt32(grade.Key),
                    GradeNo = grade.Select(x => x.Grade.GradeNo).FirstOrDefault(),
                    FontName = grade.Select(x => x.Grade.FontName).FirstOrDefault(),
                    FontStyle = grade.Select(x => x.Grade.FontStyle).FirstOrDefault(),
                    FontColor = grade.Select(x => x.Grade.FontColor).FirstOrDefault(),
                    Description = grade.Select(x => x.Grade.Description).FirstOrDefault(),
                    subSectionId = subSectionId,
                    remediesModels = grade
                        .Select(remedy => new RemediesModel
                        {
                            RemedyId = Convert.ToInt32(remedy.RemedyId),
                            RemedyName = remedy.Remedy.RemedyName,
                            RemedyAlias = string.IsNullOrEmpty(remedy.Remedy.RemedyAlias)
                                ? "Not Available"
                                : remedy.Remedy.RemedyAlias,
                            AuthorId =
                                (int?)
                                    remedy
                                        .RemedyRubricAuthorDetails.FirstOrDefault(x =>
                                            x.RubricRemedyId == remedy.RubricRemedyId
                                        )
                                        ?.AuthorId ?? 0,
                            AuthorAlias = GetAuthorAlies(Convert.ToInt32(remedy.RemedyId)),
                            //context.AuthorMaster
                            //.Where(x => x.AuthorId == (remedy.RemedyRubricAuthorDetails
                            //    .FirstOrDefault(xa => xa.RubricRemedyId == remedy.RubricRemedyId).AuthorId))
                            //.FirstOrDefault()?.AuthorAlias
                        })
                        .OrderBy(x => x.RemedyName)
                        .ToList(),
                })
                .OrderBy(x => x.GradeNo)
                .ToList();
        }

        private string GetAuthorAlies(int remedyId)
        {
            var authorAlies = (
                from authorMaster in context.AuthorMasters.AsNoTracking()
                join remedyRubricAuthorDetails in context.RemedyRubricAuthorDetails.AsNoTracking()
                    on authorMaster.AuthorId equals remedyRubricAuthorDetails.AuthorId
                join rubricRemedyDetail in context.RubricRemedyDetails.AsNoTracking()
                    on remedyRubricAuthorDetails.RubricRemedyId equals rubricRemedyDetail.RubricRemedyId
                where
                    rubricRemedyDetail.RemedyId == remedyId
                    && rubricRemedyDetail.DeletedStatus == false
                    && remedyRubricAuthorDetails.DeletedStatus == false
                select new { authorMaster.AuthorAlias }
            ).ToList();

            return string.Join(",", authorAlies.Distinct().Select(x => x.AuthorAlias));
        }

        private Dictionary<int, string> BuildAuthorAliasMapForSubSection(int subSectionId)
        {
            var authorRows = (
                from rrd in context.RubricRemedyDetails.AsNoTracking()
                join rrau in context.RemedyRubricAuthorDetails.AsNoTracking()
                    on rrd.RubricRemedyId equals rrau.RubricRemedyId
                join author in context.AuthorMasters.AsNoTracking()
                    on rrau.AuthorId equals author.AuthorId
                where
                    rrd.SubSectionId == subSectionId
                    && rrd.DeletedStatus == false
                    && rrau.DeletedStatus == false
                    && rrd.RemedyId != null
                select new { RemedyId = rrd.RemedyId.Value, author.AuthorAlias }
            ).ToList();

            return authorRows
                .Where(x => !string.IsNullOrWhiteSpace(x.AuthorAlias))
                .GroupBy(x => x.RemedyId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(",", g.Select(x => x.AuthorAlias).Distinct())
                );
        }

        private Dictionary<int, string> BuildGlobalAuthorAliasMapForRemedyIds(IEnumerable<int> remedyIds)
        {
            var ids = remedyIds?.Distinct().ToList() ?? new List<int>();
            if (!ids.Any())
            {
                return new Dictionary<int, string>();
            }

            var authorRows = (
                from rrd in context.RubricRemedyDetails.AsNoTracking()
                join rrau in context.RemedyRubricAuthorDetails.AsNoTracking()
                    on rrd.RubricRemedyId equals rrau.RubricRemedyId
                join author in context.AuthorMasters.AsNoTracking()
                    on rrau.AuthorId equals author.AuthorId
                where
                    rrd.RemedyId != null
                    && ids.Contains(rrd.RemedyId.Value)
                    && rrd.DeletedStatus == false
                    && rrau.DeletedStatus == false
                select new { RemedyId = rrd.RemedyId.Value, author.AuthorAlias }
            ).ToList();

            return authorRows
                .Where(x => !string.IsNullOrWhiteSpace(x.AuthorAlias))
                .GroupBy(x => x.RemedyId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(",", g.Select(x => x.AuthorAlias).Distinct())
                );
        }

        private static string MergeAuthorAliasStrings(params string[] parts)
        {
            var aliases = parts
                .SelectMany(part =>
                    (part ?? string.Empty).Split(
                        new[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries
                    )
                )
                .Select(alias => alias.Trim())
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return string.Join(",", aliases);
        }

        /// <summary>
        /// Get details to edit rubric remedies
        /// </summary>
        /// <param name="subSectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public RubricRemedyDetailsModel GetRemedyDetailsToEdit(
            int subSectionId,
            int grade,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var rubricRemedyDetails = context
                .RubricRemedyDetails.Where(x =>
                    x.SubSectionId == subSectionId && x.GradeId == grade
                )
                .ToList();
            if (rubricRemedyDetails.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Grade details not found";
            }
            var rubricRemedyDetailsModel = new RubricRemedyDetailsModel();
            var sectionMaster = context
                .SubSectionMasters.Where(x => x.SubSectionId == subSectionId)
                .FirstOrDefault();
            if (sectionMaster != null)
            {
                rubricRemedyDetailsModel.SectionId = sectionMaster.SectionId;
            }
            rubricRemedyDetailsModel.SubSectionId = subSectionId;
            rubricRemedyDetailsModel.RubricRemedyId = rubricRemedyDetails
                .Select(x => x.RubricRemedyId)
                .FirstOrDefault();
            rubricRemedyDetailsModel.GradeId =
                rubricRemedyDetails.Select(x => x.GradeId).FirstOrDefault() != null
                    ? Convert.ToInt32(rubricRemedyDetails.Select(x => x.GradeId).FirstOrDefault())
                    : 0;
            var remedies = string.Join(',', rubricRemedyDetails.Select(x => x.RemedyId).ToList());
            rubricRemedyDetailsModel.RemedyIds = remedies;

            return rubricRemedyDetailsModel;
        }

        public List<RubricRemedyViewModel1> GetSubSections(
            int sectionId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var subsectionModelList = new List<RubricRemedyViewModel1>();
            errorResponseModel = new ErrorResponseModel();
            var subsectionEntityList = context
                .SubSectionMasters.Where(x => x.DeleteStatus == false && x.SectionId == sectionId)
                .ToList();
            if (subsectionEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "SubSection not found";
            }

            subsectionEntityList.ForEach(item =>
            {
                subsectionModelList.Add(
                    new RubricRemedyViewModel1
                    {
                        SubSectionId = item.SubSectionId,
                        SectionId = item.SectionId,
                        SubSectionName = item.SubSectionName,
                        DeleteStatus = item.DeleteStatus,
                    }
                );
            });
            return subsectionModelList;
        }

        // Method created by Vikas More

        public RubricRemedyDetailModel GetRubricRemedyBySectionGread(
            int subSectionId,
            int greadId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var remedyModel = new List<RemedyModel>();
            errorResponseModel = new ErrorResponseModel();
            var remedyEntities = (
                from rubricRemedyDetails in context.RubricRemedyDetails
                join subSectionMaster in context.SubSectionMasters
                    on rubricRemedyDetails.SubSectionId equals subSectionMaster.SubSectionId
                join gradeMaster in context.RemedyGradeMaster
                    on rubricRemedyDetails.GradeId equals gradeMaster.GradeId
                join remedyMaster in context.RemedyMasters
                    on rubricRemedyDetails.RemedyId equals remedyMaster.RemedyId
                where
                    rubricRemedyDetails.SubSectionId == subSectionId
                    && rubricRemedyDetails.GradeId == greadId
                    && rubricRemedyDetails.DeletedStatus == false
                select new
                {
                    rubricRemedyDetails.RubricRemedyId,
                    subSectionMaster.SectionId,
                    subSectionMaster.SubSectionId,
                    gradeMaster.GradeId,
                    rubricRemedyDetails.RemedyId,
                    remedyMaster.RemedyName,
                }
            ).ToList();

            if (remedyEntities.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy not found";
            }

            RubricRemedyDetailModel rubricRemedyDetailModel = new RubricRemedyDetailModel();

            var remedyDetail = remedyEntities.FirstOrDefault();

            rubricRemedyDetailModel.SubSectionId = subSectionId;
            rubricRemedyDetailModel.GradeId = greadId;
            rubricRemedyDetailModel.SectionId = Convert.ToInt32(remedyDetail.SectionId);

            List<RubricRemedyAuthorModel> rubricRemedyAuthorsList =
                new List<RubricRemedyAuthorModel>();

            foreach (var item in remedyEntities)
            {
                RubricRemedyAuthorModel rubricRemedyAuthor = new RubricRemedyAuthorModel();

                rubricRemedyAuthor.RubricRemedyId = item.RubricRemedyId;
                rubricRemedyAuthor.RemedyId = item.RemedyId;
                rubricRemedyAuthor.RemedyName = item.RemedyName;
                var rubricAutorData = (
                    from remedyRubricAuthorDetails in context.RemedyRubricAuthorDetails
                    join auther in context.AuthorMasters
                        on remedyRubricAuthorDetails.AuthorId equals auther.AuthorId
                    where
                        remedyRubricAuthorDetails.RubricRemedyId == item.RubricRemedyId
                        && remedyRubricAuthorDetails.DeletedStatus == false
                    select new RubricAuthorModel
                    {
                        RemedyRubricAuthorId = remedyRubricAuthorDetails.RemedyRubricAuthorId,
                        AuthorId = remedyRubricAuthorDetails.AuthorId,
                        AuthorName = auther.AuthorName,
                    }
                ).ToList();
                rubricRemedyAuthor.RubricAuthorList = rubricAutorData;
                rubricRemedyAuthorsList.Add(rubricRemedyAuthor);
            }
            rubricRemedyDetailModel.RubricRemedyAuthorList = rubricRemedyAuthorsList;

            return rubricRemedyDetailModel;
        }

        public string SaveUpdateRubricRemedy(
            RubricRemedyDetailModel rubricRemedyDetail,
            int userId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string Message = "";

            foreach (var remedyItem in rubricRemedyDetail.RubricRemedyAuthorList)
            {
                if (remedyItem.RubricRemedyId == 0)
                {
                    var rubricRemedyDetailsEntityData = context.RubricRemedyDetails.FirstOrDefault(
                        x =>
                            x.SubSectionId == rubricRemedyDetail.SubSectionId
                            && x.GradeId == rubricRemedyDetail.GradeId
                            && x.RemedyId == remedyItem.RemedyId
                            && x.DeletedStatus == false
                    );

                    if (rubricRemedyDetailsEntityData == null)
                    {
                        RubricRemedyDetail rubricRemedyDetailsEntity = new RubricRemedyDetail();
                        rubricRemedyDetailsEntity.SubSectionId = rubricRemedyDetail.SubSectionId;
                        rubricRemedyDetailsEntity.GradeId = rubricRemedyDetail.GradeId;
                        rubricRemedyDetailsEntity.RemedyId = remedyItem.RemedyId;
                        rubricRemedyDetailsEntity.EnteredBy = userId;
                        rubricRemedyDetailsEntity.EnteredDate = DateTime.Now;
                        rubricRemedyDetailsEntity.DeletedStatus = false;
                        context.RubricRemedyDetails.Add(rubricRemedyDetailsEntity);
                        context.SaveChanges();

                        foreach (var authorItem in remedyItem.RubricAuthorList)
                        {
                            AddRemedyRubricAuthorDetails(
                                rubricRemedyDetailsEntity.RubricRemedyId,
                                authorItem.AuthorId
                            );
                        }
                    }
                    else
                    {
                        foreach (var authorItem in remedyItem.RubricAuthorList)
                        {
                            AddRemedyRubricAuthorDetails(
                                rubricRemedyDetailsEntityData.RubricRemedyId,
                                authorItem.AuthorId
                            );
                        }
                    }
                    Message = "Remedy Saved Successfully";
                }
                else
                {
                    var rubricRemedyDetailsEntity = context.RubricRemedyDetails.FirstOrDefault(x =>
                        x.RubricRemedyId == remedyItem.RubricRemedyId
                    );
                    if (rubricRemedyDetailsEntity != null)
                    {
                        rubricRemedyDetailsEntity.SubSectionId = rubricRemedyDetail.SubSectionId;
                        rubricRemedyDetailsEntity.GradeId = rubricRemedyDetail.GradeId;
                        rubricRemedyDetailsEntity.RemedyId = remedyItem.RemedyId;
                        rubricRemedyDetailsEntity.DeletedStatus = false;
                        context.SaveChanges();

                        foreach (var authorItem in remedyItem.RubricAuthorList)
                        {
                            if (authorItem.RemedyRubricAuthorId == 0)
                            {
                                AddRemedyRubricAuthorDetails(
                                    rubricRemedyDetailsEntity.RubricRemedyId,
                                    authorItem.AuthorId
                                );
                            }
                            else
                            {
                                var remedyRubricAuthor =
                                    context.RemedyRubricAuthorDetails.FirstOrDefault(x =>
                                        x.RemedyRubricAuthorId == authorItem.RemedyRubricAuthorId
                                    );
                                if (remedyRubricAuthor != null)
                                {
                                    remedyRubricAuthor.RubricRemedyId =
                                        rubricRemedyDetailsEntity.RubricRemedyId;
                                    remedyRubricAuthor.AuthorId = authorItem.AuthorId;
                                    remedyRubricAuthor.DeletedStatus = false;
                                    context.SaveChanges();
                                }
                            }
                        }

                        Message = "Remedy Updated Successfully";
                    }
                }
            }
            return Message;
        }

        private void AddRemedyRubricAuthorDetails(int rubricRemedyId, int? authorId)
        {
            var remedyRubricAuthorData = context.RemedyRubricAuthorDetails.FirstOrDefault(x =>
                x.RubricRemedyId == rubricRemedyId && x.AuthorId == authorId
            );

            if (remedyRubricAuthorData == null)
            {
                RemedyRubricAuthorDetail remedyRubricAuthor = new RemedyRubricAuthorDetail();
                remedyRubricAuthor.RubricRemedyId = rubricRemedyId;
                remedyRubricAuthor.AuthorId = authorId;
                remedyRubricAuthor.DeletedStatus = false;
                context.RemedyRubricAuthorDetails.Add(remedyRubricAuthor);
                context.SaveChanges();
            }
        }

        public string DeleteRubricRemedyAuthor(
            RubricRemedyDeleteModel rubricRemedyDeleteModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string message = string.Empty;

            if (rubricRemedyDeleteModel.RemedyRubricAuthorId > 0)
            {
                var remedyRubricAuthor = context.RemedyRubricAuthorDetails.FirstOrDefault(x =>
                    x.RemedyRubricAuthorId == rubricRemedyDeleteModel.RemedyRubricAuthorId
                );
                if (remedyRubricAuthor != null)
                {
                    remedyRubricAuthor.DeletedStatus = true;
                    context.SaveChanges();
                    message = "Record Deleted successfully";
                }
                else
                {
                    message = "Record not found";
                }
            }
            else if (rubricRemedyDeleteModel.RubricRemedyId > 0)
            {
                var rubricRemedyDetailsEntity = context.RubricRemedyDetails.FirstOrDefault(x =>
                    x.RubricRemedyId == rubricRemedyDeleteModel.RubricRemedyId
                );
                if (rubricRemedyDetailsEntity != null)
                {
                    rubricRemedyDetailsEntity.DeletedStatus = true;
                    context.SaveChanges();
                    message = "Record Deleted successfully";
                }
                else
                {
                    message = "Record not found";
                }
            }
            return message;
        }

        public string UpdateIsSmallRubric(
            int rubricRemedyID,
            bool isSmallRubric,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string message = string.Empty;
            var rubricRemadyEntity = context
                .RubricRemedyDetails.Where(x => x.RubricRemedyId == rubricRemedyID)
                .FirstOrDefault();
            if (rubricRemadyEntity != null)
            {
                rubricRemadyEntity.IsSmallRubric = isSmallRubric;
                context.SaveChanges();
                message = "Update sucessfully isSmall rubric remady";
            }
            else
            {
                message = "Failed to Update isSmall rubric remady";
            }

            return message;
        }

        public string UpdateIsConfirmationRubric(
            int rubricRemedyID,
            bool isConformationRubric,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string message = string.Empty;
            var rubricRemadyEntity = context
                .RubricRemedyDetails.Where(x => x.RubricRemedyId == rubricRemedyID)
                .FirstOrDefault();
            if (rubricRemadyEntity != null)
            {
                rubricRemadyEntity.IsConfirmationRubric = isConformationRubric;
                context.SaveChanges();
                message = "Update sucessfully isSmall rubric remady";
            }
            else
            {
                message = "Failed to Update isSmall rubric remady";
            }

            return message;
        }

        /// <summary>
        /// Get Grade remedies from subsection
        /// </summary>
        /// <param name="subSectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<RemediesModel> GetGradeRemedies1(
            int subSectionId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var gradeGroup = context
                .RubricRemedyDetails.Where(x =>
                    x.SubSectionId == subSectionId && x.DeletedStatus == false
                )
                .Include(x => x.Grade)
                .Include(x => x.Remedy)
                .Include(x => x.RemedyRubricAuthorDetails)
                .GroupBy(x => x.RemedyId)
                .ToList();
            errorResponseModel = new ErrorResponseModel();
            if (gradeGroup.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Grade details not found";
                return new List<RemediesModel>();
            }

            return gradeGroup
                .Select(grade => new RemediesModel
                {
                    GradeNo = grade.Select(x => x.Grade.GradeNo).FirstOrDefault(),
                    FontName = grade.Select(x => x.Grade.FontName).FirstOrDefault(),
                    FontStyle = grade.Select(x => x.Grade.FontStyle).FirstOrDefault(),
                    FontColor = grade.Select(x => x.Grade.FontColor).FirstOrDefault(),
                    RemedyId = grade.Select(x => Convert.ToInt32(x.RemedyId)).FirstOrDefault(),
                    RemedyName = grade.Select(x => x.Remedy.RemedyName).FirstOrDefault(),
                    RemedyAlias = grade
                        .Select(x =>
                            string.IsNullOrEmpty(x.Remedy.RemedyAlias)
                                ? "Not Available"
                                : x.Remedy.RemedyAlias
                        )
                        .FirstOrDefault(),
                    AuthorAlias = GetAuthorAlies(
                        grade.Select(x => Convert.ToInt32(x.RemedyId)).FirstOrDefault()
                    ),
                    //remediesModels = grade.Select(remedy => new RemediesModel
                    //{
                    //    RemedyId = Convert.ToInt32(remedy.RemedyId),
                    //    RemedyName = remedy.Remedy.RemedyName,
                    //    RemedyAlias = string.IsNullOrEmpty(remedy.Remedy.RemedyAlias) ? "Not Available" : remedy.Remedy.RemedyAlias,
                    //    AuthorId = (int?)remedy.RemedyRubricAuthorDetails
                    //                .FirstOrDefault(x => x.RubricRemedyId == remedy.RubricRemedyId)
                    //                ?.AuthorId ?? 0,
                    //    AuthorAlias = GetAuthorAlies(Convert.ToInt32(remedy.RemedyId))
                    //    //context.AuthorMaster
                    //    //.Where(x => x.AuthorId == (remedy.RemedyRubricAuthorDetails
                    //    //    .FirstOrDefault(xa => xa.RubricRemedyId == remedy.RubricRemedyId).AuthorId))
                    //    //.FirstOrDefault()?.AuthorAlias

                    //}).OrderBy(x => x.RemedyName).ToList()
                })
                .OrderBy(x => x.RemedyName)
                .ToList();
        }

        /// <summary>
        /// Get Grade remedies from subsection
        /// </summary>
        /// <param name="subSectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public RubricDetailModel GetRubricDetails(
            int subSectionId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            errorResponseModel = new ErrorResponseModel();

            var cacheKey = $"RubricDetails:v9:{subSectionId}";
            if (_cache.TryGetValue(cacheKey, out RubricDetailModel cached))
            {
                return cached;
            }

            var rubricDetails = context
                .SubSectionMasters.AsNoTracking()
                .Where(subSection =>
                    subSection.SubSectionId == subSectionId && !subSection.DeleteStatus
                )
                .Select(subSection => new RubricDetailModel
                {
                    SubSectionId = subSection.SubSectionId,
                    Description = subSection.Description,
                    SubSectionNameAlias = subSection.SubSectionNameAlias,
                    SubSectionName = subSection.SubSectionName,
                    SectionId = subSection.SectionId,
                    ParentSubSectionId = subSection.ParentSubSectionId,
                })
                .FirstOrDefault();

            if (rubricDetails == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Section not found";
                return null;
            }

            var referanceRubricList = (
                from referanceRubric in context.ReferenceRubricDetails.AsNoTracking()
                join subSection in context.SubSectionMasters.AsNoTracking()
                    on referanceRubric.RefSubSectionId equals subSection.SubSectionId
                join section in context.SectionMasters.AsNoTracking()
                    on subSection.SectionId equals section.SectionId
                where
                    referanceRubric.SubSectionId == subSectionId
                    && referanceRubric.DeleteStatus == false
                select new ReferenceRubricDetailsModel
                {
                    SectionId = subSection.SectionId,
                    SectionName = section.SectionName,
                    SubSectionId = referanceRubric.SubSectionId,
                    ReferenceRubricId = (int)referanceRubric.ReferenceRubricId,
                    RefSubSectionId = referanceRubric.RefSubSectionId,
                    RefSubSectionName = subSection.SubSectionName,
                }
            ).ToList();

            var subsectionlanguageList = (
                from subSectionLanguage in context.SubSectionLanguageDetails.AsNoTracking()
                join languageMaster in context.LanguageMasters.AsNoTracking()
                    on subSectionLanguage.LanguageId equals languageMaster.LanguageId
                where
                    subSectionLanguage.SubSectionId == subSectionId
                    && subSectionLanguage.DeleteStatus == false
                select new SubSectionLanguageDetailsModel
                {
                    SubSectionId = subSectionLanguage.SubSectionId,
                    SectionName = rubricDetails.SubSectionName,
                    LanguageId = subSectionLanguage.LanguageId,
                    SubSectionDetails = subSectionLanguage.SubSectionDetails,
                    LanguageName = languageMaster.LanguageName,
                    SubSectionLanguageId = subSectionLanguage.SubSectionLanguageId,
                    LanguageDescription = languageMaster.Description,
                }
            ).ToList();

            var remedyRows = context
                .RubricRemedyDetails.AsNoTracking()
                .Where(x => x.SubSectionId == subSectionId && x.DeletedStatus == false)
                .Where(x => x.Remedy != null && x.Remedy.DeleteStatus == false)
                .Where(x => x.Grade != null && x.RemedyId != null)
                .Select(x => new
                {
                    RemedyId = x.RemedyId.Value,
                    x.Remedy.RemedyName,
                    x.Remedy.RemedyAlias,
                    x.Grade.GradeNo,
                    x.Grade.FontName,
                    x.Grade.FontStyle,
                    x.Grade.FontColor,
                    Authors = x.RemedyRubricAuthorDetails
                        .Where(a => a.DeletedStatus == false)
                        .Select(a => a.Author.AuthorAlias),
                })
                .ToList();

            var remedyIds = remedyRows.Select(x => x.RemedyId).Distinct().ToList();
            var subsectionAuthorMap = BuildAuthorAliasMapForSubSection(subSectionId);
            var globalAuthorMap = BuildGlobalAuthorAliasMapForRemedyIds(remedyIds);

            var remediesList = remedyRows
                .GroupBy(x => x.RemedyId)
                .Select(grade =>
                {
                    var rowAuthors = string.Join(
                        ",",
                        grade
                            .SelectMany(x => x.Authors)
                            .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    );
                    subsectionAuthorMap.TryGetValue(grade.Key, out var subsectionAuthors);
                    globalAuthorMap.TryGetValue(grade.Key, out var globalAuthors);
                    var mergedAuthors = MergeAuthorAliasStrings(
                        rowAuthors,
                        subsectionAuthors,
                        globalAuthors
                    );
                    if (string.IsNullOrWhiteSpace(mergedAuthors))
                    {
                        mergedAuthors = GetAuthorAlies(grade.Key);
                    }

                    var remedy = new RemediesModel
                    {
                        RemedyId = grade.Key,
                        GradeNo = grade.Select(x => x.GradeNo).FirstOrDefault(),
                        FontName = grade.Select(x => x.FontName).FirstOrDefault(),
                        FontStyle = grade.Select(x => x.FontStyle).FirstOrDefault(),
                        FontColor = grade.Select(x => x.FontColor).FirstOrDefault(),
                        RemedyName = grade.Select(x => x.RemedyName).FirstOrDefault(),
                        RemedyAlias = grade
                            .Select(x =>
                                string.IsNullOrEmpty(x.RemedyAlias) ? x.RemedyName : x.RemedyAlias
                            )
                            .FirstOrDefault(),
                        AuthorAlias = mergedAuthors,
                    };

                    return remedy;
                })
                .OrderBy(x => x.RemedyName)
                .ToList();

            rubricDetails.Referencerubric = referanceRubricList;
            rubricDetails.SubSectionLanguageDetails = subsectionlanguageList;
            rubricDetails.RemdeyCount = remediesList.Count;
            rubricDetails.RemediesList = remediesList;

            _cache.Set(cacheKey, rubricDetails, RubricDetailsCacheOptions);

            return rubricDetails;
        }

        public async Task<ImportResultModel> ImportFromExcelAsync(IFormFile file, string originalFileName)
        {
            if (file == null || file.Length == 0)
            {
                return new ImportResultModel { Message = "File not selected or empty", Errors = new List<string> { "File not selected or empty" } };
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "niga-rubric-imports");
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}_{Path.GetFileName(originalFileName ?? file.FileName)}");
            try
            {
                await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await file.CopyToAsync(fs);
                }
                return await ImportFromExcelFileAsync(tempPath, originalFileName ?? file.FileName);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignore */ }
            }
        }

        public async Task<ImportResultModel> ImportFromExcelFileAsync(
            string filePath,
            string originalFileName,
            Action<int, int> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var result = new ImportResultModel();
            var skippedRows = new List<SkippedRowDetail>();
            int newlyAddedCount = 0;
            int existingCount = 0;
            int successCount = 0;
            int failureCount = 0;
            const int BATCH_SIZE = 2000;
            const int PROGRESS_EVERY = 1000;

            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    throw new FileNotFoundException("Import file not found.", filePath);

                // --- Phase 1: Parse file into rows (supports XLSX and CSV) ---
                var excelRows = new List<(int RowNum, ExcelRubricRemedyRow Row)>();
                var ext = Path.GetExtension(originalFileName ?? filePath)?.ToLowerInvariant();
                await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (ext == ".csv")
                    {
                        using var reader = new StreamReader(stream);
                        int lineNum = 0;
                        while (!reader.EndOfStream)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var line = await reader.ReadLineAsync();
                            lineNum++;
                            if (lineNum == 1) continue;
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var cols = ParseCsvLine(line);
                            excelRows.Add((lineNum, new ExcelRubricRemedyRow
                            {
                                Subsection = cols.Length > 0 ? cols[0].Trim() : "",
                                Grade_1 = cols.Length > 1 ? cols[1].Trim() : "",
                                Author_1 = cols.Length > 2 ? cols[2].Trim() : "",
                                Grade_2 = cols.Length > 3 ? cols[3].Trim() : "",
                                Author_2 = cols.Length > 4 ? cols[4].Trim() : "",
                                Grade_3 = cols.Length > 5 ? cols[5].Trim() : "",
                                Author_3 = cols.Length > 6 ? cols[6].Trim() : "",
                                Grade_4 = cols.Length > 7 ? cols[7].Trim() : "",
                                Author_4 = cols.Length > 8 ? cols[8].Trim() : "",
                            }));
                        }
                    }
                    else
                    {
                        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                        var worksheet = workbook.Worksheets.FirstOrDefault()
                            ?? throw new Exception("Worksheet not found in the Excel file.");

                        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                        for (int row = 2; row <= lastRow; row++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            excelRows.Add((row, new ExcelRubricRemedyRow
                            {
                                Subsection = worksheet.Cell(row, 1).GetString()?.Trim() ?? "",
                                Grade_1 = worksheet.Cell(row, 2).GetString()?.Trim() ?? "",
                                Author_1 = worksheet.Cell(row, 3).GetString()?.Trim() ?? "",
                                Grade_2 = worksheet.Cell(row, 4).GetString()?.Trim() ?? "",
                                Author_2 = worksheet.Cell(row, 5).GetString()?.Trim() ?? "",
                                Grade_3 = worksheet.Cell(row, 6).GetString()?.Trim() ?? "",
                                Author_3 = worksheet.Cell(row, 7).GetString()?.Trim() ?? "",
                                Grade_4 = worksheet.Cell(row, 8).GetString()?.Trim() ?? "",
                                Author_4 = worksheet.Cell(row, 9).GetString()?.Trim() ?? "",
                            }));
                        }
                    }
                }

                result.TotalRows = excelRows.Count;
                progressCallback?.Invoke(0, excelRows.Count);
                if (excelRows.Count == 0)
                {
                    result.Message = "No data rows found in file.";
                    return result;
                }

                // --- Phase 2: Preload masters (single round-trips, case-insensitive) ---
                var subsectionMap = await context.SubSectionMasters.AsNoTracking()
                    .Where(x => !x.DeleteStatus)
                    .Select(x => new { x.SubSectionId, x.SubSectionName, x.SectionId })
                    .ToListAsync(cancellationToken);
                var subsectionDict = subsectionMap
                    .GroupBy(x => (x.SubSectionName ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var sectionMap = await context.SectionMasters.AsNoTracking()
                    .Where(x => !x.DeleteStatus)
                    .ToListAsync(cancellationToken);
                var sectionDict = sectionMap
                    .GroupBy(x => (x.SectionName ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var remedyMap = await context.RemedyMasters.AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.RemedyAlias))
                    .Select(x => new { x.RemedyId, x.RemedyAlias })
                    .ToListAsync(cancellationToken);
                var remedyDict = remedyMap
                    .GroupBy(x => x.RemedyAlias.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().RemedyId, StringComparer.OrdinalIgnoreCase);

                var authorMap = await context.AuthorMasters.AsNoTracking()
                    .Where(x => x.IsDeleted == false && !string.IsNullOrEmpty(x.AuthorAlias))
                    .Select(x => new { x.AuthorId, x.AuthorAlias })
                    .ToListAsync(cancellationToken);
                var authorDict = authorMap
                    .GroupBy(x => x.AuthorAlias.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().AuthorId, StringComparer.OrdinalIgnoreCase);

                // Preload existing rubric IDs so we never hit DB per row
                var existingRubricRows = await context.RubricRemedyDetails.AsNoTracking()
                    .Where(x => x.DeletedStatus == false && x.SubSectionId != null && x.RemedyId != null && x.GradeId != null)
                    .Select(x => new { x.RubricRemedyId, x.SubSectionId, x.RemedyId, x.GradeId })
                    .ToListAsync(cancellationToken);
                var existingRubricIdMap = existingRubricRows
                    .GroupBy(x => x.SubSectionId + "|" + x.RemedyId + "|" + x.GradeId)
                    .ToDictionary(g => g.Key, g => g.First().RubricRemedyId);

                var existingAuthorLinkSet = new HashSet<string>(
                    await context.RemedyRubricAuthorDetails.AsNoTracking()
                        .Where(x => x.DeletedStatus == false && x.RubricRemedyId != null && x.AuthorId != null)
                        .Select(x => x.RubricRemedyId + "|" + x.AuthorId)
                        .ToListAsync(cancellationToken)
                );

                var gradeIdMap = new Dictionary<int, int> { { 1, 5 }, { 2, 2 }, { 3, 3 }, { 4, 4 } };

                // --- Phase 2b: Create all missing sections/subsections in one pass ---
                var missingSubNames = excelRows
                    .Select(r => (r.Row.Subsection ?? "").Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s) && !subsectionDict.ContainsKey(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (missingSubNames.Count > 0)
                {
                    context.ChangeTracker.AutoDetectChangesEnabled = false;
                    foreach (var subName in missingSubNames)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var secName = subName.Contains('-') ? subName.Split('-')[0].Trim() : subName;
                        int secId;
                        if (sectionDict.TryGetValue(secName, out var existingSec))
                        {
                            secId = existingSec.SectionId;
                        }
                        else
                        {
                            var newSec = new SectionMaster
                            {
                                SectionName = secName,
                                SectionAlias = secName,
                                DeleteStatus = false,
                                EnteredBy = "1",
                                EnteredDate = DateTime.Now,
                            };
                            context.Add(newSec);
                            await context.SaveChangesAsync(cancellationToken);
                            secId = newSec.SectionId;
                            sectionDict[secName] = newSec;
                        }

                        var newSubSec = new SubSectionMaster
                        {
                            SubSectionName = subName,
                            SubSectionNameAlias = subName,
                            SectionId = secId,
                            DeleteStatus = false,
                            EnteredBy = "1",
                            EnteredDate = DateTime.Now,
                        };
                        context.Add(newSubSec);
                        await context.SaveChangesAsync(cancellationToken);
                        subsectionDict[subName] = new { SubSectionId = newSubSec.SubSectionId, SubSectionName = subName, SectionId = (int?)secId };
                    }
                    context.ChangeTracker.Clear();
                    context.ChangeTracker.AutoDetectChangesEnabled = true;
                }

                // --- Phase 3: Process rows in bulk batches (no per-row DB lookups) ---
                var pendingInserts = new List<(RubricRemedyDetail Detail, string AuthorsCsv, int RowNum, string Subsection)>(BATCH_SIZE);
                var deferredAuthorLinks = new List<RemedyRubricAuthorDetail>(BATCH_SIZE);

                async Task FlushPendingAsync()
                {
                    if (pendingInserts.Count == 0 && deferredAuthorLinks.Count == 0)
                        return;

                    if (pendingInserts.Count > 0)
                    {
                        context.RubricRemedyDetails.AddRange(pendingInserts.Select(p => p.Detail));
                        await context.SaveChangesAsync(cancellationToken);

                        foreach (var pending in pendingInserts)
                        {
                            if (!string.IsNullOrWhiteSpace(pending.AuthorsCsv))
                            {
                                LinkAuthorsInMemory(
                                    pending.Detail.RubricRemedyId,
                                    pending.AuthorsCsv,
                                    authorDict,
                                    existingAuthorLinkSet,
                                    deferredAuthorLinks,
                                    skippedRows,
                                    pending.RowNum,
                                    pending.Subsection);
                            }
                        }
                        pendingInserts.Clear();
                    }

                    if (deferredAuthorLinks.Count > 0)
                    {
                        context.RemedyRubricAuthorDetails.AddRange(deferredAuthorLinks);
                        await context.SaveChangesAsync(cancellationToken);
                        deferredAuthorLinks.Clear();
                    }

                    context.ChangeTracker.Clear();
                }

                context.ChangeTracker.AutoDetectChangesEnabled = false;
                using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

                for (int i = 0; i < excelRows.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var (rowNum, row) = excelRows[i];
                    try
                    {
                        var subsectionName = (row.Subsection ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(subsectionName))
                        {
                            skippedRows.Add(new SkippedRowDetail { RowNumber = rowNum, Subsection = row.Subsection, Reason = "Subsection is empty" });
                            failureCount++;
                            continue;
                        }

                        if (!subsectionDict.TryGetValue(subsectionName, out var existingSub))
                        {
                            skippedRows.Add(new SkippedRowDetail { RowNumber = rowNum, Subsection = subsectionName, Reason = "Subsection could not be resolved" });
                            failureCount++;
                            continue;
                        }

                        int subSecId = existingSub.SubSectionId;
                        var gradeValues = new[] { row.Grade_1, row.Grade_2, row.Grade_3, row.Grade_4 };
                        var authorValues = new[] { row.Author_1, row.Author_2, row.Author_3, row.Author_4 };

                        bool rowHadSuccess = false;
                        for (int g = 0; g < 4; g++)
                        {
                            var gradeVal = gradeValues[g]?.Trim();
                            var authorVal = authorValues[g]?.Trim();
                            if (string.IsNullOrEmpty(gradeVal)) continue;

                            int gradeId = gradeIdMap[g + 1];

                            if (!remedyDict.TryGetValue(gradeVal, out var remedyId))
                            {
                                skippedRows.Add(new SkippedRowDetail
                                {
                                    RowNumber = rowNum,
                                    Subsection = subsectionName,
                                    GradeValue = gradeVal,
                                    AuthorValue = authorVal,
                                    Reason = $"Remedy alias '{gradeVal}' not found in database (Grade_{g + 1})"
                                });
                                continue;
                            }

                            var compositeKey = subSecId + "|" + remedyId + "|" + gradeId;
                            if (existingRubricIdMap.TryGetValue(compositeKey, out var existingRubricId) && existingRubricId > 0)
                            {
                                existingCount++;
                                if (!string.IsNullOrWhiteSpace(authorVal))
                                {
                                    LinkAuthorsInMemory(
                                        existingRubricId,
                                        authorVal,
                                        authorDict,
                                        existingAuthorLinkSet,
                                        deferredAuthorLinks,
                                        skippedRows,
                                        rowNum,
                                        subsectionName);
                                }
                                rowHadSuccess = true;
                                continue;
                            }

                            if (existingRubricIdMap.ContainsKey(compositeKey))
                            {
                                // Already queued in current batch; count as existing duplicate within file.
                                existingCount++;
                                rowHadSuccess = true;
                                continue;
                            }

                            var rrd = new RubricRemedyDetail
                            {
                                SubSectionId = subSecId,
                                RemedyId = remedyId,
                                GradeId = gradeId,
                                EnteredBy = 1,
                                EnteredDate = DateTime.Now,
                                DeletedStatus = false,
                            };
                            // Reserve key so duplicate rows in the same file are treated as existing after first insert.
                            existingRubricIdMap[compositeKey] = -1;
                            newlyAddedCount++;
                            rowHadSuccess = true;
                            pendingInserts.Add((rrd, authorVal ?? "", rowNum, subsectionName));

                            if (pendingInserts.Count >= BATCH_SIZE)
                            {
                                var beforeFlush = pendingInserts.ToList();
                                await FlushPendingAsync();
                                foreach (var item in beforeFlush)
                                {
                                    var key = item.Detail.SubSectionId + "|" + item.Detail.RemedyId + "|" + item.Detail.GradeId;
                                    existingRubricIdMap[key] = item.Detail.RubricRemedyId;
                                }
                            }
                        }

                        if (deferredAuthorLinks.Count >= BATCH_SIZE)
                        {
                            context.RemedyRubricAuthorDetails.AddRange(deferredAuthorLinks);
                            await context.SaveChangesAsync(cancellationToken);
                            deferredAuthorLinks.Clear();
                            context.ChangeTracker.Clear();
                        }

                        if (rowHadSuccess) successCount++;
                        else failureCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        skippedRows.Add(new SkippedRowDetail
                        {
                            RowNumber = rowNum,
                            Subsection = row.Subsection,
                            Reason = $"Processing error: {ex.Message}"
                        });
                    }

                    if ((i + 1) % PROGRESS_EVERY == 0 || i + 1 == excelRows.Count)
                        progressCallback?.Invoke(i + 1, excelRows.Count);
                }

                if (pendingInserts.Count > 0)
                {
                    var beforeFlush = pendingInserts.ToList();
                    await FlushPendingAsync();
                    foreach (var item in beforeFlush)
                    {
                        var key = item.Detail.SubSectionId + "|" + item.Detail.RemedyId + "|" + item.Detail.GradeId;
                        existingRubricIdMap[key] = item.Detail.RubricRemedyId;
                    }
                }
                else
                {
                    await FlushPendingAsync();
                }

                await transaction.CommitAsync(cancellationToken);
                context.ChangeTracker.AutoDetectChangesEnabled = true;

                string skipFilePath = null;
                string skipFileBase64 = null;
                string skipFileName = null;
                if (skippedRows.Count > 0)
                {
                    skipFilePath = WriteSkipFile(skippedRows, originalFileName ?? Path.GetFileName(filePath));
                    if (!string.IsNullOrEmpty(skipFilePath) && File.Exists(skipFilePath))
                    {
                        skipFileBase64 = Convert.ToBase64String(File.ReadAllBytes(skipFilePath));
                        skipFileName = Path.GetFileName(skipFilePath);
                    }
                }

                result.TotalRows = excelRows.Count;
                result.SuccessCount = successCount;
                result.FailureCount = failureCount;
                result.NewlyAddedremedyCount = newlyAddedCount;
                result.ExistingremedyCount = existingCount;
                result.SkippedCount = skippedRows.Count;
                result.SkippedRows = skippedRows.Take(100).ToList();
                result.SkipFilePath = skipFilePath;
                result.SkipFileBase64 = skipFileBase64;
                result.SkipFileName = skipFileName;
                result.Message = skippedRows.Count > 0
                    ? $"Import completed. {skippedRows.Count} entries skipped — see details below."
                    : "Import completed successfully. All rows processed.";

                return result;
            }
            catch (Exception ex)
            {
                try { context.ChangeTracker.AutoDetectChangesEnabled = true; } catch { /* ignore */ }
                return new ImportResultModel
                {
                    TotalRows = result.TotalRows,
                    FailureCount = result.TotalRows,
                    Message = "Error during import: " + ex.Message,
                    Errors = new List<string> { ex.Message },
                };
            }
        }

        private void LinkAuthorsInMemory(int rubricRemedyId, string authorsCsv,
            Dictionary<string, int> authorDict, HashSet<string> existingAuthorLinkSet,
            List<RemedyRubricAuthorDetail> deferredLinks, List<SkippedRowDetail> skippedRows,
            int rowNum, string subsection)
        {
            var authorNames = authorsCsv.Split(',')
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a));

            foreach (var authorName in authorNames)
            {
                if (!authorDict.TryGetValue(authorName, out var authorId))
                {
                    skippedRows.Add(new SkippedRowDetail
                    {
                        RowNumber = rowNum, Subsection = subsection,
                        AuthorValue = authorName,
                        Reason = $"Author alias '{authorName}' not found in database"
                    });
                    continue;
                }

                var linkKey = rubricRemedyId + "|" + authorId;
                if (existingAuthorLinkSet.Contains(linkKey)) continue;

                deferredLinks.Add(new RemedyRubricAuthorDetail
                {
                    RubricRemedyId = rubricRemedyId,
                    AuthorId = authorId,
                    DeletedStatus = false,
                });
                existingAuthorLinkSet.Add(linkKey);
            }
        }

        private void ProcessPendingAuthorLinks(
            List<(RubricRemedyDetail Parent, int AuthorId)> pendingAuthorLinks,
            List<RubricRemedyDetail> pendingRemedyDetails,
            string[] authorValues, string[] gradeValues,
            Dictionary<string, int> authorDict, HashSet<string> existingAuthorLinkSet,
            List<RemedyRubricAuthorDetail> deferredLinks, List<SkippedRowDetail> skippedRows,
            List<(int RowNum, ExcelRubricRemedyRow Row)> allRows, int currentIdx, int rowNum,
            ExcelRubricRemedyRow currentRow)
        {
            var gradeIdMap = new Dictionary<int, int> { { 1, 5 }, { 2, 2 }, { 3, 3 }, { 4, 4 } };
            foreach (var rrd in pendingRemedyDetails)
            {
                var aVals = new[] { currentRow.Author_1, currentRow.Author_2, currentRow.Author_3, currentRow.Author_4 };
                for (int g = 0; g < 4; g++)
                {
                    if (rrd.GradeId == gradeIdMap[g + 1] && !string.IsNullOrWhiteSpace(aVals[g]))
                    {
                        LinkAuthorsInMemory(rrd.RubricRemedyId, aVals[g], authorDict, existingAuthorLinkSet, deferredLinks, skippedRows, rowNum, currentRow.Subsection);
                    }
                }
            }
        }

        private string WriteSkipFile(List<SkippedRowDetail> skippedRows, string uploadedFileName)
        {
            try
            {
                var skipDir = Path.Combine(Environment.CurrentDirectory, "Data", "RemedyExcel", "AddRemedieSkipFiles");
                Directory.CreateDirectory(skipDir);

                var safeName = Path.GetFileNameWithoutExtension(uploadedFileName)
                    ?.Replace(" ", "_") ?? "import";
                safeName = string.Join("_", safeName.Split(Path.GetInvalidFileNameChars()));
                var now = DateTime.Now;
                var fileName = $"{safeName}_{now:yyyy-MM-dd}_{now:HH-mm-ss}.xlsx";
                var filePath = Path.Combine(skipDir, fileName);

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var sheet = workbook.Worksheets.Add("SkippedRecords");

                sheet.Cell(1, 1).Value = "Row Number";
                sheet.Cell(1, 2).Value = "Subsection";
                sheet.Cell(1, 3).Value = "Grade Value";
                sheet.Cell(1, 4).Value = "Author Value";
                sheet.Cell(1, 5).Value = "Skip Reason";
                sheet.Cell(1, 6).Value = "Date";
                sheet.Row(1).Style.Font.Bold = true;

                int rowIndex = 2;
                foreach (var skip in skippedRows)
                {
                    sheet.Cell(rowIndex, 1).Value = skip.RowNumber;
                    sheet.Cell(rowIndex, 2).Value = skip.Subsection ?? "";
                    sheet.Cell(rowIndex, 3).Value = skip.GradeValue ?? "";
                    sheet.Cell(rowIndex, 4).Value = skip.AuthorValue ?? "";
                    sheet.Cell(rowIndex, 5).Value = skip.Reason ?? "";
                    sheet.Cell(rowIndex, 6).Value = now.ToString("yyyy-MM-dd HH:mm:ss");
                    rowIndex++;
                }

                sheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Skip file write error: " + ex.Message);
                return null;
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Exports all rubric-remedy data for a section to a single Excel sheet (one row per remedy mapping).
        /// </summary>
        public async Task<(byte[] FileBytes, string SectionName)> ExportRubricsToExcelAsync(int sectionId)
        {
            var section = await context.SectionMasters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SectionId == sectionId && !x.DeleteStatus);

            var sectionName = section?.SectionName ?? $"Section_{sectionId}";

            var rows = await (
                from rrd in context.RubricRemedyDetails.AsNoTracking()
                join sub in context.SubSectionMasters.AsNoTracking()
                    on rrd.SubSectionId equals sub.SubSectionId
                join rem in context.RemedyMasters.AsNoTracking()
                    on rrd.RemedyId equals rem.RemedyId
                join grade in context.RemedyGradeMaster.AsNoTracking()
                    on rrd.GradeId equals grade.GradeId
                where sub.SectionId == sectionId
                      && rrd.DeletedStatus == false
                      && sub.DeleteStatus == false
                      && rrd.SubSectionId != null
                orderby sub.SubSectionName, grade.GradeNo, rem.RemedyAlias, rem.RemedyName
                select new
                {
                    SectionId = sectionId,
                    SectionName = sectionName,
                    SubSectionId = sub.SubSectionId,
                    Subsection = sub.SubSectionName ?? string.Empty,
                    RubricRemedyId = rrd.RubricRemedyId,
                    GradeId = grade.GradeId,
                    GradeNo = grade.GradeNo,
                    RemedyId = rem.RemedyId,
                    RemedyAlias = rem.RemedyAlias ?? string.Empty,
                    RemedyName = rem.RemedyName ?? string.Empty,
                }
            ).ToListAsync();

            var rubricRemedyIds = rows.Select(x => x.RubricRemedyId).Distinct().ToList();
            var authorMap = new Dictionary<int, string>();

            if (rubricRemedyIds.Count > 0)
            {
                var authorRows = await (
                    from authLink in context.RemedyRubricAuthorDetails.AsNoTracking()
                    join author in context.AuthorMasters.AsNoTracking()
                        on authLink.AuthorId equals (int?)author.AuthorId
                    where authLink.RubricRemedyId != null
                          && rubricRemedyIds.Contains(authLink.RubricRemedyId.Value)
                          && authLink.DeletedStatus != true
                    select new
                    {
                        RubricRemedyId = authLink.RubricRemedyId.Value,
                        Alias = author.AuthorAlias ?? author.AuthorName ?? string.Empty,
                    }
                ).ToListAsync();

                authorMap = authorRows
                    .GroupBy(x => x.RubricRemedyId)
                    .ToDictionary(
                        g => g.Key,
                        g => string.Join(", ", g.Select(x => x.Alias).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct())
                    );
            }

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Rubrics");

            sheet.Cell(1, 1).Value = "SectionId";
            sheet.Cell(1, 2).Value = "SectionName";
            sheet.Cell(1, 3).Value = "SubSectionId";
            sheet.Cell(1, 4).Value = "Subsection";
            sheet.Cell(1, 5).Value = "GradeId";
            sheet.Cell(1, 6).Value = "GradeNo";
            sheet.Cell(1, 7).Value = "RemedyId";
            sheet.Cell(1, 8).Value = "RemedyAlias";
            sheet.Cell(1, 9).Value = "RemedyName";
            sheet.Cell(1, 10).Value = "Authors";
            sheet.Cell(1, 11).Value = "RubricRemedyId";
            sheet.Row(1).Style.Font.Bold = true;

            int rowIndex = 2;
            foreach (var item in rows)
            {
                sheet.Cell(rowIndex, 1).Value = item.SectionId;
                sheet.Cell(rowIndex, 2).Value = item.SectionName;
                sheet.Cell(rowIndex, 3).Value = item.SubSectionId;
                sheet.Cell(rowIndex, 4).Value = item.Subsection;
                sheet.Cell(rowIndex, 5).Value = item.GradeId;
                sheet.Cell(rowIndex, 6).Value = item.GradeNo;
                sheet.Cell(rowIndex, 7).Value = item.RemedyId;
                sheet.Cell(rowIndex, 8).Value = item.RemedyAlias;
                sheet.Cell(rowIndex, 9).Value = item.RemedyName;
                sheet.Cell(rowIndex, 10).Value = authorMap.TryGetValue(item.RubricRemedyId, out var authors)
                    ? authors
                    : string.Empty;
                sheet.Cell(rowIndex, 11).Value = item.RubricRemedyId;
                rowIndex++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return (stream.ToArray(), sectionName);
        }
    }
}

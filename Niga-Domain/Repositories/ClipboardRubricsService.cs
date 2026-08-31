using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Business.Implementation
{
    public class ClipboardRubricsService : IClipboardRubricsService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public ClipboardRubricsService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        /// <summary>
        /// Method to get all the Clipboard Rubrics
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<ClipboardRubricsModel> GetClipboardRubrics(
            ref ErrorResponseModel errorResponseModel
        )
        {
            var clipboardRubricsModelList = new List<ClipboardRubricsModel>();
            errorResponseModel = new ErrorResponseModel();
            var clipboardRubricsEntityList = context
                .ClipboardRubrics.Where(x => x.DeleteStatus == false)
                .ToList();

            if (clipboardRubricsEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Clipboard Rubrics not found";
            }

            clipboardRubricsEntityList.ForEach(item =>
            {
                clipboardRubricsModelList.Add(
                    new ClipboardRubricsModel
                    {
                        ClipboardRubricsId = item.ClipboardRubricsId,
                        Intensity = item.Intensity,
                        PatientId = item.PatientId,
                        SubSectionId = item.SubSectionId,
                        EnteredDate = item.EnteredDate,
                        EnteredBy = item.EnteredBy,
                        ChangedBy = item.ChangedBy,
                        ChangedDate = item.ChangedDate,
                        DeleteStatus = item.DeleteStatus,
                    }
                );
            });
            return clipboardRubricsModelList;
        }

        /// <summary>
        /// Method implementation for saving new Clipboard Rubrics
        /// </summary>
        /// <param name="clipboardRubricsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveClipboardRubrics(
            List<ClipboardRubricsModel> clipboardRubricsModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string Message = "";
            foreach (var item in clipboardRubricsModel)
            {
                DateTime currentDateTime = DateTime.Now;
                if (item.ClipboardRubricsId == 0)
                {
                    var isExits = context.ClipboardRubrics.FirstOrDefault(x =>
                        x.SubSectionId == item.SubSectionId
                        && x.PatientId == item.PatientId
                        && x.Intensity == item.Intensity
                        && x.DeleteStatus == false
                    );
                    if (isExits == null)
                    {
                        ClipboardRubric clipboardRubricsEntity = new ClipboardRubric();
                        clipboardRubricsEntity.Intensity = item.Intensity;
                        clipboardRubricsEntity.PatientId = item.PatientId;
                        clipboardRubricsEntity.SubSectionId = item.SubSectionId;
                        clipboardRubricsEntity.EnteredBy = item.EnteredBy;
                        clipboardRubricsEntity.EnteredDate = currentDateTime;
                        clipboardRubricsEntity.DeleteStatus = item.DeleteStatus;
                        context.ClipboardRubrics.Add(clipboardRubricsEntity);
                    }
                }
                else
                {
                    var clipboardRubricsEntity = context.ClipboardRubrics.FirstOrDefault(x =>
                        x.ClipboardRubricsId == item.ClipboardRubricsId
                    );
                    if (clipboardRubricsEntity != null)
                    {
                        clipboardRubricsEntity.Intensity = item.Intensity;
                        clipboardRubricsEntity.PatientId = item.PatientId;
                        clipboardRubricsEntity.SubSectionId = item.SubSectionId;
                        clipboardRubricsEntity.ChangedBy = item.EnteredBy;
                        clipboardRubricsEntity.ChangedDate = currentDateTime;
                        clipboardRubricsEntity.DeleteStatus = item.DeleteStatus;
                    }
                }
            }
            context.SaveChanges();
            Message = "Clipboard Rubrics Saved Successfully";
            return Message;
        }

        /// <summary>
        /// Method imlementation for getting all Clipboard Rubrics by patientid
        /// </summary>
        /// <param name="PatientId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<ClipboardRubricsModel> GetClipboardRubricsPatientId(
            int PatientId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var clipboardRubricsModelList = new List<ClipboardRubricsModel>();

            errorResponseModel = new ErrorResponseModel();
            var clipboardRubricsEntityList = context
                .ClipboardRubrics.Where(x => x.DeleteStatus == false && x.PatientId == PatientId)
                .ToList();
            if (clipboardRubricsEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Clipboard Rubrics not found";
            }
            foreach (var item in clipboardRubricsEntityList)
            {
                ClipboardRubricsModel clipboardRubricsModel = new ClipboardRubricsModel();
                var subSectionEntity = context.SubSectionMasters.FirstOrDefault(x =>
                    x.DeleteStatus == false && x.SubSectionId == item.SubSectionId
                );
                clipboardRubricsModel.ClipboardRubricsId = item.ClipboardRubricsId;
                clipboardRubricsModel.Intensity = item.Intensity;
                clipboardRubricsModel.PatientId = item.PatientId;
                clipboardRubricsModel.SubSectionId = item.SubSectionId;
                clipboardRubricsModel.EnteredDate = item.EnteredDate;
                clipboardRubricsModel.EnteredBy = item.EnteredBy;
                clipboardRubricsModel.ChangedBy = item.ChangedBy;
                clipboardRubricsModel.ChangedDate = item.ChangedDate;
                clipboardRubricsModel.DeleteStatus = item.DeleteStatus;
                clipboardRubricsModel.SubSectionName = subSectionEntity.SubSectionName;
                var remedyCount = context
                    .RubricRemedyDetails.Where(x => x.SubSectionId == item.SubSectionId)
                    .Count();
                errorResponseModel = new ErrorResponseModel();
                if (remedyCount == 0)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "Remedy not found";
                }
                clipboardRubricsModel.RemedyCount = remedyCount;
                var remedyEntities = (
                    from remedyDetails in context.RubricRemedyDetails
                    join remedyMaster in context.RemedyMasters
                        on remedyDetails.RemedyId equals remedyMaster.RemedyId
                    join gradeMaster in context.RemedyGradeMaster
                        on remedyDetails.GradeId equals gradeMaster.GradeId
                    where remedyDetails.SubSectionId == item.SubSectionId
                    select new
                    {
                        remedyDetails.RemedyId,
                        remedyMaster.RemedyName,
                        remedyMaster.RemedyAlias,
                        gradeMaster.FontName,
                        gradeMaster.FontStyle,
                        gradeMaster.FontColor,
                        gradeMaster.GradeNo,
                    }
                ).Distinct().ToList();
                if (remedyEntities.Count == 0)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "Remedy not found";
                }
                foreach (var remedyitem in remedyEntities)
                {
                    var remedyModel = new RemedyModel();
                    remedyModel.RemedyId = Convert.ToInt32(remedyitem.RemedyId);
                    remedyModel.RemedyName = remedyitem.RemedyName;
                    remedyModel.RemedyAlias = remedyitem.RemedyAlias;
                    remedyModel.FontName = remedyitem.FontName;
                    remedyModel.FontStyle = remedyitem.FontStyle;
                    remedyModel.FontColor = remedyitem.FontColor;
                    remedyModel.GradeNo = remedyitem.GradeNo;
                    clipboardRubricsModel.remedyModels.Add(remedyModel);
                }

                clipboardRubricsModelList.Add(clipboardRubricsModel);
            }
            return clipboardRubricsModelList;
        }

        /// <summary>
        /// Method is used for delete Clipboard Rubrics.
        /// </summary>
        /// <param name="clipboardRubricsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteClipboardRubrics(
            ClipboardRubricsModel clipboardRubricsModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string Message = "";
            var clipboardRubricsEntity = context.ClipboardRubrics.FirstOrDefault(x =>
                x.ClipboardRubricsId == clipboardRubricsModel.ClipboardRubricsId
            );
            if (clipboardRubricsEntity != null)
            {
                clipboardRubricsEntity.DeleteStatus = clipboardRubricsModel.DeleteStatus;
                clipboardRubricsEntity.ChangedBy = clipboardRubricsModel.EnteredBy;
                clipboardRubricsEntity.ChangedDate = DateTime.Now;
                context.SaveChanges();
                Message = "Clipboard Rubrics Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Method imlementation for getting all ClipboardRubrics by SubsectionId
        /// </summary>
        /// <param name="SubsectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<ClipboardRubricsModel1> GetRubricsDetailsBySubsectionId(
            List<ClipboardRubricsModel1> lstIntensity,
            ref ErrorResponseModel errorResponseModel
        )
        {
            {
                var subsectionIds = lstIntensity.Select(c => c.SubSectionId).ToList(); //SubsectionId.Split(",").ToList();
                //  var intensities = Intensity.Split(",").ToList();
                errorResponseModel = new ErrorResponseModel();
                var clipboardRubricsModelList = new List<ClipboardRubricsModel1>();

                //---- For Subsection and Remedy Details------
                var clipboardRubricsEntity = (
                    from sub in context.SubSectionMasters
                    join rem in context.RubricRemedyDetails
                        on sub.SubSectionId equals rem.SubSectionId
                    join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                    where subsectionIds.Contains((sub.SubSectionId))
                    select new
                    {
                        sub.SubSectionId,
                        sub.SubSectionName,
                        rem.RemedyId,
                        rm.RemedyName,
                    }
                ).OrderBy(rem => rem.RemedyId).ToList();

                if (clipboardRubricsEntity.Count == 0)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "Clipboard Rubrics Not Found";
                }

                //-----for Subsections Counts-----
                int sum = 0;
                clipboardRubricsEntity.ForEach(item =>
                {
                    var subsections = (
                        from sub in context.SubSectionMasters
                        join rem in context.RubricRemedyDetails
                            on sub.SubSectionId equals rem.SubSectionId
                        join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                        where
                            subsectionIds.Contains((sub.SubSectionId))
                            && rem.RemedyId == item.RemedyId
                        select rem.SubSectionId
                    ).ToList();
                    // int subsectioncount = subsections.Count();

                    //---- for Grade Sum -------
                    var gs = (
                        from sub in context.SubSectionMasters
                        join rem in context.RubricRemedyDetails
                            on sub.SubSectionId equals rem.SubSectionId
                        join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                        join grade in context.RemedyGradeMaster on rem.GradeId equals grade.GradeId
                        where
                            subsectionIds.Contains((sub.SubSectionId))
                            && rem.RemedyId == item.RemedyId
                        select (grade.GradeNo)
                    );
                    int gradeSum = (int)gs.Sum();

                    //----for Intensity Sum------
                    var remSubSections = (
                        from rem in clipboardRubricsEntity
                        join sec in lstIntensity on rem.SubSectionId equals sec.SubSectionId
                        where rem.RemedyId == item.RemedyId
                        select (rem.SubSectionId)
                    ).ToList();
                    var lstintensitysum = (
                        from inten in lstIntensity
                        where remSubSections.Contains((int)inten.SubSectionId)
                        select inten.Intensity
                    );
                    int intensitysum = (int)lstintensitysum.Sum();

                    //----for Rubric Counts------
                    var rubrics = (
                        from sub in context.SubSectionMasters
                        join rem in context.RubricRemedyDetails
                            on sub.SubSectionId equals rem.SubSectionId
                        join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                        where
                            subsectionIds.Contains((sub.SubSectionId))
                            && rem.RemedyId == item.RemedyId
                        select rem.SubSectionId
                    ).ToList();
                    int rubriccount = rubrics.Count();

                    //---- for degree Multiplies-------
                    // var gradeNo = (from rem in context.RubricRemedyDetails
                    //join rmaster in context.RemedyGradeMaster
                    //on rem.GradeId equals rmaster.GradeId
                    //where rem.RemedyId == item.RemedyId
                    //&& subsectionIds.Contains((rem.SubSectionId))
                    //select rmaster.GradeNo).FirstOrDefault();

                    var gradeNo = (
                        from rem in context.RubricRemedyDetails
                        join rmaster in context.RemedyGradeMaster
                            on rem.GradeId equals rmaster.GradeId
                        where rem.RemedyId == item.RemedyId && rem.SubSectionId == item.SubSectionId
                        select rmaster.GradeNo
                    ).FirstOrDefault();

                    int GradeNo = (int)gradeNo;
                    sum = 0;
                    subsections.ForEach(item1 =>
                    {
                        var list = (
                            from sub in lstIntensity
                            where sub.SubSectionId == Convert.ToInt32(item1)
                            select sub
                        ).FirstOrDefault();
                        int dm = list.Intensity * GradeNo;
                        sum = sum + dm;
                    });

                    clipboardRubricsModelList.Add(
                        new ClipboardRubricsModel1
                        {
                            SubSectionId = item.SubSectionId,
                            SubSectionName = item.SubSectionName,
                            RemedyId = item.RemedyId,
                            RemedyName = item.RemedyName,
                            //  SubSectionCount = subsectioncount,
                            Rubriccount = rubriccount,
                            GradeSum = gradeSum,
                            intensitysum = intensitysum,
                            DegreeMultiplies = sum,
                        }
                    );
                });

                return clipboardRubricsModelList
                    .GroupBy(x => x.RemedyId)
                    .Select(x => x.FirstOrDefault())
                    .ToList();
            }
        }

        /// <summary>
        /// Method imlementation for getting all ClipboardRubrics by SubsectionId
        /// </summary>
        /// <param name="SubsectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public ClipboardRemedyModel GetCommanUnCommanRubricsDetailsBySubsectionId3(
            List<ClipboardRubricsModel1> lstIntensity,
            ref ErrorResponseModel errorResponseModel
        )
        {
            {
                var subsectionIds = lstIntensity.Select(c => c.SubSectionId).ToList();

                errorResponseModel = new ErrorResponseModel();
                var clipboardRubricsModelList = new List<ClipboardRubricsRemedyModel>();

                //---- For Subsection and Remedy Details------
                var clipboardRubricsEntity = (
                    from subSectionMaster in context.SubSectionMasters
                    join rubricRemedyDetail in context.RubricRemedyDetails
                        on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
                    join remedyMaster in context.RemedyMasters
                        on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
                    where
                        subsectionIds.Contains((subSectionMaster.SubSectionId))
                        && rubricRemedyDetail.DeletedStatus == false
                        && remedyMaster.DeleteStatus == false
                    select new
                    {
                        subSectionMaster.SubSectionId,
                        subSectionMaster.SubSectionName,
                        rubricRemedyDetail.RemedyId,
                        remedyMaster.RemedyName,
                        remedyMaster.ThermalId,
                        remedyMaster.CommonOrUncommon,
                        remedyMaster.ThemesOrCharacteristics,
                        remedyMaster.Particulars,
                        remedyMaster.Generals,
                        remedyMaster.Modalities,
                        score = (
                            from rubricRemedyDetail_ in context.RubricRemedyDetails
                            where
                                rubricRemedyDetail_.RemedyId == rubricRemedyDetail.RemedyId
                                && subsectionIds.Contains((rubricRemedyDetail_.SubSectionId))
                                && rubricRemedyDetail_.DeletedStatus == false
                            select rubricRemedyDetail_
                        ).Count()
                            + "/"
                            + subsectionIds.Count,
                    }
                ).OrderBy(rem => rem.RemedyId).ToList();

                if (clipboardRubricsEntity.Count == 0)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "Clipboard Rubrics Not Found";
                }

                //-----for Subsections Counts-----
                int sum = 0;
                clipboardRubricsEntity.ForEach(item =>
                {
                    var subsections = clipboardRubricsEntity
                        .Where(x => x.RemedyId == item.RemedyId)
                        .Select(x => x.SubSectionId)
                        .ToList();

                    //(from sub in context.SubSectionMaster
                    //                   join rem in context.RubricRemedyDetails on sub.SubSectionId equals rem.SubSectionId
                    //                   join rm in context.RemedyMaster on rem.RemedyId equals rm.RemedyId
                    //                   where subsectionIds.Contains((sub.SubSectionId)) && rem.RemedyId == item.RemedyId
                    //                   select rem.SubSectionId).ToList();
                    // int subsectioncount = subsections.Count();

                    //---- for Grade Sum -------
                    var gs = (
                        from sub in context.SubSectionMasters
                        join rem in context.RubricRemedyDetails
                            on sub.SubSectionId equals rem.SubSectionId
                        join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                        join grade in context.RemedyGradeMaster on rem.GradeId equals grade.GradeId
                        where
                            subsectionIds.Contains((sub.SubSectionId))
                            && rem.RemedyId == item.RemedyId
                        select (grade.GradeNo)
                    );
                    int gradeSum = gs.Sum();

                    //----for Intensity Sum------
                    var remSubSections = (
                        from rem in clipboardRubricsEntity
                        join sec in lstIntensity on rem.SubSectionId equals sec.SubSectionId
                        where rem.RemedyId == item.RemedyId
                        select (rem.SubSectionId)
                    ).ToList();
                    var lstintensitysum = (
                        from inten in lstIntensity
                        where remSubSections.Contains((int)inten.SubSectionId)
                        select inten.Intensity
                    );
                    int intensitysum = lstintensitysum.Sum();

                    //----for Rubric Counts------
                    var rubrics = (
                        from sub in context.SubSectionMasters
                        join rem in context.RubricRemedyDetails
                            on sub.SubSectionId equals rem.SubSectionId
                        join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                        where
                            subsectionIds.Contains((sub.SubSectionId))
                            && rem.RemedyId == item.RemedyId
                        select rem.SubSectionId
                    ).ToList();
                    int rubriccount = rubrics.Count();

                    var gradeNo = (
                        from rem in context.RubricRemedyDetails
                        join rmaster in context.RemedyGradeMaster
                            on rem.GradeId equals rmaster.GradeId
                        where rem.RemedyId == item.RemedyId && rem.SubSectionId == item.SubSectionId
                        select rmaster.GradeNo
                    ).FirstOrDefault();

                    int GradeNo = gradeNo;
                    sum = 0;
                    subsections.ForEach(item1 =>
                    {
                        var list = (
                            from sub in lstIntensity
                            where sub.SubSectionId == Convert.ToInt32(item1)
                            select sub
                        ).FirstOrDefault();
                        int dm = list.Intensity * GradeNo;
                        sum = sum + dm;
                    });

                    clipboardRubricsModelList.Add(
                        new ClipboardRubricsRemedyModel
                        {
                            SubSectionId = item.SubSectionId,
                            SubSectionName = item.SubSectionName,
                            RemedyId = item.RemedyId,
                            RemedyName = item.RemedyName,
                            Generals = item.Generals,
                            Particulars = item.Particulars,
                            Modalities = item.Modalities,
                            ThemesOrCharacteristics = item.ThemesOrCharacteristics,
                            ThermalId = item.ThermalId,
                            CommonUncommon = item.CommonOrUncommon,
                            //  SubSectionCount = subsectioncount,
                            Rubriccount = rubriccount,
                            GradeSum = gradeSum,
                            intensitysum = intensitysum,
                            DegreeMultiplies = sum,
                            score = item.score,
                        }
                    );
                });

                ClipboardRemedyModel clipboardRemedyModel = new ClipboardRemedyModel();
                clipboardRemedyModel.CommonRemedyList = clipboardRubricsModelList
                    .Where(x => x.CommonUncommon == true)
                    .GroupBy(x => x.RemedyId)
                    .Select(x => x.FirstOrDefault())
                    .ToList();
                clipboardRemedyModel.UnCommonRemedyList = clipboardRubricsModelList
                    .Where(x => x.CommonUncommon == false)
                    .GroupBy(x => x.RemedyId)
                    .Select(x => x.FirstOrDefault())
                    .ToList();

                return clipboardRemedyModel;
            }
        }

        public List<RepertorizarionRemedyModel> GetRepertorizarionRemedy(
            RepertorizarionRemedyInputModel inputModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            errorResponseModel = new ErrorResponseModel();
            var repertorizarionRemedyList = (
                from remedyDetails in context.RubricRemedyDetails
                join remedy in context.RemedyMasters
                    on remedyDetails.RemedyId equals remedy.RemedyId
                join subSection in context.SubSectionMasters
                    on remedyDetails.SubSectionId equals subSection.SubSectionId
                join gradeMaster in context.RemedyGradeMaster
                    on remedyDetails.GradeId equals gradeMaster.GradeId
                where
                    remedyDetails.RemedyId == inputModel.RemedyID
                    && remedyDetails.DeletedStatus == false
                    && (
                        inputModel.RequiredType == "SmallRubric"
                            ? remedyDetails.IsSmallRubric == true
                            : remedyDetails.IsConfirmationRubric == true
                    )
                select new RepertorizarionRemedyModel
                {
                    RubricRemedyId = remedyDetails.RubricRemedyId,
                    SectionId = subSection.SectionId,
                    SubSectionId = subSection.SubSectionId,
                    SubSectionName = subSection.SubSectionName,
                    GradeId = gradeMaster.GradeId,
                    FontName = gradeMaster.FontName,
                    FontColor = gradeMaster.FontColor,
                    FontStyle = gradeMaster.FontStyle,
                }
            ).OrderBy(x => x.SectionId).ToList();

            if (repertorizarionRemedyList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy Not Found";
            }
            return repertorizarionRemedyList;
        }

        public ClipboardRemedyModel GetCommanUnCommanRubricsDetailsByElemation(
            ClipboardRUbricModel clipboardRUbricModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var eliminateSubsectionId = 0;
            if (clipboardRUbricModel.WithEliminateRubric.Count == 1)
            {
                eliminateSubsectionId = clipboardRUbricModel
                    .WithEliminateRubric.Select(x => x.SubSectionId)
                    .FirstOrDefault();
            }
            else
            {
                eliminateSubsectionId = clipboardRUbricModel
                    .WithEliminateRubric.OrderBy(x => x.Rubriccount)
                    .Select(x => x.SubSectionId)
                    .FirstOrDefault();
            }

            var subsectionIds = clipboardRUbricModel
                .WithoutEliminateRubric.Select(c => c.SubSectionId)
                .ToList();

            errorResponseModel = new ErrorResponseModel();
            var clipboardRubricsModelList = new List<ClipboardRubricsRemedyModel>();

            //---- For Subsection and Remedy Details------
            var clipboardRubricsEntity = (
                from subSectionMaster in context.SubSectionMasters
                join rubricRemedyDetail in context.RubricRemedyDetails
                    on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
                join remedyMaster in context.RemedyMasters
                    on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
                where
                    subSectionMaster.SubSectionId == eliminateSubsectionId
                    && rubricRemedyDetail.DeletedStatus == false
                select new
                {
                    subSectionMaster.SubSectionId,
                    subSectionMaster.SubSectionName,
                    rubricRemedyDetail.RemedyId,
                    remedyMaster.RemedyName,
                    remedyMaster.ThermalId,
                    remedyMaster.CommonOrUncommon,
                    remedyMaster.ThemesOrCharacteristics,
                    remedyMaster.Particulars,
                    remedyMaster.Generals,
                    remedyMaster.Modalities,
                    score = (
                        from rubricRemedyDetail_ in context.RubricRemedyDetails
                        where
                            rubricRemedyDetail_.RemedyId == rubricRemedyDetail.RemedyId
                            && subsectionIds.Contains((rubricRemedyDetail_.SubSectionId))
                            && rubricRemedyDetail_.DeletedStatus == false
                        select rubricRemedyDetail_
                    ).Count()
                        + "/"
                        + subsectionIds.Count,
                }
            ).OrderBy(rem => rem.RemedyId).ToList();

            if (clipboardRubricsEntity.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Clipboard Rubrics Not Found";
            }

            //-----for Subsections Counts-----
            int sum = 0;
            clipboardRubricsEntity.ForEach(item =>
            {
                var subsections = clipboardRubricsEntity
                    .Where(x => x.RemedyId == item.RemedyId)
                    .Select(x => x.SubSectionId)
                    .ToList();

                //(from sub in context.SubSectionMaster
                //                   join rem in context.RubricRemedyDetails on sub.SubSectionId equals rem.SubSectionId
                //                   join rm in context.RemedyMaster on rem.RemedyId equals rm.RemedyId
                //                   where subsectionIds.Contains((sub.SubSectionId)) && rem.RemedyId == item.RemedyId
                //                   select rem.SubSectionId).ToList();
                // int subsectioncount = subsections.Count();

                //---- for Grade Sum -------
                var gs = (
                    from sub in context.SubSectionMasters
                    join rem in context.RubricRemedyDetails
                        on sub.SubSectionId equals rem.SubSectionId
                    join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                    join grade in context.RemedyGradeMaster on rem.GradeId equals grade.GradeId
                    where sub.SubSectionId == eliminateSubsectionId && rem.RemedyId == item.RemedyId
                    select (grade.GradeNo)
                );
                int gradeSum = gs.Sum();

                //----for Intensity Sum------
                var remSubSections = (
                    from rem in clipboardRubricsEntity
                    join sec in clipboardRUbricModel.WithoutEliminateRubric
                        on rem.SubSectionId equals sec.SubSectionId
                    where rem.RemedyId == item.RemedyId
                    select (rem.SubSectionId)
                ).ToList();
                var lstintensitysum = (
                    from inten in clipboardRUbricModel.WithoutEliminateRubric
                    where remSubSections.Contains((int)inten.SubSectionId)
                    select inten.Intensity
                );
                int intensitysum = lstintensitysum.Sum();

                //----for Rubric Counts------
                var rubrics = (
                    from sub in context.SubSectionMasters
                    join rem in context.RubricRemedyDetails
                        on sub.SubSectionId equals rem.SubSectionId
                    join rm in context.RemedyMasters on rem.RemedyId equals rm.RemedyId
                    where sub.SubSectionId == eliminateSubsectionId && rem.RemedyId == item.RemedyId
                    select rem.SubSectionId
                ).ToList();
                int rubriccount = rubrics.Count();

                var gradeNo = (
                    from rem in context.RubricRemedyDetails
                    join rmaster in context.RemedyGradeMaster on rem.GradeId equals rmaster.GradeId
                    where rem.RemedyId == item.RemedyId && rem.SubSectionId == item.SubSectionId
                    select rmaster.GradeNo
                ).FirstOrDefault();

                int GradeNo = gradeNo;
                sum = 0;
                subsections.ForEach(item1 =>
                {
                    var list = (
                        from sub in clipboardRUbricModel.WithoutEliminateRubric
                        where sub.SubSectionId == Convert.ToInt32(item1)
                        select sub
                    ).FirstOrDefault();
                    int dm = list.Intensity * GradeNo;
                    sum = sum + dm;
                });

                clipboardRubricsModelList.Add(
                    new ClipboardRubricsRemedyModel
                    {
                        SubSectionId = item.SubSectionId,
                        SubSectionName = item.SubSectionName,
                        RemedyId = item.RemedyId,
                        RemedyName = item.RemedyName,
                        Generals = item.Generals,
                        Particulars = item.Particulars,
                        Modalities = item.Modalities,
                        ThemesOrCharacteristics = item.ThemesOrCharacteristics,
                        ThermalId = item.ThermalId,
                        CommonUncommon = item.CommonOrUncommon,
                        //  SubSectionCount = subsectioncount,
                        Rubriccount = rubriccount,
                        GradeSum = gradeSum,
                        intensitysum = intensitysum,
                        DegreeMultiplies = sum,
                        score = item.score,
                    }
                );
            });

            ClipboardRemedyModel clipboardRemedyModel = new ClipboardRemedyModel();
            clipboardRemedyModel.CommonRemedyList = clipboardRubricsModelList
                .Where(x => x.CommonUncommon == true)
                .GroupBy(x => x.RemedyId)
                .Select(x => x.FirstOrDefault())
                .ToList();
            clipboardRemedyModel.UnCommonRemedyList = clipboardRubricsModelList
                .Where(x => x.CommonUncommon == false)
                .GroupBy(x => x.RemedyId)
                .Select(x => x.FirstOrDefault())
                .ToList();

            return clipboardRemedyModel;
        }

        public ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionId2(
            List<ClipboardRubricsModel1> lstIntensity,
            ref ErrorResponseModel errorResponseModel
        )
        {
            {
                var subsectionIds = lstIntensity.Select(c => c.SubSectionId).ToList();

                errorResponseModel = new ErrorResponseModel();

                List<Object> obj = new List<object>();
                List<RemedyArrayModel> objRemedyList = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListCount = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListGrade = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListIntensity = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListFinal = new List<RemedyArrayModel>();

                List<SortedRemedyArrayModel> sortedRemedyList = new List<SortedRemedyArrayModel>();
                ClipboardRemedyNewModel clipboardRemedyModel = new ClipboardRemedyNewModel();
                foreach (var item in lstIntensity)
                {
                    objRemedyListCount = new List<RemedyArrayModel>();
                    objRemedyListGrade = new List<RemedyArrayModel>();
                    objRemedyListIntensity = new List<RemedyArrayModel>();
                    objRemedyListFinal = new List<RemedyArrayModel>();

                    var selectedRemedy = (
                        from subSectionMaster in context.SubSectionMasters
                        join rubricRemedyDetail in context.RubricRemedyDetails
                            on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
                        join remedyMaster in context.RemedyMasters
                            on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
                        join rgm in context.RemedyGradeMaster
                            on rubricRemedyDetail.GradeId equals rgm.GradeId
                        where
                            subSectionMaster.SubSectionId == item.SubSectionId
                            && rubricRemedyDetail.DeletedStatus == false
                            && remedyMaster.DeleteStatus == false
                        select new
                        {
                            subSectionMaster.SubSectionId,
                            subSectionMaster.SubSectionName,
                            rubricRemedyDetail.RemedyId,
                            remedyMaster.RemedyName,
                            remedyMaster.RemedyAlias,
                            rgm.GradeNo,
                            rgm.FontName,
                            rgm.FontStyle,
                            rgm.FontColor,
                            item.Intensity,
                            total = (item.Intensity * rgm.GradeNo),
                            remedyMaster.ThermalId,
                            remedyMaster.CommonOrUncommon,
                            remedyMaster.ThemesOrCharacteristics,
                            remedyMaster.Particulars,
                            remedyMaster.Generals,
                            remedyMaster.Modalities,
                            score = (
                                from rubricRemedyDetail_ in context.RubricRemedyDetails
                                where
                                    rubricRemedyDetail_.RemedyId == rubricRemedyDetail.RemedyId
                                    && subsectionIds.Contains((rubricRemedyDetail_.SubSectionId))
                                    && rubricRemedyDetail_.DeletedStatus == false
                                select rubricRemedyDetail_
                            )
                                .GroupBy(x => x.SubSectionId)
                                .Count(),
                        }
                    ).OrderBy(rem => rem.RemedyId).ToList();

                    foreach (var item1 in selectedRemedy)
                    {
                        objRemedyList.Add(
                            new RemedyArrayModel
                            {
                                RemedyId = item1.RemedyId,
                                RemedyName = item1.RemedyName,
                                RemedyAlies = item1.RemedyAlias,
                                Intensity = selectedRemedy
                                    .Where(x => x.RemedyName == item1.RemedyName)
                                    .Sum(x => x.Intensity),
                                Count = selectedRemedy
                                    .Where(x => x.RemedyName == item1.RemedyName)
                                    .Count(),
                                Grade = selectedRemedy
                                    .Where(x => x.RemedyName == item1.RemedyName)
                                    .Sum(x => x.GradeNo),
                                final = selectedRemedy
                                    .Where(x => x.RemedyName == item1.RemedyName)
                                    .Sum(x => x.total),
                                Generals = item1.Generals,
                                Particulars = item1.Particulars,
                                Modalities = item1.Modalities,
                                ThemesOrCharacteristics = item1.ThemesOrCharacteristics,
                                ThermalId = item1.ThermalId,
                                CommonUncommon = item1.CommonOrUncommon,
                                score = item1.score + "/" + subsectionIds.Count,
                                scoreCount = item1.score,
                                PresentSubSection = (
                                    from rubricRemedyDetail_ in context.RubricRemedyDetails
                                    where
                                        rubricRemedyDetail_.RemedyId == item1.RemedyId
                                        && subsectionIds.Contains(
                                            (rubricRemedyDetail_.SubSectionId)
                                        )
                                        && rubricRemedyDetail_.DeletedStatus == false
                                    select rubricRemedyDetail_.SubSectionId
                                )
                                    .Distinct()
                                    .ToList(),
                            }
                        );
                    }

                    objRemedyListCount = objRemedyList.OrderByDescending(x => x.Count).ToList();
                    objRemedyListGrade = objRemedyListCount
                        .OrderByDescending(x => x.Grade)
                        .ToList();
                    objRemedyListIntensity = objRemedyListGrade
                        .OrderByDescending(x => x.Intensity)
                        .ToList();
                    objRemedyListFinal = objRemedyListIntensity
                        .OrderByDescending(x => x.final)
                        .ToList();

                    foreach (var remedyItem in objRemedyList)
                    {
                        int maxIndex =
                            objRemedyListCount.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                            + 1
                            + objRemedyListGrade.FindIndex(x =>
                                x.RemedyName == remedyItem.RemedyName
                            )
                            + 1
                            + objRemedyListIntensity.FindIndex(x =>
                                x.RemedyName == remedyItem.RemedyName
                            )
                            + 1
                            + objRemedyListFinal.FindIndex(x =>
                                x.RemedyName == remedyItem.RemedyName
                            )
                            + 1;

                        SortedRemedyArrayModel sortedRemedy = new SortedRemedyArrayModel();
                        sortedRemedy.RemedyId = remedyItem.RemedyId;
                        sortedRemedy.RemedyName = remedyItem.RemedyName;
                        sortedRemedy.RemedyAlies = remedyItem.RemedyAlies;
                        sortedRemedy.Intensity = remedyItem.Intensity;
                        sortedRemedy.Count = remedyItem.Count;
                        sortedRemedy.Grade = remedyItem.Grade;
                        sortedRemedy.final = remedyItem.final;
                        ////sortedRemedy.Generals = remedyItem.Generals;
                        ////sortedRemedy.Particulars = remedyItem.Particulars;
                        ////sortedRemedy.Modalities = remedyItem.Modalities;
                        ////sortedRemedy.ThemesOrCharacteristics = remedyItem.ThemesOrCharacteristics;
                        ////sortedRemedy.ThermalId = remedyItem.ThermalId;
                        sortedRemedy.CommonUncommon = remedyItem.CommonUncommon;
                        ////sortedRemedy.score = remedyItem.score;
                        sortedRemedy.scoreCount = remedyItem.scoreCount;
                        sortedRemedy.MaxIndex = maxIndex;
                        sortedRemedy.PresentSubSection = remedyItem.PresentSubSection;
                        sortedRemedy.progressBar = (1000 - maxIndex) / 10;
                        sortedRemedyList.Add(sortedRemedy);
                    }
                }
                clipboardRemedyModel.CommonRemedyList = sortedRemedyList
                    .Where(x => x.CommonUncommon == true)
                    .GroupBy(x => x.RemedyId)
                    .Select(x => x.FirstOrDefault())
                    .OrderByDescending(x => x.scoreCount)
                    .ToList();
                clipboardRemedyModel.UnCommonRemedyList = sortedRemedyList
                    .Where(x => x.CommonUncommon == false)
                    .GroupBy(x => x.RemedyId)
                    .Select(x => x.FirstOrDefault())
                    .OrderByDescending(x => x.scoreCount)
                    .ToList();

                return clipboardRemedyModel;
            }
        }

        public ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionId1(
            ClipboardRUbricModel clipboardRUbricModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            //var eliminateSubsectionId = 0;
            //var eliminateIntensity = 0;
            //if (clipboardRUbricModel.WithEliminateRubric.Count == 1)
            //{
            //    eliminateSubsectionId = clipboardRUbricModel.WithEliminateRubric.Select(x => x.SubSectionId).FirstOrDefault();
            //    eliminateIntensity = clipboardRUbricModel.WithEliminateRubric.Select(x => x.Intensity).FirstOrDefault();
            //}
            //else
            //{
            //    eliminateSubsectionId = clipboardRUbricModel.WithEliminateRubric.OrderBy(x => x.Rubriccount).Select(x => x.SubSectionId).FirstOrDefault();
            //    eliminateIntensity = clipboardRUbricModel.WithEliminateRubric.OrderBy(x => x.Rubriccount).Select(x => x.Intensity).FirstOrDefault();
            //}

            //var subsectionIds = clipboardRUbricModel.WithEliminateRubric.Select(c => c.SubSectionId).ToList();

            //var selectedSmallCountRemedy = (from subSectionMaster in context.SubSectionMaster
            //                                join rubricRemedyDetail in context.RubricRemedyDetails on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
            //                                join remedyMaster in context.RemedyMaster on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
            //                                join rgm in context.RemedyGradeMaster on rubricRemedyDetail.GradeId equals rgm.GradeId
            //                                where subSectionMaster.SubSectionId == eliminateSubsectionId && rubricRemedyDetail.DeletedStatus == false
            //                                select new
            //                                {

            //                                    rubricRemedyDetail.RemedyId,
            //                                    //RemedyName= remedyMaster.RemedyName,
            //                                    //RemedyAlias= remedyMaster.RemedyAlias,

            //                                }).OrderBy(rem => rem.RemedyId).ToList();

            //var OtherList = new List<List<int>>();
            //foreach (var item in clipboardRUbricModel.WithEliminateRubric)
            //{

            //        var selectedOtherRemedy = (from subSectionMaster in context.SubSectionMaster
            //                                   join rubricRemedyDetail in context.RubricRemedyDetails on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
            //                                   join remedyMaster in context.RemedyMaster on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
            //                                   join rgm in context.RemedyGradeMaster on rubricRemedyDetail.GradeId equals rgm.GradeId
            //                                   where subSectionMaster.SubSectionId == item.SubSectionId && rubricRemedyDetail.DeletedStatus == false
            //                                   select new
            //                                   {
            //                                       rubricRemedyDetail.RemedyId,
            //                                       //RemedyName = remedyMaster.RemedyName,
            //                                       //RemedyAlias = remedyMaster.RemedyAlias,

            //                                   }).OrderBy(rem => rem.RemedyId).ToList();

            //        OtherList.Add(selectedOtherRemedy.Select(x => x.RemedyId).ToList());

            //}

            ////List<int?> selectedRemedyList = selectedSmallCountRemedy.Select(x => x.RemedyId).ToList();

            ////List<int?> commonItems = new List<int?>();

            ////foreach (var item in OtherList)
            ////{
            ////    var common = selectedRemedyList.Intersect(item);
            ////    if (common.ToList().Count > 0)
            ////    {
            ////        commonItems.AddRange(common);
            ////    }
            ////}

            //var remedyIds = commonItems.Distinct().ToList();

            //string.Join(", ", OtherList.ToList().Aggregate((x, y) => x.Intersect(y)))

            ////var subsectionIds = lstIntensity.Select(c => c.SubSectionId).ToList();

            //errorResponseModel = new ErrorResponseModel();

            //List<Object> obj = new List<object>();
            //List<RemedyArrayModel> objRemedyList = new List<RemedyArrayModel>();
            //List<RemedyArrayModel> objRemedyListCount = new List<RemedyArrayModel>();
            //List<RemedyArrayModel> objRemedyListGrade = new List<RemedyArrayModel>();
            //List<RemedyArrayModel> objRemedyListIntensity = new List<RemedyArrayModel>();
            //List<RemedyArrayModel> objRemedyListFinal = new List<RemedyArrayModel>();

            //List<SortedRemedyArrayModel> sortedRemedyList = new List<SortedRemedyArrayModel>();
            //List<SortedRemedyArrayModel> sortedRemedyBYELIList = new List<SortedRemedyArrayModel>();
            //ClipboardRemedyNewModel clipboardRemedyModel = new ClipboardRemedyNewModel();
            //foreach (var item in clipboardRUbricModel.WithEliminateRubric)
            //{
            //    objRemedyListCount = new List<RemedyArrayModel>();
            //    objRemedyListGrade = new List<RemedyArrayModel>();
            //    objRemedyListIntensity = new List<RemedyArrayModel>();
            //    objRemedyListFinal = new List<RemedyArrayModel>();

            //    var selectedRemedy = (from subSectionMaster in context.SubSectionMaster
            //                          join rubricRemedyDetail in context.RubricRemedyDetails on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
            //                          join remedyMaster in context.RemedyMaster on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
            //                          join rgm in context.RemedyGradeMaster on rubricRemedyDetail.GradeId equals rgm.GradeId
            //                          where subSectionMaster.SubSectionId == item.SubSectionId && rubricRemedyDetail.DeletedStatus == false
            //                          select new
            //                          {
            //                              rubricRemedyDetail.RemedyId,
            //                              remedyMaster.RemedyName,
            //                              remedyMaster.RemedyAlias,
            //                              rgm.GradeNo,
            //                              rgm.FontName,
            //                              rgm.FontStyle,
            //                              rgm.FontColor,
            //                              eliminateIntensity,
            //                              total = (eliminateIntensity * rgm.GradeNo),
            //                              remedyMaster.ThermalId,
            //                              remedyMaster.CommonOrUncommon,
            //                              remedyMaster.ThemesOrCharacteristics,
            //                              remedyMaster.Particulars,
            //                              remedyMaster.Generals,
            //                              remedyMaster.Modalities,
            //                              score = (from rubricRemedyDetail_ in context.RubricRemedyDetails
            //                                       where
            //                                     rubricRemedyDetail_.RemedyId == rubricRemedyDetail.RemedyId &&
            //                                   subsectionIds.Contains(Convert.ToInt32(rubricRemedyDetail_.SubSectionId)) &&
            //                                     rubricRemedyDetail_.DeletedStatus == false
            //                                       select rubricRemedyDetail_).GroupBy(x => x.SubSectionId).Count(),

            //                          }).OrderBy(rem => rem.RemedyId).ToList();

            //    foreach (var item1 in selectedRemedy)
            //    {
            //        objRemedyList.Add(new RemedyArrayModel
            //        {
            //            RemedyId = item1.RemedyId,
            //            RemedyName = item1.RemedyName,
            //            RemedyAlies = item1.RemedyAlias,
            //            Intensity = selectedRemedy.Where(x => x.RemedyName == item1.RemedyName).Sum(x => x.eliminateIntensity),
            //            Count = selectedRemedy.Where(x => x.RemedyName == item1.RemedyName).Count(),
            //            Grade = selectedRemedy.Where(x => x.RemedyName == item1.RemedyName).Sum(x => x.GradeNo),
            //            final = selectedRemedy.Where(x => x.RemedyName == item1.RemedyName).Sum(x => x.total),
            //            Generals = item1.Generals,
            //            Particulars = item1.Particulars,
            //            Modalities = item1.Modalities,
            //            ThemesOrCharacteristics = item1.ThemesOrCharacteristics,
            //            ThermalId = item1.ThermalId,
            //            CommonUncommon = item1.CommonOrUncommon,
            //            score = item1.score + "/" + clipboardRUbricModel.WithEliminateRubric.Count,
            //            scoreCount = item1.score

            //        });
            //    }

            //    objRemedyListCount = objRemedyList.OrderByDescending(x => x.Count).ToList();
            //    objRemedyListGrade = objRemedyList.OrderByDescending(x => x.Grade).ToList();
            //    objRemedyListIntensity = objRemedyList.OrderByDescending(x => x.Intensity).ToList();
            //    objRemedyListFinal = objRemedyList.OrderByDescending(x => x.final).ToList();

            //    foreach (var remedyItem in objRemedyList)
            //    {
            //        int maxIndex = objRemedyListCount.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1 +
            //            objRemedyListGrade.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1 +
            //            objRemedyListIntensity.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1 +
            //            objRemedyListFinal.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1;

            //        SortedRemedyArrayModel sortedRemedy = new SortedRemedyArrayModel();
            //        sortedRemedy.RemedyId = remedyItem.RemedyId;
            //        sortedRemedy.RemedyName = remedyItem.RemedyName;
            //        sortedRemedy.RemedyAlies = remedyItem.RemedyAlies;
            //        sortedRemedy.Intensity = remedyItem.Intensity;
            //        sortedRemedy.Count = remedyItem.Count;
            //        sortedRemedy.Grade = remedyItem.Grade;
            //        sortedRemedy.final = remedyItem.final;
            //        sortedRemedy.Generals = remedyItem.Generals;
            //        sortedRemedy.Particulars = remedyItem.Particulars;
            //        sortedRemedy.Modalities = remedyItem.Modalities;
            //        sortedRemedy.ThemesOrCharacteristics = remedyItem.ThemesOrCharacteristics;
            //        sortedRemedy.ThermalId = remedyItem.ThermalId;
            //        sortedRemedy.CommonUncommon = remedyItem.CommonUncommon;
            //        sortedRemedy.score = remedyItem.score;
            //        sortedRemedy.scoreCount = remedyItem.scoreCount;
            //        sortedRemedy.MaxIndex = maxIndex;
            //        sortedRemedy.progressBar = (1000 - maxIndex) / 10;
            //        sortedRemedyList.Add(sortedRemedy);
            //    }

            //}
            //sortedRemedyBYELIList = (from sortedList in sortedRemedyList
            //                         where remedyIds.Contains(sortedList.RemedyId)
            //                         select sortedList).ToList();

            //clipboardRemedyModel.CommonRemedyList = sortedRemedyBYELIList.Where(x => x.CommonUncommon == true).GroupBy(x => x.RemedyId).Select(x => x.FirstOrDefault()).OrderBy(x => x.MaxIndex).ToList();
            //clipboardRemedyModel.UnCommonRemedyList = sortedRemedyBYELIList.Where(x => x.CommonUncommon == false).GroupBy(x => x.RemedyId).Select(x => x.FirstOrDefault()).OrderBy(x => x.MaxIndex).ToList();
            return null;
        }

        public ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionId2911(
            List<ClipboardRubricsModel1> lstIntensity,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var subsectionIds = lstIntensity.Select(c => c.SubSectionId).ToList();

            List<ClipboardRubricsRemedyInput> clipboardRubricsRemedyInputs =
                new List<ClipboardRubricsRemedyInput>();
            foreach (var item in lstIntensity)
            {
                ClipboardRubricsRemedyInput data = new ClipboardRubricsRemedyInput();
                data.SubsectionID = (int)item.SubSectionId;
                data.Intensity = item.Intensity;
                data.RemedyCount = context
                    .RubricRemedyDetails.Where(x =>
                        (x.SubSectionId == item.SubSectionId && x.DeletedStatus == false)
                    )
                    .ToList()
                    .Count();
                clipboardRubricsRemedyInputs.Add(data);
            }

            ClipboardRubricsRemedyInput smallRubricItem = clipboardRubricsRemedyInputs
                .OrderBy(x => x.RemedyCount)
                .FirstOrDefault();

            errorResponseModel = new ErrorResponseModel();

            List<Object> obj = new List<object>();
            List<RemedyArrayModel> objRemedyList = new List<RemedyArrayModel>();
            List<RemedyArrayModel> objRemedyListCount = new List<RemedyArrayModel>();
            List<RemedyArrayModel> objRemedyListGrade = new List<RemedyArrayModel>();
            List<RemedyArrayModel> objRemedyListIntensity = new List<RemedyArrayModel>();
            List<RemedyArrayModel> objRemedyListFinal = new List<RemedyArrayModel>();

            List<SortedRemedyArrayModel> sortedRemedyList = new List<SortedRemedyArrayModel>();
            ClipboardRemedyNewModel clipboardRemedyModel = new ClipboardRemedyNewModel();

            List<ClipboardRubricsRemedyViewModel> remedyArrayList =
                new List<ClipboardRubricsRemedyViewModel>();
            foreach (var item in clipboardRubricsRemedyInputs)
            {
                var selectedRemedy = (
                    from subSectionMaster in context.SubSectionMasters
                    join rubricRemedyDetail in context.RubricRemedyDetails
                        on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
                    join remedyMaster in context.RemedyMasters
                        on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
                    join rgm in context.RemedyGradeMaster
                        on rubricRemedyDetail.GradeId equals rgm.GradeId
                    where
                        subSectionMaster.SubSectionId == item.SubsectionID
                        && rubricRemedyDetail.DeletedStatus == false
                        && remedyMaster.DeleteStatus == false
                    select new ClipboardRubricsRemedyViewModel
                    {
                        SubSectionId = subSectionMaster.SubSectionId,
                        SubSectionName = subSectionMaster.SubSectionName,
                        RemedyId = rubricRemedyDetail.RemedyId,
                        RemedyName = remedyMaster.RemedyName,
                        RemedyAlias = remedyMaster.RemedyAlias,
                        GradeNo = rgm.GradeNo,
                        FontName = rgm.FontName,
                        FontStyle = rgm.FontStyle,
                        FontColor = rgm.FontColor,
                        Intensity = item.Intensity,
                        total = (item.Intensity * rgm.GradeNo),
                        ThermalId = remedyMaster.ThermalId,
                        CommonOrUncommon = remedyMaster.CommonOrUncommon,
                        ThemesOrCharacteristics = remedyMaster.ThemesOrCharacteristics,
                        Particulars = remedyMaster.Particulars,
                        Generals = remedyMaster.Generals,
                        Modalities = remedyMaster.Modalities,
                        score = (
                            from rubricRemedyDetail_ in context.RubricRemedyDetails
                            where
                                rubricRemedyDetail_.RemedyId == rubricRemedyDetail.RemedyId
                                && subsectionIds.Contains((rubricRemedyDetail_.SubSectionId))
                                && rubricRemedyDetail_.DeletedStatus == false
                            select rubricRemedyDetail_
                        )
                            .GroupBy(x => x.SubSectionId)
                            .Count(),
                        SmallRubric = item.SubsectionID == smallRubricItem.SubsectionID ? 1 : 0,
                    }
                ).OrderBy(rem => rem.RemedyId).ToList();
                remedyArrayList.AddRange(selectedRemedy);
            }

            foreach (var item1 in remedyArrayList)
            {
                objRemedyList.Add(
                    new RemedyArrayModel
                    {
                        RemedyId = item1.RemedyId,
                        RemedyName = item1.RemedyName,
                        RemedyAlies = item1.RemedyAlias,
                        Intensity = item1.Intensity,
                        IntensitySum = remedyArrayList
                            .Where(x => x.RemedyName == item1.RemedyName)
                            .Sum(x => x.Intensity),
                        Count = remedyArrayList
                            .Where(x => x.RemedyName == item1.RemedyName)
                            .Count(),
                        Grade = remedyArrayList
                            .Where(x => x.RemedyName == item1.RemedyName)
                            .Sum(x => x.GradeNo),
                        final = remedyArrayList
                            .Where(x => x.RemedyName == item1.RemedyName)
                            .Sum(x => x.total),
                        Generals = item1.Generals,
                        Particulars = item1.Particulars,
                        Modalities = item1.Modalities,
                        ThemesOrCharacteristics = item1.ThemesOrCharacteristics,
                        ThermalId = item1.ThermalId,
                        CommonUncommon = item1.CommonOrUncommon,
                        score = item1.score + "/" + subsectionIds.Count,
                        scoreCount = item1.score,
                        PresentSubSection = (
                            from rubricRemedyDetail_ in context.RubricRemedyDetails
                            where
                                rubricRemedyDetail_.RemedyId == item1.RemedyId
                                && subsectionIds.Contains((rubricRemedyDetail_.SubSectionId))
                                && rubricRemedyDetail_.DeletedStatus == false
                            select rubricRemedyDetail_.SubSectionId
                        )
                            .Distinct()
                            .ToList(),

                        SmallRubric = item1.SmallRubric,
                    }
                );
            }

            objRemedyListCount = objRemedyList.OrderByDescending(x => x.Count).ToList();
            objRemedyListGrade = objRemedyList.OrderByDescending(x => x.Grade).ToList();
            objRemedyListIntensity = objRemedyList.OrderByDescending(x => x.IntensitySum).ToList();
            objRemedyListFinal = objRemedyList.OrderByDescending(x => x.final).ToList();

            foreach (var remedyItem in objRemedyList)
            {
                int maxIndex =
                    objRemedyListCount.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                    + 1
                    + objRemedyListGrade.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                    + 1
                    + objRemedyListIntensity.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                    + 1
                    + objRemedyListFinal.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                    + 1;

                SortedRemedyArrayModel sortedRemedy = new SortedRemedyArrayModel();
                sortedRemedy.RemedyId = remedyItem.RemedyId;
                sortedRemedy.RemedyName = remedyItem.RemedyName;
                sortedRemedy.RemedyAlies = remedyItem.RemedyAlies;
                sortedRemedy.Intensity = remedyItem.Intensity;
                sortedRemedy.IntensitySum = remedyItem.IntensitySum;
                sortedRemedy.Count = remedyItem.Count;
                sortedRemedy.Grade = remedyItem.Grade;
                sortedRemedy.final = remedyItem.final;
                sortedRemedy.Generals = remedyItem.Generals;
                sortedRemedy.Particulars = remedyItem.Particulars;
                sortedRemedy.Modalities = remedyItem.Modalities;
                sortedRemedy.ThemesOrCharacteristics = remedyItem.ThemesOrCharacteristics;
                sortedRemedy.ThermalId = remedyItem.ThermalId;
                sortedRemedy.CommonUncommon = remedyItem.CommonUncommon;
                sortedRemedy.score = remedyItem.score;
                sortedRemedy.scoreCount = remedyItem.scoreCount;
                sortedRemedy.MaxIndex = maxIndex;
                sortedRemedy.PresentSubSection = remedyItem.PresentSubSection;
                sortedRemedy.SmallRubric = remedyItem.SmallRubric;
                sortedRemedy.progressBar = (1000 - maxIndex) / 10;
                sortedRemedyList.Add(sortedRemedy);
            }

            var result2 = sortedRemedyList
                .OrderByDescending(x => x.final)
                .ThenByDescending(x => x.SmallRubric)
                .ToList();
            var result = result2.GroupBy(x => x.RemedyId).Select(x => x.FirstOrDefault()).ToList();

            clipboardRemedyModel.CommonRemedyList = sortedRemedyList
                .Where(x => x.CommonUncommon == true)
                .GroupBy(x => x.RemedyId)
                .Select(x => x.FirstOrDefault())
                .OrderByDescending(x => x.scoreCount)
                .ToList();
            clipboardRemedyModel.UnCommonRemedyList = sortedRemedyList
                .Where(x => x.CommonUncommon == false)
                .GroupBy(x => x.RemedyId)
                .Select(x => x.FirstOrDefault())
                .OrderByDescending(x => x.scoreCount)
                .ToList();

            return clipboardRemedyModel;
        }

        /// <summary>
        /// Method imlementation for getting all ClipboardRubrics by SubsectionId
        /// </summary>
        /// <param name="SubsectionId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public ClipboardRemedyModel GetCommanUnCommanRubricsDetailsBySubsectionId(
            List<ClipboardRubricsModel1> lstIntensity,
            ref ErrorResponseModel errorResponseModel
        )
        {
            {
                var subsectionIds = lstIntensity.Select(c => c.SubSectionId).ToList();

                errorResponseModel = new ErrorResponseModel();
                var clipboardRubricsModelList = new List<ClipboardRubricsRemedyDataViewModelW>();

                var rawData = (
                    from rubricRemedyDetail in context.RubricRemedyDetails
                    join subSectionMaster in context.SubSectionMasters
                        on rubricRemedyDetail.SubSectionId equals subSectionMaster.SubSectionId
                    join remedyMaster in context.RemedyMasters
                        on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
                    join remedyGrade in context.RemedyGradeMaster
                        on rubricRemedyDetail.GradeId equals remedyGrade.GradeId
                    where
                        subsectionIds.Contains((subSectionMaster.SubSectionId))
                        && rubricRemedyDetail.DeletedStatus == false
                        && remedyMaster.DeleteStatus == false
                    select new ClipboardRubricsRemedyDataViewModel
                    {
                        SubSectionId = subSectionMaster.SubSectionId,
                        SubSectionName = subSectionMaster.SubSectionName,
                        RemedyId = remedyMaster.RemedyId,
                        RemedyName = remedyMaster.RemedyName,
                        RemedyAlias = remedyMaster.RemedyAlias,
                        GradeNo = remedyGrade.GradeNo,
                        SelectedIntensity = 0,
                        RubricCount = 0,
                        DegreesSum = 0,
                        SumRubricDegree = 0,
                        MultIntensityDegree = 0,
                        MultIntensityDegreeAddIntensity = 0,
                        intensityMatch = 0,
                        MaxIndex = 0,
                        SmallRubricCount = 0,
                    }
                ).OrderBy(rem => rem.RemedyId).ToList();

                if (rawData.Count == 0)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "Clipboard Rubrics Not Found";
                }

                List<ClipboardRubricsRemedyInput> rubricRemedyCountList =
                    new List<ClipboardRubricsRemedyInput>();

                foreach (var item in lstIntensity)
                {
                    ClipboardRubricsRemedyInput info = new ClipboardRubricsRemedyInput();
                    int smallRubricCount = (
                        from rubricRemedyDetail in context.RubricRemedyDetails
                        where
                            rubricRemedyDetail.SubSectionId == item.SubSectionId
                            && rubricRemedyDetail.DeletedStatus == false
                        select rubricRemedyDetail
                    )
                        .ToList()
                        .Count();
                    info.SubsectionID = (int)item.SubSectionId;
                    info.Intensity = item.Intensity;
                    info.RemedyCount = smallRubricCount;
                    rubricRemedyCountList.Add(info);
                }

                ClipboardRubricsRemedyInput smallRubricData = (
                    from rubricRemedy in rubricRemedyCountList
                    orderby rubricRemedy.RemedyCount
                    select rubricRemedy
                ).FirstOrDefault();

                foreach (var item in lstIntensity)
                {
                    rawData
                        .Where(w => w.SubSectionId == item.SubSectionId)
                        .ToList()
                        .ForEach(w => w.SelectedIntensity = item.Intensity);

                    rawData
                        .Where(w => w.SubSectionId == smallRubricData.SubsectionID)
                        .ToList()
                        .ForEach(w => w.SmallRubricCount = 1);
                }

                //-----for Subsections Counts-----
                int sum = 0;
                rawData.ForEach(item =>
                {
                    var subsections = rawData
                        .Where(x => x.RemedyId == item.RemedyId)
                        .Select(x => x.SubSectionId)
                        .ToList();

                    var rubricCount = subsections.Count;
                    var degreesSum = rawData
                        .Where(x => x.RemedyId == item.RemedyId)
                        .Select(x => x.GradeNo)
                        .ToList()
                        .Sum();
                    var sumRubricDegree =
                        Convert.ToInt32(rubricCount) + Convert.ToInt32(degreesSum);
                    var multIntensityDegree = item.SelectedIntensity * Convert.ToInt32(degreesSum);
                    var multIntensityDegreeAddIntensity =
                        (item.SelectedIntensity * Convert.ToInt32(degreesSum))
                        + item.SelectedIntensity;
                    // var intensityMatch = item.SelectedIntensity == item.GradeNo ? 1 : 0;
                    var intensityMatch = rawData
                        .Where(x => x.RemedyId == item.RemedyId)
                        .Select(x => x.SelectedIntensity)
                        .ToList()
                        .Sum();
                    var intensityMatchCount = rawData
                        .Where(x =>
                            x.GradeNo == item.SelectedIntensity && x.RemedyId == item.RemedyId
                        )
                        .ToList()
                        .Count();

                    clipboardRubricsModelList.Add(
                        new ClipboardRubricsRemedyDataViewModelW
                        {
                            SubSectionId = item.SubSectionId,
                            RemedyId = item.RemedyId,
                            RemedyName = item.RemedyName,
                            RemedyAlias = item.RemedyAlias,
                            GradeNo = item.GradeNo,
                            SelectedIntensity = item.SelectedIntensity,
                            RubricCount = rubricCount,
                            DegreesSum = Convert.ToInt32(degreesSum),
                            SumRubricDegree = sumRubricDegree,
                            MultIntensityDegree = multIntensityDegree,
                            MultIntensityDegreeAddIntensity = multIntensityDegreeAddIntensity,
                            intensityMatch = intensityMatch,
                            intensityMatchCount = intensityMatchCount,
                            MaxIndex = 0,
                            SmallRubricCount = item.SmallRubricCount,
                        }
                    );
                });

                //var result2 = clipboardRubricsModelList.GroupBy(x => x.RemedyId).Select(x => x.FirstOrDefault())
                //    .OrderByDescending(x => x.RubricCount)
                //    //.ThenByDescending(x => x.GradeNo)
                //    .ThenByDescending(x => x.DegreesSum)
                //     .ThenByDescending(x => x.SelectedIntensity)
                //    .ThenBy(x => x.SmallRubricCount)

                //    //.ThenByDescending(x => x.intensityMatch)
                //    .ToList();

                var result2 = clipboardRubricsModelList
                    .OrderByDescending(x => x.DegreesSum)
                    .ThenByDescending(x => x.RubricCount)
                    //.ThenByDescending(x => x.DegreesSum)
                    //.ThenByDescending(x => x.GradeNo)
                    .ThenByDescending(x => x.SmallRubricCount)
                    .ThenByDescending(x => x.SelectedIntensity)
                    // .ThenBy(x => x.SmallRubricCount)

                    .ThenByDescending(x => x.intensityMatchCount)
                    .ToList();

                var result3 = result2
                    .GroupBy(x => (x.RemedyId, x.SubSectionId))
                    .Select(x => x.FirstOrDefault())
                    .OrderByDescending(x => x.DegreesSum)
                    .ThenByDescending(x => x.SmallRubricCount)
                    .ThenByDescending(x => x.SelectedIntensity);
                var result4 = result3
                    .GroupBy(x => x.RemedyId)
                    .Select(x => x.FirstOrDefault())
                    .OrderByDescending(x => x.DegreesSum)
                    .ThenByDescending(x => x.SelectedIntensity);

                //var result2 = (from crml in distinctResult
                //               orderby crml.RubricCount descending,
                //                  crml.GradeNo descending,
                //                  crml.SelectedIntensity descending,
                //                  crml.DegreesSum descending,
                //                  crml.SumRubricDegree descending
                //          select crml).ToList();

                //var result3 = (from crml in result2
                //               orderby
                //                  crml.DegreesSum descending,
                //                  crml.SumRubricDegree descending,
                //                  crml.intensityMatch descending
                //               select crml).ToList();

                // result2= result.ThenByDescending(x => x.GradeNo).ToList();

                //var objRemedyListCount = clipboardRubricsModelList.OrderByDescending(x => x.Rubriccount).ToList();
                //var objRemedyListGrade = clipboardRubricsModelList.OrderByDescending(x => x.GradeSum).ToList();
                //var objRemedyListIntensity = clipboardRubricsModelList.OrderByDescending(x => x.intensitysum).ToList();
                //var objRemedyListFinal = clipboardRubricsModelList.OrderByDescending(x => x.DegreeMultiplies).ToList();

                //result4.ToList().ForEach(item =>
                //{
                //    int maxIndex = objRemedyListCount.FindIndex(x => x.RemedyName == item.RemedyName) + 1 +
                //            objRemedyListGrade.FindIndex(x => x.RemedyName == item.RemedyName) + 1 +
                //            objRemedyListIntensity.FindIndex(x => x.RemedyName == item.RemedyName) + 1 +
                //            objRemedyListFinal.FindIndex(x => x.RemedyName == item.RemedyName) + 1;

                //    item.MaxIndex = maxIndex;
                //});

                //var sorttedResult = result4.OrderBy(x => x.MaxIndex).ToList();

                //ClipboardRemedyModel clipboardRemedyModel = new ClipboardRemedyModel();
                //clipboardRemedyModel.CommonRemedyList = clipboardRubricsModelList.Where(x => x.CommonUncommon == true).GroupBy(x => x.RemedyId).Select(x => x.FirstOrDefault()).ToList();
                //clipboardRemedyModel.UnCommonRemedyList = clipboardRubricsModelList.Where(x => x.CommonUncommon == false).GroupBy(x => x.RemedyId).Select(x => x.FirstOrDefault()).ToList();

                return null;
            }
        }

        //public ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionIdFinal(List<ClipboardRubricsModel1> lstIntensity, ref ErrorResponseModel errorResponseModel)
        //{
        //    // Filter out null SubSectionIds and convert to a List<int>
        //    var subsectionIds = lstIntensity.Select(c => c.SubSectionId).Where(id => id.HasValue).Select(id => id.Value).ToList();

        //    // Initialize the list to store ClipboardRubricsRemedyInput objects
        //    List<ClipboardRubricsRemedyInput> clipboardRubricsRemedyInputs = lstIntensity
        //        .Select(item => new ClipboardRubricsRemedyInput
        //        {
        //            SubsectionID = item.SubSectionId ?? 0, // Convert null SubSectionId to 0 or an appropriate default
        //            Intensity = item.Intensity,
        //            RemedyCount = context.RubricRemedyDetails.Count(x => x.SubSectionId == item.SubSectionId && x.DeletedStatus == false)
        //        }).ToList();

        //    // Find the item with the smallest RemedyCount
        //    ClipboardRubricsRemedyInput smallRubricItem = clipboardRubricsRemedyInputs.OrderBy(x => x.RemedyCount).FirstOrDefault();

        //    errorResponseModel = new ErrorResponseModel();

        //    // Initialize collections to store RemedyArrayModel and SortedRemedyArrayModel objects
        //    List<RemedyArrayModel> objRemedyList = new List<RemedyArrayModel>();

        //    // Query the database for remedy details
        //    var remedyArrayList = (from subSectionMaster in context.SubSectionMaster
        //                           join rubricRemedyDetail in context.RubricRemedyDetails on subSectionMaster.SubSectionId equals rubricRemedyDetail.SubSectionId
        //                           join remedyMaster in context.RemedyMaster on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
        //                           join rgm in context.RemedyGradeMaster on rubricRemedyDetail.GradeId equals rgm.GradeId
        //                           where subsectionIds.Contains(subSectionMaster.SubSectionId)
        //                                 && rubricRemedyDetail.DeletedStatus == false
        //                                 && remedyMaster.DeleteStatus == false
        //                           select new ClipboardRubricsRemedyViewModel
        //                           {
        //                               SubSectionId = subSectionMaster.SubSectionId,
        //                               SubSectionName = subSectionMaster.SubSectionName,
        //                               RemedyId = rubricRemedyDetail.RemedyId,
        //                               RemedyName = remedyMaster.RemedyName,
        //                               RemedyAlias = remedyMaster.RemedyAlias,
        //                               GradeNo = rgm.GradeNo,
        //                               FontName = rgm.FontName,
        //                               FontStyle = rgm.FontStyle,
        //                               FontColor = rgm.FontColor,
        //                               Intensity = clipboardRubricsRemedyInputs.First(x => x.SubsectionID == subSectionMaster.SubSectionId).Intensity,
        //                               total = clipboardRubricsRemedyInputs.First(x => x.SubsectionID == subSectionMaster.SubSectionId).Intensity * rgm.GradeNo,
        //                               ThermalId = remedyMaster.ThermalId,
        //                               CommonOrUncommon = remedyMaster.CommonOrUncommon,
        //                               ThemesOrCharacteristics = remedyMaster.ThemesOrCharacteristics,
        //                               Particulars = remedyMaster.Particulars,
        //                               Generals = remedyMaster.Generals,
        //                               Modalities = remedyMaster.Modalities,
        //                               score = context.RubricRemedyDetails
        //                                           .Where(r => r.RemedyId == rubricRemedyDetail.RemedyId &&
        //                                                       subsectionIds.Contains(r.SubSectionId ?? 0) &&
        //                                                       r.DeletedStatus == false)
        //                                           .GroupBy(x => x.SubSectionId)
        //                                           .Count(), // Ensure 'Count' is invoked
        //                               SmallRubric = subSectionMaster.SubSectionId == smallRubricItem.SubsectionID ? 1 : 0
        //                           }).OrderBy(rem => rem.RemedyId).ToList();

        //    // Group the remedy data by RemedyName and calculate necessary values
        //    var groupedRemedyList = remedyArrayList
        //        .GroupBy(x => x.RemedyName)
        //        .Select(group => new RemedyArrayModel
        //        {
        //            RemedyId = group.First().RemedyId,
        //            RemedyName = group.Key,
        //            RemedyAlies = group.First().RemedyAlias,
        //            Intensity = group.First().Intensity,
        //            IntensitySum = group.Sum(x => x.Intensity),
        //            Count = group.Count(),
        //            Grade = group.Sum(x => x.GradeNo),
        //            final = group.Sum(x => x.total),
        //            Generals = group.First().Generals,
        //            Particulars = group.First().Particulars,
        //            Modalities = group.First().Modalities,
        //            ThemesOrCharacteristics = group.First().ThemesOrCharacteristics,
        //            ThermalId = group.First().ThermalId,
        //            CommonUncommon = group.First().CommonOrUncommon,
        //            score = group.First().score + "/" + subsectionIds.Count,
        //            scoreCount = group.First().score,
        //            PresentSubSection = group.SelectMany(item => context.RubricRemedyDetails
        //                                                    .Where(r => r.RemedyId == item.RemedyId &&
        //                                                                subsectionIds.Contains(r.SubSectionId ?? 0) &&
        //                                                                r.DeletedStatus == false)
        //                                                    .Select(r => r.SubSectionId)
        //                                                    .Distinct())
        //                                      .Distinct().ToList(),
        //            SmallRubric = group.First().SmallRubric,
        //        }).ToList();

        //    // Sort the grouped remedy list by different criteria
        //    var objRemedyListCount = groupedRemedyList.OrderByDescending(x => x.Count).ToList();
        //    var objRemedyListGrade = groupedRemedyList.OrderByDescending(x => x.Grade).ToList();
        //    var objRemedyListIntensity = groupedRemedyList.OrderByDescending(x => x.IntensitySum).ToList();
        //    var objRemedyListFinal = groupedRemedyList.OrderByDescending(x => x.final).ToList();

        //    // Create the sorted remedy list based on combined sorting criteria
        //    var sortedRemedyList = groupedRemedyList
        //        .Select(remedyItem =>
        //        {
        //            int maxIndex = objRemedyListCount.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1 +
        //                objRemedyListGrade.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1 +
        //                objRemedyListIntensity.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1 +
        //                objRemedyListFinal.FindIndex(x => x.RemedyName == remedyItem.RemedyName) + 1;

        //            return new SortedRemedyArrayModel
        //            {
        //                RemedyId = remedyItem.RemedyId,
        //                RemedyName = remedyItem.RemedyName,
        //                RemedyAlies = remedyItem.RemedyAlies,
        //                Intensity = remedyItem.Intensity,
        //                IntensitySum = remedyItem.IntensitySum,
        //                Count = remedyItem.Count,
        //                Grade = remedyItem.Grade,
        //                final = remedyItem.final,
        //                Generals = remedyItem.Generals,
        //                Particulars = remedyItem.Particulars,
        //                Modalities = remedyItem.Modalities,
        //                ThemesOrCharacteristics = remedyItem.ThemesOrCharacteristics,
        //                ThermalId = remedyItem.ThermalId,
        //                CommonUncommon = remedyItem.CommonUncommon,
        //                score = remedyItem.score,
        //                scoreCount = remedyItem.scoreCount,
        //                MaxIndex = maxIndex,
        //                PresentSubSection = remedyItem.PresentSubSection,
        //                SmallRubric = remedyItem.SmallRubric,
        //                progressBar = (1000 - maxIndex) / 10,
        //            };
        //        }).ToList();

        //    // Group the sorted remedy list by RemedyId and return the distinct items
        //    var rawOrderGroupBy = sortedRemedyList
        //        .OrderByDescending(x => x.final)
        //        .ThenByDescending(x => x.SmallRubric)
        //        .GroupBy(x => x.RemedyId)
        //        .Select(x => x.FirstOrDefault())
        //        .ToList();

        //    // Create and return the ClipboardRemedyNewModel with common and uncommon remedy lists
        //    ClipboardRemedyNewModel clipboardRemedyModel = new ClipboardRemedyNewModel
        //    {
        //        CommonRemedyList = rawOrderGroupBy.Where(x => x.CommonUncommon == true).ToList(),
        //        UnCommonRemedyList = rawOrderGroupBy.Where(x => x.CommonUncommon == false).ToList()
        //    };

        //    return clipboardRemedyModel;
        //}

        //Current in use

        public ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionIdFinal(
    List<ClipboardRubricsModel1> lstIntensity,
    ref ErrorResponseModel errorResponseModel
)
{
    errorResponseModel = new ErrorResponseModel();

    var subsectionIds = lstIntensity.Select(c => c.SubSectionId).Distinct().ToList();

    // Prefetch only relevant RubricRemedyDetails
    var rubricRemedyDetails = context.RubricRemedyDetails
        .Where(x => subsectionIds.Contains(x.SubSectionId) && x.DeletedStatus== false)
        .Select(x => new { x.SubSectionId, x.RemedyId, x.GradeId })
        .ToList();

    // Compute remedy counts efficiently using grouping
    var remedyCounts = rubricRemedyDetails
        .GroupBy(x => x.SubSectionId)
        .ToDictionary(g => g.Key, g => g.Count());

    // Build intensity inputs
    var clipboardRubricsRemedyInputs = lstIntensity
        .Select(item => new ClipboardRubricsRemedyInput
        {
            SubsectionID = (int)item.SubSectionId,
            Intensity = item.Intensity,
            RemedyCount = remedyCounts.TryGetValue(item.SubSectionId, out var count) ? count : 0
        })
        .ToList();

    var smallRubricItem = clipboardRubricsRemedyInputs
        .OrderBy(x => x.RemedyCount)
        .FirstOrDefault();

    // Prefetch remedies with joined data
    var remedyData =
        (from rrd in context.RubricRemedyDetails
         join rm in context.RemedyMasters on rrd.RemedyId equals rm.RemedyId
         join rg in context.RemedyGradeMaster on rrd.GradeId equals rg.GradeId
         join sm in context.SubSectionMasters on rrd.SubSectionId equals sm.SubSectionId
         where subsectionIds.Contains(rrd.SubSectionId)
               && rrd.DeletedStatus== false
               && rm.DeleteStatus== false
         select new
         {
             sm.SubSectionId,
             sm.SubSectionName,
             rrd.RemedyId,
             rm.RemedyName,
             rm.RemedyAlias,
             rg.GradeNo,
             rg.FontName,
             rg.FontStyle,
             rg.FontColor,
             rm.CommonOrUncommon,
             rm.ThemesOrCharacteristics,
             rm.Particulars,
             rm.Generals,
             rm.Modalities,
             rm.ThermalId
         })
        .ToList();

    // Join with intensity data (in memory now)
    var intensityMap = clipboardRubricsRemedyInputs.ToDictionary(x => x.SubsectionID, x => x.Intensity);
    var remedyArrayList = remedyData
        .Select(x => new ClipboardRubricsRemedyViewModel
        {
            SubSectionId = x.SubSectionId,
            SubSectionName = x.SubSectionName,
            RemedyId = x.RemedyId,
            RemedyName = x.RemedyName,
            RemedyAlias = x.RemedyAlias,
            GradeNo = x.GradeNo,
            FontName = x.FontName,
            FontStyle = x.FontStyle,
            FontColor = x.FontColor,
            Intensity = intensityMap.TryGetValue(x.SubSectionId, out var intensity) ? intensity : 0,
            total = (intensityMap.TryGetValue(x.SubSectionId, out var i) ? i : 0) * x.GradeNo,
            ThermalId = x.ThermalId,
            CommonOrUncommon = x.CommonOrUncommon,
            ThemesOrCharacteristics = x.ThemesOrCharacteristics,
            Particulars = x.Particulars,
            Generals = x.Generals,
            Modalities = x.Modalities,
            score = rubricRemedyDetails
                .Where(r => r.RemedyId == x.RemedyId)
                .Select(r => r.SubSectionId)
                .Distinct()
                .Count(),
            SmallRubric = x.SubSectionId == smallRubricItem.SubsectionID ? 1 : 0
        })
        .ToList();

    // Group remedies
    var objRemedyList = remedyArrayList
        .GroupBy(x => x.RemedyName)
        .Select(g => new RemedyArrayModel
        {
            RemedyId = g.First().RemedyId,
            RemedyName = g.Key,
            RemedyAlies = g.First().RemedyAlias,
            Intensity = g.First().Intensity,
            IntensitySum = g.Sum(r => r.Intensity),
            Count = g.Count(),
            Grade = g.Sum(r => r.GradeNo),
            final = g.Sum(r => r.total),
            Generals = g.First().Generals,
            Particulars = g.First().Particulars,
            Modalities = g.First().Modalities,
            ThemesOrCharacteristics = g.First().ThemesOrCharacteristics,
            ThermalId = g.First().ThermalId,
            CommonUncommon = g.First().CommonOrUncommon,
            score = $"{g.First().score}/{subsectionIds.Count}",
            scoreCount = g.First().score,
            PresentSubSection = rubricRemedyDetails
                .Where(r => r.RemedyId == g.First().RemedyId)
                .Select(r => r.SubSectionId)
                .Distinct()
                .ToList(),
            SmallRubric = g.First().SmallRubric
        })
        .ToList();

    // Sort orders (cache once)
    var countOrder = objRemedyList.OrderByDescending(x => x.Count).ToList();
    var gradeOrder = objRemedyList.OrderByDescending(x => x.Grade).ToList();
    var intensityOrder = objRemedyList.OrderByDescending(x => x.IntensitySum).ToList();
    var finalOrder = objRemedyList.OrderByDescending(x => x.final).ToList();

    // Build sorted remedy list
    var sortedRemedyList = objRemedyList.Select(remedy =>
    {
        int maxIndex =
            countOrder.FindIndex(x => x.RemedyName == remedy.RemedyName) + 1 +
            gradeOrder.FindIndex(x => x.RemedyName == remedy.RemedyName) + 1 +
            intensityOrder.FindIndex(x => x.RemedyName == remedy.RemedyName) + 1 +
            finalOrder.FindIndex(x => x.RemedyName == remedy.RemedyName) + 1;

        return new SortedRemedyArrayModel
        {
            RemedyId = remedy.RemedyId,
            RemedyName = remedy.RemedyName,
            RemedyAlies = remedy.RemedyAlies,
            Intensity = remedy.Intensity,
            IntensitySum = remedy.IntensitySum,
            Count = remedy.Count,
            Grade = remedy.Grade,
            final = remedy.final,
            Generals = remedy.Generals,
            Particulars = remedy.Particulars,
            Modalities = remedy.Modalities,
            ThemesOrCharacteristics = remedy.ThemesOrCharacteristics,
            ThermalId = remedy.ThermalId,
            CommonUncommon = remedy.CommonUncommon,
            score = remedy.score,
            scoreCount = remedy.scoreCount,
            MaxIndex = maxIndex,
            PresentSubSection = remedy.PresentSubSection,
            SmallRubric = remedy.SmallRubric,
            progressBar = (1000 - maxIndex) / 10
        };
    }).ToList();

    var rawOrderGroupBy = sortedRemedyList
        .OrderByDescending(x => x.final)
        .ThenByDescending(x => x.SmallRubric)
        .GroupBy(x => x.RemedyId)
        .Select(x => x.First())
        .ToList();

    return new ClipboardRemedyNewModel
    {
        CommonRemedyList = rawOrderGroupBy.Where(x => x.CommonUncommon.GetValueOrDefault()).ToList(),
        UnCommonRemedyList = rawOrderGroupBy.Where(x => !x.CommonUncommon.GetValueOrDefault()).ToList()
    };
}

        public ClipboardRemedyNewModel GetCommanUnCommanEliminationData(
            ClipboardRUbricModel clipboardRUbricModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var eliminateSubsectionId = 0;
            var eliminateIntensity = 0;
            //var subsectionIds = clipboardRUbricModel.WithEliminateRubric.Select(c => c.SubSectionId).ToList();
            var OtherList = new List<List<int>>();
            foreach (var item in clipboardRUbricModel.WithEliminateRubric)
            {
                var selectedOtherRemedy = (
                    from rubricRemedyDetail in context.RubricRemedyDetails
                    where
                        rubricRemedyDetail.SubSectionId == item.SubSectionId
                        && rubricRemedyDetail.DeletedStatus == false
                    select new { rubricRemedyDetail.RemedyId }
                )
                    .OrderBy(rem => rem.RemedyId)
                    .ToList();
                OtherList.Add(
                    selectedOtherRemedy.Select(x => Convert.ToInt32(x.RemedyId)).ToList()
                );
            }
            IEnumerable<int> commonElements = GetCommonElements(OtherList);
            // Convert the result to a list if needed
            List<int> result = commonElements.ToList();
            List<SortedRemedyArrayModel> sortedRemedyBYELIList = new List<SortedRemedyArrayModel>();
            List<SortedRemedyArrayModel> sortedRemedyList = new List<SortedRemedyArrayModel>();
            ClipboardRemedyNewModel clipboardRemedyModel = new ClipboardRemedyNewModel();

            List<ClipboardRubricsRemedyViewModel> remedyArrayList =
                new List<ClipboardRubricsRemedyViewModel>();

            if (result.Count > 0)
            {
                List<ClipboardRubricsRemedyInput> clipboardRubricsRemedyInputs =
                    new List<ClipboardRubricsRemedyInput>();
                foreach (var item in clipboardRUbricModel.WithEliminateRubric)
                {
                    ClipboardRubricsRemedyInput data = new ClipboardRubricsRemedyInput();
                    data.SubsectionID = (int)item.SubSectionId;
                    data.Intensity = item.Intensity;
                    data.RemedyCount = context
                        .RubricRemedyDetails.Where(x =>
                            (x.SubSectionId == item.SubSectionId && x.DeletedStatus == false)
                        )
                        .ToList()
                        .Count();
                    clipboardRubricsRemedyInputs.Add(data);
                }

                ClipboardRubricsRemedyInput smallRubricItem = clipboardRubricsRemedyInputs
                    .OrderBy(x => x.RemedyCount)
                    .FirstOrDefault();
                var subsectionIds = clipboardRubricsRemedyInputs
                    .Select(c => c.SubsectionID)
                    .ToList();

                errorResponseModel = new ErrorResponseModel();

                List<Object> obj = new List<object>();
                List<RemedyArrayModel> objRemedyList = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListCount = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListGrade = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListIntensity = new List<RemedyArrayModel>();
                List<RemedyArrayModel> objRemedyListFinal = new List<RemedyArrayModel>();

                foreach (var item in clipboardRubricsRemedyInputs)
                {
                    var selectedRemedy = (
                        from rubricRemedyDetail in context.RubricRemedyDetails
                        join remedyMaster in context.RemedyMasters
                            on rubricRemedyDetail.RemedyId equals remedyMaster.RemedyId
                        join rgm in context.RemedyGradeMaster
                            on rubricRemedyDetail.GradeId equals rgm.GradeId
                        where
                            rubricRemedyDetail.SubSectionId == item.SubsectionID
                            && rubricRemedyDetail.DeletedStatus == false
                        select new ClipboardRubricsRemedyViewModel
                        {
                            SubSectionId = Convert.ToInt32(rubricRemedyDetail.SubSectionId),
                            RemedyId = rubricRemedyDetail.RemedyId,
                            RemedyName = remedyMaster.RemedyName,
                            RemedyAlias = remedyMaster.RemedyAlias,
                            GradeNo = rgm.GradeNo,
                            FontName = rgm.FontName,
                            FontStyle = rgm.FontStyle,
                            FontColor = rgm.FontColor,
                            Intensity = item.Intensity,
                            total = (item.Intensity * rgm.GradeNo),
                            ThermalId = remedyMaster.ThermalId,
                            CommonOrUncommon = remedyMaster.CommonOrUncommon,
                            ThemesOrCharacteristics = remedyMaster.ThemesOrCharacteristics,
                            Particulars = remedyMaster.Particulars,
                            Generals = remedyMaster.Generals,
                            Modalities = remedyMaster.Modalities,
                            //score = (from rubricRemedyDetail_ in context.RubricRemedyDetails
                            //         where rubricRemedyDetail_.RemedyId == rubricRemedyDetail.RemedyId &&
                            //         subsectionIds.Contains(Convert.ToInt32(rubricRemedyDetail_.SubSectionId)) && rubricRemedyDetail_.DeletedStatus == false
                            //         select rubricRemedyDetail_).GroupBy(x => x.SubSectionId).Count(),
                            score = 0,
                            SmallRubric = item.SubsectionID == smallRubricItem.SubsectionID ? 1 : 0,
                        }
                    ).OrderBy(rem => rem.RemedyId).ToList();
                    remedyArrayList.AddRange(selectedRemedy);
                }

                foreach (var item1 in remedyArrayList)
                {
                    int score = remedyArrayList
                        .Where(x => x.RemedyId == item1.RemedyId)
                        .Select(x => x.SubSectionId)
                        .ToList()
                        .Distinct()
                        .Count();
                    objRemedyList.Add(
                        new RemedyArrayModel
                        {
                            RemedyId = item1.RemedyId,
                            RemedyName = item1.RemedyName,
                            RemedyAlies = item1.RemedyAlias,
                            Intensity = item1.Intensity,
                            IntensitySum = remedyArrayList
                                .Where(x => x.RemedyName == item1.RemedyName)
                                .Sum(x => x.Intensity),
                            Count = remedyArrayList
                                .Where(x => x.RemedyName == item1.RemedyName)
                                .Count(),
                            Grade = remedyArrayList
                                .Where(x => x.RemedyName == item1.RemedyName)
                                .Sum(x => x.GradeNo),
                            final = remedyArrayList
                                .Where(x => x.RemedyName == item1.RemedyName)
                                .Sum(x => x.total),
                            Generals = item1.Generals,
                            Particulars = item1.Particulars,
                            Modalities = item1.Modalities,
                            ThemesOrCharacteristics = item1.ThemesOrCharacteristics,
                            ThermalId = item1.ThermalId,
                            CommonUncommon = item1.CommonOrUncommon,
                            score = score + "/" + subsectionIds.Count,
                            scoreCount = score,
                            PresentSubSection = (
                                from rubricRemedyDetail_ in context.RubricRemedyDetails
                                where
                                    rubricRemedyDetail_.RemedyId == item1.RemedyId
                                    && subsectionIds.Contains(
                                        Convert.ToInt32(rubricRemedyDetail_.SubSectionId)
                                    )
                                    && rubricRemedyDetail_.DeletedStatus == false
                                select rubricRemedyDetail_.SubSectionId
                            )
                                .Distinct()
                                .ToList(),

                            SmallRubric = item1.SmallRubric,
                        }
                    );
                }

                objRemedyListCount = objRemedyList.OrderByDescending(x => x.Count).ToList();
                objRemedyListGrade = objRemedyList.OrderByDescending(x => x.Grade).ToList();
                objRemedyListIntensity = objRemedyList
                    .OrderByDescending(x => x.IntensitySum)
                    .ToList();
                objRemedyListFinal = objRemedyList.OrderByDescending(x => x.final).ToList();

                foreach (var remedyItem in objRemedyList)
                {
                    int maxIndex =
                        objRemedyListCount.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                        + 1
                        + objRemedyListGrade.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                        + 1
                        + objRemedyListIntensity.FindIndex(x =>
                            x.RemedyName == remedyItem.RemedyName
                        )
                        + 1
                        + objRemedyListFinal.FindIndex(x => x.RemedyName == remedyItem.RemedyName)
                        + 1;

                    SortedRemedyArrayModel sortedRemedy = new SortedRemedyArrayModel();
                    sortedRemedy.RemedyId = remedyItem.RemedyId;
                    sortedRemedy.RemedyName = remedyItem.RemedyName;
                    sortedRemedy.RemedyAlies = remedyItem.RemedyAlies;
                    sortedRemedy.Intensity = remedyItem.Intensity;
                    sortedRemedy.IntensitySum = remedyItem.IntensitySum;
                    sortedRemedy.Count = remedyItem.Count;
                    sortedRemedy.Grade = remedyItem.Grade;
                    sortedRemedy.final = remedyItem.final;
                    sortedRemedy.Generals = remedyItem.Generals;
                    sortedRemedy.Particulars = remedyItem.Particulars;
                    sortedRemedy.Modalities = remedyItem.Modalities;
                    sortedRemedy.ThemesOrCharacteristics = remedyItem.ThemesOrCharacteristics;
                    sortedRemedy.ThermalId = remedyItem.ThermalId;
                    sortedRemedy.CommonUncommon = remedyItem.CommonUncommon;
                    sortedRemedy.score = remedyItem.score;
                    sortedRemedy.scoreCount = remedyItem.scoreCount;
                    sortedRemedy.MaxIndex = maxIndex;
                    sortedRemedy.PresentSubSection = remedyItem.PresentSubSection;
                    sortedRemedy.SmallRubric = remedyItem.SmallRubric;
                    sortedRemedy.progressBar = (1000 - maxIndex) / 10;
                    sortedRemedyList.Add(sortedRemedy);
                }

                var rawOrder = sortedRemedyList
                    .OrderByDescending(x => x.final)
                    .ThenByDescending(x => x.SmallRubric)
                    .ToList();
                var rawOrderGroupBy = rawOrder
                    .GroupBy(x => x.RemedyId)
                    .Select(x => x.FirstOrDefault())
                    .ToList();

                sortedRemedyBYELIList = (
                    from sortedList in rawOrderGroupBy
                    where result.Contains(Convert.ToInt32(sortedList.RemedyId))
                    select sortedList
                ).ToList();
            }

            clipboardRemedyModel.CommonRemedyList = sortedRemedyBYELIList
                .Where(x => x.CommonUncommon == true)
                .ToList();
            clipboardRemedyModel.UnCommonRemedyList = sortedRemedyBYELIList
                .Where(x => x.CommonUncommon == false)
                .ToList();
            return clipboardRemedyModel;
        }

        static IEnumerable<T> GetCommonElements<T>(IEnumerable<IEnumerable<T>> lists)
        {
            // Start with the first list
            var commonElements = new HashSet<T>(lists.First());

            // Intersect with the rest of the lists
            foreach (var list in lists.Skip(1))
            {
                commonElements.IntersectWith(list);
            }

            return commonElements;
        }

        public ClipboardRemedyNewModel GetCommanUnCommanRubricsDetailsBySubsectionId1(
            List<ClipboardRubricsModel1> lstIntensity,
            ref ErrorResponseModel errorResponseModel
        )
        {
            throw new NotImplementedException();
        }
    }
}

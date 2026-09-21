using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;

namespace Niga_Domain.Business.Implementation
{
    /// <summary>
    /// This is implementation  for the master Get operations
    /// </summary>
    public class MastersAPIService : IMastersAPIService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public MastersAPIService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        /// <summary>
        /// Method to get all the states
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<StateModel> GetStates(int? CountryId ,ref ErrorResponseModel errorResponseModel)
        {
            var stateModelList = new List<StateModel>();
            errorResponseModel = new ErrorResponseModel();
            var stateEntityList = (
                from s in context.StateMasters
                select new StateModel
                {
                    StateId = s.StateId,
                    StateName = s.StateName,
                    EnteredDate = s.EnteredDate,
                    EnteredBy = s.EnteredBy,
                    ChangedBy = s.ChangedBy,
                    ChangedDate = s.ChangedDate,
                    DeleteStatus = s.DeleteStatus,
                    CountryId = s.CountryId,
                }
            ).ToList();
            if(CountryId > 0)
            {
                stateEntityList = stateEntityList.Where(x => x.CountryId == CountryId).ToList();
            }
            if (stateEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "State not found";
            }
            return stateEntityList;
        }

        /// <summary>
        /// Method to get all countries
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<CountryModel> GetCountries(ref ErrorResponseModel errorResponseModel)
        {
            var countryModelList = new List<CountryModel>();
            errorResponseModel = new ErrorResponseModel();
            var countryEntityList = (
                from c in context.CountryMasters
                select new CountryModel
                {
                    CountryId = c.CountryId,
                    CountryName = c.CountryName,
                    EnteredDate = c.EnteredDate,
                    EnteredBy = c.EnteredBy,
                    ChangedBy = c.ChangedBy,
                    ChangedDate = c.ChangedDate,
                    DeleteStatus = c.DeleteStatus,
                }
            ).ToList();

            if (countryEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Country not found";
            }
            return countryEntityList;
        }

        /// <summary>
        /// Method to get all active languages
        /// </summary>
        public List<LanguageMasterModel> GetLanguages(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var languageModelList = (
                from l in context.LanguageMasters
                where l.IsDeleted == false || l.IsDeleted == null
                orderby l.LanguageId
                select new LanguageMasterModel
                {
                    LanguageId = l.LanguageId,
                    LanguageName = l.LanguageName,
                    Description = l.Description,
                    IsDeleted = l.IsDeleted
                }
            ).ToList();

            if (languageModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Language not found";
            }

            return languageModelList;
        }

        /// <summary>
        /// Method to get all the genders
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<GenderModel> GetGenders(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var genderModelList = (
                from g in context.GenderMasters
                select new GenderModel
                {
                    GenderId = g.GenderId,
                    GenderName = g.GenderName,
                    EnteredDate = g.EnteredDate,
                    EnteredBy = g.EnteredBy,
                    ChangedBy = g.ChangedBy,
                    ChangedDate = g.ChangedDate,
                    DeleteStatus = g.DeleteStatus,
                }
            ).ToList();

            if (genderModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Gender not found";
            }
            return genderModelList;
        }

        /// <summary>
        /// Method to get all the packages
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<PackageModel> GetPackages(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var packageModelList = (
                from p in context.PackageMasters
                select new PackageModel
                {
                    PackageId = p.PackageId,
                    PackageName = p.PackageName,
                    CaseCount = p.CaseCount,
                    ValidityInDays = p.ValidityInDays,
                    Amount = p.Amount,
                    EnteredDate = p.EnteredDate,
                    EnteredBy = p.EnteredBy,
                    ChangedBy = p.ChangedBy,
                    ChangedDate = p.ChangedDate,
                    DeleteStatus = p.DeleteStatus,
                }
            ).ToList();

            if (packageModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Package not found";
            }
            return packageModelList;
        }

        /// <summary>
        /// Method to get all the qualifications
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<QualificationModel> GetQualifications(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var qualificationModelList = (
                from q in context.QualificationMasters
                where !q.DeleteStatus
                select new QualificationModel
                {
                    QualificationId = q.QualificationId,
                    QualificationName = q.QualificationName,
                    QualificationAlias = q.QualificationAlias,
                    Description = q.Description,
                    DegreeLevel = q.DegreeLevel,
                    EnteredDate = q.EnteredDate,
                    EnteredBy = q.EnteredBy,
                    ChangedBy = q.ChangedBy,
                    ChangedDate = q.ChangedDate,
                    DeleteStatus = q.DeleteStatus,
                }
            ).ToList();

            if (qualificationModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Qualification not found";
            }
            return qualificationModelList;
        }

        /// <summary>
        /// Method to get all the diagnosis groups
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<DiagnosisGroupModel> GetDiagnosisGroups(
            ref ErrorResponseModel errorResponseModel
        )
        {
            errorResponseModel = new ErrorResponseModel();
            var diagnosisGroupModelList = (
                from dg in context.DiagnosisGroupMasters
                select new DiagnosisGroupModel
                {
                    DiagnosisGroupId = dg.DiagnosisGroupId,
                    DiagnosisGroupName = dg.DiagnosisGroupName,
                    Description = dg.Description,
                    EnteredDate = dg.EnteredDate,
                    EnteredBy = dg.EnteredBy,
                    ChangedBy = dg.ChangedBy,
                    ChangedDate = dg.ChangedDate,
                    DeleteStatus = dg.DeleteStatus,
                }
            ).ToList();

            if (diagnosisGroupModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Diagnosis Group not found";
            }
            return diagnosisGroupModelList;
        }

        /// <summary>
        /// Method to get all the diagnosis
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<DiagnosisModel> GetDiagnosis(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var diagnosisModelList = (
                from d in context.DiagnosisMasters
                select new DiagnosisModel
                {
                    DiagnosisId = d.DiagnosisId,
                    DiagnosisGroupId = d.DiagnosisGroupId,
                    DiagnosisName = d.DiagnosisName,
                    DiagnosisNameAlias = d.DiagnosisNameAlias,
                    Description = d.Description,
                    Keywords = d.Keywords,
                    EnteredDate = d.EnteredDate,
                    EnteredBy = d.EnteredBy,
                    ChangedBy = d.ChangedBy,
                    ChangedDate = d.ChangedDate,
                    DeleteStatus = d.DeleteStatus,
                }
            ).ToList();

            if (diagnosisModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Diagnosis not found";
            }
            return diagnosisModelList;
        }

        /// <summary>
        /// Method to get all the sections
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<SectionModel> GetSections(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var sectionModelList = (
                from s in context.SectionMasters
                where s.DeleteStatus == false
                select new SectionModel
                {
                    SectionId = s.SectionId,
                    SectionName = s.SectionName,
                    SectionAlias = s.SectionAlias,
                    Description = s.Description,
                    EnteredDate = s.EnteredDate,
                    EnteredBy = s.EnteredBy,
                    ChangedBy = s.ChangedBy,
                    ChangedDate = s.ChangedDate,
                    DeleteStatus = s.DeleteStatus,
                    ParentSubSectionID = null,
                }
            ).ToList();

            if (sectionModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Section not found";
            }
            return sectionModelList;
        }

        /// <summary>
        /// Method to get all the subsections
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<SubSectionModel> GetSubSections(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var subsectionModelList = (
                from ss in context.SubSectionMasters
                select new SubSectionModel
                {
                    SubSectionId = ss.SubSectionId,
                    SectionId = ss.SectionId,
                    ParentSubSectionId = ss.ParentSubSectionId,
                    SubSectionName = ss.SubSectionName,
                    SubSectionNameAlias = ss.SubSectionNameAlias,
                    Description = ss.Description,
                    EnteredDate = ss.EnteredDate,
                    EnteredBy = ss.EnteredBy,
                    ChangedBy = ss.ChangedBy,
                    ChangedDate = ss.ChangedDate,
                    DeleteStatus = ss.DeleteStatus,
                }
            ).ToList();

            if (subsectionModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "SubSection not found";
            }
            return subsectionModelList;
        }

        /// <summary>
        /// Method to get all the subsections
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<SubSectionModel> GetSubsectionBySection(
            long sectionId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var subsectionModelList = new List<SubSectionModel>();
            errorResponseModel = new ErrorResponseModel();
            var subsectionEntityList = (
                from ss in context.SubSectionMasters
                where ss.SectionId == sectionId && ss.DeleteStatus == false
                select new SubSectionModel
                {
                    SubSectionId = ss.SubSectionId,
                    SectionId = ss.SectionId,
                    ParentSubSectionId = ss.ParentSubSectionId,
                    SubSectionName = ss.SubSectionName,
                    SubSectionNameAlias = ss.SubSectionNameAlias,
                    Description = ss.Description,
                    EnteredDate = ss.EnteredDate,
                    EnteredBy = ss.EnteredBy,
                    ChangedBy = ss.ChangedBy,
                    ChangedDate = ss.ChangedDate,
                    DeleteStatus = ss.DeleteStatus,
                }
            ).ToList();

            if (subsectionEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "SubSection not found";
            }
            return subsectionEntityList;
        }

        public List<RemedyModel> GetRemedies(
            RubricRemedyDetailsModel rubricRemedyDetailsModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            errorResponseModel = new ErrorResponseModel();
            var remedyModelList = (
                from remedyMaster in context.RemedyMasters
                where
                    !context
                        .RubricRemedyDetails.Where(x =>
                            x.SubSectionId == rubricRemedyDetailsModel.SubSectionId
                            && x.GradeId == rubricRemedyDetailsModel.GradeId
                            && x.DeletedStatus == false
                        )
                        .Select(x => x.RemedyId)
                        .Contains(remedyMaster.RemedyId)
                    && remedyMaster.DeleteStatus == false
                select new RemedyModel
                {
                    RemedyId = remedyMaster.RemedyId,
                    RemedyName = remedyMaster.RemedyName,
                    RemedyAlias = remedyMaster.RemedyAlias,
                    Description = remedyMaster.Description,
                    EnteredDate = remedyMaster.EnteredDate,
                    EnteredBy = remedyMaster.EnteredBy,
                    ChangedBy = remedyMaster.ChangedBy,
                    ChangedDate = remedyMaster.ChangedDate,
                    DeleteStatus = remedyMaster.DeleteStatus,
                }
            ).ToList();

            if (remedyModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy not found";
            }
            return remedyModelList;
        }

        public List<IntensityModel> GetIntensities(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var intensityModelList = (
                from intensity in context.IntensityMasters
                select new IntensityModel
                {
                    IntensityId = intensity.IntensityId,
                    IntensityNo = intensity.IntensityNo,
                    Description = intensity.Description,
                    EnteredDate = intensity.EnteredDate,
                    EnteredBy = intensity.EnteredBy,
                    ChangedBy = intensity.ChangedBy,
                    ChangedDate = intensity.ChangedDate,
                    DeleteStatus = intensity.DeleteStatus,
                }
            ).ToList();

            if (intensityModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Intensity not found";
            }
            return intensityModelList;
        }

        public List<RemedyGradeModel> GetRemedyGrades(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var remedygradeModelList = (
                from rg in context.RemedyGradeMaster
                where rg.DeleteStatus == false
                select new RemedyGradeModel
                {
                    GradeId = rg.GradeId,
                    GradeNo = rg.GradeNo,
                    Description = rg.Description,
                    FontName = rg.FontName,
                    FontStyle = rg.FontStyle,
                    FontColor = rg.FontColor,
                    EnteredDate = rg.EnteredDate,
                    EnteredBy = rg.EnteredBy,
                    ChangedBy = rg.ChangedBy,
                    ChangedDate = rg.ChangedDate,
                    DeleteStatus = rg.DeleteStatus,
                }
            ).ToList();

            if (remedygradeModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy Grade not found";
            }
            return remedygradeModelList;
        }

        public List<BodyPartModel> GetBodyParts(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var bodypartModelList = (
                from bp in context.BodyPartMasters
                select new BodyPartModel
                {
                    BodyPartId = bp.BodyPartId,
                    SectionId = bp.SectionId,
                    BodyPartName = bp.BodyPartName,
                    Description = bp.Description,
                    EnteredDate = bp.EnteredDate,
                    EnteredBy = bp.EnteredBy,
                    ChangedBy = bp.ChangedBy,
                    ChangedDate = bp.ChangedDate,
                    DeleteStatus = bp.DeleteStatus,
                }
            ).ToList();

            if (bodypartModelList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Body Part not found";
            }
            return bodypartModelList;
        }

        public List<PartLocationModel> GetPartLocations(ref ErrorResponseModel errorResponseModel)
        {
            var partlocationModelList = new List<PartLocationModel>();
            errorResponseModel = new ErrorResponseModel();
            var partlocationEntityList = (
            from pl in context.PartLocationMasters
            select new PartLocationModel
            {
                PartLocationId = pl.PartLocationId,
                PartLocationName = pl.PartLocationName,
                Description = pl.Description,
                EnteredDate = pl.EnteredDate,
                EnteredBy = pl.EnteredBy,
                ChangedBy = pl.ChangedBy,
                ChangedDate = pl.ChangedDate,
                DeleteStatus = pl.DeleteStatus,
            }
            ).ToList();

            if (partlocationEntityList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Part Location not found";
            }
            return partlocationEntityList;
        }

        public List<QuestionSectionModel> GetQuestionSections(ref ErrorResponseModel errorResponseModel)
        {
            var questionsectionModelList = new List<QuestionSectionModel>();
            errorResponseModel = new ErrorResponseModel();
            var questionsectionEntityList = (
            from qs in context.QuestionSectionMasters
            select new QuestionSectionModel
            {
                QuestionSectionId = qs.QuestionSectionId,
                QuestionSectionName = qs.QuestionSectionName,
                Description = qs.Desciption,
                EnteredDate = qs.EnteredDate,
                EnteredBy = qs.EnteredBy,
                ChangedBy = qs.ChangedBy,
                ChangedDate = qs.ChangedDate,
                DeleteStatus = qs.DeleteStatus,
            }
            ).ToList();

            if (questionsectionEntityList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Question Section not found";
            }
            return questionsectionEntityList;
        }

        // public List<CaseEntryChiefComplaintModel> GetAllChiefComplaints(ref ErrorResponseModel errorResponseModel)
        // {
        //     var listCaseEntryChiefComplaintModel = new List<CaseEntryChiefComplaintModel>();
        //     errorResponseModel = new ErrorResponseModel();
        //     var listCaseEntryChiefComplaintEntity = (
        //     from c in context.CaseEntryChiefComplaints
        //     select new CaseEntryChiefComplaintModel
        //     {
        //         ChiefComplaintName = c.ChiefComplaintName,
        //     }
        //     ).Distinct().ToList();

        //     if (listCaseEntryChiefComplaintEntity.Count == 0)
        //     {
        //     errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //     errorResponseModel.Message = "Chief Complaints not found";
        //     }
        //     return listCaseEntryChiefComplaintEntity;
        // }

        public List<ClinicalQuestionsModel> GetClinicalQuestions(ref ErrorResponseModel errorResponseModel)
        {
            var clinicalquestionsModelList = new List<ClinicalQuestionsModel>();
            errorResponseModel = new ErrorResponseModel();
            var clinicalquestionsEntityList = (
            from cq in context.ClinicalQuestions
            where cq.DeleteStatus == false
            select new ClinicalQuestionsModel
            {
                QuestionsId = cq.QuestionsId,
                QuestionGroupId = cq.QuestionGroupId,
                EnteredDate = cq.EnteredDate,
                EnteredBy = cq.EnteredBy,
                ChangedBy = cq.ChangedBy,
                ChangedDate = cq.ChangedDate,
                DeleteStatus = cq.DeleteStatus,
            }
            ).ToList();

            if (clinicalquestionsEntityList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Clinical Questions not found";
            }
            return clinicalquestionsEntityList;
        }

        public List<QuestionGroupModel> GetQuestionGroup(ref ErrorResponseModel errorResponseModel)
        {
            var questiongroupModelList = new List<QuestionGroupModel>();
            errorResponseModel = new ErrorResponseModel();
            var questiongroupEntityList = (
            from qg in context.QuestionGroupMasters
            where qg.DeleteStatus == false
            select new QuestionGroupModel
            {
                QuestionGroupId = qg.QuestionGroupId,
                QuestionGroupName = qg.QuestionGroupName,
                Description = qg.Description,
                EnteredDate = qg.EnteredDate,
                EnteredBy = qg.EnteredBy,
                ChangedBy = qg.ChangedBy,
                ChangedDate = qg.ChangedDate,
                DeleteStatus = qg.DeleteStatus,
            }
            ).ToList();

            if (questiongroupEntityList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Question Group not found";
            }
            return questiongroupEntityList;
        }

        // public List<SubSectionModel> GetSubSectionByBodyPart(long bodyPartId, ref ErrorResponseModel errorResponseModel)
        // {
        //     var subsectionModelList = new List<SubSectionModel>();
        //     errorResponseModel = new ErrorResponseModel();
        //     var subSectionEntities = (
        //     from ss in context.SubSectionMasters
        //     where ss.BodyPartId == bodyPartId
        //     select new SubSectionModel
        //     {
        //         SubSectionId = ss.SubSectionId,
        //         ChangedBy = ss.ChangedBy,
        //         ChangedDate = ss.ChangedDate,
        //         DeleteStatus = ss.DeleteStatus,
        //         Description = ss.Description,
        //         EnteredBy = ss.EnteredBy,
        //         EnteredDate = ss.EnteredDate,
        //         SectionId = ss.SectionId,
        //         ParentSubSectionId = ss.ParentSubSectionId,
        //         SubSectionName = ss.SubSectionName,
        //         SubSectionNameAlias = ss.SubSectionNameAlias,
        //     }
        //     ).ToList();

        //     if (subSectionEntities.Count == 0)
        //     {
        //     errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //     errorResponseModel.Message = "Sub section not found";
        //     }
        //     return subSectionEntities;
        // }

        public List<DoctorModel> GetDoctorList(ref ErrorResponseModel errorResponseModel)
        {
            var doctorModelList = new List<DoctorModel>();
            errorResponseModel = new ErrorResponseModel();
            var doctorEntityList = (
            from d in context.Doctors
            where d.DeleteStatus == false
            select new DoctorModel
            {
                FirstName = d.FirstName,
                MiddleName = d.MiddleName,
                LastName = d.LastName,
                CasePaperValidity = d.CasePaperValidity,
                City = d.City,
                DoctorID = d.DoctorId,
                EmailId = d.EmailId,
                MobileNo = d.MobileNo,
                PackageId = d.PackageId,
                PassingCertNo = d.PassingCertNo,
                PassingUniversity = d.PassingUniversity,
                PermanantAddress = d.PermanantAddress,
                QualificationID = d.QualificationId,
                UserId = d.UserId,
            }
            ).ToList();

            if (doctorEntityList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Doctors not found";
            }
            return doctorEntityList;
        }

        public List<ModuleMasterModel> GetModuleMaster(ref ErrorResponseModel errorResponseModel)
        {
            var moduleMasterModelList = new List<ModuleMasterModel>();
            errorResponseModel = new ErrorResponseModel();
            var moduleMasterEntityList = (
            from mm in context.ModuleMasters
            select new ModuleMasterModel
            {
                ModuleId = mm.ModuleId,
                ModuleName = mm.ModuleName,
                ModuleMarathiName = mm.ModuleMarathiName,
                ModuleIcon = mm.ModuleIcon,
                ModuleAreaName = mm.ModuleAreaName,
                Seqno = mm.Seqno,
                IsDirectNode = mm.IsDirectNode,
                ActionName = mm.ActionName,
                ControllerName = mm.ControllerName,
                ModuleUrl = mm.ModuleUrl,
                EnteredDate = mm.EnteredDate,
                EnteredBy = mm.EnteredBy,
                ChangedBy = mm.ChangedBy,
                ChangedDate = mm.ChangedDate,
                DeleteStatus = mm.DeleteStatus,
            }
            ).ToList();

            if (moduleMasterEntityList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Module Master not found";
            }
            return moduleMasterEntityList;
        }

        public List<FirmDetailsModel> GetFirmDetails(ref ErrorResponseModel errorResponseModel)
        {
            var firmDetailsModelList = new List<FirmDetailsModel>();
            errorResponseModel = new ErrorResponseModel();
            var firmDtailsEntityList = (
            from fd in context.FirmDetails
            select new FirmDetailsModel
            {
                FirmId = fd.FirmId,
                FirmName = fd.FirmName,
                FirmNameMarathi = fd.FirmNameMarathi,
                FirmRegNumber = fd.FirmRegNumber,
                FirmRegDate = fd.FirmRegDate,
                FirmBranchName = fd.FirmBranchName,
                FirmBranchNameMarathi = fd.FirmBranchNameMarathi,
                FirmOfficeAddress = fd.FirmOfficeAddress,
                FirmOfficeAddressMarathi = fd.FirmOfficeAddressMarathi,
                FirmLogo = fd.FirmLogo,
                FirmPhoneNumber = fd.FirmPhoneNumber,
                FirmFaxNumber = fd.FirmFaxNumber,
                FirmEmailIid = fd.FirmEmailIid,
                MailPassword = fd.MailPassword,
                IsFederation = fd.IsFederation,
                FirmConnectionPath = fd.FirmConnectionPath,
                LanguageIds = fd.LanguageIds,
                ParentFirmId = fd.ParentFirmId,
                ModuleIds = fd.ModuleIds,
                UserLimit = fd.UserLimit,
                IsNeedToBeSingleTerminalLogin = fd.IsNeedToBeSingleTerminalLogin,
                DatabaseBackupPath = fd.DatabaseBackupPath,
                IsDateOverlap = fd.IsDateOverlap,
                ApplicationLockDate = fd.ApplicationLockDate,
                IsLockApplication = fd.IsLockApplication,
                IsSyncStaring = fd.IsSyncStaring,
                EnteredDate = fd.EnteredDate,
                EnteredBy = fd.EnteredBy,
                ChangedBy = fd.ChangedBy,
                ChangedDate = fd.ChangedDate,
                DeleteStatus = fd.DeleteStatus,
            }
            ).ToList();

            if (firmDtailsEntityList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Firm Details not found";
            }
            return firmDtailsEntityList;
        }

        public List<CaseEntryChiefComplaintModel> getAllChiefComplaints(ref ErrorResponseModel errorResponseModel)
        {
            var listCaseEntryChiefComplaintModel = new List<CaseEntryChiefComplaintModel>();
            errorResponseModel = new ErrorResponseModel();
            var listCaseEntryChiefComplaintEntity = (
            from c in context.CaseEntryChiefComplaints
            select new CaseEntryChiefComplaintModel
            {
                ChiefComplaintName = c.ChiefComplaintName,
            }
            ).Distinct().ToList();

            if (listCaseEntryChiefComplaintEntity.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Chief Complaints not found";
            }
            return listCaseEntryChiefComplaintEntity;
        }

        public List<SubSectionModel> GetSubSectionByBodyPart(long bodyPartId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var subsectionModelList = (
            from ss in context.SubSectionMasters
            where ss.BodyPartId == bodyPartId
            select new SubSectionModel
            {
                SubSectionId = ss.SubSectionId,
                ChangedBy = ss.ChangedBy,
                ChangedDate = ss.ChangedDate,
                DeleteStatus = ss.DeleteStatus,
                Description = ss.Description,
                EnteredBy = ss.EnteredBy,
                EnteredDate = ss.EnteredDate,
                SectionId = ss.SectionId,
                ParentSubSectionId = ss.ParentSubSectionId,
                SubSectionName = ss.SubSectionName,
                SubSectionNameAlias = ss.SubSectionNameAlias,
            }
            ).ToList();

            if (subsectionModelList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Sub section not found";
            }
            return subsectionModelList;
        }

        public List<DoctorModel> GetDoctorById(long userId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var doctorModelList = (
            from d in context.Doctors
            where d.UserId == userId && !d.DeleteStatus
            select new DoctorModel
            {
                FirstName = d.FirstName,
                MiddleName = d.MiddleName,
                LastName = d.LastName,
                DoctorName = d.FirstName + " " + d.LastName,
                CasePaperValidity = d.CasePaperValidity,
                City = d.City,
                DoctorID = d.DoctorId,
                EmailId = d.EmailId,
                MobileNo = d.MobileNo,
                PackageId = d.PackageId,
                PassingCertNo = d.PassingCertNo,
                PassingUniversity = d.PassingUniversity,
                PermanantAddress = d.PermanantAddress,
                QualificationID = d.QualificationId,
                UserId = d.UserId,
            }
            ).ToList();

            if (doctorModelList.Count == 0)
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "Doctors not found";
            }
            return doctorModelList;
        }

       

        public List<SubSectionModel> GetSubSectionByBodyPart(string subSectionName, ref ErrorResponseModel errorResponseModel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// M02 W7 ADM-B04 — Restore GetMenuByRole: menus for the role assigned to userId.
        /// Does not seed Account/Pharmacy menus (see Database/Scripts/M02_W7_MenuMaster_Account_Pharmacy_Seed.sql).
        /// </summary>
        public List<MenuMasterModel> GetMenuByRole(long userId, ref ErrorResponseModel errorResponseModel)
            => GetMenuByRole(userId, jwtRoleName: null, inspectOtherUser: false, ref errorResponseModel);

        public List<MenuMasterModel> GetMenuByRole(long userId, string jwtRoleName, bool inspectOtherUser, ref ErrorResponseModel errorResponseModel)
        {
            var menuModelList = new List<MenuMasterModel>();
            errorResponseModel = new ErrorResponseModel();

            int? roleId = null;

            // Reception JWT NameIdentifier is DoctorReceptionStaff id, not UserMaster.
            // If we resolved menus from the linked doctor UserId we would leak case-taking items.
            var isReceptionJwt = !inspectOtherUser
                && !string.IsNullOrWhiteSpace(jwtRoleName)
                && jwtRoleName.Equals("Reception", StringComparison.OrdinalIgnoreCase);

            if (isReceptionJwt)
            {
                roleId = context.RoleMasters.AsNoTracking()
                    .Where(r => r.RoleName == "Reception" && !r.DeleteStatus)
                    .Select(r => (int?)r.RoleId)
                    .FirstOrDefault();
            }
            else
            {
                var user = context.UserMasters.AsNoTracking()
                    .FirstOrDefault(x => x.UserId == userId && !x.DeleteStatus);

                if (user == null || user.RoleId == null)
                {
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                    errorResponseModel.Message = "User or role not found";
                    return menuModelList;
                }

                roleId = user.RoleId.Value;
            }

            if (roleId == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "User or role not found";
                return menuModelList;
            }
            var menuEntityList = context.RoleDetails
                .AsNoTracking()
                .Where(x => x.RoleId == roleId && x.IsView)
                .Include(x => x.Menu)
                .Where(x => x.Menu != null && !x.Menu.DeleteStatus && x.Menu.ShowInMainMenu)
                .OrderBy(x => x.Menu.SeqNo)
                .ToList();

            if (menuEntityList.Count == 0)
            {
                // Mapped user/role exists but has no visible RoleDetails. Contract is 200 []
                // (missing UserMaster is 404; unauthenticated is 401).
                return menuModelList;
            }

            foreach (var item in menuEntityList)
            {
                var menu = item.Menu;
                menuModelList.Add(new MenuMasterModel
                {
                    MenuId = menu.MenuId,
                    ModuleId = menu.ModuleId,
                    MenuName = menu.MenuName,
                    MenuNameMarathi = menu.MenuNameMarathi,
                    MenuType = menu.MenuType,
                    ParentMenuId = menu.ParentMenuId,
                    MenuUrl = menu.MenuUrl,
                    Description = menu.Description,
                    MenuIcon = menu.MenuIcon,
                    ActionName = menu.ActionName,
                    ControllerName = menu.ControllerName,
                    IsLeaf = menu.IsLeaf,
                    ShowInMainMenu = menu.ShowInMainMenu,
                    SeqNo = menu.SeqNo,
                    FirmIds = menu.FirmIds,
                    DeleteStatus = menu.DeleteStatus
                });
            }

            return menuModelList;
        }

        /// <summary>
        /// Method is used for get all the subsection by bodypartId
        /// </summary>
        /// <param name="bodyPartId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        // public PaginationResult GetSubSectionBySearch(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        // {
        //     var subsectionModelList = new List<SubSectionModel>();
        //     errorResponseModel = new ErrorResponseModel();
        //     var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
        //     var pageSize = 10;
        //     var totalRecords = 0.0;
        //     var totalPages = 0.0;
        //     var skip = 0;

        //     if (!String.IsNullOrEmpty(queryString))
        //     {
        //         subsectionModelList = (from subsectionMaster in context.SubSectionMaster
        //                                where subsectionMaster.SubSectionName.ToLower().Contains(queryString.ToLower()) && subsectionMaster.DeleteStatus == false
        //                                select new SubSectionModel{
        //                                     SubSectionName=  subsectionMaster.SubSectionName,
        //                                     SubSectionId=  subsectionMaster.SubSectionId,
        //                                }).ToList();
        //     }
        //     else
        //     {
        //         //totalRecords = context.Customermerchantmappers.Count(x => x.MerchantId == customerSearchModel.MerchantID);
        //         //totalPages = Math.Ceiling((double)totalRecords / pageSize);
        //         //skip = (pageNumber - 1) * pageSize;
        //     }

        //     if (subsectionModelList.Count == 0)
        //     {
        //         errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //         errorResponseModel.Message = "Sub section not found";
        //     }
        //     totalRecords = subsectionModelList.Count;
        //     totalPages = Math.Ceiling((double)totalRecords / pageSize);
        //     skip = (pageNumber - 1) * pageSize;

        //     var result = new PaginationResult();
        //     result.TotalPageCount = totalPages;
        //     result.TotalCount = totalRecords;
        //     result.ResultObject=subsectionModelList.Skip(skip).Take(pageSize);
        //     return result;
        // }

        // /// <summary>
        // /// Method to get all the subsections
        // /// </summary>
        // /// <param name="errorResponseModel"></param>
        // /// <returns></returns>
        // public PaginationResult GetSubsectionBySectionWithPagination(int sectionId,string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        // {
        //     var subsectionModelList = new List<SubSectionViewModel>();
        //     errorResponseModel = new ErrorResponseModel();

        //     var pageNumber = (nigaParameters.PageNumber <= 0) ? 1 : nigaParameters.PageNumber;
        //     var pageSize = 10;
        //     var totalRecords = 0.0;
        //     var totalPages = 0.0;
        //     var skip = 0;

        //     if (!String.IsNullOrEmpty(queryString))
        //     {
        //         subsectionModelList = (from subsectionMaster in context.SubSectionMaster
        //                                where subsectionMaster.SubSectionName.ToLower().Contains(queryString.ToLower()) && subsectionMaster.DeleteStatus == false
        //                                select new SubSectionViewModel
        //                                {
        //                                    SubSectionId = subsectionMaster.SubSectionId,
        //                                    SectionId = subsectionMaster.SectionId,
        //                                    ParentSubSectionId = subsectionMaster.ParentSubSectionId,
        //                                    SubSectionName = subsectionMaster.SubSectionName,
        //                                    SubSectionNameAlias = subsectionMaster.SubSectionNameAlias,
        //                                    Description = subsectionMaster.Description,
        //                                }).ToList();
        //     }
        //     else
        //     {
        //         subsectionModelList = (from subsectionMaster in context.SubSectionMaster
        //                                where subsectionMaster.SectionId==sectionId && subsectionMaster.DeleteStatus == false
        //                                select new SubSectionViewModel
        //                                {
        //                                    SubSectionId = subsectionMaster.SubSectionId,
        //                                    SectionId = subsectionMaster.SectionId,
        //                                    ParentSubSectionId = subsectionMaster.ParentSubSectionId,
        //                                    SubSectionName = subsectionMaster.SubSectionName,
        //                                    SubSectionNameAlias = subsectionMaster.SubSectionNameAlias,
        //                                    Description = subsectionMaster.Description,
        //                                }).ToList();
        //     }

        //     if (subsectionModelList.Count == 0)
        //     {
        //         errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //         errorResponseModel.Message = "Sub section not found";
        //     }
        //     totalRecords = subsectionModelList.Count;
        //     totalPages = Math.Ceiling((double)totalRecords / pageSize);
        //     skip = (pageNumber - 1) * pageSize;

        //     var result = new PaginationResult();
        //     result.TotalPageCount = totalPages;
        //     result.TotalCount = totalRecords;
        //     result.ResultObject = subsectionModelList.Skip(skip).Take(pageSize);
        //     return result;
        // }
    }
}

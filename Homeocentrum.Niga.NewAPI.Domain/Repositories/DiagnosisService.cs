#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Implementation
{
    public class DiagnosisService : IDiagnosisService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public DiagnosisService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.NewAPI.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Methood to get diagnosis by DiagnosisId
        /// </summary>
        /// <param name="diagnosisId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public DiagnosisModel GetDiagnosisById(long diagnosisId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            DiagnosisModel diagnosisModel = new DiagnosisModel();
            var diagnosisEntity = context.DiagnosisMasters.FirstOrDefault(x => x.DiagnosisId == diagnosisId && !x.DeleteStatus);

            var dignosisdetail = (from detail in context.DiagnosisDetails
                                  join
                                  sub in context.SubSectionMasters on detail.SubSectionId equals sub.SubSectionId
                                  where detail.DiagnosisId == diagnosisId && detail.DeleteStatus == false
                                  select new
                                  {
                                      detail.DiagnosisDetailId,
                                      detail.DiagnosisId,
                                      detail.SubSectionId,
                                      sub.SubSectionName
                                  }
                                ).Distinct().ToList();
            if (diagnosisEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Diagnosis not found";
            }


            diagnosisModel.DiagnosisId = diagnosisEntity.DiagnosisId;
            diagnosisModel.DiagnosisGroupId = diagnosisEntity.DiagnosisGroupId;
            diagnosisModel.DiagnosisName = diagnosisEntity.DiagnosisName;
            diagnosisModel.DiagnosisNameAlias = diagnosisEntity.DiagnosisNameAlias;
            diagnosisModel.Description = diagnosisEntity.Description;
            diagnosisModel.Keywords = diagnosisEntity.Keywords;
            diagnosisModel.Investigations = diagnosisEntity.Investigations;
            diagnosisModel.Miasm = diagnosisEntity.Miasm;
            diagnosisModel.AllopathicMedicines = diagnosisEntity.AllopathicMedicines;
            diagnosisModel.Examiniations = diagnosisEntity.Examiniations;
            diagnosisModel.EnteredDate = diagnosisEntity.EnteredDate;
            diagnosisModel.EnteredBy = diagnosisEntity.EnteredBy;
            diagnosisModel.ChangedBy = diagnosisEntity.ChangedBy;
            diagnosisModel.ChangedDate = diagnosisEntity.ChangedDate;
            diagnosisModel.DeleteStatus = diagnosisEntity.DeleteStatus;
            if (dignosisdetail != null)
            {
                foreach (var item in dignosisdetail)
                {
                    DignosisDetailModel detail = new DignosisDetailModel();
                    detail.DiagnosisDetailId = item.DiagnosisDetailId;
                    detail.DiagnosisId = item.DiagnosisId;
                    detail.SubSectionId = item.SubSectionId;
                    detail.SubsectionName = item.SubSectionName;
                    diagnosisModel.ModelEx.Add(detail);
                }
            }

            //diagnosisModel.diagnosisMonogramsList = GetDiagnosisMonogramsList(diagnosisId);
            //diagnosisModel.diagnosisPathologyList = GetDiagnosisPathology(diagnosisId);
            diagnosisModel.diagnosisSystemDetailsList = GetDiagnosisSystemDetails(diagnosisId);
            diagnosisModel.emergencieDetailsModelList = GetEmergencieDetails(diagnosisId);
            diagnosisModel.OnsetDurationProgressDetails = GetOnsetDurationProgressDetails(diagnosisId);
            diagnosisModel.PatternsDetails = GetPatternsDetailEntity(diagnosisId);
            diagnosisModel.LocationExtentionDetailsModelList = GetLocationExtentionDetailsEntity(diagnosisId);
            diagnosisModel.sensationDetailsModelList = GetSensationDetailspEntity(diagnosisId);
            diagnosisModel.modalitiesDetailsModelsList = GetModalitiesDetailsEntity(diagnosisId);
            diagnosisModel.accompaniedDetailsModelsList = GetAccompaniedDetailsEntity(diagnosisId);
            diagnosisModel.observationsDetailsModelsList = GetObservationsDetailsEntity(diagnosisId);
            diagnosisModel.beforeAfterDuringDetailsModelsList = GetBeforeAfterDuringDetailsEntity(diagnosisId);
            diagnosisModel.diagnosisSymptomsList = GetDiagnosisSymptomsEntity(diagnosisId);
            diagnosisModel.diagnosisMonogramDetailsModelsList = GetDiagnosisMonogramDetailsEntity(diagnosisId);
            diagnosisModel.diagnosisCausationList = GetDiagnosisCausationEntity(diagnosisId);
            diagnosisModel.diagnosisPathologyDetailsModelsList = GetDiagnosisPathologyDetailsModelsEntity(diagnosisId);

            return diagnosisModel;

        }

        private List<ObservationsDetailsModel> GetObservationsDetailsEntity(long diagnosisId)
        {
            List<ObservationsDetailsModel> observationsDetailsModelsList = new List<ObservationsDetailsModel>();
            var observationsDetailsEntity = (from observationsDetails in context.ObservationsDetails
                                             where observationsDetails.DiagnosisId == diagnosisId && observationsDetails.DeletedStatus == false
                                             select new ObservationsDetailsModel
                                             {
                                                 ObservationsDetailsId = observationsDetails.ObservationsDetailsId,
                                                 ObservationsDetailsKeyword = observationsDetails.ObservationsDetailsKeyword,
                                                 DiagnosisId = Convert.ToInt32(observationsDetails.DiagnosisId),
                                                 DeletedStatus = observationsDetails.DeletedStatus,
                                             }
                            ).Distinct().ToList();
            if (observationsDetailsEntity != null)
            {
                for (int i = 0; i < observationsDetailsEntity.Count; i++)
                {
                    var observationsDetailsItem = observationsDetailsEntity[i];
                    var observationsRubricDetailsEntity = (from observationsRubricDetails in context.ObservationsRubricDetails
                                                           join subSectionMaster in context.SubSectionMasters on observationsRubricDetails.Subsection equals subSectionMaster.SubSectionId
                                                           join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                           where observationsRubricDetails.ObservationsDetailsId == observationsDetailsItem.ObservationsDetailsId && observationsRubricDetails.DeletedStatus == false
                                                           select new ObservationsRubricDetailsModel
                                                           {
                                                               ObservationsRubricDetailsId = observationsRubricDetails.ObservationsRubricDetailsId,
                                                               ObservationsDetailsId = observationsRubricDetails.ObservationsDetailsId,
                                                               Subsection = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                               SubsectionName = subSectionMaster.SubSectionName,
                                                               SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                               SectionName = sectionMaster.SectionName,
                                                               DeletedStatus = Convert.ToBoolean(observationsRubricDetails.DeletedStatus)
                                                           }
                                                        ).ToList();
                    observationsDetailsEntity[i].ObservationsRubricDetails = observationsRubricDetailsEntity;
                }
                observationsDetailsModelsList = observationsDetailsEntity;
                AttachDiagnosisKeywordSections("Observations",
                    observationsDetailsModelsList.Select(x => x.ObservationsDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = observationsDetailsModelsList.FirstOrDefault(x => x.ObservationsDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return observationsDetailsModelsList;
        }

        private List<BeforeAfterDuringDetailsModel> GetBeforeAfterDuringDetailsEntity(long diagnosisId)
        {
            List<BeforeAfterDuringDetailsModel> beforeAfterDuringDetailsModelsList = new List<BeforeAfterDuringDetailsModel>();
            var beforeAfterDuringDetailsEntity = (from beforeAfterDuringDetails in context.BeforeAfterDuringDetails
                                                  where beforeAfterDuringDetails.DiagnosisId == diagnosisId && beforeAfterDuringDetails.DeletedStatus == false
                                                  select new BeforeAfterDuringDetailsModel
                                                  {
                                                      BeforeAfterDuringDetailsId = beforeAfterDuringDetails.BeforeAfterDuringDetailsId,
                                                      BeforeAfterDuringDetailsKeyword = beforeAfterDuringDetails.BeforeAfterDuringDetailsKeyword,
                                                      DiagnosisId = Convert.ToInt32(beforeAfterDuringDetails.DiagnosisId),
                                                      DeletedStatus = beforeAfterDuringDetails.DeletedStatus,
                                                  }
                            ).Distinct().ToList();
            if (beforeAfterDuringDetailsEntity != null)
            {
                for (int i = 0; i < beforeAfterDuringDetailsEntity.Count; i++)
                {
                    var beforeAfterDuringDetailsItem = beforeAfterDuringDetailsEntity[i];
                    var beforeAfterDuringRubricDetailsEntity = (from beforeAfterDuringRubricDetails in context.BeforeAfterDuringRubricDetails
                                                                join subSectionMaster in context.SubSectionMasters on beforeAfterDuringRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                                join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                                where beforeAfterDuringRubricDetails.BeforeAfterDuringDetailsId == beforeAfterDuringDetailsItem.BeforeAfterDuringDetailsId && beforeAfterDuringRubricDetails.DeletedStatus == false
                                                                select new BeforeAfterDuringRubricDetailsModel
                                                                {
                                                                    BeforeAfterDuringRubricDetailsId = beforeAfterDuringRubricDetails.BeforeAfterDuringRubricDetailsId,
                                                                    BeforeAfterDuringDetailsId = beforeAfterDuringRubricDetails.BeforeAfterDuringDetailsId,
                                                                    SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                                    SubsectionName = subSectionMaster.SubSectionName,
                                                                    SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                                    SectionName = sectionMaster.SectionName,
                                                                    DeletedStatus = Convert.ToBoolean(beforeAfterDuringRubricDetails.DeletedStatus)
                                                                }
                                                        ).ToList();
                    beforeAfterDuringDetailsEntity[i].BeforeAfterDuringRubricDetails = beforeAfterDuringRubricDetailsEntity;
                }
                beforeAfterDuringDetailsModelsList = beforeAfterDuringDetailsEntity;
                AttachDiagnosisKeywordSections("BeforeAfterDuring",
                    beforeAfterDuringDetailsModelsList.Select(x => x.BeforeAfterDuringDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = beforeAfterDuringDetailsModelsList.FirstOrDefault(x => x.BeforeAfterDuringDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return beforeAfterDuringDetailsModelsList;
        }

        private List<DiagnosisSymptomsModel> GetDiagnosisSymptomsEntity(long diagnosisId)
        {
            List<DiagnosisSymptomsModel> diagnosisSymptomsModelsList = new List<DiagnosisSymptomsModel>();
            var diagnosisSymptomsEntity = (from diagnosisSymptoms in context.DiagnosisSymptoms
                                           where diagnosisSymptoms.DiagnosisId == diagnosisId && diagnosisSymptoms.DeletedStatus == false
                                           select new DiagnosisSymptomsModel
                                           {
                                               DiagnosisSymptomId = diagnosisSymptoms.DiagnosisSymptomId,
                                               Symptom = diagnosisSymptoms.Symptom,
                                               DiagnosisId = Convert.ToInt32(diagnosisSymptoms.DiagnosisId),
                                               EnteredBy = diagnosisSymptoms.EnteredBy,
                                           }
                            ).Distinct().ToList();
            if (diagnosisSymptomsEntity != null)
            {
                for (int i = 0; i < diagnosisSymptomsEntity.Count; i++)
                {
                    var diagnosisSymptomsItem = diagnosisSymptomsEntity[i];
                    var diagnosisSymptomRubricEntity = (from diagnosisSymptomRubric in context.DiagnosisSymptomRubrics
                                                        join subSectionMaster in context.SubSectionMasters on diagnosisSymptomRubric.SubsectionId equals subSectionMaster.SubSectionId
                                                        join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                        where diagnosisSymptomRubric.DiagnosisSymptomId == diagnosisSymptomsItem.DiagnosisSymptomId && diagnosisSymptomRubric.DeletedStatus == false
                                                        select new DiagnosisSymptomRubricModel
                                                        {
                                                            DiagnosisSymptomRubricId = diagnosisSymptomRubric.DiagnosisSymptomRubricId,
                                                            DiagnosisSymptomId = diagnosisSymptomRubric.DiagnosisSymptomId,
                                                            SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                            SubsectionName = subSectionMaster.SubSectionName,
                                                            SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                            SectionName = sectionMaster.SectionName,
                                                            DeletedStatus = Convert.ToBoolean(diagnosisSymptomRubric.DeletedStatus)
                                                        }
                                                        ).ToList();
                    diagnosisSymptomsEntity[i].DiagnosisSymptomRubric = diagnosisSymptomRubricEntity;
                }
                diagnosisSymptomsModelsList = diagnosisSymptomsEntity;
                AttachDiagnosisKeywordSections("Symptoms",
                    diagnosisSymptomsModelsList.Select(x => x.DiagnosisSymptomId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = diagnosisSymptomsModelsList.FirstOrDefault(x => x.DiagnosisSymptomId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return diagnosisSymptomsModelsList;
        }

        private List<DiagnosisMonogramDetailsModel> GetDiagnosisMonogramDetailsEntity(long diagnosisId)
        {
            List<DiagnosisMonogramDetailsModel> diagnosisMonogramDetailsModelsList = new List<DiagnosisMonogramDetailsModel>();
            var diagnosisMonogramDetailsEntity = (from diagnosisMonogramDetails in context.DiagnosisMonogramDetails
                                                  where diagnosisMonogramDetails.DiagnosisId == diagnosisId && diagnosisMonogramDetails.DeletedStatus == false
                                                  select new DiagnosisMonogramDetailsModel
                                                  {
                                                      DiagnosisMonogramDetailsId = diagnosisMonogramDetails.DiagnosisMonogramDetailsId,
                                                      DiagnosisMonogramKeyword = diagnosisMonogramDetails.DiagnosisMonogramKeyword,
                                                      DiagnosisId = Convert.ToInt32(diagnosisMonogramDetails.DiagnosisId),
                                                      DeletedStatus = diagnosisMonogramDetails.DeletedStatus,
                                                  }
                            ).Distinct().ToList();
            if (diagnosisMonogramDetailsEntity != null)
            {
                for (int i = 0; i < diagnosisMonogramDetailsEntity.Count; i++)
                {
                    var diagnosisMonogramDetailsItem = diagnosisMonogramDetailsEntity[i];
                    var diagnosisCausationRubricDetailsEntity = (from diagnosisMonogramRubricDetails in context.DiagnosisMonogramRubricDetails
                                                                 join subSectionMaster in context.SubSectionMasters on diagnosisMonogramRubricDetails.Subsections equals subSectionMaster.SubSectionId
                                                                 join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                                 where diagnosisMonogramRubricDetails.DiagnosisMonogramDetailsId == diagnosisMonogramDetailsItem.DiagnosisMonogramDetailsId && diagnosisMonogramRubricDetails.DeletedStatus == false
                                                                 select new DiagnosisMonogramRubricDetailsModel
                                                                 {
                                                                     DiagnosisMonogramRubricDetailsId = diagnosisMonogramRubricDetails.DiagnosisMonogramRubricDetailsId,
                                                                     DiagnosisMonogramDetailsId = diagnosisMonogramRubricDetails.DiagnosisMonogramDetailsId,
                                                                     Subsections = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                                     SubsectionName = subSectionMaster.SubSectionName,
                                                                     SectionsId = Convert.ToInt32(subSectionMaster.SectionId),
                                                                     SectionsName = sectionMaster.SectionName,
                                                                     DeletedStatus = Convert.ToBoolean(diagnosisMonogramRubricDetails.DeletedStatus)
                                                                 }
                                                        ).ToList();
                    diagnosisMonogramDetailsEntity[i].DiagnosisMonogramRubricDetails = diagnosisCausationRubricDetailsEntity;
                }
                diagnosisMonogramDetailsModelsList = diagnosisMonogramDetailsEntity;
                AttachDiagnosisKeywordSections("Monogram",
                    diagnosisMonogramDetailsModelsList.Select(x => x.DiagnosisMonogramDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = diagnosisMonogramDetailsModelsList.FirstOrDefault(x => x.DiagnosisMonogramDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return diagnosisMonogramDetailsModelsList;
        }

        private List<DiagnosisCausationModel> GetDiagnosisCausationEntity(long diagnosisId)
        {
            List<DiagnosisCausationModel> diagnosisCausationModelsList = new List<DiagnosisCausationModel>();
            var diagnosisCausationEntity = (from diagnosisCausation in context.DiagnosisCausations
                                            where diagnosisCausation.DiagnosisId == diagnosisId
                                            select new DiagnosisCausationModel
                                            {
                                                CausationId = diagnosisCausation.CausationId,
                                                CausationName = diagnosisCausation.CausationName,
                                                DiagnosisId = Convert.ToInt32(diagnosisCausation.DiagnosisId),
                                            }
                            ).Distinct().ToList();
            if (diagnosisCausationEntity != null)
            {
                for (int i = 0; i < diagnosisCausationEntity.Count; i++)
                {
                    var diagnosisCausationItem = diagnosisCausationEntity[i];
                    var diagnosisCausationRubricDetailsEntity = (from diagnosisCausationRubricDetails in context.DiagnosisCausationRubricDetails
                                                                 join subSectionMaster in context.SubSectionMasters on diagnosisCausationRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                                 join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                                 where diagnosisCausationRubricDetails.CausationId == diagnosisCausationItem.CausationId && diagnosisCausationRubricDetails.DeletedStatus == false
                                                                 select new DiagnosisCausationRubricDetailsModel
                                                                 {
                                                                     CausationRubricDetailsId = diagnosisCausationRubricDetails.CausationRubricDetailsId,
                                                                     CausationId = diagnosisCausationRubricDetails.CausationId,
                                                                     SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                                     SubsectionName = subSectionMaster.SubSectionName,
                                                                     SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                                     SectionName = sectionMaster.SectionName,
                                                                     DeletedStatus = Convert.ToBoolean(diagnosisCausationRubricDetails.DeletedStatus)
                                                                 }
                                                        ).ToList();
                    diagnosisCausationEntity[i].DiagnosisCausationRubricDetails = diagnosisCausationRubricDetailsEntity;
                }
                diagnosisCausationModelsList = diagnosisCausationEntity;
                AttachDiagnosisKeywordSections("Causations",
                    diagnosisCausationModelsList.Select(x => x.CausationId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = diagnosisCausationModelsList.FirstOrDefault(x => x.CausationId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return diagnosisCausationModelsList;
        }

        private List<DiagnosisPathologyDetailsModel> GetDiagnosisPathologyDetailsModelsEntity(long diagnosisId)
        {
            List<DiagnosisPathologyDetailsModel> diagnosisPathologyDetailsModelList = new List<DiagnosisPathologyDetailsModel>();
            var diagnosisPathologyDetailsEntity = (from diagnosisPathologyDetails in context.DiagnosisPathologyDetails
                                                   where diagnosisPathologyDetails.DiagnosisId == diagnosisId && diagnosisPathologyDetails.DeletedStatus == false
                                                   select new DiagnosisPathologyDetailsModel
                                                   {
                                                       DiagnosisPathologyDetailsId = diagnosisPathologyDetails.DiagnosisPathologyDetailsId,
                                                       DiagnosisPathologyKeyword = diagnosisPathologyDetails.DiagnosisPathologyKeyword,
                                                       DiagnosisId = Convert.ToInt32(diagnosisPathologyDetails.DiagnosisId),
                                                       DeletedStatus = diagnosisPathologyDetails.DeletedStatus,
                                                   }
                            ).Distinct().ToList();
            if (diagnosisPathologyDetailsEntity != null)
            {
                for (int i = 0; i < diagnosisPathologyDetailsEntity.Count; i++)
                {
                    var diagnosisPathologyDetailsItem = diagnosisPathologyDetailsEntity[i];
                    var diagnosisPathologyRubricDetailsEntity = (from diagnosisPathologyRubricDetails in context.DiagnosisPathologyRubricDetails
                                                                 join subSectionMaster in context.SubSectionMasters on diagnosisPathologyRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                                 join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                                 where diagnosisPathologyRubricDetails.DiagnosisPathologyDetailsId == diagnosisPathologyDetailsItem.DiagnosisPathologyDetailsId && diagnosisPathologyRubricDetails.DeletedStatus == false
                                                                 select new DiagnosisPathologyRubricDetailsModel
                                                                 {
                                                                     DiagnosisPathologyRubricDetailsId = diagnosisPathologyRubricDetails.DiagnosisPathologyRubricDetailsId,
                                                                     DiagnosisPathologyDetailsId = diagnosisPathologyRubricDetails.DiagnosisPathologyDetailsId,
                                                                     SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                                     SubsectionName = subSectionMaster.SubSectionName,
                                                                     SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                                     SectionName = sectionMaster.SectionName,
                                                                     DeletedStatus = Convert.ToBoolean(diagnosisPathologyRubricDetails.DeletedStatus)
                                                                 }
                                                        ).ToList();
                    diagnosisPathologyDetailsEntity[i].DiagnosisPathologyRubricDetails = diagnosisPathologyRubricDetailsEntity;
                }
                diagnosisPathologyDetailsModelList = diagnosisPathologyDetailsEntity;
                AttachDiagnosisKeywordSections("Pathology",
                    diagnosisPathologyDetailsModelList.Select(x => x.DiagnosisPathologyDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = diagnosisPathologyDetailsModelList.FirstOrDefault(x => x.DiagnosisPathologyDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return diagnosisPathologyDetailsModelList;
        }

        private List<AccompaniedDetailsModel> GetAccompaniedDetailsEntity(long diagnosisId)
        {
            List<AccompaniedDetailsModel> accompaniedDetailsModelsList = new List<AccompaniedDetailsModel>();
            var accompaniedDetailsEntity = (from accompaniedDetails in context.AccompaniedDetails
                                            where accompaniedDetails.DiagnosisId == diagnosisId && accompaniedDetails.DeletedStatus == false
                                            select new AccompaniedDetailsModel
                                            {
                                                AccompaniedDetailsId = accompaniedDetails.AccompaniedDetailsId,
                                                AccompaniedDetailsSystem = accompaniedDetails.AccompaniedDetailsSystem,
                                                DiagnosisId = Convert.ToInt32(accompaniedDetails.DiagnosisId),
                                                DeletedStatus = accompaniedDetails.DeletedStatus,
                                            }
                            ).Distinct().ToList();
            if (accompaniedDetailsEntity != null)
            {
                for (int i = 0; i < accompaniedDetailsEntity.Count; i++)
                {
                    var accompaniedDetailsItem = accompaniedDetailsEntity[i];
                    var accompaniedRubricDetailsEntity = (from accompaniedRubricDetails in context.AccompaniedRubricDetails
                                                          join subSectionMaster in context.SubSectionMasters on accompaniedRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                          join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                          where accompaniedRubricDetails.AccompaniedDetailsId == accompaniedDetailsItem.AccompaniedDetailsId && accompaniedRubricDetails.DeletedStatus == false
                                                          select new AccompaniedRubricDetailsModel
                                                          {
                                                              AccompaniedRubricDetailsId = accompaniedRubricDetails.AccompaniedRubricDetailsId,
                                                              AccompaniedDetailsId = accompaniedRubricDetails.AccompaniedDetailsId,
                                                              SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                              SubsectionName = subSectionMaster.SubSectionName,
                                                              SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                              SectionName = sectionMaster.SectionName,
                                                              DeletedStatus = Convert.ToBoolean(accompaniedRubricDetails.DeletedStatus)
                                                          }
                                                        ).ToList();
                    accompaniedDetailsEntity[i].AccompaniedRubricDetails = accompaniedRubricDetailsEntity;
                }
                accompaniedDetailsModelsList = accompaniedDetailsEntity;
                AttachDiagnosisKeywordSections("Accompanied",
                    accompaniedDetailsModelsList.Select(x => x.AccompaniedDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = accompaniedDetailsModelsList.FirstOrDefault(x => x.AccompaniedDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return accompaniedDetailsModelsList;
        }

        private List<ModalitiesDetailsModel> GetModalitiesDetailsEntity(long diagnosisId)
        {
            List<ModalitiesDetailsModel> modalitiesDetailsModelsList = new List<ModalitiesDetailsModel>();
            var modalitiesDetailsEntity = (from modalitiesDetails in context.ModalitiesDetails
                                           where modalitiesDetails.DiagnosisId == diagnosisId && modalitiesDetails.DeletedStatus == false
                                           select new ModalitiesDetailsModel
                                           {
                                               ModalitiesDetailsId = modalitiesDetails.ModalitiesDetailsId,
                                               ModalitiesDetailsKeyword = modalitiesDetails.ModalitiesDetailsKeyword,
                                               DiagnosisId = Convert.ToInt32(modalitiesDetails.DiagnosisId),
                                               DeletedStatus = modalitiesDetails.DeletedStatus,
                                           }
                            ).Distinct().ToList();
            if (modalitiesDetailsEntity != null)
            {
                for (int i = 0; i < modalitiesDetailsEntity.Count; i++)
                {
                    var modalitiesDetailsItem = modalitiesDetailsEntity[i];
                    var modalitiesRubricDetailsEntity = (from modalitiesRubricDetails in context.ModalitiesRubricDetails
                                                         join subSectionMaster in context.SubSectionMasters on modalitiesRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                         join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                         where modalitiesRubricDetails.ModalitiesDetailsId == modalitiesDetailsItem.ModalitiesDetailsId && modalitiesRubricDetails.DeletedStatus == false
                                                         select new ModalitiesRubricDetailsModel
                                                         {
                                                             ModalitiesRubricDetailsId = modalitiesRubricDetails.ModalitiesRubricDetailsId,
                                                             ModalitiesDetailsId = modalitiesRubricDetails.ModalitiesDetailsId,
                                                             SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                             SubsectionName = subSectionMaster.SubSectionName,
                                                             SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                             SectionName = sectionMaster.SectionName,
                                                             DeletedStatus = Convert.ToBoolean(modalitiesRubricDetails.DeletedStatus)
                                                         }
                                                        ).ToList();
                    modalitiesDetailsEntity[i].ModalitiesRubricDetails = modalitiesRubricDetailsEntity;
                }
                modalitiesDetailsModelsList = modalitiesDetailsEntity;
                AttachDiagnosisKeywordSections("Modalities",
                    modalitiesDetailsModelsList.Select(x => x.ModalitiesDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = modalitiesDetailsModelsList.FirstOrDefault(x => x.ModalitiesDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return modalitiesDetailsModelsList;
        }

        private List<OnsetDurationProgressDetailsModel> GetOnsetDurationProgressDetails(long diagnosisId)
        {
            List<OnsetDurationProgressDetailsModel> onsetDurationProgressDetailsModelList = new List<OnsetDurationProgressDetailsModel>(); ;
            var onsetDurationProgressDetailsEntity = (from onsetDurationProgressDetails in context.OnsetDurationProgressDetails
                                                      where onsetDurationProgressDetails.DiagnosisId == diagnosisId && onsetDurationProgressDetails.DeletedStatus == false
                                                      select new OnsetDurationProgressDetailsModel
                                                      {
                                                          OnsetDetailId = onsetDurationProgressDetails.OnsetDetailId,
                                                          OnsetKeyword = onsetDurationProgressDetails.OnsetKeyword,
                                                          DiagnosisId = Convert.ToInt32(onsetDurationProgressDetails.DiagnosisId),
                                                          DeletedStatus = onsetDurationProgressDetails.DeletedStatus,
                                                      }
                           ).Distinct().ToList();
            if (onsetDurationProgressDetailsEntity != null)
            {
                for (int i = 0; i < onsetDurationProgressDetailsEntity.Count; i++)
                {
                    var onsetDurationProgressDetailsItem = onsetDurationProgressDetailsEntity[i];
                    var onsetDurationProgressRubricDetailsEntity = (from onsetDurationProgressRubricDetails in context.OnsetDurationProgressRubricDetails
                                                                    join subSectionMaster in context.SubSectionMasters on onsetDurationProgressRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                                    join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                                    where onsetDurationProgressRubricDetails.OnsetDetailId == onsetDurationProgressDetailsItem.OnsetDetailId && onsetDurationProgressRubricDetails.DeletedStatus == false
                                                                    select new OnsetDurationProgressRubricDetailsModel
                                                                    {
                                                                        OnsetRubricId = onsetDurationProgressRubricDetails.OnsetRubricId,
                                                                        OnsetDetailId = onsetDurationProgressDetailsItem.OnsetDetailId,
                                                                        SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                                        SubsectionName = subSectionMaster.SubSectionName,
                                                                        SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                                        SectionName = sectionMaster.SectionName,
                                                                        DeletedStatus = Convert.ToBoolean(onsetDurationProgressRubricDetails.DeletedStatus)
                                                                    }
                                                       ).ToList();
                    onsetDurationProgressDetailsEntity[i].OnsetDurationProgressRubricDetails = onsetDurationProgressRubricDetailsEntity;
                }
                onsetDurationProgressDetailsModelList = onsetDurationProgressDetailsEntity;
                AttachDiagnosisKeywordSections("Onset",
                    onsetDurationProgressDetailsModelList.Select(x => x.OnsetDetailId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = onsetDurationProgressDetailsModelList.FirstOrDefault(x => x.OnsetDetailId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return onsetDurationProgressDetailsModelList;
        }

        private List<EmergencieDetailsModel> GetEmergencieDetails(long diagnosisId)
        {
            List<EmergencieDetailsModel> emergencieDetailsModelList = new List<EmergencieDetailsModel>();
            var emergencieDetailsModeEntity = (from emergencieDetails in context.EmergencieDetails
                                               where emergencieDetails.DiagnosisId == diagnosisId && emergencieDetails.DeletedStatus == false
                                               select new EmergencieDetailsModel
                                               {
                                                   EmergencieId = emergencieDetails.EmergencieId,
                                                   EmergencieKeyword = emergencieDetails.EmergencieKeyword,
                                                   DiagnosisId = Convert.ToInt32(emergencieDetails.DiagnosisId),
                                                   DeletedStatus = emergencieDetails.DeletedStatus,
                                               }
                            ).Distinct().ToList();
            if (emergencieDetailsModeEntity != null)
            {
                for (int i = 0; i < emergencieDetailsModeEntity.Count; i++)
                {
                    var emergencieDetailsItem = emergencieDetailsModeEntity[i];
                    var emergencieRubricDetailsEntity = (from emergencieRubricDetails in context.EmergencieRubricDetails
                                                         join subSectionMaster in context.SubSectionMasters on emergencieRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                         join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                         where emergencieRubricDetails.EmergencieId == emergencieDetailsItem.EmergencieId && emergencieRubricDetails.DeletedStatus == false
                                                         select new EmergencieRubricDetailsModel
                                                         {
                                                             EmergencieRubricId = emergencieRubricDetails.EmergencieRubricId,
                                                             EmergencieId = emergencieRubricDetails.EmergencieRubricId,
                                                             SubsectionId = Convert.ToInt32(emergencieRubricDetails.SubsectionId),
                                                             SubsectionName = subSectionMaster.SubSectionName,
                                                             SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                             SectionName = sectionMaster.SectionName,
                                                             DeletedStatus = Convert.ToBoolean(emergencieRubricDetails.DeletedStatus)
                                                         }
                                                       ).ToList();
                    emergencieDetailsModeEntity[i].EmergencieRubricDetails = emergencieRubricDetailsEntity;
                }
                emergencieDetailsModelList = emergencieDetailsModeEntity;
                AttachDiagnosisKeywordSections("Emergencies",
                    emergencieDetailsModelList.Select(x => x.EmergencieId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = emergencieDetailsModelList.FirstOrDefault(x => x.EmergencieId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return emergencieDetailsModelList;
        }

        private List<DiagnosisSystemDetailModel> GetDiagnosisSystemDetails(long diagnosisId)
        {
            List<DiagnosisSystemDetailModel> diagnosisSystemDetailModelList = new List<DiagnosisSystemDetailModel>();
            var diagnosisSystemDetailsEntity = (from diagnosisSystemDetails in context.DiagnosisSystemDetails
                                                join
                                                diagnosisSystem in context.DiagnosisSystems on diagnosisSystemDetails.DiagnosisSystemId equals diagnosisSystem.DiagnosisSystemId
                                                where diagnosisSystemDetails.DiagnosisId == diagnosisId && diagnosisSystem.IsActive == false && diagnosisSystemDetails.DeletedStatus==false
                                                select new DiagnosisSystemDetailModel
                                                {
                                                    DiagnosisSystemDetailId = diagnosisSystemDetails.DiagnosisSystemDetailId,
                                                    DiagnosisSystemId = Convert.ToInt32(diagnosisSystemDetails.DiagnosisSystemId),
                                                    DiagnosisId = Convert.ToInt32(diagnosisSystemDetails.DiagnosisId),
                                                    DeletedStatus = diagnosisSystemDetails.DeletedStatus,
                                                    DiagnosisSystemName = diagnosisSystem.DiagnosisSystemName,
                                                    Description = diagnosisSystem.Description,
                                                }
                            ).Distinct().ToList();
            if (diagnosisSystemDetailsEntity != null)
            {
                diagnosisSystemDetailModelList = diagnosisSystemDetailsEntity;
            }
            return diagnosisSystemDetailModelList;
        }

        private List<DiagnosisPathologyModel> GetDiagnosisPathology(long diagnosisId)
        {
            List<DiagnosisPathologyModel> diagnosisPathologyModelList = new List<DiagnosisPathologyModel>();
            var diagnosisPathologyEntity = (from diagnosisPathology in context.DiagnosisPathologies
                                            join
                                            pathology in context.Pathologies on diagnosisPathology.PathologyId equals pathology.PathologyId
                                            where diagnosisPathology.DiagnosisId == diagnosisId
                                            select new DiagnosisPathologyModel
                                            {
                                                DiagnosisPathologyId = diagnosisPathology.DiagnosisPathologyId,
                                                PathologyId = Convert.ToInt32(diagnosisPathology.PathologyId),
                                                DiagnosisId = Convert.ToInt32(diagnosisPathology.DiagnosisId),
                                                PathologyName = pathology.PathologyName,
                                                Description = pathology.Description,
                                            }
                            ).Distinct().ToList();
            if (diagnosisPathologyEntity != null)
            {
                diagnosisPathologyModelList = diagnosisPathologyEntity;
            }
            return diagnosisPathologyModelList;
        }

        private List<DiagnosisMonogramsModel> GetDiagnosisMonogramsList(long diagnosisId)
        {
            List<DiagnosisMonogramsModel> diagnosisMonogramsModelsList = new List<DiagnosisMonogramsModel>();
            var diagnosisMonogramsEntity = (from diagnosisMonograms in context.DiagnosisMonograms
                                            join
                                            monogram in context.Monograms on diagnosisMonograms.MonogramId equals monogram.MonogramId
                                            where diagnosisMonograms.DiagnosisId == diagnosisId
                                            select new DiagnosisMonogramsModel
                                            {
                                                DiagnosisMonogramId = diagnosisMonograms.DiagnosisMonogramId,
                                                MonogramId = diagnosisMonograms.MonogramId,
                                                DiagnosisId = diagnosisMonograms.DiagnosisId,
                                                Monogram = monogram.Monogram1,
                                            }
                             ).Distinct().ToList();
            if (diagnosisMonogramsEntity != null)
            {
                diagnosisMonogramsModelsList = diagnosisMonogramsEntity;
            }
            return diagnosisMonogramsModelsList;
        }

        private List<PatternsDetailModel> GetPatternsDetailEntity(long diagnosisId)
        {
            List<PatternsDetailModel> patternDetailList = new List<PatternsDetailModel>();
            var patternsDetailEntity = (from patternsDetail in context.PatternsDetails
                                        where patternsDetail.DiagnosisId == diagnosisId && patternsDetail.DeletedStatus == false
                                        select new PatternsDetailModel
                                        {
                                            PatternDetailsId = patternsDetail.PatternDetailsId,
                                            PatternsKeywords = patternsDetail.PatternsKeywords,
                                            DiagnosisId = Convert.ToInt32(patternsDetail.DiagnosisId),
                                            DeletedStatus = patternsDetail.DeletedStatus,
                                        }
                            ).Distinct().ToList();
            if (patternsDetailEntity != null)
            {
                for (int i = 0; i < patternsDetailEntity.Count; i++)
                {
                    var patternsDetailItem = patternsDetailEntity[i];
                    var patternRubricDetailsEntity = (from patternRubricDetailsDetails in context.PatternRubricDetails
                                                      join subSectionMaster in context.SubSectionMasters on patternRubricDetailsDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                      join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                      where patternRubricDetailsDetails.PatternDetailsId == patternsDetailItem.PatternDetailsId && patternRubricDetailsDetails.DeletedStatus == false
                                                      select new PatternRubricDetailsModel
                                                      {
                                                          PatternRubricDetailsId = patternRubricDetailsDetails.PatternRubricDetailsId,
                                                          PatternDetailsId = patternRubricDetailsDetails.PatternDetailsId,
                                                          SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                          SubsectionName = subSectionMaster.SubSectionName,
                                                          SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                          SectionName = sectionMaster.SectionName,
                                                          DeletedStatus = Convert.ToBoolean(patternRubricDetailsDetails.DeletedStatus)
                                                      }
                                                       ).ToList();
                    patternsDetailEntity[i].PatternRubricDetails = patternRubricDetailsEntity;
                }
                patternDetailList = patternsDetailEntity;
                AttachDiagnosisKeywordSections("Patterns",
                    patternDetailList.Select(x => x.PatternDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = patternDetailList.FirstOrDefault(x => x.PatternDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return patternDetailList;
        }

        private List<LocationExtentionDetailsModel> GetLocationExtentionDetailsEntity(long diagnosisId)
        {
            List<LocationExtentionDetailsModel> locationExtentionDetailsList = new List<LocationExtentionDetailsModel>();
            var locationExtentionDetailsEntity = (from locationExtentionDetails in context.LocationExtentionDetails
                                                  where locationExtentionDetails.DiagnosisId == diagnosisId && locationExtentionDetails.DeletedStatus == false
                                                  select new LocationExtentionDetailsModel
                                                  {
                                                      LocationExtentionDetailsId = locationExtentionDetails.LocationExtentionDetailsId,
                                                      LocationExtentionDetailsKeyword = locationExtentionDetails.LocationExtentionDetailsKeyword,
                                                      DiagnosisId = Convert.ToInt32(locationExtentionDetails.DiagnosisId),
                                                      DeletedStatus = locationExtentionDetails.DeletedStatus,
                                                  }
                            ).Distinct().ToList();
            if (locationExtentionDetailsEntity != null)
            {
                for (int i = 0; i < locationExtentionDetailsEntity.Count; i++)
                {
                    var locationExtentionRubricDetailsItem = locationExtentionDetailsEntity[i];
                    var locationExtentionRubricDetailsEntity = (from locationExtentionRubricDetails in context.LocationExtentionRubricDetails
                                                                join subSectionMaster in context.SubSectionMasters on locationExtentionRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                                join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                                where locationExtentionRubricDetails.LocationExtentionDetailsId == locationExtentionRubricDetailsItem.LocationExtentionDetailsId && locationExtentionRubricDetails.DeletedStatus == false
                                                                select new LocationExtentionRubricDetailsModel
                                                                {
                                                                    LocationExtentionRubricDetailsId = locationExtentionRubricDetails.LocationExtentionRubricDetailsId,
                                                                    LocationExtentionDetailsId = locationExtentionRubricDetails.LocationExtentionDetailsId,
                                                                    SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                                    SubsectionName = subSectionMaster.SubSectionName,
                                                                    SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                                    SectionName = sectionMaster.SectionName,
                                                                    DeletedStatus = Convert.ToBoolean(locationExtentionRubricDetails.DeletedStatus)
                                                                }
                                                       ).ToList();
                    locationExtentionDetailsEntity[i].LocationExtentionRubricDetails = locationExtentionRubricDetailsEntity;
                }
                locationExtentionDetailsList = locationExtentionDetailsEntity;
                AttachDiagnosisKeywordSections("LocationExtention",
                    locationExtentionDetailsList.Select(x => x.LocationExtentionDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = locationExtentionDetailsList.FirstOrDefault(x => x.LocationExtentionDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }

            return locationExtentionDetailsList;

        }

        private List<SensationDetailsModel> GetSensationDetailspEntity(long diagnosisId)
        {
            List<SensationDetailsModel> sensationDetailsList = new List<SensationDetailsModel>();
            var sensationDetailsEntity = (from sensationDetails in context.SensationDetails
                                          where sensationDetails.DiagnosisId == diagnosisId && sensationDetails.DeletedStatus == false
                                          select new SensationDetailsModel
                                          {
                                              SensationDetailsId = sensationDetails.SensationDetailsId,
                                              SensationDetailsKeyword = sensationDetails.SensationDetailsKeyword,
                                              DiagnosisId = Convert.ToInt32(sensationDetails.DiagnosisId),
                                              DeletedStatus = sensationDetails.DeletedStatus,
                                          }
                            ).Distinct().ToList();
            if (sensationDetailsEntity != null)
            {
                for (int i = 0; i < sensationDetailsEntity.Count; i++)
                {
                    var sensationRubricDetailsItem = sensationDetailsEntity[i];
                    var sensationRubricDetailsEntity = (from sensationRubricDetails in context.SensationRubricDetails
                                                        join subSectionMaster in context.SubSectionMasters on sensationRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                                        join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                                        where sensationRubricDetails.SensationDetailsId == sensationRubricDetailsItem.SensationDetailsId && sensationRubricDetails.DeletedStatus == false
                                                        select new SensationRubricDetailsModel
                                                        {
                                                            SensationDetailsId = sensationRubricDetails.SensationDetailsId,
                                                            SensationRubricDetailsId = sensationRubricDetails.SensationRubricDetailsId,
                                                            SubsectionId = Convert.ToInt32(subSectionMaster.SubSectionId),
                                                            SubsectionName = subSectionMaster.SubSectionName,
                                                            SectionId = Convert.ToInt32(subSectionMaster.SectionId),
                                                            SectionName = sectionMaster.SectionName,
                                                            DeletedStatus = Convert.ToBoolean(sensationRubricDetails.DeletedStatus)
                                                        }
                                                        ).ToList();
                    sensationDetailsEntity[i].SensationRubricDetails = sensationRubricDetailsEntity;
                }
                sensationDetailsList = sensationDetailsEntity;
                AttachDiagnosisKeywordSections("Sensation",
                    sensationDetailsList.Select(x => x.SensationDetailsId).ToList(),
                    (id, ids, sections) =>
                    {
                        var item = sensationDetailsList.FirstOrDefault(x => x.SensationDetailsId == id);
                        if (item != null)
                        {
                            item.SectionIds = ids;
                            item.Sections = sections;
                        }
                    });
            }
            return sensationDetailsList;
        }

        /// <summary>
        /// Method to get all the diagnosis
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<DiagnosisModel> GetDiagnosis(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            var diagnosisModelList = new List<DiagnosisModel>();
            errorResponseModel = new ErrorResponseModel();
            var diagnosisEntityList = context.DiagnosisMasters.Where(x => x.DeleteStatus == false).Skip((nigaParameters.PageNumber - 1) * nigaParameters.PageSize)
             .Take(nigaParameters.PageSize)
             .ToList();
            if (diagnosisEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Diagnosis not found";
            }

            diagnosisEntityList.ForEach(item =>
            {
                diagnosisModelList.Add(new DiagnosisModel
                {
                    DiagnosisId = item.DiagnosisId,
                    DiagnosisName = item.DiagnosisName,
                    DiagnosisNameAlias = item.DiagnosisNameAlias,
                    Miasm = item.Miasm,
                    EnteredDate = item.EnteredDate,
                    EnteredBy = item.EnteredBy,
                    ChangedBy = item.ChangedBy,
                    ChangedDate = item.ChangedDate,
                    DeleteStatus = item.DeleteStatus,
                });
            });
            return diagnosisModelList;
        }

        /// <summary>
        /// Method is used for delete diagnosis.
        /// </summary>
        /// <param name="diagnosisModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteDiagnosis(DiagnosisModel diagnosisModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var diagnosisEntity = context.DiagnosisMasters.FirstOrDefault(x => x.DiagnosisId == diagnosisModel.DiagnosisId);
            if (diagnosisEntity != null)
            {
                diagnosisEntity.DeleteStatus = true;
                diagnosisEntity.ChangedBy = diagnosisModel.EnteredBy;
                diagnosisEntity.ChangedDate = DateTime.Now;
                context.SaveChanges();
                Message = "Diagnosis Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Method implementation for saving new Diagnosis
        /// </summary>
        /// <param name="diagnosisModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveDiagnosis(DiagnosisModel diagnosisModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (diagnosisModel.DiagnosisId == 0)
            {
                DiagnosisMaster diagnosisEntity = new DiagnosisMaster();
                diagnosisEntity.DiagnosisName = diagnosisModel.DiagnosisName;
                diagnosisEntity.DiagnosisNameAlias = diagnosisModel.DiagnosisNameAlias;
                diagnosisEntity.DiagnosisGroupId = diagnosisModel.DiagnosisGroupId;
                diagnosisEntity.Keywords = diagnosisModel.Keywords;
                diagnosisEntity.Investigations = diagnosisModel.Investigations;
                diagnosisEntity.AllopathicMedicines = diagnosisModel.AllopathicMedicines;
                diagnosisEntity.Examiniations = diagnosisModel.Examiniations;
                diagnosisEntity.Description = diagnosisModel.Description;
                diagnosisEntity.Miasm = diagnosisModel.Miasm;
                diagnosisEntity.EnteredBy = diagnosisModel.EnteredBy;
                diagnosisEntity.EnteredDate = DateTime.Now;
                context.DiagnosisMasters.Add(diagnosisEntity);
                context.SaveChanges();

                foreach (var item in diagnosisModel.ModelEx)
                {
                    var modeldetails = new DiagnosisDetail();
                    modeldetails.DiagnosisDetailId = item.DiagnosisDetailId;
                    modeldetails.DiagnosisId = diagnosisEntity.DiagnosisId;
                    modeldetails.SubSectionId = item.SubSectionId;
                    modeldetails.DeleteStatus = false;
                    context.DiagnosisDetails.Add(modeldetails);
                    context.SaveChanges();
                }

                // Insert into DiagnosisMonogram table
                //foreach (var diagnosisMonogramsItem in diagnosisModel.diagnosisMonogramsList)
                //{
                //    var diagnosisMonogramsEntity = new DiagnosisMonogram();
                //    diagnosisMonogramsEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                //    diagnosisMonogramsEntity.MonogramId = diagnosisMonogramsItem.MonogramId;
                //    context.DiagnosisMonograms.Add(diagnosisMonogramsEntity);
                //    context.SaveChanges();
                //}
                // Insert into DiagnosisPathology table
                //foreach (var diagnosisPathologyItem in diagnosisModel.diagnosisPathologyList)
                //{
                //    var diagnosisPathologyEntity = new DiagnosisPathology();
                //    diagnosisPathologyEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                //    diagnosisPathologyEntity.PathologyId = diagnosisPathologyItem.PathologyId;
                //    context.DiagnosisPathologies.Add(diagnosisPathologyEntity);
                //    context.SaveChanges();
                //}

                // Insert into DiagnosisSystemDetail table
                foreach (var diagnosisSystemDetailsItem in diagnosisModel.diagnosisSystemDetailsList)
                {
                    var diagnosisSystemDetailsEntity = new DiagnosisSystemDetail();
                    diagnosisSystemDetailsEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                    diagnosisSystemDetailsEntity.DiagnosisSystemId = diagnosisSystemDetailsItem.DiagnosisSystemId;
                    diagnosisSystemDetailsEntity.DeletedStatus = diagnosisSystemDetailsItem.DeletedStatus;
                    context.DiagnosisSystemDetails.Add(diagnosisSystemDetailsEntity);
                    context.SaveChanges();
                }

                // Insert into EmergencieDetail table & EmergencieRubricDetail table
                foreach (var emergencieDetailsItem in diagnosisModel.emergencieDetailsModelList)
                {
                    var emergencieDetailsEntity = new EmergencieDetail();
                    emergencieDetailsEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                    emergencieDetailsEntity.EmergencieKeyword = emergencieDetailsItem.EmergencieKeyword;
                    emergencieDetailsEntity.DeletedStatus = false;
                    context.EmergencieDetails.Add(emergencieDetailsEntity);
                    context.SaveChanges();

                    foreach (var EmergencieRubricDetailsItem in emergencieDetailsItem.EmergencieRubricDetails)
                    {
                        var emergencieRubricDetailsEntity = new EmergencieRubricDetail();
                        emergencieRubricDetailsEntity.EmergencieId = emergencieDetailsEntity.EmergencieId;
                        emergencieRubricDetailsEntity.SubsectionId = EmergencieRubricDetailsItem.SubsectionId;
                        emergencieRubricDetailsEntity.DeletedStatus = false;
                        context.EmergencieRubricDetails.Add(emergencieRubricDetailsEntity);
                        context.SaveChanges();
                    }
                    SyncDiagnosisKeywordSections(diagnosisEntity.DiagnosisId, "Emergencies", emergencieDetailsEntity.EmergencieId, emergencieDetailsItem.SectionIds);

                }

                // Insert into OnsetDurationProgressDetail table & OnsetDurationProgressRubricDetail table
                foreach (var onsetDurationProgressDetailsItem in diagnosisModel.OnsetDurationProgressDetails)
                {
                    AddUpdateOnsetDurationProgressDetails(onsetDurationProgressDetailsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into PatternsDetails table & PatternRubricDetail table
                foreach (var patternsDetailsItem in diagnosisModel.PatternsDetails)
                {
                    AddUpdatePatternsDetails(patternsDetailsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into PatternsDetails table & PatternRubricDetail table
                foreach (var locationExtentionDetailsItem in diagnosisModel.LocationExtentionDetailsModelList)
                {
                    AddUpdateLocationExtentionDetails(locationExtentionDetailsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into SensationDetail table & SensationRubricDetail table
                foreach (var sensationDetailsModelItem in diagnosisModel.sensationDetailsModelList)
                {
                    AddUpdateSensationDetails(sensationDetailsModelItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into ModalitiesDetail table & ModalitiesRubricDetail table
                foreach (var modalitiesDetailsModelsItem in diagnosisModel.modalitiesDetailsModelsList)
                {
                    AddUpdateModalitiesDetails(modalitiesDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into AccompaniedDetail table & AccompaniedRubricDetail table
                foreach (var accompaniedDetailsModelsItem in diagnosisModel.accompaniedDetailsModelsList)
                {
                    AddUpdateAccompaniedDetails(accompaniedDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into AccompaniedDetail table & AccompaniedRubricDetail table
                foreach (var diagnosisSymptomsItem in diagnosisModel.diagnosisSymptomsList)
                {
                    AddUpdateDiagnosisSymptoms(diagnosisSymptomsItem, diagnosisEntity.DiagnosisId, 0, Convert.ToInt32(diagnosisModel.EnteredBy));
                }

                // Insert into BeforeAfterDuringDetail table & BeforeAfterDuringRubricDetail table
                foreach (var beforeAfterDuringDetailsModelsItem in diagnosisModel.beforeAfterDuringDetailsModelsList)
                {
                    AddUpdateBeforeAfterDuringDetails(beforeAfterDuringDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into ObservationsDetail table & ObservationsRubricDetail table
                foreach (var observationsDetailsModelsItem in diagnosisModel.observationsDetailsModelsList)
                {
                    AddUpdateObservationsDetails(observationsDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into DiagnosisMonogramDetail table & AddUpdateDiagnosisMonogramRubricDetails table
                foreach (var diagnosisMonogramDetailsModelsItem in diagnosisModel.diagnosisMonogramDetailsModelsList)
                {
                    AddUpdateDiagnosisMonogramDetails(diagnosisMonogramDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into DiagnosisCausation table & DiagnosisCausationRubricDetail table
                foreach (var diagnosisCausationItem in diagnosisModel.diagnosisCausationList)
                {
                    AddUpdateDiagnosisCausation(diagnosisCausationItem, diagnosisEntity.DiagnosisId, 0);
                }

                // Insert into into DiagnosisPathologyDetail table & DiagnosisPathologyRubricDetail table
                foreach (var diagnosisPathologyDetailsModelsList in diagnosisModel.diagnosisPathologyDetailsModelsList)
                {
                    AddUpdateDiagnosisPathologyDetails(diagnosisPathologyDetailsModelsList, diagnosisEntity.DiagnosisId, 0);
                }

                Message = "Diagnosis Saved Successfully";
            }
            // update DiagnosisMaster table
            else
            {
                var diagnosisEntity = context.DiagnosisMasters.FirstOrDefault(x => x.DiagnosisId == diagnosisModel.DiagnosisId);
                if (diagnosisEntity != null)
                {
                    diagnosisEntity.DiagnosisName = diagnosisModel.DiagnosisName;
                    diagnosisEntity.DiagnosisNameAlias = diagnosisModel.DiagnosisNameAlias;
                    diagnosisEntity.DiagnosisGroupId = diagnosisModel.DiagnosisGroupId;
                    diagnosisEntity.Keywords = diagnosisModel.Keywords;
                    diagnosisEntity.Description = diagnosisModel.Description;
                    diagnosisEntity.Miasm = diagnosisModel.Miasm;
                    diagnosisEntity.Investigations = diagnosisModel.Investigations;
                    diagnosisEntity.AllopathicMedicines = diagnosisModel.AllopathicMedicines;
                    diagnosisEntity.Examiniations = diagnosisModel.Examiniations;
                    diagnosisEntity.ChangedBy = diagnosisModel.EnteredBy;
                    diagnosisEntity.ChangedDate = DateTime.Now;
                    context.SaveChanges();

                    var diagnosisDetailList = context.DiagnosisDetails.Where(x => x.DiagnosisId == diagnosisModel.DiagnosisId).ToList();

                    foreach (var itemDetail in diagnosisDetailList)
                    {
                        itemDetail.DeleteStatus = true;
                        context.SaveChanges();
                    }

                    foreach (var item1 in diagnosisModel.ModelEx)
                    {

                        {
                            if (item1.DiagnosisDetailId != 0)//---Update 
                            {
                                var diagnosisDetailexist = context.DiagnosisDetails.FirstOrDefault(x => x.DiagnosisDetailId == item1.DiagnosisDetailId);
                                diagnosisDetailexist.SubSectionId = item1.SubSectionId;
                                diagnosisDetailexist.DeleteStatus = false;
                                context.SaveChanges();
                            }

                            else//---Add
                            {
                                DiagnosisDetail modeldetails = new DiagnosisDetail();
                                modeldetails.DiagnosisId = diagnosisEntity.DiagnosisId;
                                modeldetails.SubSectionId = item1.SubSectionId;
                                modeldetails.DeleteStatus = false;
                                context.DiagnosisDetails.Add(modeldetails);
                                context.SaveChanges();

                            }

                        }

                    }

                    //foreach (var diagnosisMonogramsItem in diagnosisModel.diagnosisMonogramsList)
                    //{
                    //    if (diagnosisMonogramsItem.DiagnosisMonogramId == 0)
                    //    {
                    //        var diagnosisMonogramsEntity = new DiagnosisMonogram();
                    //        diagnosisMonogramsEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                    //        diagnosisMonogramsEntity.MonogramId = diagnosisMonogramsItem.MonogramId;
                    //        context.DiagnosisMonograms.Add(diagnosisMonogramsEntity);
                    //        context.SaveChanges();
                    //    }
                    //    else
                    //    {
                    //        var DiagnosisMonogram = context.DiagnosisMonograms.FirstOrDefault(x => x.DiagnosisMonogramId == diagnosisMonogramsItem.DiagnosisMonogramId);
                    //        DiagnosisMonogram.DiagnosisId = diagnosisEntity.DiagnosisId;
                    //        DiagnosisMonogram.MonogramId = diagnosisMonogramsItem.MonogramId;
                    //        context.SaveChanges();
                    //    }

                    //}

                    //foreach (var diagnosisPathologyItem in diagnosisModel.diagnosisPathologyList)
                    //{
                    //    if (diagnosisPathologyItem.DiagnosisPathologyId == 0)
                    //    {
                    //        var diagnosisPathologyEntity = new DiagnosisPathology();
                    //        diagnosisPathologyEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                    //        diagnosisPathologyEntity.PathologyId = diagnosisPathologyItem.PathologyId;
                    //        context.DiagnosisPathologies.Add(diagnosisPathologyEntity);
                    //        context.SaveChanges();
                    //    }
                    //    else
                    //    {
                    //        var DiagnosisPathology = context.DiagnosisPathologies.FirstOrDefault(x => x.DiagnosisPathologyId == diagnosisPathologyItem.DiagnosisPathologyId);
                    //        DiagnosisPathology.DiagnosisId = diagnosisEntity.DiagnosisId;
                    //        DiagnosisPathology.PathologyId = diagnosisPathologyItem.PathologyId;
                    //        context.SaveChanges();
                    //    }

                    //}

                    foreach (var diagnosisSystemDetailsItem in diagnosisModel.diagnosisSystemDetailsList)
                    {
                        if (diagnosisSystemDetailsItem.DiagnosisSystemDetailId == 0)
                        {
                            var diagnosisSystemDetailsEntity = new DiagnosisSystemDetail();
                            diagnosisSystemDetailsEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                            diagnosisSystemDetailsEntity.DiagnosisSystemId = diagnosisSystemDetailsItem.DiagnosisSystemId;
                            diagnosisSystemDetailsEntity.DeletedStatus = diagnosisSystemDetailsItem.DeletedStatus;
                            context.DiagnosisSystemDetails.Add(diagnosisSystemDetailsEntity);
                            context.SaveChanges();
                        }
                        else
                        {
                            var DiagnosisSystemDetail = context.DiagnosisSystemDetails.FirstOrDefault(x => x.DiagnosisSystemDetailId == diagnosisSystemDetailsItem.DiagnosisSystemDetailId);
                            DiagnosisSystemDetail.DiagnosisId = diagnosisEntity.DiagnosisId;
                            DiagnosisSystemDetail.DiagnosisSystemId = diagnosisSystemDetailsItem.DiagnosisSystemId;
                            DiagnosisSystemDetail.DeletedStatus = diagnosisSystemDetailsItem.DeletedStatus;
                            context.SaveChanges();
                        }

                    }
                    foreach (var emergencieDetailsItem in diagnosisModel.emergencieDetailsModelList)
                    {
                        if (emergencieDetailsItem.EmergencieId == 0)
                        {
                            var emergencieDetailsEntity = new EmergencieDetail();
                            emergencieDetailsEntity.DiagnosisId = diagnosisEntity.DiagnosisId;
                            emergencieDetailsEntity.EmergencieKeyword = emergencieDetailsItem.EmergencieKeyword;
                            emergencieDetailsEntity.DeletedStatus = false;
                            context.EmergencieDetails.Add(emergencieDetailsEntity);
                            context.SaveChanges();

                            foreach (var EmergencieRubricDetailsItem in emergencieDetailsItem.EmergencieRubricDetails)
                            {
                                if (EmergencieRubricDetailsItem.EmergencieRubricId == 0)
                                {
                                    var emergencieRubricDetailsEntity = new EmergencieRubricDetail();
                                    emergencieRubricDetailsEntity.EmergencieId = emergencieDetailsEntity.EmergencieId;
                                    emergencieRubricDetailsEntity.SubsectionId = EmergencieRubricDetailsItem.SubsectionId;
                                    emergencieRubricDetailsEntity.DeletedStatus = false;
                                    context.EmergencieRubricDetails.Add(emergencieRubricDetailsEntity);
                                    context.SaveChanges();
                                }
                                else
                                {
                                    var EmergencieRubric = context.EmergencieRubricDetails.FirstOrDefault(x => x.EmergencieRubricId == EmergencieRubricDetailsItem.EmergencieRubricId);
                                    EmergencieRubric.EmergencieId = emergencieDetailsEntity.EmergencieId;
                                    EmergencieRubric.SubsectionId = EmergencieRubricDetailsItem.SubsectionId;
                                    EmergencieRubric.DeletedStatus = false;
                                    context.SaveChanges();
                                }
                            }
                            SyncDiagnosisKeywordSections(diagnosisEntity.DiagnosisId, "Emergencies", emergencieDetailsEntity.EmergencieId, emergencieDetailsItem.SectionIds);
                        }
                        else
                        {
                            var emergencieDetails = context.EmergencieDetails.FirstOrDefault(x => x.EmergencieId == emergencieDetailsItem.EmergencieId);
                            emergencieDetails.DiagnosisId = diagnosisEntity.DiagnosisId;
                            emergencieDetails.EmergencieKeyword = emergencieDetailsItem.EmergencieKeyword;
                            emergencieDetails.DeletedStatus = false;
                            context.SaveChanges();

                            foreach (var EmergencieRubricDetailsItem in emergencieDetailsItem.EmergencieRubricDetails)
                            {
                                if (EmergencieRubricDetailsItem.EmergencieRubricId == 0)
                                {
                                    var emergencieRubricDetailsEntity = new EmergencieRubricDetail();
                                    emergencieRubricDetailsEntity.EmergencieId = emergencieDetailsItem.EmergencieId;
                                    emergencieRubricDetailsEntity.SubsectionId = EmergencieRubricDetailsItem.SubsectionId;
                                    emergencieRubricDetailsEntity.DeletedStatus = false;
                                    context.EmergencieRubricDetails.Add(emergencieRubricDetailsEntity);
                                    context.SaveChanges();
                                }
                                else
                                {
                                    var EmergencieRubric = context.EmergencieRubricDetails.FirstOrDefault(x => x.EmergencieRubricId == EmergencieRubricDetailsItem.EmergencieRubricId);
                                    EmergencieRubric.EmergencieId = emergencieDetailsItem.EmergencieId;
                                    EmergencieRubric.SubsectionId = EmergencieRubricDetailsItem.SubsectionId;
                                    EmergencieRubric.DeletedStatus = false;
                                    context.SaveChanges();
                                }
                            }
                            SyncDiagnosisKeywordSections(diagnosisEntity.DiagnosisId, "Emergencies", emergencieDetailsItem.EmergencieId, emergencieDetailsItem.SectionIds);
                        }


                    }

                    foreach (var onsetDurationProgressDetailsItem in diagnosisModel.OnsetDurationProgressDetails)
                    {
                        if (onsetDurationProgressDetailsItem.OnsetDetailId == 0)
                        {
                            AddUpdateOnsetDurationProgressDetails(onsetDurationProgressDetailsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateOnsetDurationProgressDetails(onsetDurationProgressDetailsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }
                    foreach (var patternsDetailsItem in diagnosisModel.PatternsDetails)
                    {
                        if (patternsDetailsItem.PatternDetailsId == 0)
                        {
                            AddUpdatePatternsDetails(patternsDetailsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdatePatternsDetails(patternsDetailsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }

                    foreach (var locationExtentionDetailsItem in diagnosisModel.LocationExtentionDetailsModelList)
                    {
                        if (locationExtentionDetailsItem.LocationExtentionDetailsId == 0)
                        {
                            AddUpdateLocationExtentionDetails(locationExtentionDetailsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateLocationExtentionDetails(locationExtentionDetailsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }

                    foreach (var sensationDetailsModelItem in diagnosisModel.sensationDetailsModelList)
                    {
                        if (sensationDetailsModelItem.SensationDetailsId == 0)
                        {
                            AddUpdateSensationDetails(sensationDetailsModelItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateSensationDetails(sensationDetailsModelItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }

                    // Insert & Update into ModalitiesDetail table & ModalitiesRubricDetail table
                    foreach (var modalitiesDetailsModelsItem in diagnosisModel.modalitiesDetailsModelsList)
                    {
                        if (modalitiesDetailsModelsItem.ModalitiesDetailsId == 0)
                        {
                            AddUpdateModalitiesDetails(modalitiesDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateModalitiesDetails(modalitiesDetailsModelsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }

                    // Insert & Update into AccompaniedDetail table & AccompaniedRubricDetail table
                    foreach (var accompaniedDetailsModelsItem in diagnosisModel.accompaniedDetailsModelsList)
                    {
                        if (accompaniedDetailsModelsItem.AccompaniedDetailsId == 0)
                        {
                            AddUpdateAccompaniedDetails(accompaniedDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateAccompaniedDetails(accompaniedDetailsModelsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }

                    // Insert & Update into DiagnosisSymptom table & DiagnosisSymptomRubric table
                    foreach (var diagnosisSymptomsItem in diagnosisModel.diagnosisSymptomsList)
                    {
                        if (diagnosisSymptomsItem.DiagnosisSymptomId == 0)
                        {
                            AddUpdateDiagnosisSymptoms(diagnosisSymptomsItem, diagnosisEntity.DiagnosisId, 0, Convert.ToInt32(diagnosisModel.EnteredBy));
                        }
                        else
                        {
                            AddUpdateDiagnosisSymptoms(diagnosisSymptomsItem, diagnosisEntity.DiagnosisId, 1, Convert.ToInt32(diagnosisModel.EnteredBy));
                        }
                    }

                    // Insert & Update into BeforeAfterDuringDetail table & BeforeAfterDuringRubricDetail table
                    foreach (var beforeAfterDuringDetailsModels in diagnosisModel.beforeAfterDuringDetailsModelsList)
                    {
                        if (beforeAfterDuringDetailsModels.BeforeAfterDuringDetailsId == 0)
                        {
                            AddUpdateBeforeAfterDuringDetails(beforeAfterDuringDetailsModels, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateBeforeAfterDuringDetails(beforeAfterDuringDetailsModels, diagnosisEntity.DiagnosisId, 1);
                        }
                    }

                    // Insert & Update into ObservationsDetail table & ObservationsRubricDetail table
                    foreach (var observationsDetailsModelsItem in diagnosisModel.observationsDetailsModelsList)
                    {
                        if (observationsDetailsModelsItem.ObservationsDetailsId == 0)
                        {
                            AddUpdateObservationsDetails(observationsDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateObservationsDetails(observationsDetailsModelsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }


                    // Insert & Update into DiagnosisMonogramDetail table & AddUpdateDiagnosisMonogramRubricDetails table
                    foreach (var diagnosisMonogramDetailsModelsItem in diagnosisModel.diagnosisMonogramDetailsModelsList)
                    {
                        if (diagnosisMonogramDetailsModelsItem.DiagnosisMonogramDetailsId == 0)
                        {
                            AddUpdateDiagnosisMonogramDetails(diagnosisMonogramDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateDiagnosisMonogramDetails(diagnosisMonogramDetailsModelsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }


                    // Insert & Update into DiagnosisCausation table & DiagnosisCausationRubricDetail table
                    foreach (var diagnosisCausationItem in diagnosisModel.diagnosisCausationList)
                    {
                        if (diagnosisCausationItem.CausationId == 0)
                        {
                            AddUpdateDiagnosisCausation(diagnosisCausationItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateDiagnosisCausation(diagnosisCausationItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }


                    // Insert & Update into DiagnosisPathologyDetail table & DiagnosisPathologyRubricDetail table
                    foreach (var diagnosisPathologyDetailsModelsItem in diagnosisModel.diagnosisPathologyDetailsModelsList)
                    {
                        if (diagnosisPathologyDetailsModelsItem.DiagnosisPathologyDetailsId == 0)
                        {
                            AddUpdateDiagnosisPathologyDetails(diagnosisPathologyDetailsModelsItem, diagnosisEntity.DiagnosisId, 0);
                        }
                        else
                        {
                            AddUpdateDiagnosisPathologyDetails(diagnosisPathologyDetailsModelsItem, diagnosisEntity.DiagnosisId, 1);
                        }
                    }

                    Message = "Diagnosis Updated Successfully";
                }
            }
            return Message;
        }

        private void AddUpdateSensationDetails(SensationDetailsModel sensationDetailsModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var sensationDetailsEntity = new SensationDetail();
                sensationDetailsEntity.DiagnosisId = diagnosisId;
                sensationDetailsEntity.SensationDetailsKeyword = sensationDetailsModelItem.SensationDetailsKeyword;
                sensationDetailsEntity.DeletedStatus = false;
                context.SensationDetails.Add(sensationDetailsEntity);
                context.SaveChanges();

                foreach (var sensationRubricDetailsItem in sensationDetailsModelItem.SensationRubricDetails)
                {
                    AddUpdateSensationRubricDetails(sensationRubricDetailsItem, sensationDetailsEntity.SensationDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Sensation", sensationDetailsEntity.SensationDetailsId, sensationDetailsModelItem.SectionIds);
            }
            else
            {
                var sensationDetailsEntity = context.SensationDetails.FirstOrDefault(x => x.SensationDetailsId == sensationDetailsModelItem.SensationDetailsId);
                sensationDetailsEntity.DiagnosisId = diagnosisId;
                sensationDetailsEntity.SensationDetailsKeyword = sensationDetailsModelItem.SensationDetailsKeyword;
                sensationDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var sensationRubricDetailsItem in sensationDetailsModelItem.SensationRubricDetails)
                {
                    AddUpdateSensationRubricDetails(sensationRubricDetailsItem, sensationDetailsModelItem.SensationDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Sensation", sensationDetailsModelItem.SensationDetailsId, sensationDetailsModelItem.SectionIds);
            }
        }

        private void AddUpdateSensationRubricDetails(SensationRubricDetailsModel sensationRubricDetailsItem, int sensationDetailsId)
        {
            if (sensationRubricDetailsItem.SensationDetailsId == 0)
            {
                var sensationRubricDetailsEntity = new SensationRubricDetail();
                sensationRubricDetailsEntity.SensationDetailsId = sensationDetailsId;
                sensationRubricDetailsEntity.SubsectionId = sensationRubricDetailsItem.SubsectionId;
                sensationRubricDetailsEntity.DeletedStatus = false;
                context.SensationRubricDetails.Add(sensationRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var sensationRubricDetailsEntity = context.SensationRubricDetails.FirstOrDefault(x => x.SensationRubricDetailsId == sensationRubricDetailsItem.SensationRubricDetailsId);
                sensationRubricDetailsEntity.SensationDetailsId = sensationDetailsId;
                sensationRubricDetailsEntity.SubsectionId = sensationRubricDetailsItem.SubsectionId;
                sensationRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateLocationExtentionDetails(LocationExtentionDetailsModel locationExtentionDetailsItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var locationExtentionDetailsEntity = new LocationExtentionDetail();
                locationExtentionDetailsEntity.DiagnosisId = diagnosisId;
                locationExtentionDetailsEntity.LocationExtentionDetailsId = locationExtentionDetailsItem.LocationExtentionDetailsId;
                locationExtentionDetailsEntity.LocationExtentionDetailsKeyword = locationExtentionDetailsItem.LocationExtentionDetailsKeyword;
                locationExtentionDetailsEntity.DeletedStatus = false;
                context.LocationExtentionDetails.Add(locationExtentionDetailsEntity);
                context.SaveChanges();

                foreach (var locationExtentionRubricDetailsItem in locationExtentionDetailsItem.LocationExtentionRubricDetails)
                {
                    AddUpdateLocationExtentionRubricDetails(locationExtentionRubricDetailsItem, locationExtentionDetailsEntity.LocationExtentionDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "LocationExtention", locationExtentionDetailsEntity.LocationExtentionDetailsId, locationExtentionDetailsItem.SectionIds);
            }
            else
            {
                var locationExtentionDetailsEntity = context.LocationExtentionDetails.FirstOrDefault(x => x.LocationExtentionDetailsId == locationExtentionDetailsItem.LocationExtentionDetailsId);
                locationExtentionDetailsEntity.DiagnosisId = diagnosisId;
                locationExtentionDetailsEntity.LocationExtentionDetailsKeyword = locationExtentionDetailsItem.LocationExtentionDetailsKeyword;
                locationExtentionDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var locationExtentionRubricDetailsItem in locationExtentionDetailsItem.LocationExtentionRubricDetails)
                {
                    AddUpdateLocationExtentionRubricDetails(locationExtentionRubricDetailsItem, locationExtentionDetailsItem.LocationExtentionDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "LocationExtention", locationExtentionDetailsItem.LocationExtentionDetailsId, locationExtentionDetailsItem.SectionIds);
            }
        }

        private void AddUpdateLocationExtentionRubricDetails(LocationExtentionRubricDetailsModel locationExtentionRubricDetailsItem, int locationExtentionDetailsId)
        {
            if (locationExtentionRubricDetailsItem.LocationExtentionRubricDetailsId == 0)
            {
                var locationExtentionRubricDetailsEntity = new LocationExtentionRubricDetail();
                locationExtentionRubricDetailsEntity.LocationExtentionDetailsId = locationExtentionDetailsId;
                locationExtentionRubricDetailsEntity.SubsectionId = locationExtentionRubricDetailsItem.SubsectionId;
                locationExtentionRubricDetailsEntity.DeletedStatus = false;
                context.LocationExtentionRubricDetails.Add(locationExtentionRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var locationExtentionRubricDetailsEntity = context.LocationExtentionRubricDetails.FirstOrDefault(x => x.LocationExtentionRubricDetailsId == locationExtentionRubricDetailsItem.LocationExtentionRubricDetailsId);
                locationExtentionRubricDetailsEntity.LocationExtentionDetailsId = locationExtentionDetailsId;
                locationExtentionRubricDetailsEntity.SubsectionId = locationExtentionRubricDetailsItem.SubsectionId;
                locationExtentionRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }
        }

        private void AddUpdatePatternsDetails(PatternsDetailModel patternsDetailsItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var patternsDetailEntity = new PatternsDetail();
                patternsDetailEntity.DiagnosisId = diagnosisId;
                patternsDetailEntity.PatternDetailsId = patternsDetailsItem.PatternDetailsId;
                patternsDetailEntity.PatternsKeywords = patternsDetailsItem.PatternsKeywords;
                patternsDetailEntity.DeletedStatus = false;
                context.PatternsDetails.Add(patternsDetailEntity);
                context.SaveChanges();

                foreach (var patternRubricDetailsItem in patternsDetailsItem.PatternRubricDetails)
                {
                    AddUpdatePatternRubricDetails(patternRubricDetailsItem, patternsDetailEntity.PatternDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Patterns", patternsDetailEntity.PatternDetailsId, patternsDetailsItem.SectionIds);
            }
            else
            {
                var patternsDetail = context.PatternsDetails.FirstOrDefault(x => x.PatternDetailsId == patternsDetailsItem.PatternDetailsId);
                patternsDetail.DiagnosisId = diagnosisId;
                patternsDetail.PatternsKeywords = patternsDetailsItem.PatternsKeywords;
                patternsDetail.DeletedStatus = false;
                context.SaveChanges();

                foreach (var patternRubricDetailsItem in patternsDetailsItem.PatternRubricDetails)
                {
                    AddUpdatePatternRubricDetails(patternRubricDetailsItem, patternsDetailsItem.PatternDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Patterns", patternsDetailsItem.PatternDetailsId, patternsDetailsItem.SectionIds);
            }
        }

        private void AddUpdatePatternRubricDetails(PatternRubricDetailsModel patternRubricDetailsItem, int patternDetailsId)
        {
            if (patternRubricDetailsItem.PatternRubricDetailsId == 0)
            {
                var patternRubricDetailsEntity = new PatternRubricDetail();
                patternRubricDetailsEntity.PatternDetailsId = patternDetailsId;
                patternRubricDetailsEntity.SubsectionId = patternRubricDetailsItem.SubsectionId;
                patternRubricDetailsEntity.DeletedStatus = false;
                context.PatternRubricDetails.Add(patternRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var patternRubricDetailsEntity = context.PatternRubricDetails.FirstOrDefault(x => x.PatternRubricDetailsId == patternRubricDetailsItem.PatternRubricDetailsId);
                patternRubricDetailsEntity.PatternDetailsId = patternDetailsId;
                patternRubricDetailsEntity.SubsectionId = patternRubricDetailsItem.SubsectionId;
                patternRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }
        }

        private void AddUpdateOnsetDurationProgressDetails(OnsetDurationProgressDetailsModel onsetDurationProgressDetailsItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var onsetDurationProgressDetailsEntity = new OnsetDurationProgressDetail();
                onsetDurationProgressDetailsEntity.DiagnosisId = diagnosisId;
                onsetDurationProgressDetailsEntity.OnsetKeyword = onsetDurationProgressDetailsItem.OnsetKeyword;
                onsetDurationProgressDetailsEntity.DeletedStatus = false;
                context.OnsetDurationProgressDetails.Add(onsetDurationProgressDetailsEntity);
                context.SaveChanges();

                foreach (var onsetDurationProgressRubricDetailsItem in onsetDurationProgressDetailsItem.OnsetDurationProgressRubricDetails)
                {
                    AddUpdateOnsetDurationProgressRubricDetails(onsetDurationProgressRubricDetailsItem, onsetDurationProgressDetailsEntity.OnsetDetailId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Onset", onsetDurationProgressDetailsEntity.OnsetDetailId, onsetDurationProgressDetailsItem.SectionIds);
            }
            else
            {
                var emergencieDetails = context.OnsetDurationProgressDetails.FirstOrDefault(x => x.OnsetDetailId == onsetDurationProgressDetailsItem.OnsetDetailId);
                emergencieDetails.DiagnosisId = diagnosisId;
                emergencieDetails.OnsetKeyword = onsetDurationProgressDetailsItem.OnsetKeyword;
                emergencieDetails.DeletedStatus = false;
                context.SaveChanges();

                foreach (var onsetDurationProgressRubricDetailsItem in onsetDurationProgressDetailsItem.OnsetDurationProgressRubricDetails)
                {
                    AddUpdateOnsetDurationProgressRubricDetails(onsetDurationProgressRubricDetailsItem, onsetDurationProgressDetailsItem.OnsetDetailId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Onset", onsetDurationProgressDetailsItem.OnsetDetailId, onsetDurationProgressDetailsItem.SectionIds);
            }
        }

        private void AddUpdateOnsetDurationProgressRubricDetails(OnsetDurationProgressRubricDetailsModel onsetDurationProgressRubricDetailsItem, int onsetDetailId)
        {
            if (onsetDurationProgressRubricDetailsItem.OnsetRubricId == 0)
            {
                var onsetDurationProgressRubricDetailsEntity = new OnsetDurationProgressRubricDetail();
                onsetDurationProgressRubricDetailsEntity.OnsetDetailId = onsetDetailId;
                onsetDurationProgressRubricDetailsEntity.SubsectionId = onsetDurationProgressRubricDetailsItem.SubsectionId;
                onsetDurationProgressRubricDetailsEntity.DeletedStatus = false;
                context.OnsetDurationProgressRubricDetails.Add(onsetDurationProgressRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var onsetDurationProgressRubricDetailsEntity = context.OnsetDurationProgressRubricDetails.FirstOrDefault(x => x.OnsetRubricId == onsetDurationProgressRubricDetailsItem.OnsetRubricId);
                onsetDurationProgressRubricDetailsEntity.OnsetDetailId = onsetDetailId;
                onsetDurationProgressRubricDetailsEntity.SubsectionId = onsetDurationProgressRubricDetailsItem.SubsectionId;
                onsetDurationProgressRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }
        }

        private void AddUpdateModalitiesDetails(ModalitiesDetailsModel modalitiesDetailsModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var modalitiesDetailsEntity = new ModalitiesDetail();
                modalitiesDetailsEntity.DiagnosisId = diagnosisId;
                modalitiesDetailsEntity.ModalitiesDetailsKeyword = modalitiesDetailsModelItem.ModalitiesDetailsKeyword;
                modalitiesDetailsEntity.DeletedStatus = false;
                context.ModalitiesDetails.Add(modalitiesDetailsEntity);
                context.SaveChanges();

                foreach (var modalitiesRubricDetailsItem in modalitiesDetailsModelItem.ModalitiesRubricDetails)
                {
                    AddUpdateModalitiesRubricDetails(modalitiesRubricDetailsItem, modalitiesDetailsEntity.ModalitiesDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Modalities", modalitiesDetailsEntity.ModalitiesDetailsId, modalitiesDetailsModelItem.SectionIds);
            }
            else
            {
                var modalitiesDetailsEntity = context.ModalitiesDetails.FirstOrDefault(x => x.ModalitiesDetailsId == modalitiesDetailsModelItem.ModalitiesDetailsId);
                modalitiesDetailsEntity.DiagnosisId = diagnosisId;
                modalitiesDetailsEntity.ModalitiesDetailsKeyword = modalitiesDetailsModelItem.ModalitiesDetailsKeyword;
                modalitiesDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var modalitiesRubricDetailsItem in modalitiesDetailsModelItem.ModalitiesRubricDetails)
                {
                    AddUpdateModalitiesRubricDetails(modalitiesRubricDetailsItem, modalitiesDetailsModelItem.ModalitiesDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Modalities", modalitiesDetailsModelItem.ModalitiesDetailsId, modalitiesDetailsModelItem.SectionIds);
            }
        }

        private void AddUpdateModalitiesRubricDetails(ModalitiesRubricDetailsModel modalitiesRubricDetailsItem, int modalitiesDetailsId)
        {
            if (modalitiesRubricDetailsItem.ModalitiesRubricDetailsId == 0)
            {
                var modalitiesRubricDetailsEntity = new ModalitiesRubricDetail();
                modalitiesRubricDetailsEntity.ModalitiesDetailsId = modalitiesDetailsId;
                modalitiesRubricDetailsEntity.SubsectionId = modalitiesRubricDetailsItem.SubsectionId;
                modalitiesRubricDetailsEntity.DeletedStatus = false;
                context.ModalitiesRubricDetails.Add(modalitiesRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var modalitiesRubricDetailsEntity = context.ModalitiesRubricDetails.FirstOrDefault(x => x.ModalitiesRubricDetailsId == modalitiesRubricDetailsItem.ModalitiesRubricDetailsId);
                modalitiesRubricDetailsEntity.ModalitiesDetailsId = modalitiesDetailsId;
                modalitiesRubricDetailsEntity.SubsectionId = modalitiesRubricDetailsItem.SubsectionId;
                modalitiesRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateAccompaniedDetails(AccompaniedDetailsModel accompaniedDetailsModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var accompaniedDetailsEntity = new AccompaniedDetail();
                accompaniedDetailsEntity.DiagnosisId = diagnosisId;
                accompaniedDetailsEntity.AccompaniedDetailsSystem = accompaniedDetailsModelItem.AccompaniedDetailsSystem;
                accompaniedDetailsEntity.DeletedStatus = false;
                context.AccompaniedDetails.Add(accompaniedDetailsEntity);
                context.SaveChanges();

                foreach (var accompaniedRubricDetailsItem in accompaniedDetailsModelItem.AccompaniedRubricDetails)
                {
                    AddUpdateAccompaniedRubricDetails(accompaniedRubricDetailsItem, accompaniedDetailsEntity.AccompaniedDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Accompanied", accompaniedDetailsEntity.AccompaniedDetailsId, accompaniedDetailsModelItem.SectionIds);
            }
            else
            {
                var accompaniedDetailsEntity = context.AccompaniedDetails.FirstOrDefault(x => x.AccompaniedDetailsId == accompaniedDetailsModelItem.AccompaniedDetailsId);
                accompaniedDetailsEntity.DiagnosisId = diagnosisId;
                accompaniedDetailsEntity.AccompaniedDetailsSystem = accompaniedDetailsModelItem.AccompaniedDetailsSystem;
                accompaniedDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var accompaniedRubricDetailsItem in accompaniedDetailsModelItem.AccompaniedRubricDetails)
                {
                    AddUpdateAccompaniedRubricDetails(accompaniedRubricDetailsItem, accompaniedDetailsModelItem.AccompaniedDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Accompanied", accompaniedDetailsModelItem.AccompaniedDetailsId, accompaniedDetailsModelItem.SectionIds);
            }
        }

        private void AddUpdateAccompaniedRubricDetails(AccompaniedRubricDetailsModel accompaniedRubricDetailsModelItem, int accompaniedDetailsId)
        {
            if (accompaniedRubricDetailsModelItem.AccompaniedRubricDetailsId == 0)
            {
                var accompaniedRubricDetailsEntity = new AccompaniedRubricDetail();
                accompaniedRubricDetailsEntity.AccompaniedDetailsId = accompaniedDetailsId;
                accompaniedRubricDetailsEntity.SubsectionId = accompaniedRubricDetailsModelItem.SubsectionId;
                accompaniedRubricDetailsEntity.DeletedStatus = false;
                context.AccompaniedRubricDetails.Add(accompaniedRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var accompaniedRubricDetailsEntity = context.AccompaniedRubricDetails.FirstOrDefault(x => x.AccompaniedRubricDetailsId == accompaniedRubricDetailsModelItem.AccompaniedRubricDetailsId);
                accompaniedRubricDetailsEntity.AccompaniedDetailsId = accompaniedDetailsId;
                accompaniedRubricDetailsEntity.SubsectionId = accompaniedRubricDetailsModelItem.SubsectionId;
                accompaniedRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateObservationsDetails(ObservationsDetailsModel observationsDetailsModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var observationsDetailsEntity = new ObservationsDetail();
                observationsDetailsEntity.DiagnosisId = diagnosisId;
                observationsDetailsEntity.ObservationsDetailsKeyword = observationsDetailsModelItem.ObservationsDetailsKeyword;
                observationsDetailsEntity.DeletedStatus = false;
                context.ObservationsDetails.Add(observationsDetailsEntity);
                context.SaveChanges();

                foreach (var ObservationsRubricDetailsItem in observationsDetailsModelItem.ObservationsRubricDetails)
                {
                    AddUpdateObservationsRubricDetails(ObservationsRubricDetailsItem, observationsDetailsEntity.ObservationsDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Observations", observationsDetailsEntity.ObservationsDetailsId, observationsDetailsModelItem.SectionIds);
            }
            else
            {
                var observationsDetailsEntity = context.ObservationsDetails.FirstOrDefault(x => x.ObservationsDetailsId == observationsDetailsModelItem.ObservationsDetailsId);
                observationsDetailsEntity.DiagnosisId = diagnosisId;
                observationsDetailsEntity.ObservationsDetailsKeyword = observationsDetailsModelItem.ObservationsDetailsKeyword;
                observationsDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var ObservationsRubricDetailsItem in observationsDetailsModelItem.ObservationsRubricDetails)
                {
                    AddUpdateObservationsRubricDetails(ObservationsRubricDetailsItem, observationsDetailsModelItem.ObservationsDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Observations", observationsDetailsModelItem.ObservationsDetailsId, observationsDetailsModelItem.SectionIds);
            }
        }

        private void AddUpdateObservationsRubricDetails(ObservationsRubricDetailsModel observationsRubricDetailsItem, int observationsDetailsId)
        {
            if (observationsRubricDetailsItem.ObservationsRubricDetailsId == 0)
            {
                var observationsRubricDetailsEntity = new ObservationsRubricDetail();
                observationsRubricDetailsEntity.ObservationsDetailsId = observationsDetailsId;
                observationsRubricDetailsEntity.Subsection = observationsRubricDetailsItem.Subsection;
                observationsRubricDetailsEntity.DeletedStatus = false;
                context.ObservationsRubricDetails.Add(observationsRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var observationsRubricDetailsEntity = context.ObservationsRubricDetails.FirstOrDefault(x => x.ObservationsRubricDetailsId == observationsRubricDetailsItem.ObservationsRubricDetailsId);
                observationsRubricDetailsEntity.ObservationsDetailsId = observationsDetailsId;
                observationsRubricDetailsEntity.Subsection = observationsRubricDetailsItem.Subsection;
                observationsRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateBeforeAfterDuringDetails(BeforeAfterDuringDetailsModel beforeAfterDuringDetailsModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var beforeAfterDuringDetailsEntity = new BeforeAfterDuringDetail();
                beforeAfterDuringDetailsEntity.DiagnosisId = diagnosisId;
                beforeAfterDuringDetailsEntity.BeforeAfterDuringDetailsKeyword = beforeAfterDuringDetailsModelItem.BeforeAfterDuringDetailsKeyword;
                beforeAfterDuringDetailsEntity.DeletedStatus = false;
                context.BeforeAfterDuringDetails.Add(beforeAfterDuringDetailsEntity);
                context.SaveChanges();

                foreach (var beforeAfterDuringRubricDetailsItem in beforeAfterDuringDetailsModelItem.BeforeAfterDuringRubricDetails)
                {
                    AddUpdateObservationsRubricDetails(beforeAfterDuringRubricDetailsItem, beforeAfterDuringDetailsEntity.BeforeAfterDuringDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "BeforeAfterDuring", beforeAfterDuringDetailsEntity.BeforeAfterDuringDetailsId, beforeAfterDuringDetailsModelItem.SectionIds);
            }
            else
            {
                var beforeAfterDuringDetailsEntity = context.BeforeAfterDuringDetails.FirstOrDefault(x => x.BeforeAfterDuringDetailsId == beforeAfterDuringDetailsModelItem.BeforeAfterDuringDetailsId);
                beforeAfterDuringDetailsEntity.DiagnosisId = diagnosisId;
                beforeAfterDuringDetailsEntity.BeforeAfterDuringDetailsKeyword = beforeAfterDuringDetailsModelItem.BeforeAfterDuringDetailsKeyword;
                beforeAfterDuringDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var beforeAfterDuringRubricDetailsItem in beforeAfterDuringDetailsModelItem.BeforeAfterDuringRubricDetails)
                {
                    AddUpdateObservationsRubricDetails(beforeAfterDuringRubricDetailsItem, beforeAfterDuringDetailsModelItem.BeforeAfterDuringDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "BeforeAfterDuring", beforeAfterDuringDetailsModelItem.BeforeAfterDuringDetailsId, beforeAfterDuringDetailsModelItem.SectionIds);
            }
        }

        private void AddUpdateObservationsRubricDetails(BeforeAfterDuringRubricDetailsModel beforeAfterDuringRubricDetailsItem, int beforeAfterDuringDetailsId)
        {
            if (beforeAfterDuringRubricDetailsItem.BeforeAfterDuringRubricDetailsId == 0)
            {
                var beforeAfterDuringRubricDetailsEntity = new BeforeAfterDuringRubricDetail();
                beforeAfterDuringRubricDetailsEntity.BeforeAfterDuringDetailsId = beforeAfterDuringDetailsId;
                beforeAfterDuringRubricDetailsEntity.SubsectionId = beforeAfterDuringRubricDetailsItem.SubsectionId;
                beforeAfterDuringRubricDetailsEntity.DeletedStatus = false;
                context.BeforeAfterDuringRubricDetails.Add(beforeAfterDuringRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var beforeAfterDuringRubricDetailsEntity = context.BeforeAfterDuringRubricDetails.FirstOrDefault(x => x.BeforeAfterDuringRubricDetailsId == beforeAfterDuringRubricDetailsItem.BeforeAfterDuringRubricDetailsId);
                beforeAfterDuringRubricDetailsEntity.BeforeAfterDuringDetailsId = beforeAfterDuringDetailsId;
                beforeAfterDuringRubricDetailsEntity.SubsectionId = beforeAfterDuringRubricDetailsItem.SubsectionId;
                beforeAfterDuringRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateDiagnosisSymptoms(DiagnosisSymptomsModel diagnosisSymptomsModelItem, int diagnosisId, int actionFlag, int enteredBy)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var diagnosisSymptomsEntity = new DiagnosisSymptom();
                diagnosisSymptomsEntity.DiagnosisId = diagnosisId;
                diagnosisSymptomsEntity.Symptom = diagnosisSymptomsModelItem.Symptom;
                diagnosisSymptomsEntity.EnteredBy = enteredBy;
                diagnosisSymptomsEntity.DeletedStatus = false;
                context.DiagnosisSymptoms.Add(diagnosisSymptomsEntity);
                context.SaveChanges();

                foreach (var diagnosisSymptomRubricItem in diagnosisSymptomsModelItem.DiagnosisSymptomRubric)
                {
                    AddUpdateDiagnosisSymptomRubric(diagnosisSymptomRubricItem, diagnosisSymptomsEntity.DiagnosisSymptomId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Symptoms", diagnosisSymptomsEntity.DiagnosisSymptomId, diagnosisSymptomsModelItem.SectionIds);
            }
            else
            {
                var diagnosisSymptomsEntity = context.DiagnosisSymptoms.FirstOrDefault(x => x.DiagnosisSymptomId == diagnosisSymptomsModelItem.DiagnosisSymptomId);
                diagnosisSymptomsEntity.DiagnosisId = diagnosisId;
                diagnosisSymptomsEntity.Symptom = diagnosisSymptomsModelItem.Symptom;
                diagnosisSymptomsEntity.EnteredBy = enteredBy;
                diagnosisSymptomsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var diagnosisSymptomRubricItem in diagnosisSymptomsModelItem.DiagnosisSymptomRubric)
                {
                    AddUpdateDiagnosisSymptomRubric(diagnosisSymptomRubricItem, diagnosisSymptomsModelItem.DiagnosisSymptomId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Symptoms", diagnosisSymptomsModelItem.DiagnosisSymptomId, diagnosisSymptomsModelItem.SectionIds);
            }
        }

        private void AddUpdateDiagnosisSymptomRubric(DiagnosisSymptomRubricModel diagnosisSymptomRubricItem, int diagnosisSymptomId)
        {
            if (diagnosisSymptomRubricItem.DiagnosisSymptomRubricId == 0)
            {
                var diagnosisSymptomRubricsEntity = new DiagnosisSymptomRubric();
                diagnosisSymptomRubricsEntity.DiagnosisSymptomId = diagnosisSymptomId;
                diagnosisSymptomRubricsEntity.SubsectionId = diagnosisSymptomRubricItem.SubsectionId;
                diagnosisSymptomRubricsEntity.DeletedStatus = false;
                context.DiagnosisSymptomRubrics.Add(diagnosisSymptomRubricsEntity);
                context.SaveChanges();
            }
            else
            {
                var diagnosisSymptomRubricsEntity = context.DiagnosisSymptomRubrics.FirstOrDefault(x => x.DiagnosisSymptomRubricId == diagnosisSymptomRubricItem.DiagnosisSymptomRubricId);
                diagnosisSymptomRubricsEntity.DiagnosisSymptomId = diagnosisSymptomId;
                diagnosisSymptomRubricsEntity.SubsectionId = diagnosisSymptomRubricItem.SubsectionId;
                diagnosisSymptomRubricsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateDiagnosisMonogramDetails(DiagnosisMonogramDetailsModel diagnosisMonogramDetailModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var diagnosisMonogramDetailsEntity = new DiagnosisMonogramDetail();
                diagnosisMonogramDetailsEntity.DiagnosisId = diagnosisId;
                diagnosisMonogramDetailsEntity.DiagnosisMonogramKeyword = diagnosisMonogramDetailModelItem.DiagnosisMonogramKeyword;
                diagnosisMonogramDetailsEntity.DeletedStatus = false;
                context.DiagnosisMonogramDetails.Add(diagnosisMonogramDetailsEntity);
                context.SaveChanges();

                foreach (var diagnosisMonogramRubricDetailsItem in diagnosisMonogramDetailModelItem.DiagnosisMonogramRubricDetails)
                {
                    AddUpdateDiagnosisMonogramRubricDetails(diagnosisMonogramRubricDetailsItem, diagnosisMonogramDetailsEntity.DiagnosisMonogramDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Monogram", diagnosisMonogramDetailsEntity.DiagnosisMonogramDetailsId, diagnosisMonogramDetailModelItem.SectionIds);
            }
            else
            {
                var diagnosisMonogramDetailsEntity = context.DiagnosisMonogramDetails.FirstOrDefault(x => x.DiagnosisMonogramDetailsId == diagnosisMonogramDetailModelItem.DiagnosisMonogramDetailsId);
                diagnosisMonogramDetailsEntity.DiagnosisId = diagnosisId;
                diagnosisMonogramDetailsEntity.DiagnosisMonogramKeyword = diagnosisMonogramDetailModelItem.DiagnosisMonogramKeyword;
                diagnosisMonogramDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var diagnosisMonogramRubricDetailsItem in diagnosisMonogramDetailModelItem.DiagnosisMonogramRubricDetails)
                {
                    AddUpdateDiagnosisMonogramRubricDetails(diagnosisMonogramRubricDetailsItem, diagnosisMonogramDetailModelItem.DiagnosisMonogramDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Monogram", diagnosisMonogramDetailModelItem.DiagnosisMonogramDetailsId, diagnosisMonogramDetailModelItem.SectionIds);
            }
        }

        private void AddUpdateDiagnosisMonogramRubricDetails(DiagnosisMonogramRubricDetailsModel diagnosisMonogramRubricDetailsItem, int diagnosisMonogramDetailsId)
        {
            if (diagnosisMonogramRubricDetailsItem.DiagnosisMonogramRubricDetailsId == 0)
            {
                var diagnosisMonogramRubricDetailsEntity = new DiagnosisMonogramRubricDetail();
                diagnosisMonogramRubricDetailsEntity.DiagnosisMonogramDetailsId = diagnosisMonogramDetailsId;
                diagnosisMonogramRubricDetailsEntity.Subsections = diagnosisMonogramRubricDetailsItem.Subsections;
                diagnosisMonogramRubricDetailsEntity.DeletedStatus = false;
                context.DiagnosisMonogramRubricDetails.Add(diagnosisMonogramRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var diagnosisMonogramRubricDetailsEntity = context.DiagnosisMonogramRubricDetails.FirstOrDefault(x => x.DiagnosisMonogramRubricDetailsId == diagnosisMonogramRubricDetailsItem.DiagnosisMonogramRubricDetailsId);
                diagnosisMonogramRubricDetailsEntity.DiagnosisMonogramDetailsId = diagnosisMonogramDetailsId;
                diagnosisMonogramRubricDetailsEntity.Subsections = diagnosisMonogramRubricDetailsItem.Subsections;
                diagnosisMonogramRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateDiagnosisCausation(DiagnosisCausationModel diagnosisCausationModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var diagnosisCausationEntity = new DiagnosisCausation();
                diagnosisCausationEntity.DiagnosisId = diagnosisId;
                diagnosisCausationEntity.CausationName = diagnosisCausationModelItem.CausationName;
                diagnosisCausationEntity.DeletedStatus = false;
                context.DiagnosisCausations.Add(diagnosisCausationEntity);
                context.SaveChanges();

                foreach (var diagnosisCausationRubricDetailsItem in diagnosisCausationModelItem.DiagnosisCausationRubricDetails)
                {
                    AddUpdateDiagnosisCausationRubricDetails(diagnosisCausationRubricDetailsItem, diagnosisCausationEntity.CausationId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Causations", diagnosisCausationEntity.CausationId, diagnosisCausationModelItem.SectionIds);
            }
            else
            {
                var diagnosisCausationEntity = context.DiagnosisCausations.FirstOrDefault(x => x.CausationId == diagnosisCausationModelItem.CausationId);
                diagnosisCausationEntity.DiagnosisId = diagnosisId;
                diagnosisCausationEntity.CausationName = diagnosisCausationModelItem.CausationName;
                diagnosisCausationEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var diagnosisCausationRubricDetailsItem in diagnosisCausationModelItem.DiagnosisCausationRubricDetails)
                {
                    AddUpdateDiagnosisCausationRubricDetails(diagnosisCausationRubricDetailsItem, diagnosisCausationModelItem.CausationId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Causations", diagnosisCausationModelItem.CausationId, diagnosisCausationModelItem.SectionIds);
            }
        }

        private void AddUpdateDiagnosisCausationRubricDetails(DiagnosisCausationRubricDetailsModel diagnosisCausationRubricDetailsItem, int causationId)
        {
            if (diagnosisCausationRubricDetailsItem.CausationRubricDetailsId == 0)
            {
                var diagnosisCausationRubricDetailsEntity = new DiagnosisCausationRubricDetail();
                diagnosisCausationRubricDetailsEntity.CausationId = causationId;
                diagnosisCausationRubricDetailsEntity.SubsectionId = diagnosisCausationRubricDetailsItem.SubsectionId;
                diagnosisCausationRubricDetailsEntity.DeletedStatus = false;
                context.DiagnosisCausationRubricDetails.Add(diagnosisCausationRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var diagnosisCausationRubricDetailsEntity = context.DiagnosisCausationRubricDetails.FirstOrDefault(x => x.CausationRubricDetailsId == diagnosisCausationRubricDetailsItem.CausationRubricDetailsId);
                diagnosisCausationRubricDetailsEntity.CausationId = causationId;
                diagnosisCausationRubricDetailsEntity.SubsectionId = diagnosisCausationRubricDetailsItem.SubsectionId;
                diagnosisCausationRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        private void AddUpdateDiagnosisPathologyDetails(DiagnosisPathologyDetailsModel diagnosisPathologyDetailsModelItem, int diagnosisId, int actionFlag)
        {
            //actionFlag==0 for Insert && actionFlag==1 for Update
            if (actionFlag == 0)
            {
                var diagnosisPathologyDetailsEntity = new DiagnosisPathologyDetail();
                diagnosisPathologyDetailsEntity.DiagnosisId = diagnosisId;
                diagnosisPathologyDetailsEntity.DiagnosisPathologyKeyword = diagnosisPathologyDetailsModelItem.DiagnosisPathologyKeyword;
                diagnosisPathologyDetailsEntity.DeletedStatus = false;
                context.DiagnosisPathologyDetails.Add(diagnosisPathologyDetailsEntity);
                context.SaveChanges();

                foreach (var diagnosisPathologyRubricDetailsItem in diagnosisPathologyDetailsModelItem.DiagnosisPathologyRubricDetails)
                {
                    AddUpdateDiagnosisPathologyRubricDetails(diagnosisPathologyRubricDetailsItem, diagnosisPathologyDetailsEntity.DiagnosisPathologyDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Pathology", diagnosisPathologyDetailsEntity.DiagnosisPathologyDetailsId, diagnosisPathologyDetailsModelItem.SectionIds);
            }
            else
            {
                var diagnosisPathologyDetailsEntity = context.DiagnosisPathologyDetails.FirstOrDefault(x => x.DiagnosisPathologyDetailsId == diagnosisPathologyDetailsModelItem.DiagnosisPathologyDetailsId);
                diagnosisPathologyDetailsEntity.DiagnosisId = diagnosisId;
                diagnosisPathologyDetailsEntity.DiagnosisPathologyKeyword = diagnosisPathologyDetailsModelItem.DiagnosisPathologyKeyword;
                diagnosisPathologyDetailsEntity.DeletedStatus = false;
                context.SaveChanges();

                foreach (var diagnosisPathologyRubricDetailsItem in diagnosisPathologyDetailsModelItem.DiagnosisPathologyRubricDetails)
                {
                    AddUpdateDiagnosisPathologyRubricDetails(diagnosisPathologyRubricDetailsItem, diagnosisPathologyDetailsModelItem.DiagnosisPathologyDetailsId);
                }
                SyncDiagnosisKeywordSections(diagnosisId, "Pathology", diagnosisPathologyDetailsModelItem.DiagnosisPathologyDetailsId, diagnosisPathologyDetailsModelItem.SectionIds);
            }
        }

        private void AddUpdateDiagnosisPathologyRubricDetails(DiagnosisPathologyRubricDetailsModel diagnosisPathologyRubricDetailsItem, int diagnosisPathologyDetailsId)
        {
            if (diagnosisPathologyRubricDetailsItem.DiagnosisPathologyRubricDetailsId == 0)
            {
                var diagnosisPathologyRubricDetailsEntity = new DiagnosisPathologyRubricDetail();
                diagnosisPathologyRubricDetailsEntity.DiagnosisPathologyDetailsId = diagnosisPathologyDetailsId;
                diagnosisPathologyRubricDetailsEntity.SubsectionId = diagnosisPathologyRubricDetailsItem.SubsectionId;
                diagnosisPathologyRubricDetailsEntity.DeletedStatus = false;
                context.DiagnosisPathologyRubricDetails.Add(diagnosisPathologyRubricDetailsEntity);
                context.SaveChanges();
            }
            else
            {
                var diagnosisPathologyRubricDetailsEntity = context.DiagnosisPathologyRubricDetails.FirstOrDefault(x => x.DiagnosisPathologyRubricDetailsId == diagnosisPathologyRubricDetailsItem.DiagnosisPathologyRubricDetailsId);
                diagnosisPathologyRubricDetailsEntity.DiagnosisPathologyDetailsId = diagnosisPathologyDetailsId;
                diagnosisPathologyRubricDetailsEntity.SubsectionId = diagnosisPathologyRubricDetailsItem.SubsectionId;
                diagnosisPathologyRubricDetailsEntity.DeletedStatus = false;
                context.SaveChanges();
            }

        }

        public string DeleteDiagnosisRubric(DiagnosisRubricDeleteTabWise diagnosisrubricModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";

            if (diagnosisrubricModel.DiagnosisRubricId == 0 && diagnosisrubricModel.KeywordId > 0)
            {
                return SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
            }

            if (diagnosisrubricModel.DiagnosisTab == "Symptoms")
            {
                var diagnosissymptomrubricEntity = context.DiagnosisSymptomRubrics.FirstOrDefault(x => x.DiagnosisSymptomRubricId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosissymptomrubricEntity != null)
                {
                    diagnosissymptomrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Diagnosis Symptom Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }

                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Monogram")
            {
                var diagnosismonogramrubricdetailsEntity = context.DiagnosisMonogramRubricDetails.FirstOrDefault(x => x.DiagnosisMonogramRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosismonogramrubricdetailsEntity != null)
                {
                    diagnosismonogramrubricdetailsEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Diagnosis Monogram Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Causations")
            {
                var diagnosiscausationrubricEntity = context.DiagnosisCausationRubricDetails.FirstOrDefault(x => x.CausationRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosiscausationrubricEntity != null)
                {
                    diagnosiscausationrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Diagnosis Causation Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Pathology")
            {
                var diagnosispathologyrubricEntity = context.DiagnosisPathologyRubricDetails.FirstOrDefault(x => x.DiagnosisPathologyRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosispathologyrubricEntity != null)
                {
                    diagnosispathologyrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Diagnosis Pathology Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Emergencies")
            {
                var diagnosisemergencierubricEntity = context.EmergencieRubricDetails.FirstOrDefault(x => x.EmergencieRubricId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosisemergencierubricEntity != null)
                {
                    diagnosisemergencierubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Emergencie Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Onset")
            {
                var diagnosisonsetdurationprogressrubricEntity = context.OnsetDurationProgressRubricDetails.FirstOrDefault(x => x.OnsetRubricId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosisonsetdurationprogressrubricEntity != null)
                {
                    diagnosisonsetdurationprogressrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Onset/Duration/Progress Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Patterns")
            {
                var diagnosispatternrubricEntity = context.PatternRubricDetails.FirstOrDefault(x => x.PatternRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosispatternrubricEntity != null)
                {
                    diagnosispatternrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Pattern Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "LocationExtention")
            {
                var diagnosislocationextentionrubricEntity = context.LocationExtentionRubricDetails.FirstOrDefault(x => x.LocationExtentionRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosislocationextentionrubricEntity != null)
                {
                    diagnosislocationextentionrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Location/Extention Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Sensation")
            {
                var diagnosissensationrubricEntity = context.SensationRubricDetails.FirstOrDefault(x => x.SensationRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosissensationrubricEntity != null)
                {
                    diagnosissensationrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Sensation Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Modalities")
            {
                var diagnosismodalitiesrubricEntity = context.ModalitiesRubricDetails.FirstOrDefault(x => x.ModalitiesRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosismodalitiesrubricEntity != null)
                {
                    diagnosismodalitiesrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Modalities Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Accompanied")
            {
                var diagnosisaccompaniedrubricEntity = context.AccompaniedRubricDetails.FirstOrDefault(x => x.AccompaniedRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosisaccompaniedrubricEntity != null)
                {
                    diagnosisaccompaniedrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Accompanied Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "Observations")
            {
                var diagnosisobservationsrubricEntity = context.ObservationsRubricDetails.FirstOrDefault(x => x.ObservationsRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosisobservationsrubricEntity != null)
                {
                    diagnosisobservationsrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Observations Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }
            else if (diagnosisrubricModel.DiagnosisTab == "BeforeAfterDuring")
            {
                var diagnosisbeforeafterduringrubricEntity = context.BeforeAfterDuringRubricDetails.FirstOrDefault(x => x.BeforeAfterDuringRubricDetailsId == diagnosisrubricModel.DiagnosisRubricId);
                if (diagnosisbeforeafterduringrubricEntity != null)
                {
                    diagnosisbeforeafterduringrubricEntity.DeletedStatus = true;
                    context.SaveChanges();
                    Message = "Before/After/During Rubric Deleted Successfully";

                    if (diagnosisrubricModel.KeywordId > 0)
                    {
                        var keywordDeleteMessage = SoftDeleteDiagnosisKeywordByTab(diagnosisrubricModel.DiagnosisTab, diagnosisrubricModel.KeywordId);
                        if (!string.IsNullOrEmpty(keywordDeleteMessage))
                        {
                            Message = keywordDeleteMessage;
                        }
                    }
                }
            }

            return Message;
        }

        /// <summary>
        /// Method implementation for saving new Diagnosis
        /// </summary>
        /// <param name="diagnosisModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public DiagnosisSearchResultModel DiagnosisSearch(string searchKeyword, ref ErrorResponseModel errorResponseModel)
        {
            var searchResult = (from diagnosisMaster in context.DiagnosisMasters.Where(x=>x.DiagnosisName==searchKeyword && x.DeleteStatus==false)
                                 join diagnosisTherapeuticsDetail in context.DiagnosisTherapeuticsDetails.Where(dt=>dt.DeletedStatus==false)
                                 on diagnosisMaster.DiagnosisId equals diagnosisTherapeuticsDetail.DiagnosisId into DiagnosisData //Performing LINQ Group Join
                                from diagnosisDataWithNull in DiagnosisData.DefaultIfEmpty()
                             
                              select new DiagnosisSearchResultModel
                                {
                                    DiagnosisID = diagnosisMaster.DiagnosisId,
                                    DiagnosisName = diagnosisMaster.DiagnosisName,
                                    DiagnosisNameAlias = diagnosisMaster.DiagnosisNameAlias,
                                    Miasm = diagnosisMaster.Miasm,
                                    Investigations = diagnosisMaster.Investigations,
                                    AllopathicMedicines = diagnosisMaster.AllopathicMedicines,
                                    Examiniations = diagnosisMaster.Examiniations,
                                    Therapeutics = diagnosisDataWithNull.DiagnosisTherapeuticsDetail1
                              }
                ).FirstOrDefault();

            var diagnosisRemediesData = (from diagnosisDetail in context.DiagnosisDetails.Where(x=>x.DiagnosisId == searchResult.DiagnosisID && x.DeleteStatus == false)
                                         join subSectionMater in context.SubSectionMasters.Where(x=>x.DeleteStatus==false) on diagnosisDetail.SubSectionId equals subSectionMater.SubSectionId
                                         join rrd in context.RubricRemedyDetails.Where(x => x.DeletedStatus == false) on subSectionMater.SubSectionId equals rrd.SubSectionId
                                         join gradeMaster in context.RemedyGradeMaster.Where(x=>x.DeleteStatus == false) on rrd.GradeId equals gradeMaster.GradeId
                                         group gradeMaster by new  { gradeMaster.GradeId, gradeMaster.GradeNo, gradeMaster.FontName, gradeMaster.FontColor, gradeMaster.FontStyle, gradeMaster.Description, subSectionMater.SubSectionId, subSectionMater.SubSectionName } into gcs
                                         select new DiagnosisRemediesModel
                                         {
                                             GradeID = gcs.Key.GradeId,
                                             GradeNo = gcs.Key.GradeNo,
                                             FontName = gcs.Key.FontName,
                                             FontColor = gcs.Key.FontColor,
                                             FontStyle = gcs.Key.FontStyle,
                                             Description = gcs.Key.Description,
                                             SubSectionId =gcs.Key.SubSectionId,
                                             SubSectionName = gcs.Key.SubSectionName,
                                         }).ToList();



            if (diagnosisRemediesData != null)
            {
                for (int i = 0; i < diagnosisRemediesData.Count; i++)
                {

                    var item = diagnosisRemediesData[i];

                    var remedyData = (from rrd in context.RubricRemedyDetails.Where(x=> x.SubSectionId == item.SubSectionId && x.GradeId == item.GradeID && x.DeletedStatus == false)
                                      join remadyMaster in context.RemedyMasters on rrd.RemedyId equals remadyMaster.RemedyId
                                     
                                      select new DiagnosisRemedyModel
                                      {
                                          remedyId = remadyMaster.RemedyId,
                                          remedyName = remadyMaster.RemedyName,
                                          remedyAlias = remadyMaster.RemedyAlias,
                                      }
                                  ).ToList();

                    if (remedyData != null)
                    {
                        for (int j = 0; j < remedyData.Count; j++)
                        {
                            var diagnosisRemedyModelItem = remedyData[j];
                            var authorData = (from rrd in context.RubricRemedyDetails.Where(x => x.SubSectionId == item.SubSectionId && x.GradeId == item.GradeID && x.DeletedStatus == false)
                                              join rrad in context.RemedyRubricAuthorDetails on rrd.RubricRemedyId equals rrad.RubricRemedyId
                                              join authorMaster in context.AuthorMasters on rrad.AuthorId equals authorMaster.AuthorId 
                                              select new 
                                              {
                                                  authorId = authorMaster.AuthorId,
                                                  authorAlias = authorMaster.AuthorAlias,
                                                  authorName =authorMaster.AuthorName,
                                              }
                                  ).FirstOrDefault();
                            if (authorData != null)
                            {
                                diagnosisRemedyModelItem.authorId = authorData.authorId;
                                diagnosisRemedyModelItem.authorName = authorData.authorName;
                                diagnosisRemedyModelItem.authorAlias = authorData.authorAlias;
                            }
                            remedyData[j]=diagnosisRemedyModelItem;
                        }
                    }


                    diagnosisRemediesData[i].diagnosisRemedyModel = remedyData;
                }



            }
            searchResult.diagnosisRemediesModels = diagnosisRemediesData;
            return searchResult;
        }

        public List<DiagnosisKeywordModel> GetDiagnosisKeywordByTab(int diagnosisId, string type, ref ErrorResponseModel errorResponseModel)
        {
            var diagnosisKeywordModel = new List<DiagnosisKeywordModel>();
            if (type == "Symptoms")
            {
                diagnosisKeywordModel = (from diagnosisSymptom in context.DiagnosisSymptoms
                                         where diagnosisSymptom.DiagnosisId == diagnosisId && diagnosisSymptom.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = diagnosisSymptom.DiagnosisSymptomId,
                                             keyword = diagnosisSymptom.Symptom,
                                         }).ToList();

            }
            else if (type == "Monogram")
            {
                diagnosisKeywordModel = (from dmd in context.DiagnosisMonogramDetails
                                         where dmd.DiagnosisId == diagnosisId && dmd.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = dmd.DiagnosisMonogramDetailsId,
                                             keyword = dmd.DiagnosisMonogramKeyword,
                                         }).ToList();

            }
            else if (type == "Causations")
            {
                diagnosisKeywordModel = (from diagnosisCausation in context.DiagnosisCausations
                                         where diagnosisCausation.DiagnosisId == diagnosisId && diagnosisCausation.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = diagnosisCausation.CausationId,
                                             keyword = diagnosisCausation.CausationName,
                                         }).ToList();

            }
            else if (type == "Pathology")
            {
                diagnosisKeywordModel = (from diagnosisPathologyDetails in context.DiagnosisPathologyDetails
                                         where diagnosisPathologyDetails.DiagnosisId == diagnosisId && diagnosisPathologyDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = diagnosisPathologyDetails.DiagnosisPathologyDetailsId,
                                             keyword = diagnosisPathologyDetails.DiagnosisPathologyKeyword,
                                         }).ToList();

            }
            else if (type == "Emergencies")
            {
                diagnosisKeywordModel = (from emergencieDetails in context.EmergencieDetails
                                         where emergencieDetails.DiagnosisId == diagnosisId && emergencieDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = emergencieDetails.EmergencieId,
                                             keyword = emergencieDetails.EmergencieKeyword,
                                         }).ToList();
            }
            else if (type == "Onset/Duration/Progress")
            {
                diagnosisKeywordModel = (from onsetDurationProgressDetails in context.OnsetDurationProgressDetails
                                         where onsetDurationProgressDetails.DiagnosisId == diagnosisId && onsetDurationProgressDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = onsetDurationProgressDetails.OnsetDetailId,
                                             keyword = onsetDurationProgressDetails.OnsetKeyword,
                                         }).ToList();
            }
            else if (type == "Patterns")
            {
                diagnosisKeywordModel = (from patternsDetail in context.PatternsDetails
                                         where patternsDetail.DiagnosisId == diagnosisId && patternsDetail.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = patternsDetail.PatternDetailsId,
                                             keyword = patternsDetail.PatternsKeywords,
                                         }).ToList();
            }
            else if (type == "Location/Extention")
            {
                diagnosisKeywordModel = (from locationExtentionDetails in context.LocationExtentionDetails
                                         where locationExtentionDetails.DiagnosisId == diagnosisId && locationExtentionDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = locationExtentionDetails.LocationExtentionDetailsId,
                                             keyword = locationExtentionDetails.LocationExtentionDetailsKeyword,
                                         }).ToList();
            }
            else if (type == "Sensation")
            {
                diagnosisKeywordModel = (from sensationDetails in context.SensationDetails
                                         where sensationDetails.DiagnosisId == diagnosisId && sensationDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = sensationDetails.SensationDetailsId,
                                             keyword = sensationDetails.SensationDetailsKeyword,
                                         }).ToList();
            }
            else if (type == "Modalities")
            {
                diagnosisKeywordModel = (from modalitiesDetails in context.ModalitiesDetails
                                         where modalitiesDetails.DiagnosisId == diagnosisId && modalitiesDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = modalitiesDetails.ModalitiesDetailsId,
                                             keyword = modalitiesDetails.ModalitiesDetailsKeyword,
                                         }).ToList();

            }
            else if (type == "Accompanied")
            {
                diagnosisKeywordModel = (from accompaniedDetails in context.AccompaniedDetails
                                         where accompaniedDetails.DiagnosisId == diagnosisId && accompaniedDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = accompaniedDetails.AccompaniedDetailsId,
                                             keyword = accompaniedDetails.AccompaniedDetailsSystem,
                                         }).ToList();
            }
            else if (type == "Observations")
            {
                diagnosisKeywordModel = (from observationsDetails in context.ObservationsDetails
                                         where observationsDetails.DiagnosisId == diagnosisId && observationsDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = observationsDetails.ObservationsDetailsId,
                                             keyword = observationsDetails.ObservationsDetailsKeyword,
                                         }).ToList();

            }
            else if (type == "Before/After/During")
            {
                diagnosisKeywordModel = (from beforeAfterDuringDetails in context.BeforeAfterDuringDetails
                                         where beforeAfterDuringDetails.DiagnosisId == diagnosisId && beforeAfterDuringDetails.DeletedStatus == false
                                         select new DiagnosisKeywordModel
                                         {
                                             KeywordId = beforeAfterDuringDetails.BeforeAfterDuringDetailsId,
                                             keyword = beforeAfterDuringDetails.BeforeAfterDuringDetailsKeyword,
                                         }).ToList();
            }

            if (diagnosisKeywordModel.Any())
            {
                var keywordType = type;
                var keywordIds = diagnosisKeywordModel.Select(x => x.KeywordId).ToList();
                var sectionLinks = (from map in context.DiagnosisKeywordSections
                                    where map.KeywordType == keywordType
                                          && keywordIds.Contains(map.KeywordDetailId)
                                          && !map.DeleteStatus
                                    select new { map.KeywordDetailId, map.SectionId })
                                   .ToList();

                foreach (var kw in diagnosisKeywordModel)
                {
                    kw.SectionIds = sectionLinks
                        .Where(x => x.KeywordDetailId == kw.KeywordId)
                        .Select(x => x.SectionId)
                        .Distinct()
                        .ToList();
                }
            }

            return diagnosisKeywordModel;
        }

        public List<RubricKeywordModel> GetRubricByKeywordID(int keywordID, string type, ref ErrorResponseModel errorResponseModel)
        {
            var rubricKeywordList = new List<RubricKeywordModel>();
            if (type == "Symptoms")
            {

                rubricKeywordList = GetSymptomsRemedyList(keywordID);

            }
            else if (type == "Monogram")
            {
                rubricKeywordList = GetMonogramRemedyList(keywordID);
            }
            else if (type == "Causations")
            {
                rubricKeywordList = GetCausationsList(keywordID);

            }
            else if (type == "Pathology")
            {
                rubricKeywordList = GetPathologyList(keywordID);

            }
            else if (type == "Emergencies")
            {
                rubricKeywordList = GetEmergenciesList(keywordID);
            }
            else if (type == "Onset/Duration/Progress")
            {
                rubricKeywordList = GetOnsetList(keywordID);
              
            }
            else if (type == "Patterns")
            {
                rubricKeywordList = GetPatternsRemedyList(keywordID);
               
            }
            else if (type == "Location/Extention")
            {
                rubricKeywordList = GetLocationExtentionRemedyList(keywordID);
            }
            else if (type == "Sensation")
            {
                rubricKeywordList = GetSensationRemedyList(keywordID);
               
            }
            else if (type == "Modalities")
            {
                rubricKeywordList = GetModalitiesRemedyList(keywordID);
            }
            else if (type == "Accompanied")
            {
                rubricKeywordList = GetAccompaniedRemedyList(keywordID);
            }
            else if (type == "Observations")
            {
                rubricKeywordList = GetObservationsRemedyList(keywordID);
            }
            else if (type == "Before/After/During")
            {
                rubricKeywordList = GetBeforeAfterDuringRemedyList(keywordID);
            }

            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetCausationsList(int keywordID)
        {
            var rubricKeywordList = (from diagnosisCausationRubricDetails in context.DiagnosisCausationRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on diagnosisCausationRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where diagnosisCausationRubricDetails.CausationId == keywordID && diagnosisCausationRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = diagnosisCausationRubricDetails.CausationRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
           ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetPathologyList(int keywordID)
        {
            var rubricKeywordList = (from diagnosisPathologyRubricDetails in context.DiagnosisPathologyRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on diagnosisPathologyRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where diagnosisPathologyRubricDetails.DiagnosisPathologyDetailsId == keywordID && diagnosisPathologyRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = diagnosisPathologyRubricDetails.DiagnosisPathologyRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
            ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetEmergenciesList(int keywordID)
        {
            var rubricKeywordList = (from emergencieRubricDetails in context.EmergencieRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on emergencieRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where emergencieRubricDetails.EmergencieId == keywordID && emergencieRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = emergencieRubricDetails.EmergencieRubricId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
            ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetOnsetList(int keywordID)
        {
            var rubricKeywordList = (from onsetDurationProgressRubricDetails in context.OnsetDurationProgressRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on onsetDurationProgressRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where onsetDurationProgressRubricDetails.OnsetDetailId == keywordID && onsetDurationProgressRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = onsetDurationProgressRubricDetails.OnsetRubricId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
            ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetPatternsRemedyList(int keywordID)
        {
            var rubricKeywordList = (from patternRubricDetails in context.PatternRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on patternRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where patternRubricDetails.PatternDetailsId == keywordID && patternRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = patternRubricDetails.PatternRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
            ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetLocationExtentionRemedyList(int keywordID)
        {
            var rubricKeywordList = (from locationExtentionRubricDetails in context.LocationExtentionRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on locationExtentionRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where locationExtentionRubricDetails.LocationExtentionDetailsId == keywordID && locationExtentionRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = locationExtentionRubricDetails.LocationExtentionRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
             ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetSensationRemedyList(int keywordID)
        {
            var rubricKeywordList = (from sensationRubricDetails in context.SensationRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on sensationRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where sensationRubricDetails.SensationDetailsId == keywordID && sensationRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = sensationRubricDetails.SensationRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
              ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetModalitiesRemedyList(int keywordID)
        {
            var rubricKeywordList = (from modalitiesRubricDetails in context.ModalitiesRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on modalitiesRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where modalitiesRubricDetails.ModalitiesDetailsId == keywordID && modalitiesRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = modalitiesRubricDetails.ModalitiesRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
              ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetAccompaniedRemedyList(int keywordID)
        {
            var rubricKeywordList = (from accompaniedRubricDetails in context.AccompaniedRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on accompaniedRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where accompaniedRubricDetails.AccompaniedDetailsId == keywordID && accompaniedRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = accompaniedRubricDetails.AccompaniedRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
               ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetObservationsRemedyList(int keywordID)
        {
            var rubricKeywordList = (from observationsRubricDetails in context.ObservationsRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on observationsRubricDetails.Subsection equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where observationsRubricDetails.ObservationsDetailsId == keywordID && observationsRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = observationsRubricDetails.ObservationsRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
               ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetBeforeAfterDuringRemedyList(int keywordID)
        {
            var rubricKeywordList = (from beforeAfterDuringRubricDetails in context.BeforeAfterDuringRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on beforeAfterDuringRubricDetails.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where beforeAfterDuringRubricDetails.BeforeAfterDuringDetailsId == keywordID && beforeAfterDuringRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = beforeAfterDuringRubricDetails.BeforeAfterDuringRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
               ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetSymptomsRemedyList(int keywordID)
        {
            var rubricKeywordList = (from diagnosisSymptomRubric in context.DiagnosisSymptomRubrics
                                     join subSectionMaster in context.SubSectionMasters on diagnosisSymptomRubric.SubsectionId equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where diagnosisSymptomRubric.DiagnosisSymptomId == keywordID && diagnosisSymptomRubric.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = diagnosisSymptomRubric.DiagnosisSymptomRubricId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
              ).ToList();
           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetMonogramRemedyList(int keywordID)
        {
            var rubricKeywordList = (from diagnosisMonogramRubricDetails in context.DiagnosisMonogramRubricDetails
                                     join subSectionMaster in context.SubSectionMasters on diagnosisMonogramRubricDetails.Subsections equals subSectionMaster.SubSectionId
                                     join sectionMaster in context.SectionMasters on subSectionMaster.SectionId equals sectionMaster.SectionId
                                     where diagnosisMonogramRubricDetails.DiagnosisMonogramDetailsId == keywordID && diagnosisMonogramRubricDetails.DeletedStatus == false
                                     select new RubricKeywordModel
                                     {
                                         KeywordID = diagnosisMonogramRubricDetails.DiagnosisMonogramRubricDetailsId,
                                         SectionID = sectionMaster.SectionId,
                                         SectionName = sectionMaster.SectionName,
                                         SectionNameAlias = sectionMaster.SectionAlias,
                                         SubSectionID = subSectionMaster.SubSectionId,
                                         SubSectionName = subSectionMaster.SubSectionName,
                                         SubSectionNameAlias = subSectionMaster.SubSectionNameAlias,
                                     }
             ).ToList();

           // var updatedRubricKeywordList = GetUpdatedRubricKeywordList(rubricKeywordList);
            return rubricKeywordList;
        }

        private List<RubricKeywordModel> GetUpdatedRubricKeywordList(List<RubricKeywordModel> rubricKeywordList)
        {
            for (int i = 0; i < rubricKeywordList.Count; i++)
            {
                var rubricKeywordItem = rubricKeywordList[i];
                var gradeGroup = (
                from rrd in context.RubricRemedyDetails
                join rgm in context.RemedyGradeMaster on rrd.GradeId equals rgm.GradeId
                where rrd.SubSectionId == rubricKeywordItem.SubSectionID
                group rgm by new
                { rgm.GradeId, rgm.GradeNo, rgm.FontName, rgm.FontColor, rgm.FontStyle, rgm.Description } into gcs
                select new TabRubricRemedyData
                {
                    GradeID = gcs.Key.GradeId,
                    GradeNumber = gcs.Key.GradeNo,
                    FontName = gcs.Key.FontName,
                    FontColor = gcs.Key.FontColor,
                    FontStyle = gcs.Key.FontStyle,
                    Description = gcs.Key.Description,
                }).ToList();

                if (gradeGroup != null)
                {
                    for (int j = 0; j < gradeGroup.Count; j++)
                    {
                        var groupItem = gradeGroup[j];
                        var rubricRemedyModel = (
                                                    from rrd in context.RubricRemedyDetails
                                                    join rm in context.RemedyMasters on rrd.RemedyId equals rm.RemedyId
                                                    //join rrad in context.RemedyRubricAuthorDetails on rrd.RubricRemedyId equals rrad.RubricRemedyId
                                                    //join authorMaster in context.AuthorMasters on rrad.AuthorId equals authorMaster.AuthorId
                                                    where rrd.SubSectionId == rubricKeywordItem.SubSectionID && rm.DeleteStatus == false && rrd.DeletedStatus == false

                                                    select new RubricRemedyModel
                                                    {
                                                        RemedyId = rm.RemedyId,
                                                        RemedyName = rm.RemedyName,
                                                        RemedyAlias = rm.RemedyAlias,
                                                        //AuthorId = authorMaster.AuthorId,
                                                        //AuthorAlias = authorMaster.AuthorName,
                                                        //AuthorName = authorMaster.AuthorName

                                                    }).ToList();

                        if (rubricRemedyModel != null)
                        {
                            for (int k = 0; k < rubricRemedyModel.Count; k++)
                            {
                                var rubricRemedyModelItem = rubricRemedyModel[k];
                                var authorData = (from rrd in context.RubricRemedyDetails.Where(x => x.SubSectionId == groupItem.SubSectionId && x.GradeId == groupItem.GradeID && x.DeletedStatus == false)
                                                  join rrad in context.RemedyRubricAuthorDetails on rrd.RubricRemedyId equals rrad.RubricRemedyId
                                                  join authorMaster in context.AuthorMasters on rrad.AuthorId equals authorMaster.AuthorId
                                                  select new
                                                  {
                                                      authorId = authorMaster.AuthorId,
                                                      authorAlias = authorMaster.AuthorAlias,
                                                      authorName = authorMaster.AuthorName,
                                                  }
                                      ).FirstOrDefault();
                                if (authorData != null)
                                {
                                    rubricRemedyModelItem.AuthorId = authorData.authorId;
                                    rubricRemedyModelItem.AuthorName = authorData.authorName;
                                    rubricRemedyModelItem.AuthorAlias = authorData.authorAlias;
                                }
                                rubricRemedyModel[k] = rubricRemedyModelItem;
                            }
                        }
                        gradeGroup[j].SubSectionId = rubricKeywordItem.SubSectionID;
                        gradeGroup[j].rubricRemedyModel = rubricRemedyModel;
                    }
                }
                rubricKeywordList[i].tabRubricRemedyData = gradeGroup;
            }

            return rubricKeywordList;
        }

        /// <summary>
        /// Method is used for get diagnosis DDL .
        /// </summary>

        /// <returns></returns>
        public List<DiagnosisDDLModel> GetDiagnosisDDL()
        {
            List<DiagnosisDDLModel> diagnosisDDL = new List<DiagnosisDDLModel>();
            diagnosisDDL = (from diagnosisMaster in context.DiagnosisMasters
                            where diagnosisMaster.DeleteStatus == false
                            select new DiagnosisDDLModel
                            {
                                DiagnosisID = diagnosisMaster.DiagnosisId,
                                DiagnosisName = diagnosisMaster.DiagnosisName,

                            }
                            ).ToList();
           
            return diagnosisDDL;
        }

        public DiagnosisSearchResultModel DiagnosisSearch(int diagnosisID, ref ErrorResponseModel errorResponseModel)
        {
            var searchResult = (from diagnosisMaster in context.DiagnosisMasters.Where(x => x.DiagnosisId == diagnosisID && x.DeleteStatus == false)
                                select new DiagnosisSearchResultModel
                                {
                                    DiagnosisID = diagnosisMaster.DiagnosisId,
                                    DiagnosisName = diagnosisMaster.DiagnosisName,
                                    DiagnosisNameAlias = diagnosisMaster.DiagnosisNameAlias,
                                    Miasm = diagnosisMaster.Miasm,
                                    Investigations = diagnosisMaster.Investigations,
                                    AllopathicMedicines = diagnosisMaster.AllopathicMedicines,
                                    Examiniations = diagnosisMaster.Examiniations,
                                }
                ).FirstOrDefault();

            var diagnosisRemediesData = (from diagnosisDetail in context.DiagnosisDetails.Where(x => x.DiagnosisId == searchResult.DiagnosisID && x.DeleteStatus == false)
                                         join subSectionMater in context.SubSectionMasters.Where(x => x.DeleteStatus == false) on diagnosisDetail.SubSectionId equals subSectionMater.SubSectionId

                                         select new DiagnosisRemediesModel
                                         {
                                             SubSectionId = subSectionMater.SubSectionId,
                                             SubSectionName = subSectionMater.SubSectionName,
                                         }).ToList();

            var diagnosisSystemData=(from diagnosisSystemDetail in context.DiagnosisSystemDetails
                                     join diagnosisSystem in context.DiagnosisSystems on diagnosisSystemDetail.DiagnosisSystemId equals diagnosisSystem.DiagnosisSystemId
                                     where diagnosisSystemDetail.DiagnosisId == searchResult.DiagnosisID && diagnosisSystemDetail.DeletedStatus == false && diagnosisSystem.IsActive==false
                                     select new DiagnosisSystemViewModel
                                     {
                                         DiagnosisSystemID = diagnosisSystem.DiagnosisSystemId,
                                         DiagnosisSystemName = diagnosisSystem.DiagnosisSystemName,
                                     }).ToList();



            searchResult.diagnosisRemediesModels = diagnosisRemediesData;
            searchResult.DiagnosisSystemList = diagnosisSystemData;
            return searchResult;
        }

        private void SyncDiagnosisKeywordSections(int diagnosisId, string keywordType, int keywordDetailId, List<int> sectionIds)
        {
            var distinctSectionIds = (sectionIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var existingRows = context.DiagnosisKeywordSections
                .Where(x => x.KeywordType == keywordType && x.KeywordDetailId == keywordDetailId)
                .ToList();

            foreach (var row in existingRows)
            {
                if (distinctSectionIds.Contains(row.SectionId))
                {
                    if (row.DeleteStatus)
                    {
                        row.DeleteStatus = false;
                        row.ChangedDate = DateTime.Now;
                    }
                }
                else if (!row.DeleteStatus)
                {
                    row.DeleteStatus = true;
                    row.ChangedDate = DateTime.Now;
                }
            }

            foreach (var sectionId in distinctSectionIds)
            {
                if (!existingRows.Any(x => x.SectionId == sectionId))
                {
                    context.DiagnosisKeywordSections.Add(new DiagnosisKeywordSection
                    {
                        DiagnosisId = diagnosisId,
                        KeywordType = keywordType,
                        KeywordDetailId = keywordDetailId,
                        SectionId = sectionId,
                        DeleteStatus = false,
                        EnteredDate = DateTime.Now
                    });
                }
            }

            context.SaveChanges();
        }

        private void SoftDeleteDiagnosisKeywordSections(string keywordType, int keywordDetailId)
        {
            var rows = context.DiagnosisKeywordSections
                .Where(x => x.KeywordType == keywordType
                            && x.KeywordDetailId == keywordDetailId
                            && !x.DeleteStatus)
                .ToList();

            if (!rows.Any())
            {
                return;
            }

            foreach (var row in rows)
            {
                row.DeleteStatus = true;
                row.ChangedDate = DateTime.Now;
            }

            context.SaveChanges();
        }

        private string SoftDeleteDiagnosisKeywordByTab(string diagnosisTab, int keywordId)
        {
            string message = string.Empty;

            if (keywordId <= 0)
            {
                return message;
            }

            switch (diagnosisTab)
            {
                case "Symptoms":
                    var diagnosisSymptom = context.DiagnosisSymptoms.FirstOrDefault(x => x.DiagnosisSymptomId == keywordId);
                    if (diagnosisSymptom != null)
                    {
                        diagnosisSymptom.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Symptoms", keywordId);
                        context.SaveChanges();
                        message = "Diagnosis Symptom Deleted Successfully";
                    }
                    break;
                case "Monogram":
                    var diagnosisMonogram = context.DiagnosisMonogramDetails.FirstOrDefault(x => x.DiagnosisMonogramDetailsId == keywordId);
                    if (diagnosisMonogram != null)
                    {
                        diagnosisMonogram.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Monogram", keywordId);
                        context.SaveChanges();
                        message = "Diagnosis Monogram Deleted Successfully";
                    }
                    break;
                case "Causations":
                    var diagnosisCausation = context.DiagnosisCausations.FirstOrDefault(x => x.CausationId == keywordId);
                    if (diagnosisCausation != null)
                    {
                        diagnosisCausation.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Causations", keywordId);
                        context.SaveChanges();
                        message = "Diagnosis Causation Deleted Successfully";
                    }
                    break;
                case "Pathology":
                    var diagnosisPathology = context.DiagnosisPathologyDetails.FirstOrDefault(x => x.DiagnosisPathologyDetailsId == keywordId);
                    if (diagnosisPathology != null)
                    {
                        diagnosisPathology.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Pathology", keywordId);
                        context.SaveChanges();
                        message = "Diagnosis Pathology Deleted Successfully";
                    }
                    break;
                case "Emergencies":
                    var emergencie = context.EmergencieDetails.FirstOrDefault(x => x.EmergencieId == keywordId);
                    if (emergencie != null)
                    {
                        emergencie.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Emergencies", keywordId);
                        context.SaveChanges();
                        message = "Emergencie Deleted Successfully";
                    }
                    break;
                case "Onset":
                    var onset = context.OnsetDurationProgressDetails.FirstOrDefault(x => x.OnsetDetailId == keywordId);
                    if (onset != null)
                    {
                        onset.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Onset", keywordId);
                        context.SaveChanges();
                        message = "Onset/Duration/Progress Deleted Successfully";
                    }
                    break;
                case "Patterns":
                    var pattern = context.PatternsDetails.FirstOrDefault(x => x.PatternDetailsId == keywordId);
                    if (pattern != null)
                    {
                        pattern.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Patterns", keywordId);
                        context.SaveChanges();
                        message = "Pattern Deleted Successfully";
                    }
                    break;
                case "LocationExtention":
                    var locationExtention = context.LocationExtentionDetails.FirstOrDefault(x => x.LocationExtentionDetailsId == keywordId);
                    if (locationExtention != null)
                    {
                        locationExtention.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("LocationExtention", keywordId);
                        context.SaveChanges();
                        message = "Location-Extension Deleted Successfully";
                    }
                    break;
                case "Sensation":
                    var sensation = context.SensationDetails.FirstOrDefault(x => x.SensationDetailsId == keywordId);
                    if (sensation != null)
                    {
                        sensation.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Sensation", keywordId);
                        context.SaveChanges();
                        message = "Sensation Deleted Successfully";
                    }
                    break;
                case "Modalities":
                    var modalities = context.ModalitiesDetails.FirstOrDefault(x => x.ModalitiesDetailsId == keywordId);
                    if (modalities != null)
                    {
                        modalities.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Modalities", keywordId);
                        context.SaveChanges();
                        message = "Modalities Deleted Successfully";
                    }
                    break;
                case "Accompanied":
                    var accompanied = context.AccompaniedDetails.FirstOrDefault(x => x.AccompaniedDetailsId == keywordId);
                    if (accompanied != null)
                    {
                        accompanied.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Accompanied", keywordId);
                        context.SaveChanges();
                        message = "Accompanied Deleted Successfully";
                    }
                    break;
                case "Observations":
                    var observations = context.ObservationsDetails.FirstOrDefault(x => x.ObservationsDetailsId == keywordId);
                    if (observations != null)
                    {
                        observations.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("Observations", keywordId);
                        context.SaveChanges();
                        message = "Observations Deleted Successfully";
                    }
                    break;
                case "BeforeAfterDuring":
                    var beforeAfterDuring = context.BeforeAfterDuringDetails.FirstOrDefault(x => x.BeforeAfterDuringDetailsId == keywordId);
                    if (beforeAfterDuring != null)
                    {
                        beforeAfterDuring.DeletedStatus = true;
                        SoftDeleteDiagnosisKeywordSections("BeforeAfterDuring", keywordId);
                        context.SaveChanges();
                        message = "Before/After/During Deleted Successfully";
                    }
                    break;
            }

            return message;
        }

        private void AttachDiagnosisKeywordSections(string keywordType, List<int> keywordDetailIds, Action<int, List<int>, List<SectionViewModel>> apply)
        {
            if (keywordDetailIds == null || keywordDetailIds.Count == 0)
            {
                return;
            }

            var sectionLinks = (from map in context.DiagnosisKeywordSections
                                join section in context.SectionMasters on map.SectionId equals section.SectionId
                                where map.KeywordType == keywordType
                                      && keywordDetailIds.Contains(map.KeywordDetailId)
                                      && !map.DeleteStatus
                                      && !section.DeleteStatus
                                select new
                                {
                                    map.KeywordDetailId,
                                    Section = new SectionViewModel
                                    {
                                        SectionId = section.SectionId,
                                        SectionName = section.SectionName,
                                        SectionAlias = section.SectionAlias,
                                        Description = section.Description
                                    }
                                }).ToList();

            foreach (var detailId in keywordDetailIds.Distinct())
            {
                var sections = sectionLinks
                    .Where(x => x.KeywordDetailId == detailId)
                    .Select(x => x.Section)
                    .ToList();
                var sectionIds = sections
                    .Where(x => x.SectionId.HasValue)
                    .Select(x => x.SectionId.Value)
                    .ToList();
                apply(detailId, sectionIds, sections);
            }
        }

        public DiagnosisTherapeuticsModel GetdiagnosisTherapeuticsDetail(int diagnosisID, ref ErrorResponseModel errorResponseModel)
            {
                var diagnosisTherapeutics = (from diagnosisMaster in context.DiagnosisMasters
                                    join diagnosisTherapeuticsDetail in context.DiagnosisTherapeuticsDetails
                                  on diagnosisMaster.DiagnosisId equals diagnosisTherapeuticsDetail.DiagnosisId
                                    where diagnosisTherapeuticsDetail.DiagnosisId==diagnosisID && diagnosisTherapeuticsDetail.DeletedStatus== false
                                    select new DiagnosisTherapeuticsModel
                                    {
                                        DiagnosisID = diagnosisMaster.DiagnosisId,
                                        DiagnosisTherapeuticID = diagnosisTherapeuticsDetail.DiagnosisTherapeuticsDetailId,
                                        DiagnosisTherapeutics = diagnosisTherapeuticsDetail.DiagnosisTherapeuticsDetail1,
                                    }
                    ).FirstOrDefault();
                return diagnosisTherapeutics;
            }
    }
}

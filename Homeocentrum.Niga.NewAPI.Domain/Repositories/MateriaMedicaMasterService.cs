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
    public class MateriaMedicaMasterService : IMateriaMedicaMasterService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public MateriaMedicaMasterService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.NewAPI.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Methood to get MateriaMedicaHead by MateriaMedicaHeadId
        /// </summary>
        /// <param name="materiamedicaId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public MateriaMedicaMasterModel GetMateriaMedicaById(long materiamedicaId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var materiamedicaEntity = context.MateriaMedicaMasters.FirstOrDefault(x => x.MateriaMedicaId == materiamedicaId && x.IsDeleted == false);
            var materiadetail = context.MateriaMedicaDetails.FirstOrDefault(x => x.MateriaMedicaId == materiamedicaId);
            if (materiamedicaEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "MateriaMedica Not Found";
                return null;
            }
            MateriaMedicaDetailsModel matDetails = new MateriaMedicaDetailsModel();
            List<MateriaMedicaDetailsModel> lstmatDetails = new List<MateriaMedicaDetailsModel>();
            if (materiadetail != null)
            {
                matDetails.MatriaMedicaDetailId = materiadetail.MatriaMedicaDetailId;
                matDetails.MateriaMedicaId = materiadetail.MateriaMedicaId;
                matDetails.Details = materiadetail.MateriaMedicaDetail1;
                lstmatDetails.Add(matDetails);
            }



            return new MateriaMedicaMasterModel
            {
                MateriaMedicaId = materiamedicaEntity.MateriaMedicaId,
                AuthorId = materiamedicaEntity.AuthorId,
                RemedyId = materiamedicaEntity.RemedyId,
                MateriaMedicaHeadId = materiamedicaEntity.MateriaMedicaHeadId,
                Dose = materiamedicaEntity.Dose,
                EnteredBy = materiamedicaEntity.EnteredBy,
                EnteredDate = materiamedicaEntity.EnteredDate,
                ChangedBy = materiamedicaEntity.ChangedBy,
                ChangedDate = materiamedicaEntity.ChangedDate,
                SeqNo = materiamedicaEntity.SeqNo,
                IsActive = materiamedicaEntity.IsActive,
                IsDeleted = materiamedicaEntity.IsDeleted,
                ModelEx = lstmatDetails



            };
        }

        /// <summary>
        /// Method to get all the MateriaMedicaHead
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<MateriaMedicaMasterModel1> GetMateriaMedica(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel)
        {
            var MateriaMedicaList = new List<MateriaMedicaMasterModel1>();
            errorResponseModel = new ErrorResponseModel();

            var materiamedicalistEntity = (from m in context.MateriaMedicaMasters
                                          join auth in context.AuthorMasters on m.AuthorId equals auth.AuthorId
                                          join rem in context.RemedyMasters on m.RemedyId equals rem.RemedyId
                                          join mhead in context.MateriaMedicaHeadMasters
                                          on m.MateriaMedicaHeadId equals mhead.MateriaMedicaHeadId
                                           where m.IsDeleted==false
                                          select new
                                          {
                                              m.MateriaMedicaId,
                                              m.AuthorId,
                                              m.RemedyId,
                                              m.MateriaMedicaHeadId,
                                              m.Dose,
                                              m.EnteredBy,
                                              m.EnteredDate,
                                              m.ChangedBy,
                                              m.ChangedDate,
                                              m.SeqNo,
                                              m.IsDeleted,
                                              m.IsActive
,                                             auth.AuthorName,
                                              rem.RemedyName,
                                              mhead.MateriaMedicaHeadName
                                          }).Skip((nigaParameters.PageNumber - 1) * nigaParameters.PageSize)
             .Take(nigaParameters.PageSize)
             .ToList();




            if (materiamedicalistEntity.Count == 0) 
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "MateriaMedica Not Found";
            }
            materiamedicalistEntity.ForEach(item =>
            {
                MateriaMedicaList.Add(new MateriaMedicaMasterModel1
                {
                    MateriaMedicaId = item.MateriaMedicaId,
                    AuthorId = item.AuthorId,
                    RemedyId = item.RemedyId,
                    MateriaMedicaHeadId = item.MateriaMedicaHeadId,
                    Dose = item.Dose,
                    EnteredBy = item.EnteredBy,
                    EnteredDate = item.EnteredDate,
                    ChangedBy = item.ChangedBy,
                    ChangedDate = item.ChangedDate,
                    SeqNo = item.SeqNo,
                    IsActive = item.IsActive,
                    IsDeleted = item.IsDeleted,
                    AuthorName = item.AuthorName,
                    RemedyName = item.RemedyName,
                    MateriaMedicaHeadName=item.MateriaMedicaHeadName,
                });
            });
            return MateriaMedicaList;
        }

        /// <summary>
        /// Method implementation for saving new MateriaMedica
        /// </summary>
        /// <param name="materiamedicamodel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveMateriaMedica(MateriaMedicaMasterModel materiamedicamodel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (materiamedicamodel.MateriaMedicaId == 0)
            {
                MateriaMedicaMaster materiamedicaEntity = new MateriaMedicaMaster();
                materiamedicaEntity.MateriaMedicaId = materiamedicamodel.MateriaMedicaId;
                materiamedicaEntity.AuthorId = materiamedicamodel.AuthorId;
                materiamedicaEntity.RemedyId = materiamedicamodel.RemedyId;
                materiamedicaEntity.MateriaMedicaHeadId = materiamedicamodel.MateriaMedicaHeadId;
                materiamedicaEntity.Dose = materiamedicamodel.Dose;
                materiamedicaEntity.EnteredBy = materiamedicamodel.EnteredBy;
                materiamedicaEntity.EnteredDate = materiamedicamodel.EnteredDate;
                materiamedicaEntity.ChangedBy = materiamedicamodel.ChangedBy;
                materiamedicaEntity.ChangedDate = materiamedicamodel.ChangedDate;
                materiamedicaEntity.SeqNo = materiamedicamodel.SeqNo;
                materiamedicaEntity.IsActive = materiamedicamodel.IsActive;
                materiamedicaEntity.IsDeleted = materiamedicamodel.IsDeleted;
                context.MateriaMedicaMasters.Add(materiamedicaEntity);
                context.SaveChanges();
                foreach (var item in materiamedicamodel.ModelEx)
                {
                    var modeldetails = new MateriaMedicaDetail();
                    modeldetails.MateriaMedicaId = materiamedicaEntity.MateriaMedicaId;
                    modeldetails.MateriaMedicaDetail1 = item.Details;
                    context.MateriaMedicaDetails.Add(modeldetails);
                    context.SaveChanges();
                }
                Message = "MateriaMedica Saved Successfully";
            }
            else//update
            {
                var materiamedicaEntity = context.MateriaMedicaMasters.FirstOrDefault(x => x.MateriaMedicaId == materiamedicamodel.MateriaMedicaId);
                if (materiamedicaEntity != null)
                {
                    materiamedicaEntity.AuthorId = materiamedicamodel.AuthorId;
                    materiamedicaEntity.RemedyId = materiamedicamodel.RemedyId;
                    materiamedicaEntity.MateriaMedicaHeadId = materiamedicamodel.MateriaMedicaHeadId;
                    materiamedicaEntity.Dose = materiamedicamodel.Dose;
                    materiamedicaEntity.EnteredBy = materiamedicamodel.EnteredBy;
                    materiamedicaEntity.EnteredDate = materiamedicamodel.EnteredDate;
                    materiamedicaEntity.ChangedBy = materiamedicamodel.ChangedBy;
                    materiamedicaEntity.ChangedDate = materiamedicamodel.ChangedDate;
                   materiamedicaEntity.SeqNo = materiamedicamodel.SeqNo;
                    materiamedicaEntity.IsActive = materiamedicamodel.IsActive;
                    materiamedicaEntity.IsDeleted = materiamedicamodel.IsDeleted;




                    var materiamedicaEntity1 = context.MateriaMedicaDetails.FirstOrDefault(x => x.MateriaMedicaId == materiamedicamodel.MateriaMedicaId);
                    // foreach (var item in materiamedicamodel.ModelEx)
                    //{
                    if (materiamedicaEntity1 != null)
                    {
                        var modeldetails = new MateriaMedicaDetail();
                        materiamedicaEntity1.MateriaMedicaDetail1 = materiamedicamodel.ModelEx[0].Details;
                        //context.MateriaMedicaDetails.Add(modeldetails);
                        //context.SaveChanges();
                    }
                    // }
                    context.SaveChanges();
                    Message = "MateriaMedica Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete MateriaMedica.
        /// </summary>
        /// <param name="materiamedicamodel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteMateriaMedica(MateriaMedicaMasterModel materiamedicamodel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var materiamedicadetailEntity = context.MateriaMedicaDetails.FirstOrDefault(x => x.MateriaMedicaId == materiamedicamodel.MateriaMedicaId);


            if (materiamedicadetailEntity != null)
            {
                
                context.Remove(materiamedicadetailEntity);
                context.SaveChanges();
               // Message = "MateriaMedica Deleted Successfully";
            }


            var materiamedicaEntity = context.MateriaMedicaMasters.FirstOrDefault(x => x.MateriaMedicaId == materiamedicamodel.MateriaMedicaId);
                                        

            if (materiamedicaEntity != null)
            {
                materiamedicaEntity.IsDeleted =true ;
                //context.Remove(materiamedicaEntity);
                context.SaveChanges();
                Message = "MateriaMedica Deleted Successfully";
            }
            return Message;
        }

        /// </summary>
        /// <param name="authorId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        //public List<MasterModel> GetMateriaMedicaAuthor(long authorId, ref ErrorResponseModel errorResponseModel)
        //{
        //    var materiamedicaModel = new List<MasterModel>();
        //    errorResponseModel = new ErrorResponseModel();
        //    var materiamedicaEntity = (from m in context.MateriaMedicaMasters
        //                               join auth in context.AuthorMasters on m.AuthorId equals auth.AuthorId
        //                               where m.AuthorId == authorId

        //                               select new
        //                                   {

        //                                   m.MateriaMedicaHeadName


        //                               }).ToList();
        //    if (materiamedicaEntity.Count == 0)
        //    {
        //        errorResponseModel.StatusCode = HttpStatusCode.NotFound;
        //        errorResponseModel.Message = "MateriaMedica not found";
        //    }
        //   materiamedicaEntity.ForEach(item =>
        //    {
        //        materiamedicaModel.Add(new MateriaMedicaMasterModel
        //        {
                    
        //            MateriaMedicaHeadName=item.MateriaMedicaHeadName

        //        });

        //    });

        //    return materiamedicaModel;
        //}

      public  List<MateriaMedicaMasterModel2> GetMateriaMedicaHeadByAuthorId(long authorId, ref ErrorResponseModel errorResponseModel)
        {
            var materiamedicaModel = new List<MateriaMedicaMasterModel2>();
            errorResponseModel = new ErrorResponseModel();
            var materiamedicaEntity=(from mhead in context.MateriaMedicaHeadMasters
                                     join auth in context.AuthorMasters 
                                     on mhead.AuthorId equals auth.AuthorId
                                     where mhead.AuthorId == authorId && mhead.IsDeleted==false
                                     select new
                                      {
                                          mhead.MateriaMedicaHeadId,
                                          mhead.MateriaMedicaHeadName
                                      }).ToList();
            if (materiamedicaEntity.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "MateriaMedica not found";
            }
            materiamedicaEntity.ForEach(item =>
            {
                materiamedicaModel.Add(new MateriaMedicaMasterModel2
                {
                   MateriaMedicaHeadId=item.MateriaMedicaHeadId,
                    MateriaMedicaHeadName = item.MateriaMedicaHeadName,
                });

            });

            return materiamedicaModel;

        }

        //Created by Vikas 12/09/2023

        /// <summary>
        /// Method for getting all the authors
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<AuthorDDLModel> GetAuthorDDL(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var authorEntityList = (from authorMaster in context.AuthorMasters
                                    where authorMaster.IsDeleted == false && authorMaster.IsForRepertory == false
                                    select new AuthorDDLModel
                                    {
                                        AuthorId = authorMaster.AuthorId,
                                        AuthorName = authorMaster.AuthorName,

                                    }).ToList();
            if (authorEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Author not found";
            }
           
            return authorEntityList;
        }

        /// <summary>
        /// Method for getting all the authors
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<RemedyDDLModel> GetRemedyDDL(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var remedyList = (from remedyMaster in context.RemedyMasters
                                    where remedyMaster.DeleteStatus == false
                                    select new RemedyDDLModel
                                    {
                                        RemedyId = remedyMaster.RemedyId,
                                        RemedyName = remedyMaster.RemedyName,

                                    }).ToList();




            if (remedyList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Remedy not found";
            }

            return remedyList;
        }

        /// <summary>
        /// Method to get all the MateriaMedicaHead
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<MateriaMedicaModel> GetMateriaMedicaByAuthorRemedy(MateriaMedicaFilterModel materiaMedicaFilter, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var MateriaMedicaList = (from m in context.MateriaMedicaMasters
                                           join auth in context.AuthorMasters on m.AuthorId equals auth.AuthorId
                                           join rem in context.RemedyMasters on m.RemedyId equals rem.RemedyId
                                           join mhead in context.MateriaMedicaHeadMasters
                                           on m.MateriaMedicaHeadId equals mhead.MateriaMedicaHeadId
                                           where m.IsDeleted == false &&
                                           ((materiaMedicaFilter.AuthorId == 0 || m.AuthorId == materiaMedicaFilter.AuthorId) && 
                                            (materiaMedicaFilter.RemedyId == 0 || m.RemedyId == materiaMedicaFilter.RemedyId))
                                           select new MateriaMedicaModel
                                           {
                                                MateriaMedicaId= m.MateriaMedicaId,
                                                AuthorId= m.AuthorId,
                                                RemedyId= m.RemedyId,
                                                MateriaMedicaHeadId= m.MateriaMedicaHeadId,
                                                AuthorName= auth.AuthorName,
                                                RemedyName = rem.RemedyName,
                                                MateriaMedicaHeadName= mhead.MateriaMedicaHeadName
                                           }).Skip((materiaMedicaFilter.NigaParameter.PageNumber - 1) * materiaMedicaFilter.NigaParameter.PageSize)
             .Take(materiaMedicaFilter.NigaParameter.PageSize)
             .ToList();

            if (MateriaMedicaList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "MateriaMedica Not Found";
            }
           
            return MateriaMedicaList;
        }
    }
}

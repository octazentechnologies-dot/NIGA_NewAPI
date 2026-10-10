#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    public class MateriaMedicaHeadService : IMateriaMedicaHeadMasterService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public MateriaMedicaHeadService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method implementation for saving new MateriaMedicaHead
        /// </summary>
        /// <param name="materiamedicaheadModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        public string SaveMateriaMedicaHead(MateriaMedicaHeadMasterModel materiamedicaheadModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (materiamedicaheadModel.MateriaMedicaHeadId == 0)
            {
                MateriaMedicaHeadMaster materiamedicaheadEntity = new MateriaMedicaHeadMaster();
                materiamedicaheadEntity.AuthorId = materiamedicaheadModel.AuthorId;
                materiamedicaheadEntity.MateriaMedicaHeadName = materiamedicaheadModel.MateriaMedicaHeadName;
                materiamedicaheadEntity.Description = materiamedicaheadModel.Description;
                materiamedicaheadEntity.IsSection = materiamedicaheadModel.IsSection;
                materiamedicaheadEntity.SeqNo = materiamedicaheadModel.SeqNo;
                materiamedicaheadEntity.IsDeleted = materiamedicaheadModel.IsDeleted;
                materiamedicaheadEntity.DifferentialMm = false;
                context.MateriaMedicaHeadMasters.Add(materiamedicaheadEntity);
                context.SaveChanges();
                Message = "Materia Medica Head Saved Successfully";
            }
            else
            {
                var materiamedicaheadEntity = context.MateriaMedicaHeadMasters.FirstOrDefault(x => x.MateriaMedicaHeadId == materiamedicaheadModel.MateriaMedicaHeadId);
                if (materiamedicaheadEntity != null)
                {

                    materiamedicaheadEntity.AuthorId = materiamedicaheadModel.AuthorId;
                    materiamedicaheadEntity.MateriaMedicaHeadName = materiamedicaheadModel.MateriaMedicaHeadName;
                    materiamedicaheadEntity.Description = materiamedicaheadModel.Description;
                    materiamedicaheadEntity.IsSection = materiamedicaheadModel.IsSection;
                    materiamedicaheadEntity.SeqNo = materiamedicaheadModel.SeqNo;
                    materiamedicaheadEntity.IsDeleted = materiamedicaheadModel.IsDeleted;
                    materiamedicaheadEntity.DifferentialMm = materiamedicaheadModel.DifferentialMM;
                    context.SaveChanges();
                    Message = "Materia Medica Head Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete MateriaMedicaHead.
        /// </summary>
        /// <param name="materiamedicaheadModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteMateriaMedicaHead(MateriaMedicaHeadMasterModel materiamedicaheadModel, ref ErrorResponseModel errorResponseModel)
        {
            //string Message = "";
            //var materiamedicadetailEntity = context.MateriaMedicaDetails.FirstOrDefault(x => x.MateriaMedicaId == materiamedicamodel.MateriaMedicaId);


            //if (materiamedicadetailEntity != null)
            //{
            //    context.Remove(materiamedicadetailEntity);
            //    context.SaveChanges();
            //    // Message = "MateriaMedica Deleted Successfully";
            //}



            string Message = "";
            //var materiamedicamasterEntity = context.MateriaMedicaMasters.FirstOrDefault(x => x.MateriaMedicaHeadId == materiamedicaheadModel.MateriaMedicaHeadId);
            //if (materiamedicamasterEntity != null)
            //{
            //    context.Remove(materiamedicamasterEntity);
            //    context.SaveChanges();
            //   // Message = "MateriaMedicaHead Deleted Successfully";
            //}


            var materiamedicaheadEntity = context.MateriaMedicaHeadMasters.FirstOrDefault(x => x.MateriaMedicaHeadId == materiamedicaheadModel.MateriaMedicaHeadId);
            if (materiamedicaheadEntity != null)
            {
                materiamedicaheadEntity.IsDeleted = true;
                //context.Remove(materiamedicaheadEntity);
                context.SaveChanges();
                Message = "Materia Medica Head Deleted Successfully";
            }
            return Message;
        }

        public string UpdateDifferentialMateriaMedicadDefaultStatus(int materiaMedicaHeadId, bool differentialMMDefaultStatus)
        {
            string Message = "Fail to update differential materia Medica default status";
            var materiamedicamasterEntity = context.MateriaMedicaHeadMasters.FirstOrDefault(x => x.MateriaMedicaHeadId == materiaMedicaHeadId);
            if (materiamedicamasterEntity != null)
            {
                materiamedicamasterEntity.DifferentialMm = differentialMMDefaultStatus;
                context.SaveChanges();
                Message = "Update successfully differential materia Medica default status";
            }

            return Message;
        }
    }
}

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

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Implementation
{
    public class BodyPartService : IBodyPartService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public BodyPartService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.NewAPI.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method implementation for saving new BodyPart
        /// </summary>
        /// <param name="bodypartModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveBodyPart(BodyPartModel bodypartModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (bodypartModel.BodyPartId == 0)
            {
                BodyPartMaster bodypartEntity = new BodyPartMaster();
                bodypartEntity.SectionId = bodypartModel.SectionId;
                bodypartEntity.BodyPartName = bodypartModel.BodyPartName;
                bodypartEntity.Description = bodypartModel.Description;
                bodypartEntity.EnteredBy = bodypartModel.EnteredBy;
                bodypartEntity.EnteredDate = DateTime.Now;
                context.BodyPartMasters.Add(bodypartEntity);
                context.SaveChanges();
                Message = "Body Part Saved Successfully";
            }
            else
            {
                var bodypartEntity = context.BodyPartMasters.FirstOrDefault(x => x.BodyPartId == bodypartModel.BodyPartId);
                if (bodypartEntity != null)
                {

                    bodypartEntity.SectionId = bodypartModel.SectionId;
                    bodypartEntity.BodyPartName = bodypartModel.BodyPartName;
                    bodypartEntity.Description = bodypartModel.Description;
                    bodypartEntity.ChangedBy = bodypartModel.EnteredBy;
                    bodypartEntity.ChangedDate = DateTime.Now;
                    context.SaveChanges();
                    Message = "Body Part Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete bodypart.
        /// </summary>
        /// <param name="bodypartModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteBodyPart(BodyPartModel bodypartModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var bodypartEntity = context.BodyPartMasters.FirstOrDefault(x => x.BodyPartId == bodypartModel.BodyPartId);
            if (bodypartEntity != null)
            {
                bodypartEntity.DeleteStatus = bodypartModel.DeleteStatus;
                bodypartEntity.ChangedBy = bodypartModel.EnteredBy;
                bodypartEntity.ChangedDate = DateTime.Now;
                context.SaveChanges();
                Message = "Body Part Deleted Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Interface is used to deactivate bodypart.
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<BodyPartModel> GetBodyPartBySection(long SectionId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var bodypartModelList = new List<BodyPartModel>();
            var bodypartEntityList = context.BodyPartMasters.Where(x => x.SectionId == SectionId && x.DeleteStatus==false).ToList();
            if (bodypartEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Body Part not found";
            }
            bodypartEntityList.ForEach(item =>
            {
                bodypartModelList.Add(new BodyPartModel
                {
                    BodyPartId = item.BodyPartId,
                    SectionId = item.SectionId,
                    BodyPartName = item.BodyPartName,
                    Description = item.Description,
                    EnteredDate = item.EnteredDate,
                    EnteredBy = item.EnteredBy,
                    ChangedBy = item.ChangedBy,
                    ChangedDate = item.ChangedDate,
                    DeleteStatus = item.DeleteStatus
                });
            });
            return bodypartModelList;
        }
    }
}

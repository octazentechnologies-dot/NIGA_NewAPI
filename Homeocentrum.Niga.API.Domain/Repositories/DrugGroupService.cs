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
    public class DrugGroupService : IDrugGroupService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public DrugGroupService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Methood to get DrugGroup by drugGroupId
        /// </summary>
        /// <param name="drugGroupId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public DrugGroupModel GetDrugGroupById(long drugGroupId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var drugGroupEntity = context.DrugGroupMasters.Include(x=>x.DrugSystem).FirstOrDefault(x => x.DrugGroupId == drugGroupId && x.DeleteStatus == false);
            if (drugGroupEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "DrugGroup not found";
            }
            return new DrugGroupModel
            {
                DrugGroupId = drugGroupEntity.DrugGroupId,
                DrugGroupName = drugGroupEntity.DrugGroupName,
                DrugSystemName = drugGroupEntity.DrugSystem.DrugSystemName,
                DrugSystemId = drugGroupEntity.DrugSystemId ,
                DeleteStatus = drugGroupEntity.DeleteStatus,
            };
        }

        /// <summary>
        /// Method to get all the DrugGroup
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<DrugGroupModel> GetDrugGroup(ref ErrorResponseModel errorResponseModel)
        {
            var drugGroupModelList = new List<DrugGroupModel>();
            errorResponseModel = new ErrorResponseModel();
            var drugGroupEntityList = context.DrugGroupMasters.Include(x=>x.DrugSystem).Where(x => x.DeleteStatus == false).ToList();
            if (drugGroupEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "DrugGroup not found";
            }

            drugGroupEntityList.ForEach(item =>
            {
                drugGroupModelList.Add(new DrugGroupModel
                {
                    DrugGroupId = item.DrugGroupId,
                    DrugGroupName = item.DrugGroupName,
                    DrugSystemName = item.DrugSystem.DrugSystemName,
                    DrugSystemId = item.DrugSystemId,
                    DeleteStatus = item.DeleteStatus,
                });
            });
            return drugGroupModelList;
        }

        /// <summary>
        /// Method implementation for saving new DrugGroup
        /// </summary>
        /// <param name="DrugGroupModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveDrugGroup(DrugGroupModel drugGroupModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (drugGroupModel.DrugGroupId == 0)
            {
                DrugGroupMaster drugGroupEntity = new DrugGroupMaster();
                drugGroupEntity.DrugGroupName = drugGroupModel.DrugGroupName;
                drugGroupEntity.DrugSystemId = drugGroupModel.DrugSystemId;
                drugGroupEntity.DeleteStatus = false;
                context.DrugGroupMasters.Add(drugGroupEntity);
                context.SaveChanges();
                Message = "DrugGroup Saved Successfully";
            }
            else
            {
                var drugGroupEntity = context.DrugGroupMasters.FirstOrDefault(x => x.DrugGroupId == drugGroupModel.DrugGroupId);
                if (drugGroupEntity != null)
                {

                    drugGroupEntity.DrugGroupName = drugGroupModel.DrugGroupName;
                    drugGroupEntity.DrugSystemId = drugGroupModel.DrugSystemId;
                    drugGroupEntity.DeleteStatus = false;

                    context.SaveChanges();
                    Message = "DrugGroup Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete DrugGroup.
        /// </summary>
        /// <param name="drugGroupModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteDrugGroup(DrugGroupModel drugGroupModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var drugGroupEntity = context.DrugGroupMasters.FirstOrDefault(x => x.DrugGroupId == drugGroupModel.DrugGroupId);
            if (drugGroupEntity != null)
            {
                drugGroupEntity.DeleteStatus = true;
                context.SaveChanges();
                Message = "DrugGroup Deleted Successfully";
            }
            return Message;
        }
    }
}

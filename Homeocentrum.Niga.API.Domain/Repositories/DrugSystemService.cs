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
    public class DrugSystemService : IDrugSystemService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public DrugSystemService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method implementation for saving new DrugSystem
        /// </summary>
        /// <param name="DrugSystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveDrugSystem(DrugSystemModel drugSystemModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (drugSystemModel.DrugSystemId == 0)
            {
                DrugSystemMaster drugSystemEntity = new DrugSystemMaster();
                drugSystemEntity.DrugSystemName = drugSystemModel.DrugSystemName;
                drugSystemEntity.DeleteStatus = false;
                context.DrugSystemMasters.Add(drugSystemEntity);
                context.SaveChanges();
                Message = "DrugSystem Saved Successfully";
            }
            else
            {
                var drugSystemEntity = context.DrugSystemMasters.FirstOrDefault(x => x.DrugSystemId == drugSystemModel.DrugSystemId);
                if (drugSystemEntity != null)
                {

                    drugSystemEntity.DrugSystemName = drugSystemModel.DrugSystemName;
                    drugSystemEntity.DeleteStatus = false;

                    context.SaveChanges();
                    Message = "DrugSystem Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete DrugSystem.
        /// </summary>
        /// <param name="drugSystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteDrugSystem(DrugSystemModel drugSystemModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var drugSystemEntity = context.DrugSystemMasters.FirstOrDefault(x => x.DrugSystemId == drugSystemModel.DrugSystemId);
            if (drugSystemEntity != null)
            {
                drugSystemEntity.DeleteStatus = true;
                context.SaveChanges();
                Message = "DrugSystem Deleted Successfully";
            }
            return Message;
        }
    }
}

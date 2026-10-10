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

namespace Homeocentrum.Niga.API.Domain.Business.Interface
{
    public interface IDrugGroupService
    {
        /// <summary>
        /// Method is used for to get DrugGroup by drugGroupId
        /// </summary>
        /// <param name="drugGroupId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        DrugGroupModel GetDrugGroupById(long drugGroupId, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Method is used for get all the DrugGroup
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<DrugGroupModel> GetDrugGroup(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save DrugGroup
        /// </summary>
        /// <param name="drugSystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveDrugGroup(DrugGroupModel drugSystemModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate DrugGroup.
        /// </summary>
        /// <param name="drugSystemModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteDrugGroup(DrugGroupModel drugSystemModel, ref ErrorResponseModel errorResponseModel);
    }
}

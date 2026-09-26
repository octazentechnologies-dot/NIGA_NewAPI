using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
{
    public interface IRoleDetailsService
    {
        /// <summary>
        /// Method is used for to get role details by recordid
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        RoleDetailsModel GetRoleDetailsById(long recordId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the RoleDetails
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<RoleDetailsModel> GetRoleDetails(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to save RoleDetails
        /// </summary>
        /// <param name="roleDetailsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveRoleDetails(RoleDetailsModel roleDetailsModel, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to deactivate RoleDetails.
        /// </summary>
        /// <param name="roleDetailsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteRoleDetails(RoleDetailsModel roleDetailsModel, ref ErrorResponseModel errorResponseModel);

     
    }
}

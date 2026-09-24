using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
{
    public interface IRoleMasterService
    {
        /// <summary>
        /// Method is used for to get role by roleid
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        RoleMasterModel GetRoleById(long roleId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for get all the RoleMaster
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<RoleMasterModel> GetRoleMaster(ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to save RoleMaster
        /// </summary>
        /// <param name="roleMasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveRoleMaster(RoleMasterModel roleMasterModel, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Interface is used to deactivate RoleMaster.
        /// </summary>
        /// <param name="roleMasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteRoleMaster(RoleMasterModel roleMasterModel, ref ErrorResponseModel errorResponseModel);
    }
}

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
using Homeocentrum.Niga.API.Domain.Interface;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    public class RoleMasterService : IRoleMasterService
    {
        NIGACentrumContext context;
        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public RoleMasterService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Methood to get role by roleid
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public RoleMasterModel GetRoleById(long roleId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var roleMasterEntity = context.RoleMasters.FirstOrDefault(x => x.RoleId == roleId && !x.DeleteStatus);
            if (roleMasterEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Role not found";
            }
            return new RoleMasterModel
            {
                RoleId = roleMasterEntity.RoleId,
                RoleName = roleMasterEntity.RoleName,
                FirmIds = roleMasterEntity.FirmIds,
                FirmName = ResolveFirmName(roleMasterEntity.FirmIds, context.FirmDetails
                    .Select(x => new { x.FirmId, x.FirmName })
                    .ToDictionary(x => x.FirmId, x => x.FirmName)),
                EnteredDate = roleMasterEntity.EnteredDate,
                EnteredBy = roleMasterEntity.EnteredBy,
                ChangedBy = roleMasterEntity.ChangedBy,
                ChangedDate = roleMasterEntity.ChangedDate,
                DeleteStatus = roleMasterEntity.DeleteStatus,
            };
        }

        /// <summary>
        /// Method to get all the roleMaster
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<RoleMasterModel> GetRoleMaster(ref ErrorResponseModel errorResponseModel)
        {
            var roleMasterModelList = new List<RoleMasterModel>();
            errorResponseModel = new ErrorResponseModel();
            var roleMasterEntityList = context.RoleMasters.Where(x => x.DeleteStatus == false).ToList();
            if (roleMasterEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Role Master not found";
            }

            var firmNames = context.FirmDetails
                .Select(x => new { x.FirmId, x.FirmName })
                .ToDictionary(x => x.FirmId, x => x.FirmName);

            roleMasterEntityList.ForEach(item =>
            {
                roleMasterModelList.Add(new RoleMasterModel
                {
                    RoleId = item.RoleId,
                    RoleName = item.RoleName,
                    FirmIds = item.FirmIds,
                    FirmName = ResolveFirmName(item.FirmIds, firmNames),
                    EnteredDate = item.EnteredDate,
                    EnteredBy = item.EnteredBy,
                    ChangedBy = item.ChangedBy,
                    ChangedDate = item.ChangedDate,
                    DeleteStatus = item.DeleteStatus,
                });
            });
            return roleMasterModelList;
        }

        /// <summary>
        /// FirmIds is a comma-separated list of FirmDetail ids; non-numeric or unknown entries are ignored.
        /// </summary>
        private static string ResolveFirmName(string firmIds, Dictionary<int, string> firmNames)
        {
            if (string.IsNullOrWhiteSpace(firmIds))
                return null;

            var names = firmIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var id) && firmNames.TryGetValue(id, out var name) ? name : null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return names.Count == 0 ? null : string.Join(", ", names);
        }

        /// <summary>
        /// Method implementation for saving new roleMaster
        /// </summary>
        /// <param name="roleMasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveRoleMaster(RoleMasterModel roleMasterModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            if (roleMasterModel.RoleId == 0)
            {
                RoleMaster roleMasterEntity = new RoleMaster();

                roleMasterEntity.RoleName = roleMasterModel.RoleName;
                roleMasterEntity.FirmIds = roleMasterModel.FirmIds;
                roleMasterEntity.EnteredBy = roleMasterModel.EnteredBy;
                roleMasterEntity.EnteredDate = DateTime.Now;
                context.RoleMasters.Add(roleMasterEntity);
                context.SaveChanges();
                Message = "RoleMaster Saved Successfully";
            }
            else
            {
                var roleMasterEntity = context.RoleMasters.FirstOrDefault(x => x.RoleId == roleMasterModel.RoleId);
                if (roleMasterEntity != null)
                {
                    roleMasterEntity.RoleName = roleMasterModel.RoleName;
                    roleMasterEntity.FirmIds = roleMasterModel.FirmIds;
                    roleMasterEntity.ChangedBy = roleMasterModel.EnteredBy;
                    roleMasterEntity.ChangedDate = DateTime.Now;
                    context.SaveChanges();
                    Message = "RoleMaster Updated Successfully";
                }
            }
            return Message;
        }

        /// <summary>
        /// Method is used for delete RoleMaster.
        /// </summary>
        /// <param name="roleMasterModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string DeleteRoleMaster(RoleMasterModel roleMasterModel, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var roleMasterEntity = context.RoleMasters.FirstOrDefault(x => x.RoleId == roleMasterModel.RoleId);
            if (roleMasterEntity != null)
            {
                roleMasterEntity.DeleteStatus = roleMasterModel.DeleteStatus;
                roleMasterEntity.ChangedBy = roleMasterModel.EnteredBy;
                roleMasterEntity.ChangedDate = DateTime.Now;
                context.SaveChanges();
                Message = "RoleMaster Deleted Successfully";
            }
            return Message;
        }

    }
}

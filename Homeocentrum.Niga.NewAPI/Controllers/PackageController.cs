using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using System;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for package entity 
    /// </summary>
    [Route("api/package")]
    [ApiController]
  //  [Authorize]
    public class PackageController : BaseAPIController
    {
        private readonly IPackageService _packageService;
        private readonly IAuditEventWriter _auditEventWriter;

        public PackageController(IPackageService packageService, IAuditEventWriter auditEventWriter)
        {
            _packageService = packageService;
            _auditEventWriter = auditEventWriter;
        }

        [HttpGet("{packageId}")]
        public async Task<IActionResult> GetPackageById(long packageId)
        {
            try
            {
                var packageModel = await _packageService.GetPackageByIdAsync(packageId);
                if (packageModel != null)
                {
                    return Ok(new { Status = 200, Data = packageModel });
                }
                return NotFound(new { Status = 404, Message = "Package not found" });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("GetAllPackages")]
        public async Task<IActionResult> GetAllPackages([FromQuery] Homeocentrum.Niga.NewAPI.Domain.Helpers.ParameterParams parameterParams)
        {
            try
            {
                var pagedList = await _packageService.GetAllPackagesAsync(parameterParams);
                Response.AddPaginationHeader(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount, pagedList.TotalPages);
                return Ok(new { Status = 200, Data = pagedList });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpPost("Save")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> SavePackage([FromBody] Homeocentrum.Niga.NewAPI.Domain.DTOs.PackageModel packageModel)
        {
            try
            {
                var result = await _packageService.SavePackageAsync(packageModel);
                await WriteAuditAsync("SavePackage", "Package");
                return Ok(new { Status = 200, Message = result });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpPost("Delete/{packageId}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> DeletePackage(long packageId, [FromQuery] string changedBy)
        {
            try
            {
                var result = await _packageService.DeletePackageAsync(packageId, changedBy);
                if (result == "Package Deleted Successfully")
                {
                    await WriteAuditAsync("DeletePackage", "Package");
                    return Ok(new { Status = 200, Message = result });
                }
                return NotFound(new { Status = 404, Message = result });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("GetPackageTopups")]
        public async Task<IActionResult> GetPackageTopups([FromQuery] Homeocentrum.Niga.NewAPI.Domain.Helpers.ParameterParams parameterParams)
        {
            try
            {
                var pagedList = await _packageService.GetAllPackageTopupsAsync(parameterParams);
                Response.AddPaginationHeader(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount, pagedList.TotalPages);
                return Ok(new { Status = 200, Data = pagedList });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpPost("SavePackageTopup")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> SavePackageTopup([FromBody] Homeocentrum.Niga.NewAPI.Domain.DTOs.PackageTopupModel packageTopupModel)
        {
            try
            {
                var result = await _packageService.SavePackageTopupAsync(packageTopupModel);
                await WriteAuditAsync("SavePackageTopup", "PackageTopup");
                return Ok(new { Status = 200, Message = result });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>SEC-08.02 — explicit audit helper call on mutate (not blanket middleware).</summary>
        private async Task WriteAuditAsync(string action, string entity)
        {
            try
            {
                long? userId = null;
                try { userId = User.GetUserId(); } catch { /* anonymous edge */ }
                await _auditEventWriter.WriteAsync(
                    userId,
                    AdminAuthorizationPolicies.GetRoleName(User),
                    action,
                    entity);
            }
            catch
            {
                // Never fail the business mutate because audit write failed.
            }
        }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To Get all Packages
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PackageModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult GetPackages([FromServices] IPackageService packageService)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var packageModelList = packageService.GetPackages(ref errorResponseModel);

                if (packageModelList != null)
                {
                    return Ok(packageModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new Package 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(PackageModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult SavePackage([FromServices] IPackageService packageService, PackageModel packageModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var packagemodel = packageService.SavePackage(packageModel, ref errorResponseModel);

                if (packagemodel != null)
                {
                    return Ok(packagemodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete Package 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeletePackage")]
        [ProducesResponseType(typeof(PackageModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeletePackage([FromServices] IPackageService packageService, PackageModel packageModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var packagemodel = packageService.DeletePackage(packageModel, ref errorResponseModel);

                if (packagemodel != null)
                {
                    return Ok(packagemodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

#nullable restore
        #endregion
    }
}

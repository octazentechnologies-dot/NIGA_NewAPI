using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Business.Interface;

using Niga_Domain.Authorization;
namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for package entity 
    /// </summary>
    [Route("api/package")]
    [ApiController]
  //  [Authorize]
    public class PackageController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public PackageController(IPackageService packageService)
        {
            _packageService = packageService;
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
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }

        [HttpGet("GetAllPackages")]
        public async Task<IActionResult> GetAllPackages([FromQuery] Niga_Domain.Helpers.ParameterParams parameterParams)
        {
            try
            {
                var pagedList = await _packageService.GetAllPackagesAsync(parameterParams);
                Response.AddPaginationHeader(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount, pagedList.TotalPages);
                return Ok(new { Status = 200, Data = pagedList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }

        [HttpPost("Save")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> SavePackage([FromBody] Niga_Domain.DTOs.PackageModel packageModel)
        {
            try
            {
                var result = await _packageService.SavePackageAsync(packageModel);
                return Ok(new { Status = 200, Message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
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
                    return Ok(new { Status = 200, Message = result });
                return NotFound(new { Status = 404, Message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }

        [HttpGet("GetPackageTopups")]
        public async Task<IActionResult> GetPackageTopups([FromQuery] Niga_Domain.Helpers.ParameterParams parameterParams)
        {
            try
            {
                var pagedList = await _packageService.GetAllPackageTopupsAsync(parameterParams);
                Response.AddPaginationHeader(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount, pagedList.TotalPages);
                return Ok(new { Status = 200, Data = pagedList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }

        [HttpPost("SavePackageTopup")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> SavePackageTopup([FromBody] Niga_Domain.DTOs.PackageTopupModel packageTopupModel)
        {
            try
            {
                var result = await _packageService.SavePackageTopupAsync(packageTopupModel);
                return Ok(new { Status = 200, Message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }
    }
}
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [OldApiContract]
    public class DrugSystemController : BaseAPIController
    {
        IDrugSystemService _drugSystemService;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="authorService"></param>
        public DrugSystemController(IDrugSystemService drugSystemService)
        {
            _drugSystemService = drugSystemService;
        }

        /// <summary>
        /// To add new DrugSystem 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DrugSystemModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveDrugSystem(DrugSystemModel drugSystemModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var drugSystemEntity = _drugSystemService.SaveDrugSystem(drugSystemModel, ref errorResponseModel);

                if (drugSystemEntity != null)
                {
                    return Ok(drugSystemEntity);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete DrugSystem 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteDrugSystem")]
        [ProducesResponseType(typeof(DrugSystemModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteDrugSystem(DrugSystemModel drugSystemModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var drugSystemEntity = _drugSystemService.DeleteDrugSystem(drugSystemModel, ref errorResponseModel);

                if (drugSystemEntity != null)
                {
                    return Ok(drugSystemEntity);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Errors;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interface;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Homeocentrum.Niga.API.Domain.Compatibility;

namespace Homeocentrum.Niga.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [OldApiContract]
    public class MateriaMedicaHeadController : BaseAPIController
    {
        IMateriaMedicaHeadMasterService _materiamedicaheadService;

        /// <summary>
        /// Used to initialize controller and inject MateriaMedicaHead service
        /// </summary>
        /// <param name="materiamedicaheadService"></param>
        public MateriaMedicaHeadController(IMateriaMedicaHeadMasterService materiamedicaheadService)
            {
                _materiamedicaheadService = materiamedicaheadService;
            }

        /// <summary>
        /// To add new MateriaMedicaHead 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
            [ProducesResponseType(typeof(MateriaMedicaHeadMasterModel), 200)]
            [ProducesResponseType(typeof(string), 404)]
            [ProducesResponseType(typeof(string), 400)]
            [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveMateriaMedicaHead(MateriaMedicaHeadMasterModel materiamedicheadModel)
            {
                ErrorResponseModel errorResponseModel = null;
                try
                {
                    var materiamedicheadmodel = _materiamedicaheadService.SaveMateriaMedicaHead(materiamedicheadModel, ref errorResponseModel);

                    if (materiamedicheadmodel != null)
                    {
                        return Ok(materiamedicheadmodel);
                    }
                    return ReturnErrorResponse(errorResponseModel);
                }
                catch (Exception ex)
                {
                    return this.ServerError(ex);
                }
            }

        /// <summary>
        /// To delete MateriaMedicaHead 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
            [Route("DeleteMateriaMedicaHead")]
            [ProducesResponseType(typeof(MateriaMedicaHeadMasterModel), 200)]
            [ProducesResponseType(typeof(string), 404)]
            [ProducesResponseType(typeof(string), 400)]
            [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteMateriaMedicaHead(MateriaMedicaHeadMasterModel materiamedicaheadModel)
            {
                ErrorResponseModel errorResponseModel = null;
                try
                {
                    var materiamedicaheadmodel = _materiamedicaheadService.DeleteMateriaMedicaHead(materiamedicaheadModel, ref errorResponseModel);

                    if (materiamedicaheadmodel != null)
                    {
                        return Ok(materiamedicaheadmodel);
                    }
                    return ReturnErrorResponse(errorResponseModel);
                }
                catch (Exception ex)
                {
                    return this.ServerError(ex);
                }
            }

        // Created by Vikas More


        /// <summary>
        /// To update differential materia medica status
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("UpdateDifferentialMateriaMedicadDefaultStatus")]
        [ProducesResponseType(typeof(MateriaMedicaHeadMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult UpdateDifferentialMateriaMedicadDefaultStatus(DifferentialMateriaMedicadDefaultStatusModel differentialMateriaMedicadDefaultStatus)
        {
            try
            {
                var updatedStatus = _materiamedicaheadService.UpdateDifferentialMateriaMedicadDefaultStatus(differentialMateriaMedicadDefaultStatus.MateriaMedicaHeadId, differentialMateriaMedicadDefaultStatus.DifferentialMM);
                return Ok(updatedStatus);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}

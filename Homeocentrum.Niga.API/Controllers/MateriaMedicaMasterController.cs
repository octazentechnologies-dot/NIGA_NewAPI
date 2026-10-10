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
using Homeocentrum.Niga.API.Domain.Compatibility;

namespace Homeocentrum.Niga.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [OldApiContract]
    public class MateriaMedicaMasterController : BaseAPIController
    {
        IMateriaMedicaMasterService _materiamedicaService;

        /// <summary>
        /// Used to initialize controller and inject MateriaMedica service
        /// </summary>
        /// <param name="materiamedicaheadService"></param>
        public MateriaMedicaMasterController(IMateriaMedicaMasterService materiamedicaService)
        {
            _materiamedicaService = materiamedicaService;
        }

        /// <summary>
        /// To get MateriaMedica by MateriamedicaID 
        /// </summary>
        /// <param name="materiamedicaId"></param>
        /// <returns></returns>
        [HttpGet("{materiamedicaId}")]
        [ProducesResponseType(typeof(MateriaMedicaMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedicaById(long materiamedicaId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaModel = _materiamedicaService.GetMateriaMedicaById(materiamedicaId, ref errorResponseModel);

                if (materiamedicaModel != null)
                {
                    return Ok(materiamedicaModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all MateriaMedica
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetMateriaMedica")]
        [ProducesResponseType(typeof(MateriaMedicaMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedica([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaList = _materiamedicaService.GetMateriaMedica(nigaParameters,ref errorResponseModel);

                if (materiamedicaList != null)
                {
                    return Ok(materiamedicaList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new MateriaMedica 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(MateriaMedicaMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveMateriaMedica(MateriaMedicaMasterModel materiamedicaModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicamodel = _materiamedicaService.SaveMateriaMedica(materiamedicaModel, ref errorResponseModel);

                if (materiamedicamodel != null)
                {
                    return Ok(materiamedicamodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete MateriaMedica
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteMateriaMedica")]
        [ProducesResponseType(typeof(MateriaMedicaMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteMateriaMedica(MateriaMedicaMasterModel materiamedicaModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicamodel = _materiamedicaService.DeleteMateriaMedica(materiamedicaModel, ref errorResponseModel);

                if (materiamedicamodel != null)
                {
                    return Ok(materiamedicamodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all MateriaMedica by authorId
        /// </summary>
        /// <param name="authorId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetMateriaMedicaHeadByAuthorId/{authorId}")]
        [ProducesResponseType(typeof(MateriaMedicaMasterModel2), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedicaAuthor(long authorId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var meteriamedicaModelList = _materiamedicaService.GetMateriaMedicaHeadByAuthorId(authorId, ref errorResponseModel);

                if (meteriamedicaModelList != null)
                {
                    return Ok(meteriamedicaModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get Author dropdown list data
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetAuthorDDL")]
        [ProducesResponseType(typeof(MateriaMedicaMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAuthorDDL()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaList = _materiamedicaService.GetAuthorDDL(ref errorResponseModel);

                if (materiamedicaList != null)
                {
                    return Ok(materiamedicaList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get Remedy dropdown list data
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRemedyDDL")]
        [ProducesResponseType(typeof(MateriaMedicaMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRemedyDDL()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaList = _materiamedicaService.GetRemedyDDL( ref errorResponseModel);

                if (materiamedicaList != null)
                {
                    return Ok(materiamedicaList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all MateriaMedica
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetMateriaMedicaByAuthorRemedy")]
        [ProducesResponseType(typeof(MateriaMedicaFilterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedicaByAuthorRemedy([FromQuery] MateriaMedicaFilterModel materiaMedicaFilter)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaList = _materiamedicaService.GetMateriaMedicaByAuthorRemedy(materiaMedicaFilter, ref errorResponseModel);

                if (materiamedicaList != null)
                {
                    return Ok(materiamedicaList);
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

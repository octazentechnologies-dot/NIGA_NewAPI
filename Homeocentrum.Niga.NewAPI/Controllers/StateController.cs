using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interface;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for State entity 
    /// </summary>
    [Route("api/state")]
    [ApiController]
    [Authorize]
    public class StateController : BaseAPIController
    {
        IStateService _stateService;
        /// <summary>
        /// Used to initialize controller and inject state service
        /// </summary>
        /// <param name="stateService"></param>
        public StateController(IStateService stateService)
        {
            _stateService = stateService;
        }

        /// <summary>
        /// To get state by State ID 
        /// </summary>
        /// <param name="stateId"></param>
        /// <returns></returns>
        [HttpGet("{stateId}")]
        [ProducesResponseType(typeof(StateModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetStatteById(long stateId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var stateModel = _stateService.GetStateById(stateId, ref errorResponseModel);

                if (stateModel != null)
                {
                    return Ok(stateModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get state by State ID 
        /// </summary>
        /// <param name="stateId"></param>
        /// <returns></returns>
        [HttpGet("GetStates")]
        [ProducesResponseType(typeof(StateModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetStates()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var stateModelList = _stateService.GetStates(ref errorResponseModel);

                if (stateModelList != null)
                {
                    return Ok(stateModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
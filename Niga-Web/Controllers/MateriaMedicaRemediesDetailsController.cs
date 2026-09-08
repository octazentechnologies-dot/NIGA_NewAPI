using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Business.Interface;
using Niga_Domain.DTOs;
using System;

using Niga_Domain.Authorization;
namespace Niga_Domain.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MateriaMedicaRemediesDetailsController : BaseAPIController
    {
        IMateriaMedicaRemediesDetails _materiamediService;
        /// <summary>
        /// Used to initialize controller and inject MateriaMedicaHead service
        /// </summary>
        /// <param name="materiamedicaheadService"></param>
        public MateriaMedicaRemediesDetailsController(IMateriaMedicaRemediesDetails materiamedicaheadService)
        {
            _materiamediService = materiamedicaheadService;
        }

        [HttpGet("GetMateriaMedicaRemediesDetails")]
        [ProducesResponseType(typeof(MateriaMedicaRemediesDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedicaRemediesDetails(long remedyId, long authorId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaList = _materiamediService.GetMateriaMedicaRemediesDetails(remedyId,authorId, ref errorResponseModel);

                if (materiamedicaList != null)
                {
                    return Ok(materiamedicaList);
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

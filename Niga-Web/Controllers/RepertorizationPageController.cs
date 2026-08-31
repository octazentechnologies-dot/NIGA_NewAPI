using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Interface;

namespace Niga_Domain.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class RepertorizationPageController : ControllerBase
    {
        IRepertorizationPageService repertorizationPageService;
        /// <summary>
        /// Used to initialize controller and inject MateriaMedicaHead service
        /// </summary>
        /// <param name="materiamedicaheadService"></param>
        public RepertorizationPageController(IRepertorizationPageService _repertorizationPageService)
        {
            repertorizationPageService = _repertorizationPageService;
        }

        [HttpGet("GetMateriaMedicaHeadingbyAuthorId/{authorId}")]
        [ProducesResponseType(typeof(MateriaMedicaHeadMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedicaHeadingbyAuthorId(int authorId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaheadModel = repertorizationPageService.GetMateriaMedicaHeadingbyAuthorId(authorId);

                if (materiamedicaheadModel != null)
                {
                    return Ok(materiamedicaheadModel);
                }
                return BadRequest("Data Not Found");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all MateriaMedicaHead
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("GetDifferentialMateriaMedica")]
        [ProducesResponseType(typeof(MateriaMedicaHeadMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDifferentialMateriaMedica(DifferentialMateriaMedica differentialMateriaMedica)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var differentialMateriaMedicaLists = repertorizationPageService.GetDifferentialMateriaMedica(differentialMateriaMedica);

                if (differentialMateriaMedicaLists != null)
                {
                    return Ok(differentialMateriaMedicaLists);
                }
                return BadRequest("Data Not Found");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}

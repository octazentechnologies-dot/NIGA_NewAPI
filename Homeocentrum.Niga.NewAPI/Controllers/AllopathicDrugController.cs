using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;

using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Security;
namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class AllopathicDrugController : ControllerBase
    {
        private readonly IAllopathicDrugService _allopathicDrugService;

        public AllopathicDrugController(IAllopathicDrugService allopathicDrugService)
        {
            _allopathicDrugService = allopathicDrugService;
        }

        [HttpGet("allopathicDrugId")]
        public IActionResult GetAllopathicDrugById(long allopathicDrugId)
        {
            try
            {
                var drug = _allopathicDrugService.GetAllopathicDrugById(allopathicDrugId);
                if (drug == null)
                    return NotFound(new { Status = 404, Data = "No Data Found" });
                return Ok(new { Status = 200, Data = drug });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Data = ex.Message });
            }
        }

        [HttpGet("GetAllopathicDrug")]
        public async Task<List<AllopathicDrugModel>> GetAllopathicDrug([FromQuery] ParameterParams parameterParams)
        {
            try
            {
                var drugs = await _allopathicDrugService.GetAllopathicDrugsAsync(parameterParams);
                Response.AddPaginationHeader(drugs.CurrentPage, drugs.PageSize, drugs.TotalCount, drugs.TotalPages);
                return drugs;

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        [HttpGet("GetAllAdverseReactions")]
        public async Task<List<AdverseReactionModel>> GetAllAdverseReactionsAsync([FromQuery] ParameterParams parameterParams)
        {
            try
            {
                var drugs = await _allopathicDrugService.GetAllAdverseReactionsAsync(parameterParams);
                Response.AddPaginationHeader(drugs.CurrentPage, drugs.PageSize, drugs.TotalCount, drugs.TotalPages);
                return drugs;

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        [HttpGet("GetAllOtherSideEffects")]
        public async Task<List<OtherSideEffectModel>> GetAllOtherSideEffectsAsync([FromQuery] ParameterParams parameterParams)
        {
            try
            {
                var drugs = await _allopathicDrugService.GetAllOtherSideEffectsAsync(parameterParams);
                Response.AddPaginationHeader(drugs.CurrentPage, drugs.PageSize, drugs.TotalCount, drugs.TotalPages);
                return drugs;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        [HttpGet("GetAllSeriousSideEffects")]
        public async Task<List<SeriousSideEffectModel>> GetAllSeriousSideEffectsAsync([FromQuery] ParameterParams parameterParams)
        {
            try
            {
                var drugs = await _allopathicDrugService.GetAllSeriousSideEffectsAsync(parameterParams);
                Response.AddPaginationHeader(drugs.CurrentPage, drugs.PageSize, drugs.TotalCount, drugs.TotalPages);
                return drugs;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> SaveAllopathicDrug([FromBody] AllopathicDrugModel model)
        {
            try
            {
                var result = await _allopathicDrugService.SaveAllopathicDrug(model);
                return Ok(new { Status = 200, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Data = ex.Message });
            }
        }

        [HttpPost("DeleteAllopathicDrug")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> DeleteAllopathicDrug([FromBody] long allopathicDrugId)
        {
            try
            {
                var result = await _allopathicDrugService.DeleteAllopathicDrug(allopathicDrugId);
                if (result == "Not found")
                    return NotFound(new { Status = 404, Data = result });
                return Ok(new { Status = 200, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Data = ex.Message });
            }
        }

        [HttpGet("GetAllopathicDrugByName/{allopathicDrugName}")]
        public async Task<IActionResult> GetAllopathicDrugByName(string allopathicDrugName)
        {
            try
            {
                var drug = await _allopathicDrugService.GetAllopathicDrugByNameAsync(allopathicDrugName);
                if (drug == null)
                    return NotFound(new { Status = 404, Data = "No Data Found" });
                return Ok(new { Status = 200, Data = drug });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Data = ex.Message });
            }
        }

        [HttpGet("GetAllopathicDrugfordropdown")]
        public async Task<IActionResult> GetAllopathicDrugDDL([FromQuery] string? search = null)
        {
            try
            {
                var list = await _allopathicDrugService.GetAllopathicDrugDropdownAsync(search);
                return Ok(new { Status = 200, Data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Data = ex.Message });
            }
        }
        
           [HttpGet("GetAllopathicDrugById/{allopathicDrugId}")]
        [ProducesResponseType(typeof(AllopathicDrugModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllopathicDrugById(int allopathicDrugId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var allopathicDrugModel = _allopathicDrugService.GetAllopathicDrugByID(allopathicDrugId, ref errorResponseModel);

                if (allopathicDrugModel != null)
                {
                    return Ok(allopathicDrugModel);
                }
                return BadRequest(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}

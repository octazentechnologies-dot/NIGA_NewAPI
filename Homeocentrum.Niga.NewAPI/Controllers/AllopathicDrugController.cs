using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;

using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;
namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class AllopathicDrugController : BaseAPIController
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
                return this.ServerError(ex);
            }
        }

        [HttpGet("{allopathicDrugId:long}")]
        [ProducesResponseType(typeof(AllopathicDrugModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllopathicDrugDetailsById(long allopathicDrugId)
        {
            ErrorResponseModel? errorResponseModel = null;
            try
            {
                var allopathicDrugModel = _allopathicDrugService.GetAllopathicDrugById(allopathicDrugId, ref errorResponseModel);
                if (allopathicDrugModel != null)
                {
                    return Ok(allopathicDrugModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("GetAllopathicDrug")]
        public async Task<IActionResult> GetAllopathicDrug([FromQuery] ParameterParams parameterParams)
        {
            if (!Request.Query.ContainsKey("PageNumber") && !Request.Query.ContainsKey("PageSize"))
            {
                ErrorResponseModel? errorResponseModel = null;
                try
                {
                    var allDrugs = _allopathicDrugService.GetAllopathicDrug(ref errorResponseModel);
                    if (allDrugs != null)
                    {
                        return Ok(allDrugs);
                    }
                    return ReturnErrorResponse(errorResponseModel);
                }
                catch (Exception ex)
                {
                    return this.ServerError(ex);
                }
            }
            try
            {
                var drugs = await _allopathicDrugService.GetAllopathicDrugsAsync(parameterParams);
                Response.AddPaginationHeader(drugs.CurrentPage, drugs.PageSize, drugs.TotalCount, drugs.TotalPages);
                return Ok(drugs);

            }
            catch (Exception ex)
            {
                return NoContent();
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
        [OldApiContract]
        public async Task<IActionResult> SaveAllopathicDrug([FromBody] AllopathicDrugModel model)
        {
            if (model == null || model.DrugGroupId <= 0)
                return BadRequest("Drug group is required.");
            try
            {
                var result = await _allopathicDrugService.SaveAllopathicDrug(model);
                // The admin screens render this response directly, so it stays the Old API's plain message string.
                return Ok(result);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
            {
                return BadRequest("Selected drug group does not exist.");
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpPost("DeleteAllopathicDrug")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public async Task<IActionResult> DeleteAllopathicDrug([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                if (body.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // Old API contract: { allopathicDrugId } in, the posted model echoed back.
                    var jsonOptions = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;
                    var model = System.Text.Json.JsonSerializer.Deserialize<AllopathicDrugModel>(body.GetRawText(), jsonOptions);
                    await _allopathicDrugService.DeleteAllopathicDrug(model?.AllopathicDrugId ?? 0);
                    return Ok(model);
                }
                long allopathicDrugId;
                if (body.ValueKind == System.Text.Json.JsonValueKind.Number && body.TryGetInt64(out var numericId))
                    allopathicDrugId = numericId;
                else if (body.ValueKind == System.Text.Json.JsonValueKind.String && long.TryParse(body.GetString(), out var textId))
                    allopathicDrugId = textId;
                else
                    return BadRequest(new { Status = 400, Data = "allopathicDrugId is required" });

                var result = await _allopathicDrugService.DeleteAllopathicDrug(allopathicDrugId);
                if (result == "Not found")
                    return NotFound(new { Status = 404, Data = result });
                return Ok(new { Status = 200, Data = result });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
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
                return this.ServerError(ex);
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
                return this.ServerError(ex);
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
                return this.ServerError(ex);
            }
        }

    }
}

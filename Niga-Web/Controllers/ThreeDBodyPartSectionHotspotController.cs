using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

using Niga_Domain.Authorization;
namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for ThreeDBodyPartSectionHotspot entity
    /// </summary>
    [Route("api/threeDBodyPartSectionHotspot")]
    [ApiController]
    [Authorize]
    public class ThreeDBodyPartSectionHotspotController : ControllerBase
    {
        private readonly IThreeDBodyPartSectionHotspotRepository _hotspotService;

        public ThreeDBodyPartSectionHotspotController(IThreeDBodyPartSectionHotspotRepository hotspotService)
        {
            _hotspotService = hotspotService;
        }

        /// <summary>
        /// Add a new section hotspot
        /// </summary>
        [HttpPost("AddThreeDBodyPartSectionHotspot")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddThreeDBodyPartSectionHotspot([FromBody] AddThreeDBodyPartSectionHotspotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                if (!await _hotspotService.SectionExistsAsync(request.SectionId))
                {
                    return new { Status = 400, Message = "Section not found" };
                }

                if (await _hotspotService.IsDuplicateHotspotNameAsync(request.SectionId, request.HotspotName))
                {
                    return new { Status = 400, Message = "Hotspot name already exists for this section" };
                }

                var hotspot = new ThreeDBodyPartSectionHotspot
                {
                    SectionId = request.SectionId,
                    HotspotName = request.HotspotName.Trim(),
                    EnteredBy = request.EnteredBy,
                    EnteredDate = DateTime.Now,
                    DeleteStatus = false
                };

                _hotspotService.SaveHotspot(hotspot);

                if (await _hotspotService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Added Successfully",
                        Data = new { hotspot.SectionHotspotId }
                    };
                }

                return new { Status = 400, Message = "Failed To Add Data" };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// Update an existing section hotspot
        /// </summary>
        [HttpPost("UpdateThreeDBodyPartSectionHotspot")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateThreeDBodyPartSectionHotspot([FromBody] UpdateThreeDBodyPartSectionHotspotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var entity = await _hotspotService.GetHotspotEntityByIdAsync(request.SectionHotspotId);
                if (entity == null)
                {
                    return new { Status = 400, Message = "Hotspot not found" };
                }

                if (!await _hotspotService.SectionExistsAsync(request.SectionId))
                {
                    return new { Status = 400, Message = "Section not found" };
                }

                if (await _hotspotService.IsDuplicateHotspotNameAsync(
                        request.SectionId,
                        request.HotspotName,
                        request.SectionHotspotId))
                {
                    return new { Status = 400, Message = "Hotspot name already exists for this section" };
                }

                entity.SectionId = request.SectionId;
                entity.HotspotName = request.HotspotName.Trim();
                entity.ChangedBy = request.ChangedBy;
                entity.ChangedDate = DateTime.Now;

                _hotspotService.UpdateHotspot(entity);

                if (await _hotspotService.SaveAllAsync())
                {
                    return new { Status = 200, Message = "Data Updated Successfully" };
                }

                return new { Status = 400, Message = "Failed To Update Data" };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// Soft delete a section hotspot
        /// </summary>
        [HttpPost("DeleteThreeDBodyPartSectionHotspot")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteThreeDBodyPartSectionHotspot([FromBody] DeleteThreeDBodyPartSectionHotspotRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var entity = await _hotspotService.GetHotspotEntityByIdAsync(request.SectionHotspotId);
                if (entity == null)
                {
                    return new { Status = 400, Message = "Hotspot not found" };
                }

                entity.ChangedBy = request.ChangedBy;
                entity.ChangedDate = DateTime.Now;

                _hotspotService.DeleteHotspot(entity);

                if (await _hotspotService.SaveAllAsync())
                {
                    return new { Status = 200, Message = "Data Deleted Successfully" };
                }

                return new { Status = 400, Message = "Failed To Delete Data" };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get section hotspot details by ID
        /// </summary>
        [HttpPost("GetThreeDBodyPartSectionHotspotById")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> GetThreeDBodyPartSectionHotspotById([FromBody] GetThreeDBodyPartSectionHotspotByIdRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var hotspot = await _hotspotService.GetHotspotDetailsByIdAsync(request.SectionHotspotId);

                if (hotspot != null)
                {
                    return new { Status = 200, Data = hotspot };
                }

                return new { Status = 400, Data = "No Data Found" };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get paginated non-deleted section hotspots (query string)
        /// </summary>
        [HttpGet("GetThreeDBodyPartSectionHotspotList")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionHotspotListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionHotspotList([FromQuery] PaginationRequestModel request)
            => GetThreeDBodyPartSectionHotspotListInternal(request);

        /// <summary>
        /// Get paginated non-deleted section hotspots (request body)
        /// </summary>
        [HttpPost("GetThreeDBodyPartSectionHotspotList")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionHotspotListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionHotspotListPost([FromBody] PaginationRequestModel request)
            => GetThreeDBodyPartSectionHotspotListInternal(request);

        /// <summary>
        /// Get paginated hotspots for a specific section (query string)
        /// </summary>
        [HttpGet("GetThreeDBodyPartSectionHotspotBySectionId/{sectionId}")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionHotspotListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionHotspotBySectionId(
            int sectionId,
            [FromQuery] PaginationRequestModel request)
            => GetThreeDBodyPartSectionHotspotBySectionIdInternal(sectionId, request);

        /// <summary>
        /// Get paginated hotspots for a specific section (request body)
        /// </summary>
        [HttpPost("GetThreeDBodyPartSectionHotspotBySectionId/{sectionId}")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionHotspotListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionHotspotBySectionIdPost(
            int sectionId,
            [FromBody] PaginationRequestModel request)
            => GetThreeDBodyPartSectionHotspotBySectionIdInternal(sectionId, request);

        private async Task<object> GetThreeDBodyPartSectionHotspotListInternal(PaginationRequestModel request)
        {
            try
            {
                if (!TryValidatePagination(request, out var validationError))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(validationError!);
                }

                var result = await _hotspotService.GetAllHotspotsAsync(request);
                return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(result);
            }
            catch (Exception ex)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
            }
        }

        private async Task<object> GetThreeDBodyPartSectionHotspotBySectionIdInternal(
            int sectionId,
            PaginationRequestModel request)
        {
            try
            {
                if (sectionId <= 0)
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure("SectionID is required");
                }

                if (!TryValidatePagination(request, out var validationError))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(validationError!);
                }

                if (!await _hotspotService.SectionExistsAsync(sectionId))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure("Section not found");
                }

                var result = await _hotspotService.GetHotspotsBySectionIdAsync(sectionId, request);
                return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(result);
            }
            catch (Exception ex)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
            }
        }

        private bool TryValidatePagination(PaginationRequestModel request, out string? errorMessage)
        {
            if (!ModelState.IsValid)
            {
                errorMessage = GetValidationMessage();
                return false;
            }

            if (request.PageNumber < 1)
            {
                errorMessage = "PageNumber must be greater than 0";
                return false;
            }

            if (request.PageSize < 1)
            {
                errorMessage = "PageSize must be greater than 0";
                return false;
            }

            if (request.PageSize > PaginationRequestModel.MaxPageSize)
            {
                errorMessage = $"PageSize cannot exceed {PaginationRequestModel.MaxPageSize}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private string GetValidationMessage()
        {
            return string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
        }
    }
}

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for ThreeDBodyPartSectionMaster entity
    /// </summary>
    [Route("api/threeDBodyPartSectionMaster")]
    [ApiController]
    [Authorize]
    public class ThreeDBodyPartSectionMasterController : ControllerBase
    {
        private readonly IThreeDBodyPartSectionMasterRepository _sectionService;

        public ThreeDBodyPartSectionMasterController(IThreeDBodyPartSectionMasterRepository sectionService)
        {
            _sectionService = sectionService;
        }

        /// <summary>
        /// Add a new 3D body part section mapping
        /// </summary>
        [HttpPost("AddThreeDBodyPartSectionMaster")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> AddThreeDBodyPartSectionMaster([FromBody] AddThreeDBodyPartSectionMasterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                if (!await _sectionService.MeshKeyExistsAsync(request.ThreeDBodyPartMeshKeyId))
                {
                    return new { Status = 400, Message = "Mesh key not found" };
                }

                if (!await _sectionService.SectionExistsAsync(request.ThreeDBodyPartSectionId))
                {
                    return new { Status = 400, Message = "Section not found" };
                }

                if (await _sectionService.IsDuplicateMappingAsync(
                        request.ThreeDBodyPartMeshKeyId,
                        request.ThreeDBodyPartSectionId))
                {
                    return new { Status = 400, Message = "This mesh key and section combination already exists" };
                }

                var section = new ThreeDBodyPartSectionMaster
                {
                    ThreeDBodyPartMeshKeyId = request.ThreeDBodyPartMeshKeyId,
                    ThreeDBodyPartSectionId = request.ThreeDBodyPartSectionId,
                    EnteredBy = request.EnteredBy,
                    EnteredDate = DateTime.Now,
                    DeleteStatus = false
                };

                _sectionService.SaveSection(section);

                if (await _sectionService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Added Successfully",
                        Data = new { section.ThreeDBodyPartSectionMasterId }
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
        /// Update an existing 3D body part section mapping
        /// </summary>
        [HttpPost("UpdateThreeDBodyPartSectionMaster")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> UpdateThreeDBodyPartSectionMaster([FromBody] UpdateThreeDBodyPartSectionMasterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var entity = await _sectionService.GetSectionEntityByIdAsync(request.ThreeDBodyPartSectionMasterId);
                if (entity == null)
                {
                    return new { Status = 400, Message = "Section mapping not found" };
                }

                if (!await _sectionService.MeshKeyExistsAsync(request.ThreeDBodyPartMeshKeyId))
                {
                    return new { Status = 400, Message = "Mesh key not found" };
                }

                if (!await _sectionService.SectionExistsAsync(request.ThreeDBodyPartSectionId))
                {
                    return new { Status = 400, Message = "Section not found" };
                }

                if (await _sectionService.IsDuplicateMappingAsync(
                        request.ThreeDBodyPartMeshKeyId,
                        request.ThreeDBodyPartSectionId,
                        request.ThreeDBodyPartSectionMasterId))
                {
                    return new { Status = 400, Message = "This mesh key and section combination already exists" };
                }

                entity.ThreeDBodyPartMeshKeyId = request.ThreeDBodyPartMeshKeyId;
                entity.ThreeDBodyPartSectionId = request.ThreeDBodyPartSectionId;
                entity.ChangedBy = request.ChangedBy;
                entity.ChangedDate = DateTime.Now;

                _sectionService.UpdateSection(entity);

                if (await _sectionService.SaveAllAsync())
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
        /// Soft delete a 3D body part section mapping
        /// </summary>
        [HttpPost("DeleteThreeDBodyPartSectionMaster")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> DeleteThreeDBodyPartSectionMaster([FromBody] DeleteThreeDBodyPartSectionMasterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var entity = await _sectionService.GetSectionEntityByIdAsync(request.ThreeDBodyPartSectionMasterId);
                if (entity == null)
                {
                    return new { Status = 400, Message = "Section mapping not found" };
                }

                entity.ChangedBy = request.ChangedBy;
                entity.ChangedDate = DateTime.Now;

                _sectionService.DeleteSection(entity);

                if (await _sectionService.SaveAllAsync())
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
        /// Get 3D body part section mapping details by ID
        /// </summary>
        [HttpPost("GetThreeDBodyPartSectionMasterById")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> GetThreeDBodyPartSectionMasterById([FromBody] GetThreeDBodyPartSectionMasterByIdRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var section = await _sectionService.GetSectionDetailsByIdAsync(request.ThreeDBodyPartSectionMasterId);

                if (section != null)
                {
                    return new { Status = 200, Data = section };
                }

                return new { Status = 400, Data = "No Data Found" };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get paginated non-deleted section mappings with mesh key and section names (query string)
        /// </summary>
        [HttpGet("GetThreeDBodyPartSectionMasterList")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionMasterListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionMasterList([FromQuery] PaginationRequestModel request)
            => GetThreeDBodyPartSectionMasterListInternal(request);

        /// <summary>
        /// Get paginated non-deleted section mappings with mesh key and section names (request body)
        /// </summary>
        [HttpPost("GetThreeDBodyPartSectionMasterList")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionMasterListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionMasterListPost([FromBody] PaginationRequestModel request)
            => GetThreeDBodyPartSectionMasterListInternal(request);

        /// <summary>
        /// Get paginated section mappings for a specific mesh key (query string)
        /// </summary>
        [HttpGet("GetThreeDBodyPartSectionMasterByMeshKeyId/{meshKeyId}")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionMasterListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionMasterByMeshKeyId(
            int meshKeyId,
            [FromQuery] PaginationRequestModel request)
            => GetThreeDBodyPartSectionMasterByMeshKeyIdInternal(meshKeyId, request);

        /// <summary>
        /// Get paginated section mappings for a specific mesh key (request body)
        /// </summary>
        [HttpPost("GetThreeDBodyPartSectionMasterByMeshKeyId/{meshKeyId}")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartSectionMasterListItem>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartSectionMasterByMeshKeyIdPost(
            int meshKeyId,
            [FromBody] PaginationRequestModel request)
            => GetThreeDBodyPartSectionMasterByMeshKeyIdInternal(meshKeyId, request);

        private async Task<object> GetThreeDBodyPartSectionMasterListInternal(PaginationRequestModel request)
        {
            try
            {
                if (!TryValidatePagination(request, out var validationError))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(validationError!);
                }

                var result = await _sectionService.GetAllSectionsAsync(request);
                return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(result);
            }
            catch (Exception ex)
            {
                return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
            }
        }

        private async Task<object> GetThreeDBodyPartSectionMasterByMeshKeyIdInternal(
            int meshKeyId,
            PaginationRequestModel request)
        {
            try
            {
                if (meshKeyId <= 0)
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure("ThreeD_BodyPart_MeshKeyID is required");
                }

                if (!TryValidatePagination(request, out var validationError))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(validationError!);
                }

                if (!await _sectionService.MeshKeyExistsAsync(meshKeyId))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure("Mesh key not found");
                }

                var result = await _sectionService.GetSectionsByMeshKeyIdAsync(meshKeyId, request);
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

        /// <summary>
        /// Get active mesh keys for dropdown
        /// </summary>
        [HttpGet("GetActiveMeshKeyDropdown")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> GetActiveMeshKeyDropdown()
        {
            try
            {
                var result = await _sectionService.GetActiveMeshKeysDropdownAsync();
                return new { Status = 200, Data = result };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get active sections for dropdown
        /// </summary>
        [HttpGet("GetActiveSectionDropdown")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> GetActiveSectionDropdown()
        {
            try
            {
                var result = await _sectionService.GetActiveSectionsDropdownAsync();
                return new { Status = 200, Data = result };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        private string GetValidationMessage()
        {
            return string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
        }
    }
}

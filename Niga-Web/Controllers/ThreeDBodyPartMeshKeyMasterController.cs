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
    /// APIs for ThreeDBodyPartMeshKeyMaster entity
    /// </summary>
    [Route("api/threeDBodyPartMeshKeyMaster")]
    [ApiController]
    [Authorize]
    public class ThreeDBodyPartMeshKeyMasterController : ControllerBase
    {
        private readonly IThreeDBodyPartMeshKeyMasterRepository _meshKeyService;

        public ThreeDBodyPartMeshKeyMasterController(IThreeDBodyPartMeshKeyMasterRepository meshKeyService)
        {
            _meshKeyService = meshKeyService;
        }

        /// <summary>
        /// Add a new 3D body part mesh key
        /// </summary>
        [HttpPost("AddThreeDBodyPartMeshKeyMaster")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddThreeDBodyPartMeshKeyMaster([FromBody] AddThreeDBodyPartMeshKeyMasterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                if (await _meshKeyService.IsDuplicateMeshKeyNameAsync(request.ThreeDBodyPartMeshKeyName))
                {
                    return new { Status = 400, Message = "Mesh key name already exists" };
                }

                var meshKey = new ThreeDBodyPartMeshKeyMaster
                {
                    ThreeDBodyPartMeshKeyName = request.ThreeDBodyPartMeshKeyName.Trim(),
                    EnteredBy = request.EnteredBy,
                    EnteredDate = DateTime.Now,
                    DeleteStatus = false
                };

                _meshKeyService.SaveMeshKey(meshKey);

                if (await _meshKeyService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Added Successfully",
                        Data = new { meshKey.ThreeDBodyPartMeshKeyId }
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
        /// Update an existing 3D body part mesh key
        /// </summary>
        [HttpPost("UpdateThreeDBodyPartMeshKeyMaster")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateThreeDBodyPartMeshKeyMaster([FromBody] UpdateThreeDBodyPartMeshKeyMasterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var entity = await _meshKeyService.GetMeshKeyEntityByIdAsync(request.ThreeDBodyPartMeshKeyId);
                if (entity == null)
                {
                    return new { Status = 400, Message = "Mesh key not found" };
                }

                if (await _meshKeyService.IsDuplicateMeshKeyNameAsync(
                        request.ThreeDBodyPartMeshKeyName,
                        request.ThreeDBodyPartMeshKeyId))
                {
                    return new { Status = 400, Message = "Mesh key name already exists" };
                }

                entity.ThreeDBodyPartMeshKeyName = request.ThreeDBodyPartMeshKeyName.Trim();
                entity.ChangedBy = request.ChangedBy;
                entity.ChangedDate = DateTime.Now;

                _meshKeyService.UpdateMeshKey(entity);

                if (await _meshKeyService.SaveAllAsync())
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
        /// Soft delete a 3D body part mesh key
        /// </summary>
        [HttpPost("DeleteThreeDBodyPartMeshKeyMaster")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteThreeDBodyPartMeshKeyMaster([FromBody] DeleteThreeDBodyPartMeshKeyMasterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var entity = await _meshKeyService.GetMeshKeyEntityByIdAsync(request.ThreeDBodyPartMeshKeyId);
                if (entity == null)
                {
                    return new { Status = 400, Message = "Mesh key not found" };
                }

                entity.ChangedBy = request.ChangedBy;
                entity.ChangedDate = DateTime.Now;

                _meshKeyService.DeleteMeshKey(entity);

                if (await _meshKeyService.SaveAllAsync())
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
        /// Get 3D body part mesh key details by ID
        /// </summary>
        [HttpPost("GetThreeDBodyPartMeshKeyMasterById")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<object> GetThreeDBodyPartMeshKeyMasterById([FromBody] GetThreeDBodyPartMeshKeyMasterByIdRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new { Status = 400, Message = GetValidationMessage() };
                }

                var meshKey = await _meshKeyService.GetMeshKeyDetailsByIdAsync(request.ThreeDBodyPartMeshKeyId);

                if (meshKey != null)
                {
                    return new { Status = 200, Data = meshKey };
                }

                return new { Status = 400, Data = "No Data Found" };
            }
            catch (Exception ex)
            {
                return new { Status = 500, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get paginated non-deleted mesh keys (query string)
        /// </summary>
        [HttpGet("GetThreeDBodyPartMeshKeyMasterList")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartMeshKeyMasterModel>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartMeshKeyMasterList([FromQuery] PaginationRequestModel request)
            => GetThreeDBodyPartMeshKeyMasterListInternal(request);

        /// <summary>
        /// Get paginated non-deleted mesh keys (request body)
        /// </summary>
        [HttpPost("GetThreeDBodyPartMeshKeyMasterList")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ThreeDBodyPartMeshKeyMasterModel>), 200)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 400)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), 500)]
        public Task<object> GetThreeDBodyPartMeshKeyMasterListPost([FromBody] PaginationRequestModel request)
            => GetThreeDBodyPartMeshKeyMasterListInternal(request);

        private async Task<object> GetThreeDBodyPartMeshKeyMasterListInternal(PaginationRequestModel request)
        {
            try
            {
                if (!TryValidatePagination(request, out var validationError))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(validationError!);
                }

                var result = await _meshKeyService.GetAllMeshKeysAsync(request);
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

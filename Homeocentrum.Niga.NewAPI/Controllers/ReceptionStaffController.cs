using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for doctor reception staff management
    /// </summary>
    [Route("api/ReceptionStaff")]
    [ApiController]
    [Authorize]
    public class ReceptionStaffController : ControllerBase
    {
        private readonly IReceptionStaffService _receptionStaffService;
        private readonly ILogger<ReceptionStaffController> _logger;

        public ReceptionStaffController(
            IReceptionStaffService receptionStaffService,
            ILogger<ReceptionStaffController> logger)
        {
            _receptionStaffService = receptionStaffService;
            _logger = logger;
        }

        /// <summary>
        /// Add reception staff for the logged-in doctor
        /// </summary>
        [HttpPost("AddReceptionStaff")]
        [ProducesResponseType(typeof(ApiResponse<AddReceptionStaffResultModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status500InternalServerError)]
        public async Task<object> AddReceptionStaff([FromBody] AddReceptionStaffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
                }

                BindDoctorUserId(request);
                if (request.DoctorUserID <= 0)
                    return ThreeDBodyPartApiResponseHelper.Failure("DoctorUserID is required.");

                var (success, message, result) = await _receptionStaffService.AddReceptionStaffAsync(request);
                if (!success)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(message);
                }

                return ThreeDBodyPartApiResponseHelper.Success(result!, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddReceptionStaff failed for DoctorUserID={DoctorUserId}", request.DoctorUserID);
                return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
            }
        }

        /// <summary>
        /// Update reception staff profile
        /// </summary>
        [HttpPost("UpdateReceptionStaff")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status500InternalServerError)]
        public async Task<object> UpdateReceptionStaff([FromBody] UpdateReceptionStaffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
                }

                var deny = await ForbidIfNotOwnStaffAsync(request.ReceptionStaffID);
                if (deny != null)
                    return deny;

                var (success, message) = await _receptionStaffService.UpdateReceptionStaffAsync(request);
                if (!success)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(message);
                }

                return ThreeDBodyPartApiResponseHelper.Success<object>(null!, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateReceptionStaff failed for ReceptionStaffID={ReceptionStaffId}", request.ReceptionStaffID);
                return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
            }
        }

        /// <summary>
        /// Soft delete reception staff
        /// </summary>
        [HttpPost("DeleteReceptionStaff")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status500InternalServerError)]
        public async Task<object> DeleteReceptionStaff([FromBody] DeleteReceptionStaffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
                }

                var deny = await ForbidIfNotOwnStaffAsync(request.ReceptionStaffID);
                if (deny != null)
                    return deny;

                var (success, message) = await _receptionStaffService.DeleteReceptionStaffAsync(request);
                if (!success)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(message);
                }

                return ThreeDBodyPartApiResponseHelper.Success<object>(null!, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteReceptionStaff failed for ReceptionStaffID={ReceptionStaffId}", request.ReceptionStaffID);
                return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
            }
        }

        /// <summary>
        /// Get reception staff details by ID
        /// </summary>
        [HttpGet("GetReceptionStaffById/{receptionStaffID}")]
        [ProducesResponseType(typeof(ApiResponse<ReceptionStaffResponseModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status500InternalServerError)]
        public async Task<object> GetReceptionStaffById(int receptionStaffID)
        {
            try
            {
                if (receptionStaffID <= 0)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure("ReceptionStaffID is required.");
                }

                var result = await _receptionStaffService.GetReceptionStaffByIdAsync(receptionStaffID);
                if (result == null)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure("Reception staff not found or has been deleted.");
                }

                var deny = DoctorOwnership.ForbidIfNotOwner(User, result.DoctorID);
                if (deny != null)
                    return deny;

                return ThreeDBodyPartApiResponseHelper.Success(result, "Reception staff retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetReceptionStaffById failed for ReceptionStaffID={ReceptionStaffId}", receptionStaffID);
                return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
            }
        }

        /// <summary>
        /// Get paginated reception staff list for a doctor
        /// </summary>
        [HttpGet("GetReceptionStaffList")]
        [ProducesResponseType(typeof(PaginatedApiResponse<ReceptionStaffListItemModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status500InternalServerError)]
        public async Task<object> GetReceptionStaffList([FromQuery] GetReceptionStaffListRequest request)
        {
            try
            {
                request ??= new GetReceptionStaffListRequest();
                BindDoctorUserId(request);
                if (request.DoctorUserID <= 0)
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure("DoctorUserID is required.");
                if (request.PageNumber < 1) request.PageNumber = 1;
                if (request.PageSize < 1) request.PageSize = 10;

                var (success, message, result) = await _receptionStaffService.GetReceptionStaffListAsync(request);
                if (!success || result == null)
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(message);
                }

                return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(result, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetReceptionStaffList failed for DoctorUserID={DoctorUserId}", request.DoctorUserID);
                return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
            }
        }

        private void BindDoctorUserId(AddReceptionStaffRequest request)
        {
            if (DoctorOwnership.IsAdminPortalUser(User))
                return;
            request.DoctorUserID = User.GetUserId();
        }

        private void BindDoctorUserId(GetReceptionStaffListRequest request)
        {
            if (DoctorOwnership.IsAdminPortalUser(User))
                return;
            request.DoctorUserID = User.GetUserId();
        }

        private async Task<IActionResult?> ForbidIfNotOwnStaffAsync(int receptionStaffId)
        {
            if (DoctorOwnership.IsAdminPortalUser(User))
                return null;

            var row = await _receptionStaffService.GetReceptionStaffByIdAsync(receptionStaffId);
            if (row == null)
                return null;

            return DoctorOwnership.ForbidIfNotOwner(User, row.DoctorID);
        }

        private string GetValidationMessage()
        {
            return string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
        }
    }
}

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// User / doctor registration and account APIs.
    /// </summary>
    [Route("api/users")]
    [ApiController]
    public class UsersController : BaseAPIController
    {
        private readonly IUserService _userService;
        private readonly IOptions<SmtpSettingsModel> _mailSettings;

        public UsersController(IUserService userService, IOptions<SmtpSettingsModel> mailSettings)
        {
            _userService = userService;
            _mailSettings = mailSettings;
        }

        /// <summary>
        /// Public doctor self-registration (full doctor profile). Package is selected after login.
        /// </summary>
        [HttpPost("RegisterDoctor")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public IActionResult RegisterDoctor([FromBody] DoctorRegistrationModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest("Invalid request, please verify details");
            }

            try
            {
                var errorMessage = new ErrorResponseModel();
                var result = _userService.RegisterDoctor(model, _mailSettings.Value, ref errorMessage);

                if (result == "User already exists" || result == "User name already exists")
                {
                    return BadRequest(result);
                }

                if (!string.IsNullOrWhiteSpace(result))
                {
                    return Ok(new
                    {
                        success = true,
                        message = result
                    });
                }

                return ReturnErrorResponse(errorMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{userId}")]
        [Authorize]
        [ProducesResponseType(typeof(UserModel), 200)]
        public IActionResult Get(long userId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                if (userId <= 0)
                {
                    return BadRequest("Invalid data");
                }

                var userModel = _userService.GetUserById(userId, ref errorResponseModel);
                if (userModel != null)
                {
                    return Ok(userModel);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Admin create / update user (creates Doctor row when RoleId is 3).
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Post([FromBody] UserModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest("Invalid request, please verify details");
            }

            try
            {
                var errorMessage = new ErrorResponseModel();
                var userModel = _userService.AddUser(model, _mailSettings.Value, ref errorMessage);
                if (!string.IsNullOrEmpty(userModel))
                {
                    return Ok(userModel);
                }

                return ReturnErrorResponse(errorMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("ActivateUser")]
        [AllowAnonymous]
        public IActionResult ActivateUser([FromBody] UserModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.EncryptedUserId))
            {
                return BadRequest("Invalid request, please verify details");
            }

            try
            {
                var errorMessage = new ErrorResponseModel();
                var activated = _userService.ActivateUser(model, ref errorMessage);
                if (activated)
                {
                    return Ok(new { success = true, message = "Account activated successfully" });
                }

                return ReturnErrorResponse(errorMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("GetCount")]
        [Authorize]
        public IActionResult GetCount()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var count = _userService.GetCount(ref errorResponseModel);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(NewUserModel), 200)]
        public IActionResult GetAllUser()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var userModel = _userService.GetAllUser(ref errorResponseModel);
                if (userModel != null)
                {
                    return Ok(userModel);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("DeleteUser")]
        [Authorize]
        public IActionResult DeleteUser([FromBody] UserModel userModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var result = _userService.DeleteUser(userModel, ref errorResponseModel);
                if (!string.IsNullOrEmpty(result))
                {
                    return Ok(result);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("ForgetPassword")]
        [AllowAnonymous]
        public IActionResult ForgetPassword([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Invalid request, please verify details");
            }

            try
            {
                var errorMessage = new ErrorResponseModel();
                var result = _userService.ForgetPassword(email, _mailSettings.Value, ref errorMessage);
                if (!string.IsNullOrEmpty(result))
                {
                    return Ok(result);
                }

                return ReturnErrorResponse(errorMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

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
        private readonly NIGACentrumContext _context;
        private readonly IWebHostEnvironment _env;

        public UsersController(
            IUserService userService,
            IOptions<SmtpSettingsModel> mailSettings,
            NIGACentrumContext context,
            IWebHostEnvironment env)
        {
            _userService = userService;
            _mailSettings = mailSettings;
            _context = context;
            _env = env;
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

        /// <summary>WEB-09.03 — same RegisterDoctor fields plus qualification/registration files.</summary>
        [HttpPost("RegisterDoctorWithDocuments")]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RegisterDoctorWithDocuments(
            [FromForm] DoctorRegistrationModel model,
            [FromForm] IFormFile? qualificationDoc,
            [FromForm] IFormFile? registrationDoc)
        {
            if (model == null || !ModelState.IsValid)
                return BadRequest("Invalid request, please verify details");

            try
            {
                var errorMessage = new ErrorResponseModel();
                var result = _userService.RegisterDoctor(model, _mailSettings.Value, ref errorMessage);

                if (result == "User already exists" || result == "User name already exists")
                    return BadRequest(result);

                if (string.IsNullOrWhiteSpace(result))
                    return ReturnErrorResponse(errorMessage);

                var doctor = await _context.Doctors
                    .OrderByDescending(d => d.DoctorId)
                    .FirstOrDefaultAsync(d => d.EmailId == model.EmailId.Trim() && !d.DeleteStatus);

                var saved = 0;
                if (doctor != null)
                {
                    saved += await SaveRegistrationDocumentAsync(doctor, qualificationDoc, "Qualification");
                    saved += await SaveRegistrationDocumentAsync(doctor, registrationDoc, "Registration");
                }

                return Ok(new
                {
                    success = true,
                    message = result,
                    documentsSaved = saved,
                    verificationStatus = "Pending",
                    directoryVisible = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>WEB-09.03 — public registration status by email (no extra PII).</summary>
        [HttpGet("RegistrationStatus")]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrationStatus([FromQuery] string emailId)
        {
            if (string.IsNullOrWhiteSpace(emailId))
                return BadRequest(new { success = false, message = "emailId is required." });

            var email = emailId.Trim();
            var user = await _context.UserMasters.AsNoTracking()
                .FirstOrDefaultAsync(u => u.EmailId == email && !u.DeleteStatus);
            if (user == null)
                return Ok(new { success = true, found = false, message = "No registration found for that email." });

            var doctorUserId = Convert.ToInt32(user.UserId);
            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == doctorUserId && !d.DeleteStatus);

            return Ok(new
            {
                success = true,
                found = true,
                activated = user.IsUserActivated == true,
                verificationStatus = doctor?.VerificationStatus ?? "Pending",
                directoryVisible = doctor?.DirectoryVisible == true,
                practiceActivated = doctor?.PracticeActivated == true
            });
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

        [HttpPost("ActivateByToken")]
        [AllowAnonymous]
        public IActionResult ActivateByToken([FromBody] ActivateByTokenRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Token))
                return BadRequest("Token is required");

            try
            {
                var errorMessage = new ErrorResponseModel();
                var result = _userService.ActivateByToken(model.Token, ref errorMessage);
                if (result != null)
                    return Ok(result);
                return ReturnErrorResponse(errorMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("ResendActivation")]
        [AllowAnonymous]
        public IActionResult ResendActivation([FromBody] ResendActivationRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.EmailId))
                return BadRequest("EmailId is required");

            try
            {
                var errorMessage = new ErrorResponseModel();
                var result = _userService.ResendActivation(model.EmailId.Trim(), _mailSettings.Value, ref errorMessage);
                return Ok(result);
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
            // SEC-02.02 — plaintext password email removed; redirect clients to Account/ForgotPassword
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
                    return Ok(new
                    {
                        success = false,
                        deprecated = true,
                        message = result,
                        useInstead = "POST /api/Account/ForgotPassword"
                    });
                }

                return ReturnErrorResponse(errorMessage);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private async Task<int> SaveRegistrationDocumentAsync(Doctor doctor, IFormFile? file, string documentType)
        {
            if (file == null || file.Length == 0)
                return 0;

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png"))
                return 0;

            var verification = await _context.DoctorVerifications
                .FirstOrDefaultAsync(v => v.DoctorId == doctor.DoctorId && !v.DeleteStatus);
            if (verification == null)
            {
                verification = new DoctorVerification
                {
                    DoctorId = doctor.DoctorId,
                    Status = "Pending",
                    EnteredDate = DateTime.UtcNow,
                    DeleteStatus = false
                };
                _context.DoctorVerifications.Add(verification);
                await _context.SaveChangesAsync();
            }

            var folder = Path.Combine(_env.ContentRootPath, "Data", "DoctorCredentials");
            Directory.CreateDirectory(folder);
            var name = $"doc_{doctor.DoctorId}_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream);

            _context.DoctorCredentialDocuments.Add(new DoctorCredentialDocument
            {
                DoctorId = doctor.DoctorId,
                DoctorVerificationId = verification.DoctorVerificationId,
                DocumentType = documentType,
                FileName = Path.GetExtension(file.FileName) != null ? Path.GetFileName(file.FileName) : name,
                FilePath = Path.Combine("DoctorCredentials", name).Replace("\\", "/"),
                ContentType = file.ContentType,
                EnteredDate = DateTime.UtcNow,
                DeleteStatus = false
            });
            await _context.SaveChangesAsync();
            return 1;
        }
    }
}

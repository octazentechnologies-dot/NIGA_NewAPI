using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers
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
        private readonly ITokenService _tokenService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            IOptions<SmtpSettingsModel> mailSettings,
            NIGACentrumContext context,
            IWebHostEnvironment env,
            ITokenService tokenService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _mailSettings = mailSettings;
            _context = context;
            _env = env;
            _tokenService = tokenService;
            _logger = logger;
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
                    return BadRequest(new
                    {
                        success = false,
                        message = result,
                        code = result == "User already exists" ? "EMAIL_EXISTS" : "USERNAME_EXISTS",
                        isUserAlreadyRegistered = true
                    });
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

        /// <summary>
        /// WEB-09.03 / DMO mobile — same RegisterDoctor fields plus optional files.
        /// Every file is optional; zero files is a valid registration. Rejected files are listed
        /// in rejectedDocuments and never fail the registration.
        /// </summary>
        [HttpPost("RegisterDoctorWithDocuments")]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(80_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 80_000_000)]
        public async Task<IActionResult> RegisterDoctorWithDocuments(
            [FromForm] RegisterDoctorWithDocumentsForm model)
        {
            if (model == null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid request, please verify details" });

            try
            {
                var errorMessage = new ErrorResponseModel();
                var result = _userService.RegisterDoctor(model, _mailSettings.Value, ref errorMessage);

                if (result == "User already exists" || result == "User name already exists")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result,
                        code = result == "User already exists" ? "EMAIL_EXISTS" : "USERNAME_EXISTS",
                        isUserAlreadyRegistered = true,
                        documentsSaved = 0
                    });
                }

                if (string.IsNullOrWhiteSpace(result))
                    return ReturnErrorResponse(errorMessage);

                var doctor = await _context.Doctors
                    .OrderByDescending(d => d.DoctorId)
                    .FirstOrDefaultAsync(d => d.EmailId == model.EmailId.Trim() && !d.DeleteStatus);

                var saved = new List<DoctorCredentialDocument>();
                var rejected = new List<object>();
                async Task SaveAsync(IFormFile? file, string documentType)
                {
                    if (file == null)
                        return;
                    var reason = CredentialFileRules.RejectReason(file.FileName, file.Length);
                    if (reason == null && doctor == null)
                        reason = "Doctor profile was not found after registration.";
                    if (reason == null)
                    {
                        try
                        {
                            var row = await SaveRegistrationDocumentAsync(doctor!, file, documentType);
                            if (row != null)
                            {
                                saved.Add(row);
                                return;
                            }
                            reason = "This file type cannot be stored.";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Registration document save failed for {FileName}", file.FileName);
                            reason = "The file could not be saved on the server.";
                        }
                    }
                    rejected.Add(new { documentType, fileName = Path.GetFileName(file.FileName), reason });
                }

                await SaveAsync(model.QualificationDoc, "Qualification");
                await SaveAsync(model.RegistrationDoc, "Registration");
                if (model.Documents != null)
                {
                    for (var i = 0; i < model.Documents.Count; i++)
                    {
                        var type = CredentialFileRules.CanonicalType(
                            model.DocumentTypes != null && i < model.DocumentTypes.Count ? model.DocumentTypes[i] : null);
                        await SaveAsync(model.Documents[i], type);
                    }
                }

                var user = await _context.UserMasters
                    .FirstOrDefaultAsync(u => u.EmailId == model.EmailId.Trim() && !u.DeleteStatus);
                string? token = null;
                object? userDetails = null;
                if (user != null)
                {
                    var roleName = await _context.RoleMasters.AsNoTracking()
                        .Where(r => r.RoleId == user.RoleId && !r.DeleteStatus)
                        .Select(r => r.RoleName)
                        .FirstOrDefaultAsync() ?? "Doctor";
                    token = await _tokenService.CreateToken(user, 7 * 24 * 60, roleName, doctor?.DoctorId);
                    userDetails = new
                    {
                        userId = user.UserId,
                        userName = user.UserName,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        email = user.EmailId,
                        mobileNo = user.MobileNo,
                        role = roleName,
                        roleId = user.RoleId,
                        doctorId = doctor?.DoctorId
                    };
                }

                var savedTypes = saved.Select(d => d.DocumentType).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var payload = new Dictionary<string, object?>
                {
                    ["success"] = true,
                    ["message"] = result,
                    ["mailSent"] = result.StartsWith("Registration successful", StringComparison.Ordinal),
                    ["doctorId"] = doctor?.DoctorId,
                    ["documentsSaved"] = saved.Count,
                    ["documents"] = saved.Select(d => new
                    {
                        documentId = d.DoctorCredentialDocumentId,
                        documentType = d.DocumentType,
                        fileName = d.FileName,
                        filePath = d.FilePath,
                        contentType = d.ContentType,
                        downloadUrl = CredentialFileRules.DownloadUrl(d.DoctorCredentialDocumentId)
                    }),
                    ["documentsRejected"] = rejected.Count,
                    ["rejectedDocuments"] = rejected,
                    ["hasQualificationDoc"] = savedTypes.Contains("Qualification"),
                    ["hasRegistrationDoc"] = savedTypes.Contains("Registration"),
                    ["missingDocumentTypes"] = new[] { "Qualification", "Registration" }.Where(t => !savedTypes.Contains(t)).ToArray(),
                    ["verificationStatus"] = string.IsNullOrWhiteSpace(doctor?.VerificationStatus) ? "Pending" : doctor!.VerificationStatus,
                    ["directoryVisible"] = doctor?.DirectoryVisible == true,
                    ["isUserAlreadyRegistered"] = false,
                    ["mobileUsedByAnotherDoctor"] = await MobileAlreadyOnAnotherDoctorAsync(model.MobileNo, doctor?.DoctorId)
                };
                if (token != null)
                {
                    payload["token"] = token;
                    payload["user"] = userDetails;
                }

                return Ok(payload);
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

        /// <summary>True when this mobile is already stored on a different Doctor row.</summary>
        private async Task<bool> MobileAlreadyOnAnotherDoctorAsync(string? mobileNo, int? currentDoctorId)
        {
            var digits = PhoneNormalizer.Digits(mobileNo);
            if (digits.Length < 8)
                return false;

            var mobiles = await _context.Doctors.AsNoTracking()
                .Where(d => !d.DeleteStatus && d.MobileNo != null && d.DoctorId != currentDoctorId)
                .Select(d => d.MobileNo!)
                .ToListAsync();
            return mobiles.Any(m => PhoneNormalizer.EqualsNormalized(m, digits));
        }

        private async Task<DoctorCredentialDocument?> SaveRegistrationDocumentAsync(Doctor doctor, IFormFile? file, string documentType)
        {
            if (file == null || file.Length == 0)
                return null;
            if (!CredentialFileRules.TryGetExtension(file.FileName, out var ext))
                return null;

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

            var folder = UploadedMedia.Folder(_env.ContentRootPath, UploadedMedia.DoctorCredentials);
            Directory.CreateDirectory(folder);
            var name = $"doc_{doctor.DoctorId}_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream);

            var row = new DoctorCredentialDocument
            {
                DoctorId = doctor.DoctorId,
                DoctorVerificationId = verification.DoctorVerificationId,
                DocumentType = documentType,
                FileName = Path.GetFileName(file.FileName),
                FilePath = UploadedMedia.MediaRelative(UploadedMedia.DoctorCredentials, name),
                ContentType = file.ContentType,
                EnteredDate = DateTime.UtcNow,
                DeleteStatus = false
            };
            _context.DoctorCredentialDocuments.Add(row);
            await _context.SaveChangesAsync();
            return row;
        }
    }
}

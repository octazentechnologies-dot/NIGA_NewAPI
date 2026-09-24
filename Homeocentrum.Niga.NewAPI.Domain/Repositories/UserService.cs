using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
{
    public class UserService : IUserService
    {
        private readonly NIGACentrumContext _context;
        private readonly EmailSenderService _emailSenderService;
        private readonly IConfiguration _configuration;

        public UserService(NIGACentrumContext context, IConfiguration configuration)
        {
            _context = context;
            _emailSenderService = new EmailSenderService();
            _configuration = configuration;
        }

        public string RegisterDoctor(DoctorRegistrationModel model, SmtpSettingsModel smtpSettingsModel, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel ??= new ErrorResponseModel();

            var existingByEmail = _context.UserMasters
                .FirstOrDefault(x => x.EmailId == model.EmailId && !x.DeleteStatus);
            if (existingByEmail != null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "User already exists";
                return "User already exists";
            }

            var existingByUserName = _context.UserMasters
                .FirstOrDefault(x => x.UserName == model.UserName && !x.DeleteStatus);
            if (existingByUserName != null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "User name already exists";
                return "User name already exists";
            }

            var userEntity = new UserMaster
            {
                UserName = model.UserName.Trim(),
                UserPassword = UserPasswordHasher.Hash(model.UserPassword),
                MobileNo = model.MobileNo?.Trim() ?? string.Empty,
                EmailId = model.EmailId.Trim(),
                CountryId = model.CountryId,
                StateId = model.StateId,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                DeleteStatus = false,
                EnteredBy = model.UserName.Trim(),
                EnteredDate = DateTime.Now,
                RoleId = 3,
                UserStatus = true,
                IsUserActivated = false
            };

            var rawToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            userEntity.ActivationTokenHash = SecurityTokenHash.Sha256Hex(rawToken);
            userEntity.ActivationExpiresAt = DateTime.UtcNow.AddHours(48);

            _context.UserMasters.Add(userEntity);
            _context.SaveChanges();

            var doctorEntity = new Doctor
            {
                UserId = Convert.ToInt32(userEntity.UserId),
                FirstName = model.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(),
                LastName = model.LastName.Trim(),
                MobileNo = model.MobileNo?.Trim() ?? string.Empty,
                EmailId = model.EmailId.Trim(),
                QualificationId = model.QualificationId,
                PermanantAddress = string.IsNullOrWhiteSpace(model.PermanantAddress) ? null : model.PermanantAddress.Trim(),
                PassingUniversity = string.IsNullOrWhiteSpace(model.PassingUniversity) ? null : model.PassingUniversity.Trim(),
                PassingCertNo = string.IsNullOrWhiteSpace(model.PassingCertNo) ? null : model.PassingCertNo.Trim(),
                City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                CountryId = model.CountryId,
                StateId = model.StateId,
                ClinicName = model.CompanyName?.Trim(),
                DirectoryVisible = false,
                VerificationStatus = "Pending",
                PracticeActivated = false,
                IsOnline = false,
                EnteredBy = model.UserName.Trim(),
                EnteredDate = DateTime.Now,
                DeleteStatus = false
            };

            _context.Doctors.Add(doctorEntity);
            _context.SaveChanges();

            _context.DoctorVerifications.Add(new DoctorVerification
            {
                DoctorId = doctorEntity.DoctorId,
                Status = "Pending",
                EnteredDate = DateTime.UtcNow,
                DeleteStatus = false
            });
            _context.SaveChanges();

            TrySendActivationEmail(userEntity, rawToken, smtpSettingsModel);

            return "Registration successful. Check your email to activate the account. Directory listing stays pending until verification.";
        }

        public string AddUser(UserModel model, SmtpSettingsModel smtpSettingsModel, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel ??= new ErrorResponseModel();
            var message = string.Empty;

            if (model.UserId <= 0)
            {
                var existingUser = _context.UserMasters
                    .FirstOrDefault(x => x.EmailId == model.EmailId && !x.DeleteStatus);
                if (existingUser != null)
                {
                    return "User already exists";
                }

                var userEntity = new UserMaster
                {
                    UserName = model.UserName,
                    UserPassword = UserPasswordHasher.Hash(model.UserPassword),
                    MobileNo = model.MobileNo ?? string.Empty,
                    EmailId = model.EmailId,
                    CountryId = model.CountryId > 0 ? model.CountryId : null,
                    StateId = model.StateId > 0 ? model.StateId : null,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CompanyName = model.CompanyName,
                    DeleteStatus = false,
                    EnteredBy = model.EnteredBy,
                    EnteredDate = DateTime.Now,
                    RoleId = model.RoleId > 0 ? model.RoleId : 3,
                    UserStatus = model.UserStatus,
                    IsUserActivated = true
                };

                _context.UserMasters.Add(userEntity);
                _context.SaveChanges();

                if (userEntity.RoleId == 3)
                {
                    var doctorEntity = new Doctor
                    {
                        UserId = Convert.ToInt32(userEntity.UserId),
                        FirstName = userEntity.FirstName,
                        LastName = userEntity.LastName,
                        MobileNo = userEntity.MobileNo,
                        EmailId = userEntity.EmailId,
                        EnteredBy = userEntity.EnteredBy,
                        EnteredDate = DateTime.Now,
                        DeleteStatus = false
                    };
                    _context.Doctors.Add(doctorEntity);
                    _context.SaveChanges();
                }

                TrySendWelcomeEmail(userEntity, smtpSettingsModel);
                message = "Activation link is sent to your email address.Please check your inbox to activate account.";
            }
            else
            {
                var userEntity = _context.UserMasters.FirstOrDefault(x => x.UserId == model.UserId);
                if (userEntity != null)
                {
                    userEntity.UserName = model.UserName;
                    if (!string.IsNullOrWhiteSpace(model.UserPassword))
                        userEntity.UserPassword = UserPasswordHasher.Hash(model.UserPassword);
                    userEntity.FirstName = model.FirstName;
                    userEntity.LastName = model.LastName;
                    userEntity.RoleId = model.RoleId;
                    userEntity.MobileNo = model.MobileNo;
                    userEntity.EmailId = model.EmailId;
                    userEntity.UserStatus = model.UserStatus;
                    userEntity.ChangedBy = model.ChangedBy ?? model.EnteredBy;
                    userEntity.ChangedDate = DateTime.Now;
                    _context.SaveChanges();
                    message = "User Updated Successfully";
                }
            }

            return message;
        }

        public UserModel GetUserById(long userId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var userEntity = _context.UserMasters.FirstOrDefault(x => x.UserId == userId && !x.DeleteStatus);

            if (userEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "User not found";
                return null;
            }

            return new UserModel
            {
                UserId = userEntity.UserId,
                UserName = userEntity.UserName,
                MobileNo = userEntity.MobileNo,
                EmailId = userEntity.EmailId,
                UserStatus = userEntity.UserStatus,
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                // SEC-01 — never return password hash/plaintext in API responses
                UserPassword = null!,
                RoleId = userEntity.RoleId ?? 0,
                CompanyName = userEntity.CompanyName,
                CountryId = userEntity.CountryId ?? 0,
                StateId = userEntity.StateId ?? 0
            };
        }

        public bool ActivateUser(UserModel model, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel ??= new ErrorResponseModel();
            var decryptedUserId = EncryptionHelper.Decrypt(model.EncryptedUserId);
            if (!int.TryParse(decryptedUserId, out var userId))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Invalid activation link";
                return false;
            }

            var userEntity = _context.UserMasters.FirstOrDefault(x => x.UserId == userId);
            if (userEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "User not found";
                return false;
            }

            userEntity.IsUserActivated = true;
            userEntity.ActivationTokenHash = null;
            userEntity.ActivationExpiresAt = null;
            userEntity.ChangedBy = model.ChangedBy;
            userEntity.ChangedDate = DateTime.Now;
            _context.SaveChanges();
            return true;
        }

        public object ActivateByToken(string token, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel ??= new ErrorResponseModel();
            if (string.IsNullOrWhiteSpace(token))
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Token is required";
                return null!;
            }

            var hash = SecurityTokenHash.Sha256Hex(token.Trim());
            var userEntity = _context.UserMasters.FirstOrDefault(x =>
                x.ActivationTokenHash == hash && !x.DeleteStatus);
            if (userEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Invalid or already used activation link";
                return null!;
            }

            if (userEntity.ActivationExpiresAt.HasValue && userEntity.ActivationExpiresAt.Value < DateTime.UtcNow)
            {
                errorResponseModel.StatusCode = HttpStatusCode.Gone;
                errorResponseModel.Message = "Activation link expired. Request a new email.";
                return null!;
            }

            userEntity.IsUserActivated = true;
            userEntity.ActivationTokenHash = null;
            userEntity.ActivationExpiresAt = null;
            userEntity.ChangedDate = DateTime.Now;
            _context.SaveChanges();
            return new { success = true, message = "Account activated successfully", userId = userEntity.UserId };
        }

        public object ResendActivation(string emailId, SmtpSettingsModel smtpSettingsModel, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel ??= new ErrorResponseModel();
            var userEntity = _context.UserMasters.FirstOrDefault(x =>
                x.EmailId == emailId && !x.DeleteStatus);
            if (userEntity == null)
            {
                // Do not reveal whether the email exists.
                return new { success = true, message = "If the account exists and is not activated, an email was sent." };
            }

            if (userEntity.IsUserActivated == true)
            {
                return new { success = true, message = "Account is already activated. Please sign in." };
            }

            var rawToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            userEntity.ActivationTokenHash = SecurityTokenHash.Sha256Hex(rawToken);
            userEntity.ActivationExpiresAt = DateTime.UtcNow.AddHours(48);
            userEntity.ChangedDate = DateTime.Now;
            _context.SaveChanges();
            TrySendActivationEmail(userEntity, rawToken, smtpSettingsModel);
            return new { success = true, message = "If the account exists and is not activated, an email was sent." };
        }

        public int GetCount(ref ErrorResponseModel errorResponseModel)
        {
            return _context.UserMasters.Count(x => x.UserStatus && x.RoleId == 3 && !x.DeleteStatus);
        }

        public List<NewUserModel> GetAllUser(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var userEntityList = _context.UserMasters.Where(x => !x.DeleteStatus).ToList();
            if (userEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "User not found";
            }

            return userEntityList.Select(item => new NewUserModel
            {
                UserId = item.UserId,
                UserName = item.UserName,
                UserStatus = item.UserStatus,
                EmailId = item.EmailId,
                FirstName = item.FirstName,
                LastName = item.LastName,
                RoleId = item.RoleId,
            }).ToList();
        }

        public string DeleteUser(UserModel userModel, ref ErrorResponseModel errorResponseModel)
        {
            var userEntity = _context.UserMasters.FirstOrDefault(x => x.UserId == userModel.UserId);
            if (userEntity == null)
            {
                return string.Empty;
            }

            userEntity.DeleteStatus = true;
            _context.SaveChanges();
            return "User Deleted Successfully";
        }

        public string ForgetPassword(string email, SmtpSettingsModel smtpSettingsModel, ref ErrorResponseModel errorResponseModel)
        {
            // SEC-02.02 — Do not email plaintext passwords. Use POST /api/Account/ForgotPassword (reset-link flow).
            errorResponseModel ??= new ErrorResponseModel();
            errorResponseModel.StatusCode = HttpStatusCode.Gone;
            errorResponseModel.Message =
                "This endpoint no longer emails passwords. Use POST /api/Account/ForgotPassword for the secure reset-link flow.";
            return "Deprecated: use POST /api/Account/ForgotPassword (reset link). Passwords are never emailed in plaintext.";
        }

        private void TrySendWelcomeEmail(UserMaster userEntity, SmtpSettingsModel smtpSettingsModel)
        {
            try
            {
                var encryptedUserId = EncryptionHelper.Encrypt(userEntity.UserId.ToString());
                var siteUrl = _configuration["ConfigurationModel:SiteUrl"]
                    ?? _configuration["AppSettings:UiBaseUrl"]
                    ?? "https://homeocentrum.com";
                siteUrl = siteUrl.TrimEnd('/');

                var strBody = new StringBuilder();
                strBody.Append("<body style='font-family:Arial,sans-serif;color:#1f2937;'>");
                strBody.Append("<h2 style='color:#1e88e5;'>Welcome to Homeocentrum</h2>");
                strBody.Append("<p>Hello Dr. " + (userEntity.FirstName ?? userEntity.UserName) + ",</p>");
                strBody.Append("<p>Your account has been created successfully.</p>");
                strBody.Append("<p>Please sign in and choose a subscription plan to start practising.</p>");
                strBody.Append("<p><a href='" + siteUrl + "/login?UserId=" + Uri.EscapeDataString(encryptedUserId) + "' style='display:inline-block;padding:10px 18px;background:#1e88e5;color:#fff;text-decoration:none;border-radius:6px;'>Sign in to Homeocentrum</a></p>");
                strBody.Append("<p style='color:#6b7280;font-size:12px;'>If the button does not work, open: " + siteUrl + "/login</p>");
                strBody.Append("</body>");

                var emailSenderModel = new EmailSenderModel
                {
                    ToAddress = userEntity.EmailId,
                    Body = strBody.ToString(),
                    isHtml = true,
                    Subject = "Welcome to Homeocentrum"
                };

                _emailSenderService.SendMail(emailSenderModel, smtpSettingsModel);
            }
            catch (Exception)
            {
                // Registration should succeed even if SMTP is unavailable.
            }
        }

        private void TrySendActivationEmail(UserMaster userEntity, string rawToken, SmtpSettingsModel smtpSettingsModel)
        {
            try
            {
                var siteUrl = _configuration["ConfigurationModel:SiteUrl"]
                    ?? _configuration["AppSettings:UiBaseUrl"]
                    ?? "https://homeocentrum.com";
                siteUrl = siteUrl.TrimEnd('/');
                var link = siteUrl + "/activate?token=" + Uri.EscapeDataString(rawToken);

                var strBody = new StringBuilder();
                strBody.Append("<body style='font-family:Arial,sans-serif;color:#1f2937;'>");
                strBody.Append("<h2 style='color:#1e88e5;'>Activate your Homeocentrum account</h2>");
                strBody.Append("<p>Hello Dr. " + (userEntity.FirstName ?? userEntity.UserName) + ",</p>");
                strBody.Append("<p>Confirm your email to activate login. Directory listing stays pending until verification.</p>");
                strBody.Append("<p><a href='" + link + "' style='display:inline-block;padding:10px 18px;background:#1e88e5;color:#fff;text-decoration:none;border-radius:6px;'>Activate account</a></p>");
                strBody.Append("<p style='color:#6b7280;font-size:12px;'>This link expires in 48 hours.</p>");
                strBody.Append("</body>");

                var emailSenderModel = new EmailSenderModel
                {
                    ToAddress = userEntity.EmailId,
                    Body = strBody.ToString(),
                    isHtml = true,
                    Subject = "Activate your Homeocentrum account"
                };

                _emailSenderService.SendMail(emailSenderModel, smtpSettingsModel);
            }
            catch (Exception)
            {
                // Activation email is best-effort.
            }
        }
    }
}

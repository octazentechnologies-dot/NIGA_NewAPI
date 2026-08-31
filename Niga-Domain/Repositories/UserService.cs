using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;
using Niga_Domain.Services;

namespace Niga_Domain.Repositories
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
                UserPassword = model.UserPassword,
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
                IsUserActivated = true
            };

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
                EnteredBy = model.UserName.Trim(),
                EnteredDate = DateTime.Now,
                DeleteStatus = false
            };

            _context.Doctors.Add(doctorEntity);
            _context.SaveChanges();

            TrySendWelcomeEmail(userEntity, smtpSettingsModel);

            return "Registration successful. Please sign in and choose a subscription plan to continue.";
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
                    UserPassword = model.UserPassword,
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
                    userEntity.UserPassword = model.UserPassword;
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
                UserPassword = userEntity.UserPassword,
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
            userEntity.ChangedBy = model.ChangedBy;
            userEntity.ChangedDate = DateTime.Now;
            _context.SaveChanges();
            return true;
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
            errorResponseModel ??= new ErrorResponseModel();
            var userEntity = _context.UserMasters.FirstOrDefault(x => x.EmailId == email);
            if (userEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                return "Email Not Found";
            }

            try
            {
                var strBody = new StringBuilder();
                strBody.Append("<body>");
                strBody.Append("Hello " + userEntity.UserName);
                strBody.Append("<p>Your password for Homeocentrum portal is:</p>");
                strBody.Append("<p><b>" + userEntity.UserPassword + "</b></p>");
                strBody.Append("</body>");

                var emailModel = new EmailSenderModel
                {
                    ToAddress = email,
                    Body = strBody.ToString(),
                    isHtml = true,
                    Subject = "Homeocentrum - Forgot Password"
                };

                if (!string.IsNullOrEmpty(emailModel.ToAddress))
                {
                    _emailSenderService.SendMail(emailModel, smtpSettingsModel);
                }

                return "Email Send Successfully";
            }
            catch (Exception)
            {
                return "Email Send Successfully";
            }
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
    }
}

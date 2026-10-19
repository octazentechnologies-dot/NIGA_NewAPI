using System;
using System.Threading.Tasks;
using API.Mapper;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Repositories
{
    public class ReceptionStaffService : IReceptionStaffService
    {
        private readonly IReceptionStaffRepository _repository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<ReceptionStaffService> _logger;

        public ReceptionStaffService(
            IReceptionStaffRepository repository,
            ITokenService tokenService,
            ILogger<ReceptionStaffService> logger)
        {
            _repository = repository;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, AddReceptionStaffResultModel? Result)> AddReceptionStaffAsync(
            AddReceptionStaffRequest request)
        {
            var validationMessage = ValidateAddRequest(request);
            if (validationMessage != null)
            {
                return (false, validationMessage, null);
            }

            var doctor = await _repository.GetActiveDoctorByUserIdAsync(request.DoctorUserID);
            if (doctor == null)
            {
                return (false, "Doctor not found or has been deleted.", null);
            }

            if (await _repository.IsUserIdExistsAsync(request.UserID))
            {
                return (false, "UserID already exists.", null);
            }

            if (!string.IsNullOrWhiteSpace(request.EmailId)
                && await _repository.IsEmailExistsAsync(request.EmailId))
            {
                return (false, "Email already exists.", null);
            }

            if (await _repository.IsContactNumberExistsAsync(request.ContactNumber))
            {
                return (false, "Contact number already exists.", null);
            }

            var entity = request.ToDoctorReceptionStaff();
            entity.DoctorId = doctor.DoctorId;
            entity.UserId = request.UserID.Trim();
            entity.Password = ReceptionStaffPasswordHelper.HashPassword(request.Password);
            entity.FullName = request.FullName.Trim();
            entity.ContactNumber = request.ContactNumber.Trim();
            entity.EmailId = string.IsNullOrWhiteSpace(request.EmailId) ? null : request.EmailId.Trim();
            entity.EnteredDate = DateTime.Now;
            entity.DeleteStatus = false;
            entity.IsActive = true;

            _repository.AddReceptionStaff(entity);

            if (!await _repository.SaveAllAsync())
            {
                return (false, "Failed to create reception staff.", null);
            }

            _logger.LogInformation(
                "Reception staff created. ReceptionStaffID={ReceptionStaffId}, DoctorID={DoctorId}",
                entity.ReceptionStaffId,
                entity.DoctorId);

            return (true, "Reception staff created successfully.", new AddReceptionStaffResultModel
            {
                ReceptionStaffID = entity.ReceptionStaffId,
                DoctorID = entity.DoctorId,
                UserID = entity.UserId,
                FullName = entity.FullName
            });
        }

        public async Task<(bool Success, string Message)> UpdateReceptionStaffAsync(UpdateReceptionStaffRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return (false, "FullName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ContactNumber))
            {
                return (false, "ContactNumber is required.");
            }

            var entity = await _repository.GetActiveReceptionStaffEntityByIdAsync(request.ReceptionStaffID);
            if (entity == null)
            {
                return (false, "Reception staff not found or has been deleted.");
            }

            if (!string.IsNullOrWhiteSpace(request.EmailId)
                && await _repository.IsEmailExistsAsync(request.EmailId, request.ReceptionStaffID))
            {
                return (false, "Email already exists.");
            }

            if (await _repository.IsContactNumberExistsAsync(request.ContactNumber, request.ReceptionStaffID))
            {
                return (false, "Contact number already exists.");
            }

            entity.FullName = request.FullName.Trim();
            entity.Address = request.Address;
            entity.ContactNumber = request.ContactNumber.Trim();
            entity.EmailId = string.IsNullOrWhiteSpace(request.EmailId) ? null : request.EmailId.Trim();
            entity.Country = request.Country;
            entity.State = request.State;
            entity.City = request.City;
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (request.Password.Trim().Length < 6)
                    return (false, "Password must be at least 6 characters.");
                entity.Password = ReceptionStaffPasswordHelper.HashPassword(request.Password.Trim());
            }
            entity.ChangedBy = request.ChangedBy;
            entity.ChangedDate = DateTime.Now;

            _repository.UpdateReceptionStaff(entity);

            if (!await _repository.SaveAllAsync())
            {
                return (false, "Failed to update reception staff.");
            }

            _logger.LogInformation(
                "Reception staff updated. ReceptionStaffID={ReceptionStaffId}",
                entity.ReceptionStaffId);

            return (true, "Reception staff updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteReceptionStaffAsync(DeleteReceptionStaffRequest request)
        {
            // "Delete" from the UI means disable — keep the row visible for re-enable.
            return await SetReceptionStaffActiveAsync(new SetReceptionStaffActiveRequest
            {
                ReceptionStaffID = request.ReceptionStaffID,
                IsActive = false,
                ChangedBy = request.ChangedBy
            });
        }

        public async Task<(bool Success, string Message)> SetReceptionStaffActiveAsync(
            SetReceptionStaffActiveRequest request)
        {
            var entity = await _repository.GetActiveReceptionStaffEntityByIdAsync(request.ReceptionStaffID);
            if (entity == null)
            {
                return (false, "Reception staff not found or has been deleted.");
            }

            var effectivelyActive = entity.IsActive && !entity.DeleteStatus;
            if (effectivelyActive == request.IsActive)
            {
                return (true, request.IsActive
                    ? "Reception staff is already enabled."
                    : "Reception staff is already disabled.");
            }

            entity.IsActive = request.IsActive;
            // Clear soft-delete so disabled staff stay on the list and can log in again when enabled.
            entity.DeleteStatus = false;
            entity.ChangedBy = request.ChangedBy;
            entity.ChangedDate = DateTime.Now;

            _repository.UpdateReceptionStaff(entity);

            if (!await _repository.SaveAllAsync())
            {
                return (false, request.IsActive
                    ? "Failed to enable reception staff."
                    : "Failed to disable reception staff.");
            }

            _logger.LogInformation(
                "Reception staff {Action}. ReceptionStaffID={ReceptionStaffId}",
                request.IsActive ? "enabled" : "disabled",
                entity.ReceptionStaffId);

            return (true, request.IsActive
                ? "Reception staff enabled successfully."
                : "Reception staff disabled successfully. They can no longer log in.");
        }

        public Task<ReceptionStaffResponseModel?> GetReceptionStaffByIdAsync(int receptionStaffId)
        {
            return _repository.GetReceptionStaffByIdAsync(receptionStaffId);
        }

        public async Task<(bool Success, string Message, PaginatedResult<ReceptionStaffListItemModel>? Result)> GetReceptionStaffListAsync(
            GetReceptionStaffListRequest request)
        {
            var doctor = await _repository.GetActiveDoctorByUserIdAsync(request.DoctorUserID);
            if (doctor == null)
            {
                return (false, "Doctor not found or has been deleted.", null);
            }

            var pagination = new PaginationRequestModel
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = "EnteredDate",
                SortDirection = "desc"
            };

            var result = await _repository.GetReceptionStaffListByDoctorIdAsync(doctor.DoctorId, pagination);
            return (true, result.TotalRecords == 0
                ? "No reception staff records found."
                : "Reception staff list retrieved successfully.", result);
        }

        public async Task<AuthModel?> TryBuildAuthModelForLoginAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var entity = await _repository.GetActiveReceptionStaffByUserIdAsync(userName.Trim());
            if (entity == null || !ReceptionStaffPasswordHelper.VerifyPassword(password, entity.Password))
            {
                return null;
            }

            if (ReceptionStaffPasswordHelper.NeedsRehash(entity.Password))
            {
                entity.Password = ReceptionStaffPasswordHelper.HashPassword(password);
                _repository.UpdateReceptionStaff(entity);
                if (!await _repository.SaveAllAsync())
                {
                    _logger.LogWarning(
                        "Reception staff password upgrade to PBKDF2 failed. ReceptionStaffID={ReceptionStaffId}",
                        entity.ReceptionStaffId);
                }
            }

            var doctor = await _repository.GetActiveDoctorByDoctorIdAsync(entity.DoctorId);
            var token = await _tokenService.CreateReceptionStaffToken(
                entity.ReceptionStaffId,
                entity.UserId,
                entity.DoctorId,
                entity.FullName,
                7 * 24 * 60,
                roleId: null,
                roleName: "Reception",
                doctorUserId: doctor?.UserId);

            _logger.LogInformation(
                "Reception staff login successful. ReceptionStaffID={ReceptionStaffId}",
                entity.ReceptionStaffId);

            return new AuthModel
            {
                UserId = entity.ReceptionStaffId,
                UserName = entity.FullName,
                Role = "Reception",
                RoleId = null,
                FirmIds = string.Empty,
                IsSuperUser = false,
                DoctorId = entity.DoctorId,
                ReceptionStaffId = entity.ReceptionStaffId,
                Token = token,
                IsPlanActive = false,
                IslastFiveDays = false,
                DaysRemaining = 0
            };
        }

        private static string? ValidateAddRequest(AddReceptionStaffRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserID))
            {
                return "UserID is required.";
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return "Password is required.";
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return "FullName is required.";
            }

            if (string.IsNullOrWhiteSpace(request.ContactNumber))
            {
                return "ContactNumber is required.";
            }

            return null;
        }
    }
}

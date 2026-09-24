using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories
{
    public class ReceptionStaffService : IReceptionStaffService
    {
        private readonly IReceptionStaffRepository _repository;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<ReceptionStaffService> _logger;

        public ReceptionStaffService(
            IReceptionStaffRepository repository,
            ITokenService tokenService,
            IMapper mapper,
            ILogger<ReceptionStaffService> logger)
        {
            _repository = repository;
            _tokenService = tokenService;
            _mapper = mapper;
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

            var entity = _mapper.Map<DoctorReceptionStaff>(request);
            entity.DoctorId = doctor.DoctorId;
            entity.UserId = request.UserID.Trim();
            entity.Password = EncodePassword(request.Password);
            entity.FullName = request.FullName.Trim();
            entity.ContactNumber = request.ContactNumber.Trim();
            entity.EmailId = string.IsNullOrWhiteSpace(request.EmailId) ? null : request.EmailId.Trim();
            entity.EnteredDate = DateTime.Now;
            entity.DeleteStatus = false;

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
            var entity = await _repository.GetActiveReceptionStaffEntityByIdAsync(request.ReceptionStaffID);
            if (entity == null)
            {
                return (false, "Reception staff not found or has been deleted.");
            }

            entity.DeleteStatus = true;
            entity.IsActive = false;
            entity.ChangedBy = request.ChangedBy;
            entity.ChangedDate = DateTime.Now;

            _repository.UpdateReceptionStaff(entity);

            if (!await _repository.SaveAllAsync())
            {
                return (false, "Failed to delete reception staff.");
            }

            _logger.LogInformation(
                "Reception staff soft deleted. ReceptionStaffID={ReceptionStaffId}",
                entity.ReceptionStaffId);

            return (true, "Reception staff deleted successfully.");
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

        private static string EncodePassword(string password)
        {
            return CommonMethods.Encoding(password);
        }

    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
{
    public class ReceptionStaffRepository : IReceptionStaffRepository
    {
        private readonly NIGACentrumContext _context;

        public ReceptionStaffRepository(NIGACentrumContext context)
        {
            _context = context;
        }

        public Task<Doctor?> GetActiveDoctorByUserIdAsync(int doctorUserId)
        {
            return _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == doctorUserId && !d.DeleteStatus);
        }

        public Task<Doctor?> GetActiveDoctorByDoctorIdAsync(int doctorId)
        {
            return _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.DeleteStatus);
        }

        public Task<DoctorReceptionStaff?> GetActiveReceptionStaffEntityByIdAsync(int receptionStaffId)
        {
            return _context.DoctorReceptionStaffs
                .FirstOrDefaultAsync(x => x.ReceptionStaffId == receptionStaffId && !x.DeleteStatus);
        }

        public async Task<ReceptionStaffResponseModel?> GetReceptionStaffByIdAsync(int receptionStaffId)
        {
            return await (
                from rs in _context.DoctorReceptionStaffs.AsNoTracking()
                join d in _context.Doctors.AsNoTracking() on rs.DoctorId equals d.DoctorId
                where rs.ReceptionStaffId == receptionStaffId && !rs.DeleteStatus
                select new ReceptionStaffResponseModel
                {
                    ReceptionStaffID = rs.ReceptionStaffId,
                    DoctorID = rs.DoctorId,
                    DoctorName = BuildDoctorName(d.FirstName, d.MiddleName, d.LastName),
                    UserID = rs.UserId,
                    FullName = rs.FullName,
                    Address = rs.Address,
                    ContactNumber = rs.ContactNumber,
                    EmailId = rs.EmailId,
                    Country = rs.Country,
                    State = rs.State,
                    City = rs.City,
                    EnteredBy = rs.EnteredBy,
                    EnteredDate = rs.EnteredDate,
                    ChangedBy = rs.ChangedBy,
                    ChangedDate = rs.ChangedDate
                }).FirstOrDefaultAsync();
        }

        public async Task<PaginatedResult<ReceptionStaffListItemModel>> GetReceptionStaffListByDoctorIdAsync(
            int doctorId,
            PaginationRequestModel request)
        {
            var query = _context.DoctorReceptionStaffs
                .AsNoTracking()
                .Where(x => x.DoctorId == doctorId && !x.DeleteStatus)
                .OrderByDescending(x => x.EnteredDate)
                .Select(x => new ReceptionStaffListItemModel
                {
                    ReceptionStaffID = x.ReceptionStaffId,
                    DoctorID = x.DoctorId,
                    UserID = x.UserId,
                    FullName = x.FullName,
                    ContactNumber = x.ContactNumber,
                    EmailId = x.EmailId,
                    City = x.City,
                    EnteredDate = x.EnteredDate
                });

            return await query.ToPaginatedResultAsync(request);
        }

        public Task<bool> IsUserIdExistsAsync(string userId, int? excludeReceptionStaffId = null)
        {
            var normalizedUserId = userId.Trim().ToLower();
            return _context.DoctorReceptionStaffs.AnyAsync(x =>
                !x.DeleteStatus
                && x.UserId.ToLower() == normalizedUserId
                && (excludeReceptionStaffId == null || x.ReceptionStaffId != excludeReceptionStaffId));
        }

        public Task<bool> IsEmailExistsAsync(string emailId, int? excludeReceptionStaffId = null)
        {
            if (string.IsNullOrWhiteSpace(emailId))
            {
                return Task.FromResult(false);
            }

            var normalizedEmail = emailId.Trim().ToLower();
            return _context.DoctorReceptionStaffs.AnyAsync(x =>
                !x.DeleteStatus
                && x.EmailId != null
                && x.EmailId.ToLower() == normalizedEmail
                && (excludeReceptionStaffId == null || x.ReceptionStaffId != excludeReceptionStaffId));
        }

        public Task<bool> IsContactNumberExistsAsync(string contactNumber, int? excludeReceptionStaffId = null)
        {
            var normalizedContact = contactNumber.Trim();
            return _context.DoctorReceptionStaffs.AnyAsync(x =>
                !x.DeleteStatus
                && x.ContactNumber == normalizedContact
                && (excludeReceptionStaffId == null || x.ReceptionStaffId != excludeReceptionStaffId));
        }

        public Task<DoctorReceptionStaff?> GetActiveReceptionStaffByUserIdAsync(string userId)
        {
            var normalizedUserId = userId.Trim().ToLower();
            return _context.DoctorReceptionStaffs
                .FirstOrDefaultAsync(x =>
                    !x.DeleteStatus
                    && x.IsActive
                    && x.UserId.ToLower() == normalizedUserId);
        }

        public void AddReceptionStaff(DoctorReceptionStaff receptionStaff)
        {
            _context.DoctorReceptionStaffs.Add(receptionStaff);
        }

        public void UpdateReceptionStaff(DoctorReceptionStaff receptionStaff)
        {
            _context.Entry(receptionStaff).State = EntityState.Modified;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        private static string BuildDoctorName(string firstName, string? middleName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(middleName))
            {
                return $"{firstName} {lastName}".Trim();
            }

            return $"{firstName} {middleName} {lastName}".Trim();
        }
    }
}

using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    public interface IReceptionStaffRepository
    {
        Task<Doctor?> GetActiveDoctorByUserIdAsync(int doctorUserId);

        Task<Doctor?> GetActiveDoctorByDoctorIdAsync(int doctorId);

        Task<DoctorReceptionStaff?> GetActiveReceptionStaffEntityByIdAsync(int receptionStaffId);

        Task<ReceptionStaffResponseModel?> GetReceptionStaffByIdAsync(int receptionStaffId);

        Task<PaginatedResult<ReceptionStaffListItemModel>> GetReceptionStaffListByDoctorIdAsync(
            int doctorId,
            PaginationRequestModel request);

        Task<bool> IsUserIdExistsAsync(string userId, int? excludeReceptionStaffId = null);

        Task<bool> IsEmailExistsAsync(string emailId, int? excludeReceptionStaffId = null);

        Task<bool> IsContactNumberExistsAsync(string contactNumber, int? excludeReceptionStaffId = null);

        Task<DoctorReceptionStaff?> GetActiveReceptionStaffByUserIdAsync(string userId);

        void AddReceptionStaff(DoctorReceptionStaff receptionStaff);

        void UpdateReceptionStaff(DoctorReceptionStaff receptionStaff);

        Task<bool> SaveAllAsync();
    }
}

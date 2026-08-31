using System.Threading.Tasks;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces
{
    public interface IReceptionStaffRepository
    {
        Task<Doctor?> GetActiveDoctorByUserIdAsync(int doctorUserId);

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

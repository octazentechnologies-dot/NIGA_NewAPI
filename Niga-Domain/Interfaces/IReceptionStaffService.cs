using System.Threading.Tasks;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces
{
    public interface IReceptionStaffService
    {
        Task<(bool Success, string Message, AddReceptionStaffResultModel? Result)> AddReceptionStaffAsync(
            AddReceptionStaffRequest request);

        Task<(bool Success, string Message)> UpdateReceptionStaffAsync(UpdateReceptionStaffRequest request);

        Task<(bool Success, string Message)> DeleteReceptionStaffAsync(DeleteReceptionStaffRequest request);

        Task<ReceptionStaffResponseModel?> GetReceptionStaffByIdAsync(int receptionStaffId);

        Task<(bool Success, string Message, PaginatedResult<ReceptionStaffListItemModel>? Result)> GetReceptionStaffListAsync(
            GetReceptionStaffListRequest request);

        Task<AuthModel?> TryBuildAuthModelForLoginAsync(string userName, string password);
    }
}

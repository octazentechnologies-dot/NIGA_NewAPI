using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
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

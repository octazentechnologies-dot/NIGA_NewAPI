using API.Entities;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateToken(UserMaster user, int expiryMin = 0);

        Task<string> CreateReceptionStaffToken(
            int receptionStaffId,
            string userId,
            int doctorId,
            string fullName,
            int expiryMin = 0);
    }
}

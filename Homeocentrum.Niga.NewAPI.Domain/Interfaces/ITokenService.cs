using API.Entities;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    public interface ITokenService
    {
        /// <param name="roleName">RoleMaster.RoleName — embedded in JWT for AdminPortal ACL (M02 W0).</param>
        /// <param name="doctorId">When set, embeds DoctorID claim (SEC-01.02).</param>
        Task<string> CreateToken(UserMaster user, int expiryMin = 0, string roleName = null, int? doctorId = null);

        /// <summary>
        /// Reception staff JWT — Role "Reception" (+ RoleId when known), DoctorID retained (align Old-API).
        /// </summary>
        Task<string> CreateReceptionStaffToken(
            int receptionStaffId,
            string userId,
            int doctorId,
            string fullName,
            int expiryMin = 0,
            int? roleId = null,
            string roleName = "Reception",
            int? doctorUserId = null);
    }
}

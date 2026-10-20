using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Interfaces
{
    /// <summary>
    /// Patient-role UserMaster + PatientUserMap provisioning and mobile uniqueness.
    /// Shared by clinic onboard (POST /api/patient) and PatientAuth OTP login.
    /// </summary>
    public interface IPatientAccountService
    {
        Task<int?> GetPatientRoleIdAsync(CancellationToken ct = default);

        /// <summary>Active Patient-role UserMaster for this mobile, if any.</summary>
        Task<UserMaster?> FindPatientLoginByMobileAsync(string digits, CancellationToken ct = default);

        /// <summary>Non-deleted clinical Patient for this mobile (oldest id first).</summary>
        Task<(int PatientId, string? PatientName, string? Email)?> FindPatientByMobileAsync(
            string digits,
            CancellationToken ct = default);

        /// <summary>
        /// True when a Patient-role UserMaster already owns this mobile
        /// (optionally ignoring the login linked to excludePatientId).
        /// </summary>
        Task<bool> IsPatientMobileTakenInUserMasterAsync(
            string digits,
            int? excludePatientId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Create Patient-role UserMaster + primary PatientUserMap when missing.
        /// Returns the user id (existing or new).
        /// </summary>
        Task<long?> EnsurePatientLoginAsync(PatientModel model, CancellationToken ct = default);

        /// <summary>
        /// After OTP: resolve or create login for the Patient chart on this mobile.
        /// </summary>
        Task<(UserMaster User, int PatientId, string? PatientName, string? Email)?> ResolveOrClaimAfterOtpAsync(
            string digits,
            CancellationToken ct = default);

        /// <summary>Registered for OTP UX: Patient-role login exists, or a claimable Patient chart exists.</summary>
        Task<bool> IsRegisteredOrClaimableAsync(string digits, CancellationToken ct = default);
    }
}

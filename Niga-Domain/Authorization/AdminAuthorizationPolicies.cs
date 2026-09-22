using System.Security.Claims;

namespace Niga_Domain.Authorization
{
    /// <summary>
    /// M02 W0 foundation: shared Admin Portal authorization for clinical masters mutate APIs.
    /// Apply with [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)] on mutate endpoints (W1+).
    /// </summary>
    public static class AdminAuthorizationPolicies
    {
        public const string AdminPortal = "AdminPortal";

        /// <summary>SEC-04.01 — money APIs (M08 + OTP audit). Account role only.</summary>
        public const string AccountPortal = "AccountPortal";

        /// <summary>SEC-07.03 — OTP audit for Account and Admin (not Doctor/Patient).</summary>
        public const string AccountOrAdmin = "AccountOrAdmin";

        /// <summary>RoleId 1 = SuperUser / Admin in HomeoCentrum RoleMaster.</summary>
        public const int SuperUserRoleId = 1;

        public static readonly string[] AdminPortalRoleNames =
        {
            "Admin",
            "Management"
        };

        public static readonly string[] AccountPortalRoleNames =
        {
            "Account"
        };

        public static readonly string[] PharmacyPartnerRoleNames =
        {
            "PharmacyPartner"
        };

        public static bool IsAdminPortalUser(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            if (IsDevPrivilegedDoctor(user))
                return true;

            var roleIdValue = user.FindFirst("RoleId")?.Value
                ?? user.FindFirst("roleId")?.Value;
            if (int.TryParse(roleIdValue, out var roleId) && roleId == SuperUserRoleId)
                return true;

            foreach (var claim in user.FindAll(ClaimTypes.Role))
            {
                if (IsAdminPortalRoleName(claim.Value))
                    return true;
            }

            var roleName = user.FindFirst("RoleName")?.Value
                ?? user.FindFirst("role")?.Value;
            return IsAdminPortalRoleName(roleName);
        }

        /// <summary>
        /// Dev-only: Tufan_Doctor (UserId 10032) gets Admin-portal rights other Doctor
        /// logins do not have. Do not copy this to production doctors.
        /// </summary>
        public const int TufanDoctorUserId = 10032;

        public static bool IsDevPrivilegedDoctor(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var roleName = GetRoleName(user);
            if (!string.Equals(roleName, "Doctor", StringComparison.OrdinalIgnoreCase))
                return false;

            foreach (var claim in user.Claims)
            {
                if (claim.Type != ClaimTypes.NameIdentifier
                    && claim.Type != "nameid"
                    && claim.Type != "UserId"
                    && claim.Type != "userId"
                    && claim.Type != "sub")
                    continue;
                if (int.TryParse(claim.Value, out var id) && id == TufanDoctorUserId)
                    return true;
            }

            var name = user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("unique_name")?.Value
                ?? user.FindFirst("UserName")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                return false;
            return name.Equals("Tufan_Doctor", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Tufan Doctor", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAdminPortalRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            foreach (var allowed in AdminPortalRoleNames)
            {
                if (string.Equals(allowed, roleName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsAccountPortalUser(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            foreach (var claim in user.FindAll(ClaimTypes.Role))
            {
                if (IsAccountPortalRoleName(claim.Value))
                    return true;
            }

            return IsAccountPortalRoleName(GetRoleName(user));
        }

        public static bool IsAccountPortalRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            foreach (var allowed in AccountPortalRoleNames)
            {
                if (string.Equals(allowed, roleName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static int? GetRoleId(ClaimsPrincipal? user)
        {
            var roleIdValue = user?.FindFirst("RoleId")?.Value
                ?? user?.FindFirst("roleId")?.Value;
            return int.TryParse(roleIdValue, out var roleId) ? roleId : null;
        }

        public static string? GetRoleName(ClaimsPrincipal? user)
        {
            return user?.FindFirst("RoleName")?.Value
                ?? user?.FindFirst(ClaimTypes.Role)?.Value
                ?? user?.FindFirst("role")?.Value;
        }

        public static bool IsPharmacyPartnerUser(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            foreach (var claim in user.FindAll(ClaimTypes.Role))
            {
                if (IsPharmacyPartnerRoleName(claim.Value))
                    return true;
            }

            return IsPharmacyPartnerRoleName(GetRoleName(user));
        }

        public static bool IsPharmacyPartnerRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            foreach (var allowed in PharmacyPartnerRoleNames)
            {
                if (string.Equals(allowed, roleName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// SEC-04.03 — Account/Pharmacy money roles cannot use patient PII APIs.
        /// Admin portal still can (support).
        /// </summary>
        public static bool IsMoneyOnlyRole(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;
            if (IsAdminPortalUser(user))
                return false;
            return IsAccountPortalUser(user) || IsPharmacyPartnerUser(user);
        }

        public static bool IsAccountOrAdminUser(ClaimsPrincipal? user)
            => IsAccountPortalUser(user) || IsAdminPortalUser(user);
    }
}

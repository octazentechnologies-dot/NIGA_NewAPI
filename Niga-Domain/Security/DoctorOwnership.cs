using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Authorization;
using Niga_Domain.Extensions;

namespace Niga_Domain.Security
{
    /// <summary>
    /// SEC-05.01 — Doctor ownership helpers for patient / appointment / board / clinical resources.
    /// Prefer JWT DoctorID claim; AdminPortal users may bypass ownership checks.
    /// </summary>
    public static class DoctorOwnership
    {
        public const string DoctorIdClaim = "DoctorID";

        public static int? GetDoctorId(ClaimsPrincipal? user)
        {
            if (user == null)
                return null;

            var value = user.FindFirst(DoctorIdClaim)?.Value
                ?? user.FindFirst("DoctorId")?.Value
                ?? user.FindFirst("doctorId")?.Value;

            return int.TryParse(value, out var doctorId) && doctorId > 0 ? doctorId : null;
        }

        public static bool IsAdminPortalUser(ClaimsPrincipal? user)
            => AdminAuthorizationPolicies.IsAdminPortalUser(user);

        public static int? GetDoctorUserId(ClaimsPrincipal? user)
        {
            var value = user?.FindFirst("DoctorUserID")?.Value
                ?? user?.FindFirst("DoctorUserId")?.Value;
            return int.TryParse(value, out var doctorUserId) && doctorUserId > 0 ? doctorUserId : null;
        }

        public static string? GetRoleName(ClaimsPrincipal? user)
            => user?.FindFirst(ClaimTypes.Role)?.Value
               ?? user?.FindFirst("RoleName")?.Value;

        /// <summary>
        /// Returns true when the caller may access a resource owned by <paramref name="resourceDoctorId"/>.
        /// AdminPortal always allowed. Otherwise JWT DoctorID must match.
        /// </summary>
        public static bool EnsureDoctorOwns(ClaimsPrincipal? user, int resourceDoctorId)
        {
            if (IsAdminPortalUser(user))
                return true;

            var jwtDoctorId = GetDoctorId(user);
            return jwtDoctorId.HasValue && jwtDoctorId.Value == resourceDoctorId;
        }

        public static bool EnsureDoctorOwns(ClaimsPrincipal? user, int? resourceDoctorId)
        {
            if (!resourceDoctorId.HasValue || resourceDoctorId.Value <= 0)
                return IsAdminPortalUser(user);

            return EnsureDoctorOwns(user, resourceDoctorId.Value);
        }

        /// <summary>
        /// Forbid result when ownership fails; null when allowed.
        /// </summary>
        public static IActionResult? ForbidIfNotOwner(ClaimsPrincipal? user, int? resourceDoctorId)
        {
            if (EnsureDoctorOwns(user, resourceDoctorId))
                return null;

            return new ObjectResult(new { success = false, message = "Access denied for this doctor resource." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        /// <summary>
        /// CLN-02.02 — Only the treating doctor (or AdminPortal) may run case-taking.
        /// Reception and Patient JWTs are 403 even when DoctorID is present (reception staff).
        /// </summary>
        public static IActionResult? ForbidIfReception(ClaimsPrincipal? user)
            => ForbidIfNotTreatingDoctor(user);

        public static IActionResult? ForbidIfNotTreatingDoctor(ClaimsPrincipal? user)
        {
            if (IsAdminPortalUser(user))
                return null;

            var role = GetRoleName(user);
            if (!string.IsNullOrWhiteSpace(role)
                && (role.Equals("Reception", StringComparison.OrdinalIgnoreCase)
                    || role.Equals("Patient", StringComparison.OrdinalIgnoreCase)))
            {
                return new ObjectResult(new { success = false, message = "Only the treating doctor can run case taking." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (!string.IsNullOrWhiteSpace(role)
                && role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
                return null;

            if (GetDoctorId(user).HasValue)
                return null;

            return new ObjectResult(new { success = false, message = "Only the treating doctor can run case taking." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        /// <summary>
        /// JWT user id or reception DoctorUserID must match the doctor UserMaster id. AdminPortal bypass.
        /// </summary>
        public static bool EnsureCallerIsUserOrAdmin(ClaimsPrincipal? user, long targetUserId)
        {
            if (IsAdminPortalUser(user))
                return true;

            try
            {
                if (user != null && user.GetUserId() == (int)targetUserId)
                    return true;
            }
            catch
            {
                // reception NameIdentifier is staff id
            }

            var doctorUserId = GetDoctorUserId(user);
            return doctorUserId.HasValue && doctorUserId.Value == (int)targetUserId;
        }
    }
}

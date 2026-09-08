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
        /// For JWT-bound endpoints that key by user id: reception/doctor must match GetUserId unless AdminPortal.
        /// </summary>
        public static bool EnsureCallerIsUserOrAdmin(ClaimsPrincipal? user, long targetUserId)
        {
            if (IsAdminPortalUser(user))
                return true;

            try
            {
                return user != null && user.GetUserId() == (int)targetUserId;
            }
            catch
            {
                return false;
            }
        }
    }
}

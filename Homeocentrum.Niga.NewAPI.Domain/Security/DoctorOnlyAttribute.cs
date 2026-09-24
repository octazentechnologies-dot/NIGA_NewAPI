using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Homeocentrum.Niga.NewAPI.Domain.Security
{
    /// <summary>
    /// CLN-02.02 / CLN-19.02 — Reception and Patient JWT cannot run case-taking APIs.
    /// Implemented as an authorization filter so 403 is returned before model validation
    /// (otherwise a Reception token with a missing BackupPayload would get 400 instead of 403).
    /// </summary>
    public sealed class DoctorOnlyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Unauthenticated callers must keep 401 from [Authorize], not 403 from this filter.
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
                return;

            var deny = DoctorOwnership.ForbidIfReception(context.HttpContext.User);
            if (deny is ObjectResult obj)
                context.Result = obj;
        }
    }
}

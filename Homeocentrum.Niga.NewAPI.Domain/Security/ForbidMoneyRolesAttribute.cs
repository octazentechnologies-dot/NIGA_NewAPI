using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;

namespace Homeocentrum.Niga.NewAPI.Domain.Security
{
    /// <summary>
    /// SEC-04.01 / SEC-04.03 — Account and PharmacyPartner cannot call patient PII APIs.
    /// </summary>
    public sealed class ForbidMoneyRolesAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!AdminAuthorizationPolicies.IsMoneyOnlyRole(context.HttpContext.User))
                return;

            context.Result = new ObjectResult(new
            {
                success = false,
                message = "Account and Pharmacy roles cannot access patient personal data."
            })
            {
                StatusCode = 403
            };
        }
    }
}

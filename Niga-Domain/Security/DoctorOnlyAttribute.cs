using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Niga_Domain.Security
{
    /// <summary>CLN-02.02 — Reception JWT cannot run case-taking / clinical mutate APIs.</summary>
    public sealed class DoctorOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var deny = DoctorOwnership.ForbidIfReception(context.HttpContext.User);
            if (deny is ObjectResult obj)
                context.Result = obj;
        }
    }
}

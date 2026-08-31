using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        //public string UserId => throw new NotImplementedException();

        public int getUserId() {

            if (_httpContextAccessor.HttpContext != null)
                {
                    //var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (claimValue != null)
                    {
                        return Convert.ToInt32(claimValue);
                    }
                    return 0;
                }
                else
                {
                    return 0;
                }
            //return 1;
            //return _httpContextAccessor.HttpContext.User.GetUserId();
        }
        public int getCompanyId() {
            if (_httpContextAccessor.HttpContext != null)
                {
                    //var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var claimValue = _httpContextAccessor.HttpContext?.User?.Claims?.SingleOrDefault(x => x.Type == "CompanyID")?.Value;
                    if (claimValue != null)
                    {
                        return Convert.ToInt32(claimValue);
                    }
                    return 0;
                }
                else
                {
                    return 0;
                }
            //return _httpContextAccessor.HttpContext.User.GetCompanyId();
        }
    }
    public class SessionTimeout : ActionFilterAttribute
     {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Session == null ||
                             !context.HttpContext.Session.TryGetValue("token", out byte[] val))
            {
                context.Result =
                    new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Account",
                                                                             action = "login" }));
            }
            base.OnActionExecuting(context);
        }
     }
}

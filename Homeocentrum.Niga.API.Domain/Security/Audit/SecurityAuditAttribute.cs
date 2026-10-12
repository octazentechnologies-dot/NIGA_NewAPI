using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Homeocentrum.Niga.API.Domain.Security.Audit;

/// <summary>Writes one security audit row per call: SUCCESS for 2xx, DENIED for 401/403, FAILURE otherwise.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SecurityAuditAttribute : TypeFilterAttribute
{
    public SecurityAuditAttribute(string eventType) : base(typeof(SecurityAuditFilter))
    {
        Arguments = new object[] { eventType };
    }
}

public sealed class SecurityAuditFilter : IAsyncActionFilter
{
    private static readonly string[] SubjectProperties =
        { "UserName", "Username", "LoginId", "Email", "MobileNo", "MobileNumber", "PhoneNumber", "Mobile", "Phone" };
    private static readonly string[] SubjectArguments = { "patientId", "caseId", "id", "doctorId", "sectionId" };

    private readonly string _eventType;
    private readonly ISecurityAuditLog _audit;

    public SecurityAuditFilter(string eventType, ISecurityAuditLog audit)
    {
        _eventType = eventType;
        _audit = audit;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var subject = Subject(context.ActionArguments);
        var executed = await next();

        var status = executed.Exception != null && !executed.ExceptionHandled
            ? 500
            : executed.Result switch
            {
                ObjectResult o => o.StatusCode ?? 200,
                IStatusCodeActionResult s => s.StatusCode ?? 200,
                _ => context.HttpContext.Response.StatusCode
            };
        var outcome = status is >= 200 and < 300 ? "SUCCESS" : status is 401 or 403 ? "DENIED" : "FAILURE";
        var query = context.HttpContext.Request.QueryString.HasValue ? context.HttpContext.Request.QueryString.Value : "";
        await _audit.WriteAsync(_eventType, context.HttpContext, outcome, subject: subject,
            detail: $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}{query} -> {status}");
    }

    private static string? Subject(IDictionary<string, object?> arguments)
    {
        foreach (var name in SubjectArguments)
        {
            if (arguments.TryGetValue(name, out var value) && value != null)
                return $"{name}={value}";
        }
        foreach (var value in arguments.Values)
        {
            if (value == null || value is string || value.GetType().IsPrimitive) continue;
            foreach (var property in SubjectProperties)
            {
                var info = value.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (info?.GetValue(value) is string text && !string.IsNullOrWhiteSpace(text))
                    return $"{property}={text.Trim()}";
            }
        }
        return null;
    }
}

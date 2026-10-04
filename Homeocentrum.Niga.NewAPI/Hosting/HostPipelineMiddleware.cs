using Homeocentrum.Niga.NewAPI.Domain.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Hosting;

/// <summary>
/// Security headers, cookie CSRF, a request trace id, and one JSON shape for unhandled exceptions.
/// </summary>
public sealed class HostPipelineMiddleware
{
    private readonly RequestDelegate _next;

    public HostPipelineMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IConfiguration configuration)
    {
        var path = context.Request.Path.Value ?? "";
        var swagger = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["X-Trace-Id"] = context.TraceIdentifier;
            if (!swagger)
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers.Remove("Server");
            return Task.CompletedTask;
        });

        var bearer = context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        var hasCookie = context.Request.Headers.ContainsKey("Cookie");
        var cookieCsrfEnabled = configuration.GetValue("CookieCsrf:Enabled", true);
        if (HostSecurity.NeedsCsrfCheck(context.Request.Method, bearer, hasCookie, cookieCsrfEnabled))
        {
            await ApiProblem.WriteAsync(context, StatusCodes.Status403Forbidden,
                "This browser call needs a Bearer token. Cookie-only changes are refused.");
            return;
        }

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppFileLog.Write("errors", "ERROR", "Unhandled", ex.Message, ex, new Dictionary<string, string>
            {
                ["TraceId"] = context.TraceIdentifier,
                ["Path"] = path,
                ["Method"] = context.Request.Method
            });
            await ApiProblem.WriteAsync(context, StatusCodes.Status500InternalServerError,
                "The request could not be completed.");
        }
        finally
        {
            if (!HostSecurity.IsOpenPath(path))
                ApiTraffic.Hit(context.Response.StatusCode);
        }
    }
}

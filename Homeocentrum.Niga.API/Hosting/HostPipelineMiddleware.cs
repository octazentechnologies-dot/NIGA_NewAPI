using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Errors;
using Homeocentrum.Niga.API.Domain.Logging;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Hosting;

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
        var securityHeaders = FeatureFlags.Current.EnableSecurityHeaders;
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            if (securityHeaders)
            {
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
                if (!swagger)
                    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            }
            headers["X-Trace-Id"] = context.TraceIdentifier;
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
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var body = SafeError.Capture(ex, context, "Unhandled");
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(body,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
        }
        finally
        {
            if (!HostSecurity.IsOpenPath(path))
                ApiTraffic.Hit(context.Response.StatusCode);
        }
    }
}

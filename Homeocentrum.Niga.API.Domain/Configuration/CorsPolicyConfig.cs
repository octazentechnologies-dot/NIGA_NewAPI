using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Homeocentrum.Niga.API.Domain.Configuration.CorsPolicyConfig;

public static class Startup
{
    private const string CorsPolicy = nameof(CorsPolicy);

    /// <summary>
    /// Any browser origin may call the API unless Cors:AllowedOrigins lists specific origins.
    /// Bearer tokens travel in headers, so credentials (cookies) are never allowed cross-origin.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? System.Array.Empty<string>();
        var allowed = new System.Collections.Generic.HashSet<string>(configured, System.StringComparer.OrdinalIgnoreCase);

        return services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, builder =>
            {
                builder.SetIsOriginAllowed(origin => allowed.Count == 0 || allowed.Contains(origin.TrimEnd('/')))
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("X-Trace-Id");
            });
        });
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
        app.UseCors(CorsPolicy);
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Homeocentrum.Niga.NewAPI.Domain.Configuration.CorsPolicyConfig;

public static class Startup
{
    private const string CorsPolicy = nameof(CorsPolicy);

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IWebHostEnvironment env)
    {
        return services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, builder =>
            {
                // Reflect any origin (localhost:3000, homeocentrum.com, etc.)
                // Do not combine AllowAnyOrigin() with AllowCredentials().
                builder.SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
        app.UseCors(CorsPolicy);
}

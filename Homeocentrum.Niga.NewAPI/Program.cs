
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using API.Entities;
using Homeocentrum.Niga.NewAPI.Domain.Configuration.CorsPolicyConfig;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Microsoft.Extensions.Logging;



var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter<AppFileLoggerProvider>(null, LogLevel.Debug);
builder.Logging.AddProvider(new AppFileLoggerProvider());

ConfigurationManager configuration = builder.Configuration;
IWebHostEnvironment environment = builder.Environment;

// Keep API alive if a background embedding job throws after a long run.
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Development validates DI scopes on build. Several singleton caches still take scoped
// repositories (same as production, where this check is off). Allow local startup.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

// Add services to the container.

builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FlexibleTimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new FlexibleNullableTimeOnlyJsonConverter());
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ApiProblem.Validation;
});
builder.Services.AddHealthChecks();
var rateLimitEnabled = builder.Configuration.GetValue("RateLimit:Enabled", true);
if (rateLimitEnabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (ctx, _) =>
        {
            await ApiProblem.WriteAsync(ctx.HttpContext, 429, "Too many requests. Please wait a moment and try again.");
        };
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var path = httpContext.Request.Path.Value ?? "";
            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                return RateLimitPartition.GetNoLimiter("open");
            }
            var limit = 300;
            var window = 60;
            if (int.TryParse(builder.Configuration["RateLimit:PermitLimit"], out var parsed) && parsed > 0)
                limit = parsed;
            if (int.TryParse(builder.Configuration["RateLimit:WindowSeconds"], out parsed) && parsed > 0)
                window = parsed;
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = httpContext.User?.FindFirst("UserId")?.Value
                ?? httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User?.FindFirst("sub")?.Value;
            var partitionKey = !string.IsNullOrWhiteSpace(userId) && userId != "0"
                ? "u:" + userId
                : "ip:" + ip;
            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromSeconds(window),
                QueueLimit = 0
            });
        });
    });
}
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Homeocentrum New API", Version = "v1" });
  opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "Enter your JWT token **including** 'Bearer ' prefix. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9'",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
});

opt.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            },
            In = ParameterLocation.Header,
        },
        new string[] {}
    }
});

});

builder.Services.AddApplicationServices(configuration) 
    .AddCorsPolicy(builder.Environment);   

var sqlOnly = new ConfigurationBuilder()
    .SetBasePath(environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();
var defaultConnection = sqlOnly.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in appsettings.json.");
builder.Services.AddDbContext<NIGACentrumContext>(options => options.UseSqlServer(defaultConnection));
builder.Services.AddScoped<NIGACentrumContext>();

builder.Services.AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<NIGACentrumContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = false,

        ValidateAudience = false,

        ValidateLifetime = true,

        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["TokenKey"])),
    };
});

// M02 W0 — Admin Portal policy for clinical masters mutate APIs (apply in W1+)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminAuthorizationPolicies.AdminPortal, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx => AdminAuthorizationPolicies.IsAdminPortalUser(ctx.User)));
    options.AddPolicy(AdminAuthorizationPolicies.AccountPortal, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx => AdminAuthorizationPolicies.IsAccountPortalUser(ctx.User)));
    options.AddPolicy(AdminAuthorizationPolicies.AccountOrAdmin, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx => AdminAuthorizationPolicies.IsAccountOrAdminUser(ctx.User)));
});

var app = builder.Build();
AppFileLog.Initialize(app.Environment.ContentRootPath, app.Configuration, "NIGA New-API (Niga-Web :5002)");

app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method)
        && (context.Request.Path == "/" || context.Request.Path == PathString.Empty))
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});

// Same favicon as the SPA website tab (also overrides Swagger UI's default icons).
app.UseHomeocentrumFavicon();
app.UseStaticFiles();

// Swagger is behind SwaggerAuth. The sign-in page fills the docs URL from this browser host.
app.UseMiddleware<SwaggerGateMiddleware>("Homeocentrum New API");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Homeocentrum New API");
    c.DocumentTitle = "Homeocentrum New API";
    c.HeadContent =
        "<link rel=\"icon\" type=\"image/png\" href=\"/favicon.png\" />" +
        "<link rel=\"shortcut icon\" href=\"/favicon.ico\" />" +
        "<script>document.addEventListener('DOMContentLoaded',function(){" +
        "document.querySelectorAll('link[rel*=\"icon\"]').forEach(function(el){el.parentNode.removeChild(el);});" +
        "var l=document.createElement('link');l.rel='icon';l.type='image/png';l.href='/favicon.png?v=hc';document.head.appendChild(l);" +
        "});</script>";
});

app.UseCors(builder => 
builder.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
    );
app.UseResponseCompression();
var listenUrls = app.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
var httpsPort = app.Configuration["HTTPS_PORT"] ?? Environment.GetEnvironmentVariable("HTTPS_PORT");
if (listenUrls.Contains("https://", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(httpsPort))
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<AppDiagnosticsMiddleware>();
if (rateLimitEnabled)
    app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<Homeocentrum.Niga.NewAPI.Domain.Services.MutatingAuditMiddleware>();
app.UseCorsPolicy()
    .UseResponseCaching()
    .UseDefaultFiles()
    .UseStaticFiles();

// SEC-05.02 — do not serve /attachments or /Blogs anonymously.
// Use GET /api/SecureFile/{root}/{*path} (JWT) or signed /api/SecureFile/Download.

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"success\":true,\"status\":\"" + report.Status + "\",\"api\":\"New API\"}");
    }
});
app.MapControllers();




app.Run();

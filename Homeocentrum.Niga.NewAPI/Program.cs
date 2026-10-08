
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
using Microsoft.OpenApi;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using API.Entities;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Configuration.CorsPolicyConfig;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Hosting;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Logging;



var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Nothing goes to the console. Logs are written by AppFileLoggerProvider under Logs/ (redacted).
builder.Logging.ClearProviders();
builder.Logging.AddFilter<AppFileLoggerProvider>(null, LogLevel.Debug);
builder.Logging.AddProvider(new AppFileLoggerProvider());
builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

ConfigurationManager configuration = builder.Configuration;
IWebHostEnvironment environment = builder.Environment;

// This API has one appsettings.json. Do not load appsettings.Development.json or any other environment file.
configuration.Sources.Clear();
configuration.SetBasePath(environment.ContentRootPath);
configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
var featureFlags = FeatureFlags.Load(configuration);

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
if (featureFlags.EnableResponseCompression)
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });
}

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<SafeServerErrorResultFilter>();
        options.Filters.AddService<Homeocentrum.Niga.NewAPI.Domain.Security.Uploads.UploadSecurityFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FlexibleTimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new FlexibleNullableTimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new LenientStringJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new LenientBooleanJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new EmptyStringAsNullJsonConverterFactory());
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ApiProblem.Validation;
});
builder.Services.AddHealthChecks();
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromMinutes(5),
        TimeoutStatusCode = StatusCodes.Status408RequestTimeout
    };
});
var rateLimitEnabled = featureFlags.EnableRateLimiting;
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
            if (HostSecurity.IsOpenPath(path))
            {
                return RateLimitPartition.GetNoLimiter("open");
            }
            var limit = 300;
            var window = 60;
            if (int.TryParse(builder.Configuration["RateLimit:PermitLimit"], out var parsed) && parsed > 0)
                limit = parsed;
            if (int.TryParse(builder.Configuration["RateLimit:WindowSeconds"], out parsed) && parsed > 0)
                window = parsed;
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var userId = httpContext.User?.FindFirst("UserId")?.Value
                ?? httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User?.FindFirst("sub")?.Value;
            var partitionKey = HostSecurity.RatePartition(userId, ip);
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
if (featureFlags.EnableSwagger)
{
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Homeocentrum New API", Version = "v1" });
    opt.OperationFilter<ApiDocOperationFilter>();
  opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "Enter your JWT token **including** 'Bearer ' prefix. Example: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9'",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
});

opt.AddSecurityRequirement(document => new OpenApiSecurityRequirement
{
    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
});

});
}

builder.Services.AddApplicationServices(configuration);
if (featureFlags.EnableCors)
    builder.Services.AddCorsPolicy(builder.Environment, builder.Configuration);
if (!featureFlags.EnableBackgroundJobs)
{
    // Only the app's own jobs; the framework's hosted services (the web server itself) must stay.
    var appJobs = builder.Services
        .Where(d => d.ServiceType == typeof(IHostedService)
            && d.ImplementationType?.Namespace?.StartsWith("Homeocentrum", StringComparison.Ordinal) == true)
        .ToList();
    foreach (var job in appJobs)
        builder.Services.Remove(job);
}

var defaultConnection = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in appsettings.json.");
builder.Services.AddDbContext<NIGACentrumContext>(options =>
{
    options.UseSqlServer(defaultConnection);
    if (featureFlags.EnableSensitiveDataLogging)
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
});
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
    options.TokenValidationParameters = JwtSettings.Validation(configuration);
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = ctx =>
        {
            var jti = ctx.Principal?.FindFirst("jti")?.Value;
            var denylist = ctx.HttpContext.RequestServices
                .GetRequiredService<Homeocentrum.Niga.NewAPI.Domain.Services.IJwtDenylistService>();
            if (denylist.IsDenied(jti))
                ctx.Fail("This session was signed out.");
            return Task.CompletedTask;
        }
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
    options.AddPolicy(AdminAuthorizationPolicies.ClinicStaff, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(ctx => AdminAuthorizationPolicies.IsClinicStaffUser(ctx.User)));
});

var app = builder.Build();
AdminAuthorizationPolicies.DevPrivilegedDoctorEnabled =
    app.Environment.IsDevelopment() && app.Configuration.GetValue("Security:DevPrivilegedDoctor", true);
AppFileLog.Initialize(app.Environment.ContentRootPath, app.Configuration, "NIGA New-API (Niga-Web :5002)");
// Upload folders live under ContentRootPath/Data/UploadedMedia; a fresh publish may not contain them yet.
Homeocentrum.Niga.NewAPI.Domain.Helpers.UploadedMedia.EnsureFolders(
    app.Environment.ContentRootPath,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("UploadedMedia"));
AppFileLog.SendDeployNotice("started");
app.Lifetime.ApplicationStarted.Register(() => AppFileLog.SendDeployNotice("ready"));
app.Lifetime.ApplicationStopping.Register(AppFileLog.SendStopNotice);
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    var ex = e.ExceptionObject as Exception;
    AppFileLog.Write("errors", "CRITICAL", "Process", "Unhandled exception; process terminating=" + e.IsTerminating, ex, sendAlert: false);
    AppFileLog.SendOpsAlert("crash", "API crashed", new Dictionary<string, string>
    {
        ["Exception"] = ex?.GetType().FullName ?? "unknown",
        ["Message"] = ex?.Message ?? "",
        ["Terminating"] = e.IsTerminating.ToString(),
    }, cooldownMinutes: 0, wait: true);
};

// IIS reverse-proxies to Kestrel on this machine. X-Forwarded-For is trusted only from loopback proxies (the default KnownNetworks),
// so rate limits, the failed-login throttle and the audit trail see the real client address.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Outermost catch-all: no stack trace or developer exception page ever reaches a client, in any environment.
app.Use(async (context, next) =>
{
    try
    {
        await next();
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
});

if (featureFlags.EnableCors)
    app.UseCorsPolicy();

if (featureFlags.EnableMaintenanceMode)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next();
            return;
        }
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Retry-After"] = "300";
            return Task.CompletedTask;
        });
        await ApiProblem.WriteAsync(context, StatusCodes.Status503ServiceUnavailable, featureFlags.MaintenanceMessage);
    });
}

if (featureFlags.EnableSwagger)
{
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
}

// Same favicon as the SPA website tab (also overrides Swagger UI's default icons).
app.UseHomeocentrumFavicon();
app.UseStaticFiles();

if (featureFlags.EnableSwagger)
{
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
}

if (featureFlags.EnableResponseCompression)
    app.UseResponseCompression();
app.UseMiddleware<HostPipelineMiddleware>();
var listenUrls = app.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
var httpsPort = app.Configuration["HTTPS_PORT"] ?? Environment.GetEnvironmentVariable("HTTPS_PORT");
if (listenUrls.Contains("https://", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(httpsPort))
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<AppDiagnosticsMiddleware>();
if (rateLimitEnabled)
    app.UseRateLimiter();
app.UseAuthorization();
app.UseRequestTimeouts();
if (featureFlags.EnableAuditLogging)
    app.UseMiddleware<Homeocentrum.Niga.NewAPI.Domain.Services.MutatingAuditMiddleware>();
app.UseResponseCaching()
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


using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Niga_Domain.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Niga_Domain.Data;
using API.Entities;
using Niga_Domain.Configuration.CorsPolicyConfig;
using Niga_Domain.Authorization;



var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "MyAPI", Version = "v1" });
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

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(builder => 
builder.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
    );
app.UseResponseCompression();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Niga_Domain.Services.MutatingAuditMiddleware>();
app.UseCorsPolicy()
    .UseResponseCaching()
    .UseDefaultFiles()
    .UseStaticFiles();

// SEC-05.02 — do not serve /attachments or /Blogs anonymously.
// Use GET /api/SecureFile/{root}/{*path} (JWT) or signed /api/SecureFile/Download.

app.MapControllers();




app.Run();

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>
/// One Swagger document per audience, plus the full v1 document.
/// v1 keeps its URL (/swagger/v1/swagger.json) because tooling reads it.
/// </summary>
public static class ApiAudienceSwagger
{
    public const string All = "v1";
    public const string MobilePatient = "mobile-patient";
    public const string MobileDoctor = "mobile-doctor";
    public const string Common = "common";
    public const string Web = "web";

    private const string LegacyTag = "Legacy routes (deprecated)";

    private static readonly (string Name, string Title, string Description)[] Documents =
    {
        (All, "Homeocentrum API",
            "Every endpoint. Tags start with the audience: Mobile Patient App, Mobile Doctor App, Common, or Web. " +
            "Old URLs of endpoints that moved to /api/MobilePatient or /api/MobileDoctor still work and are listed under \"" + LegacyTag + "\"."),
        (MobilePatient, "Homeocentrum API - Patient Mobile App",
            "Everything the Patient Mobile App calls. /api/MobilePatient/* exists only for the patient app. " +
            "The \"Common\" tags are shared with the web and the doctor app; call them at the path shown."),
        (MobileDoctor, "Homeocentrum API - Doctor Mobile App",
            "Everything the Doctor Mobile App calls. /api/MobileDoctor/* exists only for the doctor app. " +
            "The \"Common\" tags are shared with the web and the patient app; call them at the path shown."),
        (Common, "Homeocentrum API - Common",
            "Endpoints shared by the web and a mobile app, or by both mobile apps. Each description names the clients that use it."),
        (Web, "Homeocentrum API - Web",
            "Web application and back-office endpoints. The mobile apps do not call these."),
    };

    public static void AddDocuments(SwaggerGenOptions opt)
    {
        foreach (var (name, title, description) in Documents)
            opt.SwaggerDoc(name, new OpenApiInfo { Title = title, Version = "v1", Description = description });

        opt.DocInclusionPredicate(Includes);
        opt.TagActionsBy(api => new[] { TagOf(api) });
        opt.OrderActionsBy(api => $"{Rank(api)}_{TagOf(api)}_{api.RelativePath}_{api.HttpMethod}");
        opt.OperationFilter<ApiAudienceOperationFilter>();
    }

    public static void AddEndpoints(SwaggerUIOptions ui)
    {
        foreach (var (name, title, _) in Documents)
            ui.SwaggerEndpoint($"/swagger/{name}/swagger.json", name == All ? "All APIs" : title.Replace("Homeocentrum API - ", ""));
    }

    private static bool Includes(string document, ApiDescription api)
    {
        var legacy = ApiAudienceCatalog.LegacyAliasOf(api) != null;
        if (document == All)
            return true;
        if (legacy)
            return false;

        var audience = ApiAudienceCatalog.Classify(api);
        var clients = ApiAudienceCatalog.ClientsOf(api);
        return document switch
        {
            MobilePatient => audience == ApiAudience.MobilePatient
                || (audience == ApiAudience.Common && clients.HasFlag(ApiClients.PatientApp)),
            MobileDoctor => audience == ApiAudience.MobileDoctor
                || (audience == ApiAudience.Common && clients.HasFlag(ApiClients.DoctorApp)),
            Common => audience == ApiAudience.Common,
            Web => audience == ApiAudience.Web,
            _ => false
        };
    }

    internal static string TagOf(ApiDescription api)
    {
        if (ApiAudienceCatalog.LegacyAliasOf(api) != null)
            return LegacyTag;
        var controller = (api.ActionDescriptor as ControllerActionDescriptor)?.ControllerName ?? "Other";
        return ApiAudienceCatalog.Classify(api) switch
        {
            ApiAudience.MobilePatient => "Mobile Patient App",
            ApiAudience.MobileDoctor => "Mobile Doctor App",
            ApiAudience.Common => "Common - " + controller,
            _ => "Web - " + controller
        };
    }

    private static int Rank(ApiDescription api)
    {
        if (ApiAudienceCatalog.LegacyAliasOf(api) != null)
            return 9;
        return ApiAudienceCatalog.Classify(api) switch
        {
            ApiAudience.MobilePatient => 1,
            ApiAudience.MobileDoctor => 2,
            ApiAudience.Common => 3,
            _ => 4
        };
    }
}

/// <summary>Puts the audience, the client list, and the moved-route notice at the top of each operation description.</summary>
public sealed class ApiAudienceOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var api = context.ApiDescription;
        var metadata = api.ActionDescriptor.EndpointMetadata;

        if (string.IsNullOrWhiteSpace(operation.Summary))
            operation.Summary = metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;

        var audience = ApiAudienceCatalog.Classify(api);
        var lead = audience switch
        {
            ApiAudience.MobilePatient => "Audience: Patient Mobile App only.",
            ApiAudience.MobileDoctor => "Audience: Doctor Mobile App only.",
            ApiAudience.Common => $"Audience: Common. Used by: {ApiAudienceCatalog.Describe(ApiAudienceCatalog.ClientsOf(api))}." +
                (ApiAudienceCatalog.UseOf(api) is { } use ? $" ({use})" : ""),
            _ => "Audience: Web."
        };

        var detail = metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;
        if (!string.IsNullOrWhiteSpace(detail))
            lead += " " + detail.Trim();

        var legacy = ApiAudienceCatalog.LegacyAliasOf(api);
        var current = legacy == null ? "" : CurrentPath(context);
        if (legacy != null)
        {
            operation.Deprecated = true;
            lead = $"Deprecated alias. Use {api.HttpMethod} /{current}. This URL keeps working for existing clients. " + lead;
        }

        var body = (operation.Description ?? "").Trim();
        if (legacy != null)
            body = body.Replace("Deprecation: none.", $"Deprecation: use /{current}.");
        operation.Description = string.IsNullOrEmpty(body) ? lead : lead + " " + body;
    }

    /// <summary>The action's main route: controller [Route] plus its own [HttpXxx] template.</summary>
    private static string CurrentPath(OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var prefix = method?.DeclaringType?
            .GetCustomAttributes(typeof(RouteAttribute), true).OfType<RouteAttribute>().FirstOrDefault()?.Template;
        var own = method?
            .GetCustomAttributes(typeof(HttpMethodAttribute), true).OfType<HttpMethodAttribute>().FirstOrDefault()?.Template;
        var combined = AttributeRouteModel.CombineTemplates(prefix, own) ?? "";
        var controller = (context.ApiDescription.ActionDescriptor as ControllerActionDescriptor)?.ControllerName ?? "";
        return LegacyRouteAttribute.Normalize(combined.Replace("[controller]", controller));
    }
}

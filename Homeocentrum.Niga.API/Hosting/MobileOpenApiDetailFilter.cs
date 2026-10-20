using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>
/// OpenAPI helpers for mobile clinic routes: deployed server list, and a live AppConfig example
/// built from the same configuration the API returns (no hardcoded sample bodies).
/// Other operations rely on controller signatures / XML docs so docs stay in sync when routes change.
/// </summary>
public sealed class MobileOpenApiDetailFilter : IOperationFilter, IDocumentFilter
{
    public const string DeployedBase = "https://devapi.homeocentrum.com";

    private readonly IConfiguration _configuration;

    public MobileOpenApiDetailFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Servers ??= new List<OpenApiServer>();
        if (swaggerDoc.Servers.Count == 0)
        {
            swaggerDoc.Servers.Add(new OpenApiServer
            {
                Url = DeployedBase,
                Description = "Deployed New API. Paths start with /api."
            });
        }
    }

    public static void ApplyServers(OpenApiDocument document, HttpRequest request)
    {
        var host = request.Host.Value ?? "";
        var here = string.IsNullOrEmpty(host) ? DeployedBase : $"{request.Scheme}://{host}";
        var onDeployed = host.Contains("devapi.homeocentrum.com", StringComparison.OrdinalIgnoreCase);
        document.Servers = new List<OpenApiServer>
        {
            new()
            {
                Url = DeployedBase,
                Description = "Deployed New API. Mobile apps and other clients call this host. Paths start with /api."
            }
        };
        if (!onDeployed && !string.Equals(here.TrimEnd('/'), DeployedBase, StringComparison.OrdinalIgnoreCase))
        {
            document.Servers.Add(new OpenApiServer
            {
                Url = here,
                Description = "The host that served this file. Mobile development uses the deployed server above."
            });
        }
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var api = context.ApiDescription;
        var path = (api.RelativePath ?? "").Trim('/');
        if (!IsAppConfig(path))
            return;

        operation.Summary = "Public app settings";
        var body = (operation.Description ?? "").Trim();
        const string notes =
            "No token. Values are read live from configuration: " +
            "siteUrl ← ConfigurationModel:SiteUrl, " +
            "supportPhone / supportWhatsApp / supportEmail ← Support:*. " +
            "Razorpay KeyId and KeySecret are not returned here. " +
            "Same body as GET /api/Public/AppConfig and GET /api/App/Config.";
        if (!body.Contains("Values are read live from configuration", StringComparison.Ordinal))
            operation.Description = notes + (body.Length > 0 ? " " + body : "");

        SetResponseExample(operation, AppConfigPayload.ToJsonExample(_configuration));
    }

    private static bool IsAppConfig(string path) =>
        path.Equals("api/Public/AppConfig", StringComparison.OrdinalIgnoreCase)
        || path.Equals("api/App/Config", StringComparison.OrdinalIgnoreCase);

    private static void SetResponseExample(OpenApiOperation operation, string json)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Success. Example matches the current appsettings (restart the API after config changes).",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Example = JsonNode.Parse(json) }
            }
        };
    }
}

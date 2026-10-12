using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>The padlock only on endpoints that need a token. There is no fallback policy, so [Authorize] decides.</summary>
public sealed class ApiSecurityOperationFilter : IOperationFilter
{
    public const string SchemeName = "Bearer";

    public static bool RequiresToken(ApiDescription api)
    {
        var metadata = api.ActionDescriptor.EndpointMetadata;
        return metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!RequiresToken(context.ApiDescription))
            return;
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = new List<string>()
        });
    }
}

/// <summary>Operations without an XML summary get one from the action name, so every row in the list reads as a sentence.</summary>
public sealed class ApiReadableSummaryOperationFilter : IOperationFilter
{
    private static readonly Regex WordBoundary = new("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.IsNullOrWhiteSpace(operation.Summary))
            return;
        var action = (context.ApiDescription.ActionDescriptor as ControllerActionDescriptor)?.ActionName;
        if (!string.IsNullOrWhiteSpace(action))
            operation.Summary = Humanize(action);
    }

    public static string Humanize(string name)
    {
        if (name.EndsWith("Async", StringComparison.Ordinal) && name.Length > 5)
            name = name[..^5];
        var words = WordBoundary.Split(name.Replace('_', ' ')).Where(w => w.Length > 0).ToList();
        for (var i = 1; i < words.Count; i++)
        {
            if (words[i].Length > 1 && words[i].All(char.IsUpper))
                continue;
            words[i] = words[i].ToLowerInvariant();
        }
        return string.Join(' ', words);
    }
}

/// <summary>
/// Describes each tag with its controller's XML summary, orders tags by audience, and adds a getting-started block.
/// </summary>
public sealed class ApiTagDescriptionsDocumentFilter : IDocumentFilter
{
    private const string LegacyTag = "Legacy routes (deprecated)";

    private const string GettingStarted =
        "\n\n**Getting started**\n\n" +
        "1. Sign in with `POST /api/Account/Login` (staff and doctors) or `POST /api/PatientAuth/VerifyOtp` (patients). " +
        "Swagger keeps the returned token and sends it on later calls, also after a page reload.\n" +
        "2. Or click **Authorize** and paste a token without the `Bearer ` prefix.\n" +
        "3. A padlock marks the endpoints that need the token. The others are public.\n" +
        "4. The filter box searches tag names, paths, and summaries, for example `Tele`, `/api/Patient`, or `refund`.\n" +
        "5. Validation and server errors return `success: false`, a `message`, and a `traceId` to quote when reporting a problem.";

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Summaries = new(LoadSummaries);

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Info != null && !(swaggerDoc.Info.Description ?? "").Contains("Getting started", StringComparison.Ordinal))
            swaggerDoc.Info.Description = (swaggerDoc.Info.Description ?? "").TrimEnd() + GettingStarted;

        var descriptions = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var api in context.ApiDescriptions)
        {
            var tag = ApiAudienceSwagger.TagOf(api);
            if (descriptions.TryGetValue(tag, out var known) && known != null)
                continue;
            descriptions[tag] = tag == LegacyTag
                ? "Old URLs kept so existing clients keep working. Each operation names the URL to use instead."
                : SummaryOf((api.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.FullName);
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in swaggerDoc.Paths.Values)
            foreach (var operation in path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
                foreach (var tag in operation.Tags ?? new HashSet<OpenApiTagReference>())
                    if (tag.Name != null)
                        used.Add(tag.Name);

        var ordered = used.OrderBy(Rank).ThenBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
        var tags = new HashSet<OpenApiTag>();
        foreach (var name in ordered)
            tags.Add(new OpenApiTag { Name = name, Description = descriptions.GetValueOrDefault(name) });
        swaggerDoc.Tags = tags;
    }

    private static int Rank(string tag) => tag switch
    {
        "Mobile Patient App" => 1,
        "Mobile Doctor App" => 2,
        LegacyTag => 9,
        _ when tag.StartsWith("Common - ", StringComparison.Ordinal) => 3,
        _ => 4
    };

    private static string? SummaryOf(string? typeName)
        => typeName != null && Summaries.Value.TryGetValue("T:" + typeName, out var text) ? text : null;

    private static IReadOnlyDictionary<string, string> LoadSummaries()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(AppContext.BaseDirectory, "Homeocentrum.Niga.API*.xml"))
        {
            try
            {
                foreach (var member in XDocument.Load(file).Descendants("member"))
                {
                    var name = member.Attribute("name")?.Value;
                    var summary = member.Element("summary");
                    if (name == null || !name.StartsWith("T:", StringComparison.Ordinal) || summary == null)
                        continue;
                    var text = Regex.Replace(string.Concat(summary.Nodes().Select(n =>
                        n is XElement e ? (e.Attribute("cref")?.Value.Split('.').Last() ?? e.Value) : n.ToString())), @"\s+", " ").Trim();
                    if (text.Length > 0)
                        map[name] = System.Net.WebUtility.HtmlDecode(text);
                }
            }
            catch (System.Xml.XmlException)
            {
            }
        }
        return map;
    }
}

/// <summary>Browser-side additions for Swagger UI.</summary>
public static class SwaggerUiScripts
{
    /// <summary>The filter box matches tag names, paths, methods, and summaries (Swagger UI matches tag names only).</summary>
    public const string SearchPlugin = """
        (function () {
          return {
            fn: {
              opsFilter: function (taggedOps, phrase) {
                var q = (phrase || '').toString().trim().toLowerCase();
                if (!q) return taggedOps;
                return taggedOps
                  .map(function (tagObj, tag) {
                    if (String(tag).toLowerCase().indexOf(q) >= 0) return tagObj;
                    return tagObj.set('operations', tagObj.get('operations').filter(function (op) {
                      var text = (op.get('method') + ' ' + op.get('path') + ' ' +
                        (op.getIn(['operation', 'summary']) || '')).toLowerCase();
                      return text.indexOf(q) >= 0;
                    }));
                  })
                  .filter(function (tagObj) { return tagObj.get('operations').size > 0; });
              }
            }
          };
        })
        """;

    /// <summary>A download button under the title for the document picked in the top bar, ready to import in Postman or Insomnia.</summary>
    public const string SpecDownloadPlugin = """
        (function () {
          return {
            wrapComponents: {
              InfoContainer: function (Original, system) {
                return function (props) {
                  var h = system.React.createElement;
                  var url = system.specSelectors.url() || '';
                  var doc = (url.match(/\/swagger\/([^\/]+)\/swagger\.json/) || [])[1] || 'v1';
                  return h('div', null,
                    h(Original, props),
                    url && h('div', { className: 'wrapper hc-spec-download-row' },
                      h('a', {
                          className: 'hc-spec-download',
                          href: url,
                          download: 'homeocentrum-' + doc + '.openapi.json',
                          title: 'Download this OpenAPI document. Postman: Import > File. Insomnia: Import > From File.'
                        },
                        h('strong', null, '\u2B07 OpenAPI spec'),
                        h('span', null, 'JSON \u00B7 Postman / Insomnia'))));
                };
              }
            }
          };
        })
        """;

    /// <summary>Styles for <see cref="SpecDownloadPlugin"/>.</summary>
    public const string SpecDownloadStyles = """
        <style>
        .swagger-ui .hc-spec-download-row { margin-top: -10px; margin-bottom: 20px; }
        .swagger-ui a.hc-spec-download { display: inline-flex; align-items: baseline; gap: 18px; padding: 10px 18px; border-radius: 8px;
          background: #d9ab2f; color: #1b1b1b; text-decoration: none; font-family: sans-serif; font-size: 14px;
          box-shadow: 0 1px 2px rgba(0, 0, 0, .25); }
        .swagger-ui a.hc-spec-download:hover { background: #e6b93a; color: #000; }
        .swagger-ui a.hc-spec-download strong { font-size: 15px; }
        .swagger-ui a.hc-spec-download span { color: #3d3420; }
        </style>
        """;

    /// <summary>After a successful sign-in call from Swagger, the returned token is used for the following calls.</summary>
    public const string CaptureTokenInterceptor = """
        function (res) {
          try {
            if (res && res.ok && /\/api\/[^?]*(Login|VerifyOtp|RefreshToken)[^\/?]*(\?|$)/i.test(res.url || '')) {
              var body = res.obj || JSON.parse(res.text || res.data || '{}');
              var data = body && (body.data || body.Data);
              var token = (data && (data.token || data.accessToken)) || (body && (body.token || body.accessToken));
              if (typeof token === 'string' && token.length > 20 && window.ui) {
                window.ui.preauthorizeApiKey('Bearer', token);
              }
            }
          } catch (e) { }
          return res;
        }
        """;
}

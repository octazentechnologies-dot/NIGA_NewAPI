using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>
/// Keeps an older URL working after an action moved to another controller.
/// The alias uses the same HTTP method, filters and authorization as the action's main route.
/// Swagger lists it only in the full document, marked deprecated.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class LegacyRouteAttribute : Attribute, IActionModelConvention
{
    private static readonly Regex Constraint = new(@"\{(\w+)[^}]*\}", RegexOptions.Compiled);

    public LegacyRouteAttribute(string template)
    {
        Template = template;
    }

    /// <summary>Absolute template, for example "/api/Welcome/Patient".</summary>
    public string Template { get; }

    /// <summary>The template as ApiExplorer reports it: no leading slash and no route constraints.</summary>
    public string RelativePath => Normalize(Template);

    public static string Normalize(string template)
        => Constraint.Replace(template.TrimStart('~', '/'), "{$1}");

    public void Apply(ActionModel action)
    {
        var primary = action.Selectors.FirstOrDefault(s => s.AttributeRouteModel != null)
            ?? throw new InvalidOperationException(
                $"{action.Controller.ControllerName}.{action.ActionName} needs an attribute route before [LegacyRoute].");

        var alias = new SelectorModel(primary)
        {
            AttributeRouteModel = new AttributeRouteModel { Template = Template }
        };
        action.Selectors.Add(alias);
    }

    public bool Matches(string? relativePath)
        => string.Equals(RelativePath, relativePath?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}

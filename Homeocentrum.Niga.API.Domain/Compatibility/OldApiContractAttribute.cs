using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeocentrum.Niga.API.Domain.Compatibility
{
    /// <summary>
    /// Marks actions that serve the Old API request contract. The Old API (netcoreapp2.2) never treated
    /// non-nullable reference properties as required, so Old consumers routinely post partial models
    /// (for example only the id on delete). This filter runs before the [ApiController] automatic 400 and
    /// drops only those implicit "field is required" errors; explicit [Required] attributes and JSON
    /// conversion errors still produce 400.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class OldApiContractAttribute : Attribute, IActionFilter, IOrderedFilter
    {
        private const string RequiredSuffix = " field is required.";
        private static readonly ConcurrentDictionary<ActionDescriptor, HashSet<string>> ExplicitRequiredCache = new();

        public int Order => -3000;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var modelState = context.ModelState;
            if (modelState.IsValid)
            {
                return;
            }

            var explicitRequired = ExplicitRequiredCache.GetOrAdd(context.ActionDescriptor, BuildExplicitRequired);

            foreach (var key in modelState.Keys.ToList())
            {
                var entry = modelState[key];
                if (entry == null || entry.ValidationState != ModelValidationState.Invalid)
                {
                    continue;
                }

                var implicitErrors = entry.Errors
                    .Where(e => e.Exception == null && e.ErrorMessage.EndsWith(RequiredSuffix, StringComparison.Ordinal))
                    .ToList();
                if (implicitErrors.Count == 0 || explicitRequired.Contains(LastSegment(key)))
                {
                    continue;
                }

                var remaining = entry.Errors.Except(implicitErrors).ToList();
                modelState.ClearValidationState(key);
                if (remaining.Count == 0)
                {
                    modelState.MarkFieldValid(key);
                }
                else
                {
                    foreach (var error in remaining)
                    {
                        var message = !string.IsNullOrEmpty(error.ErrorMessage)
                            ? error.ErrorMessage
                            : error.Exception?.Message ?? "The value is invalid.";
                        modelState.AddModelError(key, message);
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        private static string LastSegment(string key)
        {
            var name = key;
            var dot = name.LastIndexOf('.');
            if (dot >= 0)
            {
                name = name[(dot + 1)..];
            }
            var bracket = name.IndexOf('[');
            if (bracket >= 0)
            {
                name = name[..bracket];
            }
            return name;
        }

        private static HashSet<string> BuildExplicitRequired(ActionDescriptor descriptor)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<Type>();
            foreach (var parameter in descriptor.Parameters)
            {
                Collect(parameter.ParameterType, names, visited, 0);
            }
            return names;
        }

        private static void Collect(Type type, HashSet<string> names, HashSet<Type> visited, int depth)
        {
            if (depth > 6)
            {
                return;
            }
            var element = ElementType(type);
            if (element.IsPrimitive || element == typeof(string) || element.IsEnum || element.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
            {
                return;
            }
            if (!visited.Add(element))
            {
                return;
            }
            foreach (var property in element.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<RequiredAttribute>() != null)
                {
                    names.Add(property.Name);
                }
                Collect(property.PropertyType, names, visited, depth + 1);
            }
        }

        private static Type ElementType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsArray)
            {
                return type.GetElementType()!;
            }
            if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                return type.GetGenericArguments()[^1];
            }
            return type;
        }
    }
}

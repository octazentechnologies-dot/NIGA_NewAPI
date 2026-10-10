using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>
/// Adds the same documentation block to every operation: auth, standard errors, version.
/// Per-endpoint samples stay in ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx.
/// </summary>
public sealed class ApiDocOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? operation.Summary
            : operation.Description;
        var requiresToken = ApiSecurityOperationFilter.RequiresToken(context.ApiDescription);
        var notes = new[]
        {
            "Version: v1.",
            "Deprecation: none.",
            requiresToken ? "Authentication: Bearer JWT." : "Authentication: none (public endpoint).",
            "Authorization: the role checks on the action. Admin, Account, Doctor, Reception, Patient, and Pharmacy are the clinic roles. One clinic; there is no second tenant.",
            "Validation: invalid body fields return 400 with success=false, message, traceId, and errors.",
            "Error codes: 400 validation, 401 missing or expired token, 403 role refused, 404 not found, 408 timeout, 429 rate limit, 500 unexpected failure."
        };
        var extra = string.Join(" ", notes);
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? extra
            : operation.Description.Trim() + " " + extra;

        Add(operation, "400", "Validation failed.");
        if (requiresToken)
        {
            Add(operation, "401", "Authentication required.");
            Add(operation, "403", "This role cannot call the endpoint.");
        }
        Add(operation, "429", "Too many requests for this user, or this IP when anonymous.");
        Add(operation, "500", "Unexpected failure. The body includes traceId.");
    }

    private static void Add(OpenApiOperation operation, string code, string description)
    {
        operation.Responses ??= new OpenApiResponses();
        if (operation.Responses.ContainsKey(code))
            return;
        operation.Responses[code] = new OpenApiResponse { Description = description };
    }
}

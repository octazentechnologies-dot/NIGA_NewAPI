using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>How the feature controllers turn a service result into HTTP and build the caller.</summary>
internal static class ServiceResultSupport
{
    public static async Task<IActionResult> Respond(this ControllerBase controller, Task<S4ActionResult> work, ILogger logger)
    {
        try
        {
            var result = await work;
            if (result.FileBytes != null)
                return controller.File(result.FileBytes, result.FileMime ?? "application/octet-stream", result.FileName);
            return controller.StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request failed for {Path}", controller.HttpContext.Request.Path);
            return controller.StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                code = "SERVER",
                message = "The request could not be completed."
            });
        }
    }

    public static async Task<IActionResult> Respond(this ControllerBase controller, Task<S3ActionResult> work)
    {
        var result = await work;
        return controller.StatusCode(result.StatusCode, result.Body);
    }

    public static S4Caller S4Caller(this ControllerBase controller, NIGACentrumContext context)
    {
        var user = controller.User;
        var role = DoctorOwnership.GetRoleName(user) ?? "";
        int? patientId = null;
        if (role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
        {
            var owner = PatientPortalOwnerResolver.ResolveAsync(context, user.GetUserId(), createIfMissing: false).GetAwaiter().GetResult();
            patientId = owner?.PatientId;
        }
        return new S4Caller
        {
            UserId = user.GetUserId(),
            DoctorId = DoctorOwnership.GetDoctorId(user),
            PatientId = patientId,
            Role = role,
            IsAdmin = DoctorOwnership.IsGlobalAdminPortalUser(user)
        };
    }

    public static S3Caller S3Caller(this ControllerBase controller)
        => new()
        {
            UserId = controller.User.GetUserId(),
            DoctorId = DoctorOwnership.GetDoctorId(controller.User),
            Role = DoctorOwnership.GetRoleName(controller.User) ?? "",
            IsAdmin = DoctorOwnership.IsGlobalAdminPortalUser(controller.User)
        };

    /// <summary>The signed-in treating doctor's id, or the 403 to return.</summary>
    public static (int Id, IActionResult? Error) RequireSelfDoctor(ControllerBase controller)
    {
        var denyTreat = DoctorOwnership.ForbidIfNotTreatingDoctor(controller.User);
        if (denyTreat != null)
            return (0, denyTreat);
        var id = DoctorOwnership.GetDoctorId(controller.User);
        if (!id.HasValue)
            return (0, ForbidRole(controller, "Doctor context is required."));
        return (id.Value, null);
    }

    public static bool IsReception(ControllerBase controller)
    {
        var role = DoctorOwnership.GetRoleName(controller.User);
        return role != null && role.Equals("Reception", StringComparison.OrdinalIgnoreCase);
    }

    public static IActionResult ForbidRole(ControllerBase controller, string message)
        => controller.StatusCode(StatusCodes.Status403Forbidden, new { success = false, message });
}

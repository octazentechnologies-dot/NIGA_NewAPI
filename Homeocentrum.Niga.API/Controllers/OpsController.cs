using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>Admin view of process metrics. Logs are files, this is the counter, and X-Trace-Id is the trace.</summary>
[ApiController]
[Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
[Route("api/[controller]")]
public class OpsController : ControllerBase
{
    [HttpGet("Metrics")]
    public IActionResult Metrics()
    {
        var traffic = ApiTraffic.Snapshot();
        return Ok(new
        {
            success = true,
            version = "v1",
            tenant = "single-clinic",
            requests = traffic.Total,
            serverErrors = traffic.ServerErrors,
            traceId = HttpContext.TraceIdentifier
        });
    }
}

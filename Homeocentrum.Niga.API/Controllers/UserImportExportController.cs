using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Admin export and import of users.
/// </summary>
[ApiController]
[Authorize]
public class UserImportExportController : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<UserImportExportController> _logger;

    public UserImportExportController(IS5Week5Service s5, NIGACentrumContext context, ILogger<UserImportExportController> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [SecurityAudit(SecurityAuditEvents.PatientDataExport)]
    [HttpGet("/api/Admin/Users/Export")]
    public Task<IActionResult> UsersExport() => Done(_s5.ExportUsersAsync(Caller()));

    [HttpPost("/api/Admin/Users/Import")]
    public Task<IActionResult> UsersImport([FromBody] UserImportRequest request) => Done(_s5.ImportUsersAsync(request, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

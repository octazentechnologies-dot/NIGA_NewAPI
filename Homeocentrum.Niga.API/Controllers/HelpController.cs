using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Help centre articles. Reading is public; publishing is admin only.
/// </summary>
[ApiController]
[Authorize]
public class HelpController : ControllerBase
{
    private readonly IS3Week3Service _s3;

    public HelpController(IS3Week3Service s3)
    {
        _s3 = s3;
    }

    /// <summary>
    /// SUP-06.02 — public GET published help articles (no token). Unpublished rows are omitted.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/api/Help")]
    public Task<IActionResult> Help()
        => Done(_s3.ListHelpAsync(includeUnpublished: false));

    /// <summary>
    /// SUP-06.02 — public GET one published article by slug. Unpublished / unknown → 404.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/api/Help/{slug}")]
    public Task<IActionResult> HelpArticle(string slug)
        => Done(_s3.GetHelpAsync(slug, includeUnpublished: false));

    [HttpPost("/api/Help")]
    public Task<IActionResult> SaveHelp([FromBody] HelpArticleWrite request)
    {
        if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
            return Task.FromResult<IActionResult>(ForbidRole("Only admin can publish help articles."));
        return Done(_s3.SaveHelpAsync(request));
    }

    private Task<IActionResult> Done(Task<S3ActionResult> work) => this.Respond(work);
    private IActionResult ForbidRole(string message) => ServiceResultSupport.ForbidRole(this, message);
}

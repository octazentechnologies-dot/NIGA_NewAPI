using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Doctor reviews: patient reviews, doctor replies, appeals, and admin moderation.
/// </summary>
[ApiController]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IS4Week4Service s4, IS5Week5Service s5, NIGACentrumContext context, ILogger<ReviewsController> logger)
    {
        _s4 = s4;
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpPost("/api/Reviews")]
    public Task<IActionResult> Review([FromBody] ReviewCreateRequest request) => Done(_s4.CreateReviewAsync(request, Caller()));

    [AllowAnonymous]
    [HttpGet("/api/Reviews/Doctor/{doctorId:int}")]
    public Task<IActionResult> PublicReviews(int doctorId) => Done(_s4.ListPublicReviewsAsync(doctorId));

    [HttpGet("/api/Reviews/Mine")]
    public Task<IActionResult> MyReviews() => Done(_s4.ListMyReviewsAsync(Caller()));

    [HttpPost("/api/Reviews/{id:int}/Appeal")]
    public Task<IActionResult> Appeal(int id, [FromBody] ReviewAppealRequest request) => Done(_s4.AppealReviewAsync(id, request, Caller()));

    [HttpGet("/api/Reviews/Appeals")]
    public Task<IActionResult> Appeals() => Done(_s4.ListAppealsAsync(Caller()));

    [HttpPost("/api/Reviews/Appeals/{id:int}/Resolve")]
    public Task<IActionResult> ResolveAppeal(int id, [FromBody] AppealResolveRequest request) => Done(_s4.ResolveAppealAsync(id, request, Caller()));

    [HttpGet("/api/Doctor/Reviews")]
    public Task<IActionResult> DoctorReviews() => Done(_s5.ListDoctorReviewsAsync(Caller()));

    [HttpPut("/api/Doctor/Reviews/{reviewId:int}/Reply")]
    public Task<IActionResult> ReviewReply(int reviewId, [FromBody] ReviewReplyWrite request)
        => Done(_s5.SaveReviewReplyAsync(reviewId, request, Caller()));

    [HttpGet("/api/Admin/Reviews")]
    public Task<IActionResult> AdminReviews() => Done(_s5.ListAdminReviewsAsync(Caller()));

    [HttpPut("/api/Admin/Reviews/{reviewId:int}/Status")]
    public Task<IActionResult> AdminReviewStatus(int reviewId, [FromBody] ReviewStatusWrite request)
        => Done(_s5.SetReviewStatusAsync(reviewId, request, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}

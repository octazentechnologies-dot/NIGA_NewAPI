using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Authorization;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Master;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>WEB-06 — public enquiry POST on New-API + admin list with TicketStatus/AssignedTo.</summary>
    [Route("api/Enquiry")]
    [ApiController]
    public class EnquiryController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public EnquiryController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] EnquiryCreateRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Name and details are required." });

            var row = new EnquiryDetail
            {
                EnquiryName = request.EnquiryName.Trim(),
                EmailId = request.EmailId,
                MobileNo = request.MobileNo,
                EnquiryDetails = request.EnquiryDetails.Trim(),
                EnquiryDate = DateTime.UtcNow,
                EnquiryStatus = true,
                TicketStatus = "New"
            };
            _context.EnquiryDetails.Add(row);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = new { row.EnquiryId, row.TicketStatus } });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> List([FromQuery] string? status = null)
        {
            if (!AdminAuthorizationPolicies.IsAdminPortalUser(User) && DoctorOwnership.GetDoctorId(User) == null)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Admin or doctor required." });

            var q = _context.EnquiryDetails.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(e => e.TicketStatus == status);

            var rows = await q.OrderByDescending(e => e.EnquiryId)
                .Take(200)
                .Select(e => new EnquiryListItemDto
                {
                    EnquiryId = e.EnquiryId,
                    EnquiryName = e.EnquiryName,
                    EnquiryDate = e.EnquiryDate,
                    EmailId = e.EmailId,
                    MobileNo = e.MobileNo,
                    EnquiryDetails = e.EnquiryDetails,
                    EnquiryStatus = e.EnquiryStatus,
                    TicketStatus = e.TicketStatus,
                    AssignedTo = e.AssignedTo
                })
                .ToListAsync();

            return Ok(new { success = true, data = rows });
        }
    }
}

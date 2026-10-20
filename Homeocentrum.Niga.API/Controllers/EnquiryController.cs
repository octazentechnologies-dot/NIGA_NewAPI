using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>WEB-06 — public enquiry POST on API + admin list with TicketStatus/AssignedTo.</summary>
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

            var now = DateTime.UtcNow;
            foreach (var row in rows)
            {
                row.SlaDueAt = row.EnquiryDate?.ToUniversalTime().AddHours(24);
                row.SlaBreached = row.SlaDueAt != null
                    && row.SlaDueAt < now
                    && !string.Equals(row.TicketStatus, "Closed", StringComparison.OrdinalIgnoreCase);
            }

            return Ok(new { success = true, data = rows });
        }

        [HttpPut("{enquiryId:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int enquiryId, [FromBody] EnquiryUpdateRequest request)
        {
            if (!AdminAuthorizationPolicies.IsAdminPortalUser(User))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Admin required." });

            var row = await _context.EnquiryDetails.FirstOrDefaultAsync(e => e.EnquiryId == enquiryId);
            if (row == null)
                return NotFound(new { success = false, message = "Enquiry not found." });

            if (!string.IsNullOrWhiteSpace(request?.TicketStatus))
            {
                var status = request.TicketStatus.Trim();
                var allowed = new[] { "New", "Open", "In Progress", "Closed" };
                if (!allowed.Any(a => string.Equals(a, status, StringComparison.OrdinalIgnoreCase)))
                    return BadRequest(new { success = false, message = "Ticket status must be New, Open, In Progress, or Closed." });
                row.TicketStatus = allowed.First(a => string.Equals(a, status, StringComparison.OrdinalIgnoreCase));
            }

            if (request?.ClearAssignee == true)
                row.AssignedTo = null;
            else if (request?.AssignedTo.HasValue == true)
                row.AssignedTo = request.AssignedTo.Value;

            await _context.SaveChangesAsync();

            var slaDue = row.EnquiryDate?.ToUniversalTime().AddHours(24);
            var breached = slaDue != null
                && slaDue < DateTime.UtcNow
                && !string.Equals(row.TicketStatus, "Closed", StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                success = true,
                data = new EnquiryListItemDto
                {
                    EnquiryId = row.EnquiryId,
                    EnquiryName = row.EnquiryName,
                    EnquiryDate = row.EnquiryDate,
                    EmailId = row.EmailId,
                    MobileNo = row.MobileNo,
                    EnquiryDetails = row.EnquiryDetails,
                    EnquiryStatus = row.EnquiryStatus,
                    TicketStatus = row.TicketStatus,
                    AssignedTo = row.AssignedTo,
                    SlaDueAt = slaDue,
                    SlaBreached = breached
                }
            });
        }
    }
}

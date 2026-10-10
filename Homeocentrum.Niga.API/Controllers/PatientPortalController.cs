using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>PAT-09 — care categories (web and patient app). The patient home dashboard is GET /api/MobilePatient/Home.</summary>
    [Route("api/PatientPortal")]
    [ApiController]
    [Authorize]
    public class PatientPortalController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public PatientPortalController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("CareCategories")]
        [AllowAnonymous]
        public async Task<IActionResult> CareCategories()
        {
            var rows = await _context.HumanSystemMasters.AsNoTracking()
                .OrderBy(h => h.HumanSystemName)
                .Select(h => new CareCategoryDto
                {
                    Id = h.HumanSystemId,
                    Name = h.HumanSystemName ?? string.Empty,
                    Description = h.Description
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }
    }
}

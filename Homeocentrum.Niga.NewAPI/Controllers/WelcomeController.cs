using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>PAT-02.02 — Welcome &amp; introduction content for the patient app (no RN UI).</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WelcomeController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public WelcomeController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("Patient")]
        [AllowAnonymous]
        public async Task<IActionResult> Patient()
        {
            List<object> slides;
            try
            {
                var rows = await _context.WelcomeSlides
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.Audience == "Patient")
                    .OrderBy(s => s.SortOrder)
                    .ToListAsync();
                slides = rows
                    .Select(s => (object)new { s.WelcomeSlideId, s.SortOrder, s.Title, s.Body, s.Version })
                    .ToList();
            }
            catch
            {
                slides = new List<object>();
            }

            if (slides.Count == 0)
            {
                slides = new List<object>
                {
                    new { welcomeSlideId = 1L, sortOrder = 1, title = "Welcome to Homeocentrum", body = "Your homeopathy care in one place — appointments, family, and prescriptions.", version = "1" },
                    new { welcomeSlideId = 2L, sortOrder = 2, title = "Family first", body = "Add family members and book for them with one account.", version = "1" },
                    new { welcomeSlideId = 3L, sortOrder = 3, title = "Your privacy", body = "We ask for consent before sharing or recording. You can withdraw anytime.", version = "1" },
                };
            }

            return Ok(new { success = true, data = new { version = "1", audience = "Patient", slides } });
        }
    }
}

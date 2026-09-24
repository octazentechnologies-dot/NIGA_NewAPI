using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interface;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class RepertorizationPageController : ControllerBase
    {
        IRepertorizationPageService repertorizationPageService;
        private readonly Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext _context;

        public RepertorizationPageController(
            IRepertorizationPageService _repertorizationPageService,
            Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext context)
        {
            repertorizationPageService = _repertorizationPageService;
            _context = context;
        }

        /// <summary>CLN-13.02 — Center of Gravity from selected rubric ids + intensities.</summary>
        [Authorize]
        [HttpPost("CenterOfGravity")]
        [HttpPost("/api/Repertorization/CenterOfGravity")]
        public async Task<IActionResult> CenterOfGravity([FromBody] CenterOfGravityRequest request)
        {
            var deny = Homeocentrum.Niga.NewAPI.Domain.Security.DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            if (request?.Rubrics == null || request.Rubrics.Count == 0)
                return Ok(new { success = true, data = Array.Empty<CenterOfGravityRemedyDto>(), message = "Empty clipboard." });

            var jwtDoctor = Homeocentrum.Niga.NewAPI.Domain.Security.DoctorOwnership.GetDoctorId(User);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userId, out var parsedUserId);

            var ids = request.Rubrics.Select(r => r.SubSectionId).Distinct().ToList();
            var intensity = request.Rubrics
                .GroupBy(r => r.SubSectionId)
                .ToDictionary(g => g.Key, g => Math.Max(1, g.Max(x => x.Intensity)));

            var rows = await _context.RubricRemedyDetails.AsNoTracking()
                .Where(r => r.SubSectionId != null && ids.Contains(r.SubSectionId.Value) && r.DeletedStatus != true)
                .Select(r => new
                {
                    SubSectionId = r.SubSectionId!.Value,
                    RemedyId = r.RemedyId ?? 0,
                    GradeNo = r.Grade != null ? r.Grade.GradeNo : 1,
                    RemedyName = r.Remedy != null ? r.Remedy.RemedyName : string.Empty
                })
                .ToListAsync();

            var grouped = rows
                .Where(r => r.RemedyId > 0)
                .GroupBy(r => r.RemedyId)
                .Select(g =>
                {
                    var contributing = g.Select(x => x.SubSectionId).Distinct().ToList();
                    var score = g.Sum(x => (intensity.TryGetValue(x.SubSectionId, out var iv) ? iv : 1) * Math.Max(1, x.GradeNo));
                    return new CenterOfGravityRemedyDto
                    {
                        RemedyId = g.Key,
                        RemedyName = g.Select(x => x.RemedyName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty,
                        Score = score,
                        ContributingSubSectionIds = contributing,
                        Reason = "Weighted by intensity × grade across " + contributing.Count + " rubric(s)."
                    };
                })
                .OrderByDescending(x => x.Score)
                .Take(20)
                .ToList();

            var doctorId = jwtDoctor ?? 0;
            if (doctorId <= 0 && parsedUserId > 0)
            {
                doctorId = await _context.Doctors.AsNoTracking()
                    .Where(d => d.UserId == parsedUserId && !d.DeleteStatus)
                    .Select(d => d.DoctorId)
                    .FirstOrDefaultAsync();
            }

            if (doctorId > 0)
            {
                _context.CogRuns.Add(new Homeocentrum.Niga.NewAPI.Domain.Master.CogRun
                {
                    DoctorId = doctorId,
                    PatientId = request.PatientId,
                    InputJson = System.Text.Json.JsonSerializer.Serialize(request),
                    OutputJson = System.Text.Json.JsonSerializer.Serialize(grouped),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, data = grouped });
        }

        [HttpGet("GetMateriaMedicaHeadingbyAuthorId/{authorId}")]
        [ProducesResponseType(typeof(MateriaMedicaHeadMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedicaHeadingbyAuthorId(int authorId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var materiamedicaheadModel = repertorizationPageService.GetMateriaMedicaHeadingbyAuthorId(authorId);

                if (materiamedicaheadModel != null)
                {
                    return Ok(materiamedicaheadModel);
                }
                return BadRequest("Data Not Found");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all MateriaMedicaHead
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("GetDifferentialMateriaMedica")]
        [ProducesResponseType(typeof(MateriaMedicaHeadMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDifferentialMateriaMedica(DifferentialMateriaMedica differentialMateriaMedica)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var differentialMateriaMedicaLists = repertorizationPageService.GetDifferentialMateriaMedica(differentialMateriaMedica);

                if (differentialMateriaMedicaLists != null)
                {
                    return Ok(differentialMateriaMedicaLists);
                }
                return BadRequest("Data Not Found");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}

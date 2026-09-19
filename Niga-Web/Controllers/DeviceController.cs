using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Master;

namespace Niga_Domain.API.Controllers
{
    /// <summary>DMO-03.02 / COM-03 — Register FCM or APNs token for the JWT user.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public DeviceController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] DeviceRegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Platform) || string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { success = false, message = "Platform and Token are required." });

            var platform = request.Platform.Trim();
            if (!platform.Equals("FCM", StringComparison.OrdinalIgnoreCase)
                && !platform.Equals("APNs", StringComparison.OrdinalIgnoreCase)
                && !platform.Equals("APNS", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Platform must be FCM or APNs." });
            }

            var userId = (long)User.GetUserId();
            var token = request.Token.Trim();
            var now = DateTime.UtcNow;

            DevicePushToken? row;
            try
            {
                row = await _context.DevicePushTokens
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.Token == token && !d.DeleteStatus);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "DevicePushToken table missing. Run 05_S1_Week1_Mobile_Menus_And_Prefs.sql.",
                    detail = ex.Message
                });
            }

            if (row == null)
            {
                row = new DevicePushToken
                {
                    UserId = userId,
                    Platform = platform.Equals("APNS", StringComparison.OrdinalIgnoreCase) ? "APNs" : platform,
                    Token = token,
                    DeviceId = request.DeviceId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    DeleteStatus = false
                };
                _context.DevicePushTokens.Add(row);
            }
            else
            {
                row.Platform = platform.Equals("APNS", StringComparison.OrdinalIgnoreCase) ? "APNs" : platform;
                row.DeviceId = request.DeviceId ?? row.DeviceId;
                row.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                data = new
                {
                    row.DevicePushTokenId,
                    row.Platform,
                    row.DeviceId,
                    row.UpdatedAt
                }
            });
        }

        [HttpGet("Mine")]
        public async Task<IActionResult> Mine()
        {
            var userId = (long)User.GetUserId();
            try
            {
                var rows = await _context.DevicePushTokens.AsNoTracking()
                    .Where(d => d.UserId == userId && !d.DeleteStatus)
                    .OrderByDescending(d => d.UpdatedAt)
                    .Select(d => new { d.DevicePushTokenId, d.Platform, d.DeviceId, d.UpdatedAt })
                    .ToListAsync();
                return Ok(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}

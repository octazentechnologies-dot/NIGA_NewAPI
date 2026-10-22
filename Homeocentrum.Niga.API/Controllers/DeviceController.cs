using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Services;

namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>
    /// Save / update / list FCM tokens for the signed-in user (Website, Android, iOS).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        private readonly NIGACentrumContext _context;
        private readonly IPushNotificationSender _push;

        public DeviceController(NIGACentrumContext context, IPushNotificationSender push)
        {
            _context = context;
            _push = push;
        }

        /// <summary>
        /// Insert the FCM token the first time; update when the same token or same device is sent again.
        /// Body: token, userId (optional, must match JWT), deviceType (Website | Android | iOS), optional deviceId.
        /// </summary>
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] DeviceRegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { success = false, message = "Token is required." });

            var jwtUserId = (long)User.GetUserId();
            if (jwtUserId <= 0)
                return Unauthorized(new { success = false, message = "Sign in required." });

            if (request.UserId.HasValue && request.UserId.Value > 0 && request.UserId.Value != jwtUserId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "userId must match the signed-in user." });

            DevicePushToken row;
            try
            {
                row = await _push.UpsertTokenAsync(
                    jwtUserId,
                    request.Token,
                    !string.IsNullOrWhiteSpace(request.DeviceType) ? request.DeviceType : request.Platform,
                    request.DeviceId);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    row.DevicePushTokenId,
                    userId = row.UserId,
                    deviceType = row.DeviceType,
                    row.DeviceId,
                    row.UpdatedAt
                }
            });
        }

        /// <summary>Clear one token (or soft-unregister by id). Logout clears all tokens for the user.</summary>
        [HttpPost("Unregister")]
        public async Task<IActionResult> Unregister([FromBody] DeviceUnregisterRequest request)
        {
            var token = request?.Token?.Trim();
            if (string.IsNullOrWhiteSpace(token) && (request?.DevicePushTokenId == null || request.DevicePushTokenId <= 0))
                return BadRequest(new { success = false, message = "Token or DevicePushTokenId is required." });

            var userId = (long)User.GetUserId();
            DevicePushToken? row;
            try
            {
                if (request!.DevicePushTokenId.HasValue && request.DevicePushTokenId.Value > 0)
                {
                    row = await _context.DevicePushTokens
                        .FirstOrDefaultAsync(d =>
                            d.DevicePushTokenId == request.DevicePushTokenId.Value
                            && d.UserId == userId
                            && !d.DeleteStatus);
                }
                else
                {
                    row = await _context.DevicePushTokens
                        .FirstOrDefaultAsync(d => d.UserId == userId && d.Token == token && !d.DeleteStatus);
                }
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }

            if (row == null)
                return NotFound(new { success = false, message = "Device token not found." });

            row.DeleteStatus = true;
            row.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Unregistered." });
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
                    .Select(d => new
                    {
                        d.DevicePushTokenId,
                        d.UserId,
                        d.DeviceType,
                        d.Platform,
                        d.DeviceId,
                        d.UpdatedAt
                    })
                    .ToListAsync();
                return Ok(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}

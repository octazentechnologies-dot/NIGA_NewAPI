using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Interfaces;

namespace Niga_Domain.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly NIGACentrumContext _context;
        private readonly IReceptionStaffService _receptionStaffService;
        private readonly ISubscriptionStatusService _subscriptionStatusService;

        public AccountController(
            ITokenService tokenService,
            NIGACentrumContext context,
            IReceptionStaffService receptionStaffService,
            ISubscriptionStatusService subscriptionStatusService)
        {
            _tokenService = tokenService;
            _context = context;
            _receptionStaffService = receptionStaffService;
            _subscriptionStatusService = subscriptionStatusService;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Invalid request data" });

                if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
                    return BadRequest(new { message = "Username and password are required" });

                var userEntity = await _context.UserMasters
                    .FirstOrDefaultAsync(x => x.UserName == model.UserName);

                if (userEntity != null)
                {
                    if (userEntity.UserPassword != model.Password)
                        return Unauthorized(new { message = "Invalid username or password" });

                    if (userEntity.IsUserActivated != true)
                        return Unauthorized(new { message = "Account is deactivated. Please contact administrator." });

                    var roleEntity = await _context.RoleMasters
                        .FirstOrDefaultAsync(x => x.RoleId == userEntity.RoleId);

                    if (roleEntity == null)
                        return BadRequest(new { message = "User role not found" });

                    var token = await _tokenService.CreateToken(userEntity, 7 * 24 * 60);

                    var userData = new AuthModel
                    {
                        IsSuperUser = roleEntity.RoleId == 1,
                        UserId = userEntity.UserId,
                        UserName = $"{userEntity.FirstName} {userEntity.LastName}".Trim(),
                        Role = roleEntity.RoleName,
                        RoleId = userEntity.RoleId,
                        FirmIds = userEntity.FirmIds,
                        Token = token,
                        IsPlanActive = false,
                        IslastFiveDays = false,
                        DaysRemaining = 0
                    };

                    if (roleEntity.RoleId == 3)
                    {
                        var doctorEntity = await _context.Doctors
                            .FirstOrDefaultAsync(d =>
                                d.UserId == userEntity.UserId &&
                                d.DeleteStatus == false);

                        if (doctorEntity != null)
                        {
                            userData.DoctorId = doctorEntity.DoctorId;

                            var subscriptionStatus = await _subscriptionStatusService
                                .GetForDoctorAsync(doctorEntity.DoctorId);

                            userData.IsPlanActive = subscriptionStatus.IsPlanActive;
                            userData.DaysRemaining = subscriptionStatus.DaysRemaining;
                            userData.IslastFiveDays = subscriptionStatus.IslastFiveDays;
                        }
                    }
                    else if (string.Equals(roleEntity.RoleName, "Doctor", StringComparison.OrdinalIgnoreCase))
                    {
                        var doctor = await _context.Doctors
                            .AsNoTracking()
                            .FirstOrDefaultAsync(d => d.UserId == userEntity.UserId && !d.DeleteStatus);
                        if (doctor != null)
                            userData.DoctorId = doctor.DoctorId;
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Login successful",
                        data = userData
                    });
                }

                var receptionAuth = await _receptionStaffService.TryBuildAuthModelForLoginAsync(
                    model.UserName,
                    model.Password);

                if (receptionAuth == null)
                    return Unauthorized(new { message = "Invalid username or password" });

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = receptionAuth
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during login. Please try again."
                });
            }
        }

        [HttpGet("SubscriptionStatus")]
        [Authorize]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            try
            {
                int userId = User.GetUserId();

                var doctorEntity = await _context.Doctors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.DeleteStatus == false);

                if (doctorEntity == null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new SubscriptionStatusModel()
                    });
                }

                var subscriptionStatus = await _subscriptionStatusService
                    .GetForDoctorAsync(doctorEntity.DoctorId);

                return Ok(new
                {
                    success = true,
                    data = subscriptionStatus
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to fetch subscription status."
                });
            }
        }
    }
}

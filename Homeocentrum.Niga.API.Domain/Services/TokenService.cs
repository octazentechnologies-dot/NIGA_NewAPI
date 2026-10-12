using API.Entities;
using Homeocentrum.Niga.API.Domain.Constants;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Security.Audit;

namespace Homeocentrum.Niga.API.Domain.Services
{
    public class TokenService:ITokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly UserManager<AppUser> _userManager;
        private readonly ISecurityAuditLog? _audit;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public TokenService(IConfiguration config, UserManager<AppUser> userManager,
            ISecurityAuditLog? audit = null, IHttpContextAccessor? httpContextAccessor = null)
        {
            _userManager = userManager;
            _key = JwtSettings.SigningKey(config);
            _issuer = JwtSettings.Issuer(config);
            _audience = JwtSettings.Audience(config);
            _audit = audit;
            _httpContextAccessor = httpContextAccessor;
        }

        private Task AuditIssuedAsync(long userId, string? role, SecurityTokenDescriptor descriptor, IEnumerable<Claim> claims)
        {
            if (_audit == null) return Task.CompletedTask;
            var jti = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            return _audit.WriteAsync(SecurityAuditEvents.TokenIssued, _httpContextAccessor?.HttpContext, "SUCCESS",
                actorUserId: userId, actorRole: role,
                detail: $"jti={jti}; expiresUtc={descriptor.Expires?.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}; iss={_issuer}");
        }

        public async Task<string> CreateToken(UserMaster user, int expiryMin = 0, string roleName = null, int? doctorId = null)
        {
            // M02 W0: RoleId + RoleName + ClaimTypes.Role enable AdminPortal policy on mutate APIs.
            // SEC-01.02: DoctorID claim when doctorId provided.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim("RoleId", user.RoleId?.ToString() ?? string.Empty),
                new Claim("FirmIds", user.FirmIds ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                claims.Add(new Claim("RoleName", roleName));
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            if (doctorId.HasValue && doctorId.Value > 0)
            {
                claims.Add(new Claim("DoctorID", doctorId.Value.ToString()));
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(expiryMin == 0 ? TokenConstants.accessTokenTimeInMins : expiryMin),
                SigningCredentials = creds,
                Issuer = _issuer,
                Audience = _audience,
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var written = tokenHandler.WriteToken(token);
            await AuditIssuedAsync(user.UserId, roleName, tokenDescriptor, claims);
            return written;
        }

        public async Task<string> CreateReceptionStaffToken(
            int receptionStaffId,
            string userId,
            int doctorId,
            string fullName,
            int expiryMin = 0,
            int? roleId = null,
            string roleName = "Reception",
            int? doctorUserId = null)
        {
            var effectiveRole = string.IsNullOrWhiteSpace(roleName) ? "Reception" : roleName.Trim();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, receptionStaffId.ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, receptionStaffId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, userId),
                new Claim("DoctorID", doctorId.ToString()),
                new Claim("FullName", fullName),
                new Claim(ClaimTypes.Role, effectiveRole),
                new Claim("RoleName", effectiveRole),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (doctorUserId.HasValue && doctorUserId.Value > 0)
                claims.Add(new Claim("DoctorUserID", doctorUserId.Value.ToString()));

            if (roleId.HasValue)
                claims.Add(new Claim("RoleId", roleId.Value.ToString()));

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddMinutes(expiryMin == 0 ? TokenConstants.accessTokenTimeInMins : expiryMin),
                SigningCredentials = creds,
                Issuer = _issuer,
                Audience = _audience,
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var written = tokenHandler.WriteToken(token);
            await AuditIssuedAsync(receptionStaffId, effectiveRole, tokenDescriptor, claims);
            return written;
        }
    }
}

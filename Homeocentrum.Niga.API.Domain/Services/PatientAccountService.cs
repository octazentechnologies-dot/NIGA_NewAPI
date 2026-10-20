using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Services
{
    public sealed class PatientAccountService : IPatientAccountService
    {
        private readonly NIGACentrumContext _context;

        public PatientAccountService(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task<int?> GetPatientRoleIdAsync(CancellationToken ct = default)
        {
            return await _context.RoleMasters.AsNoTracking()
                .Where(r => r.RoleName == "Patient" && !r.DeleteStatus)
                .Select(r => (int?)r.RoleId)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<UserMaster?> FindPatientLoginByMobileAsync(string digits, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(digits) || digits.Length < 8)
                return null;

            var patientRoleId = await GetPatientRoleIdAsync(ct);
            if (!patientRoleId.HasValue)
                return null;

            var candidates = await _context.UserMasters
                .Where(u => !u.DeleteStatus && u.RoleId == patientRoleId.Value && u.MobileNo != null)
                .ToListAsync(ct);

            return candidates.FirstOrDefault(u => PhoneNormalizer.EqualsNormalized(u.MobileNo, digits));
        }

        public async Task<(int PatientId, string? PatientName, string? Email)?> FindPatientByMobileAsync(
            string digits,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(digits) || digits.Length < 8)
                return null;

            var patients = await _context.Patients.AsNoTracking()
                .Where(p => p.DeleteStatus != true && p.MobileNo != null)
                .Select(p => new { p.PatientId, p.PatientName, p.MobileNo, p.Email })
                .ToListAsync(ct);

            var match = patients
                .Where(p => PhoneNormalizer.EqualsNormalized(p.MobileNo, digits))
                .OrderBy(p => p.PatientId)
                .FirstOrDefault();

            if (match == null)
                return null;

            return (match.PatientId, match.PatientName, match.Email);
        }

        public async Task<bool> IsPatientMobileTakenInUserMasterAsync(
            string digits,
            int? excludePatientId = null,
            CancellationToken ct = default)
        {
            var login = await FindPatientLoginByMobileAsync(digits, ct);
            if (login == null)
                return false;

            if (!excludePatientId.HasValue || excludePatientId.Value <= 0)
                return true;

            var linkedPatientIds = await _context.PatientUserMaps.AsNoTracking()
                .Where(m => m.UserId == login.UserId && !m.DeleteStatus)
                .Select(m => m.PatientId)
                .ToListAsync(ct);

            // Same chart's own login is allowed; another patient's login is not.
            return linkedPatientIds.Count == 0 || !linkedPatientIds.Contains(excludePatientId.Value);
        }

        public async Task<long?> EnsurePatientLoginAsync(PatientModel model, CancellationToken ct = default)
        {
            var digits = PhoneNormalizer.Digits(model.MobileNo);
            if (digits.Length < 8 || model.PatientID <= 0)
                return null;

            var existing = await FindPatientLoginByMobileAsync(digits, ct);
            if (existing != null)
            {
                await EnsurePrimaryMapAsync(existing.UserId, model.PatientID, existing.UserId.ToString(), ct);
                model.UserId = (int)existing.UserId;
                model.IsUserAlreadyRegistered = true;
                return existing.UserId;
            }

            var patientRoleId = await GetPatientRoleIdAsync(ct);
            if (!patientRoleId.HasValue)
                return null;

            var userName = digits;
            if (await _context.UserMasters.AnyAsync(u => u.UserName == userName && !u.DeleteStatus, ct))
                userName = $"{digits}_{model.PatientID}";

            var nameParts = (model.PatientName ?? string.Empty).Trim()
                .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var email = model.Email?.Trim();

            var user = new UserMaster
            {
                UserName = userName,
                UserPassword = UserPasswordHasher.Hash(
                    Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))),
                MobileNo = digits,
                EmailId = string.IsNullOrEmpty(email) ? null : (email.Length > 50 ? email[..50] : email),
                FirstName = nameParts.Length > 0 ? nameParts[0] : null,
                LastName = nameParts.Length > 1 ? nameParts[1] : null,
                CountryId = model.CountryId > 0 ? model.CountryId : null,
                StateId = model.StateId > 0 ? model.StateId : null,
                RoleId = patientRoleId.Value,
                UserStatus = true,
                IsUserActivated = true,
                DeleteStatus = false,
                EnteredBy = string.IsNullOrWhiteSpace(model.EntityType)
                    ? (model.EnteredBy ?? "PatientOnboard")
                    : model.EntityType.Trim(),
                EnteredDate = DateTime.Now
            };
            _context.UserMasters.Add(user);
            await _context.SaveChangesAsync(ct);

            _context.PatientUserMaps.Add(new PatientUserMap
            {
                UserId = user.UserId,
                PatientId = model.PatientID,
                IsPrimary = true,
                DeleteStatus = false,
                EnteredBy = user.UserId.ToString(),
                EnteredDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(ct);

            model.UserId = (int)user.UserId;
            model.IsUserAlreadyRegistered = false;
            return user.UserId;
        }

        public async Task<(UserMaster User, int PatientId, string? PatientName, string? Email)?> ResolveOrClaimAfterOtpAsync(
            string digits,
            CancellationToken ct = default)
        {
            var patient = await FindPatientByMobileAsync(digits, ct);
            if (patient == null)
                return null;

            var mappedUserId = await _context.PatientUserMaps.AsNoTracking()
                .Where(m => m.PatientId == patient.Value.PatientId && !m.DeleteStatus)
                .OrderByDescending(m => m.IsPrimary)
                .Select(m => (long?)m.UserId)
                .FirstOrDefaultAsync(ct);

            UserMaster? user = null;
            if (mappedUserId.HasValue)
            {
                user = await _context.UserMasters.FirstOrDefaultAsync(
                    u => u.UserId == mappedUserId.Value && !u.DeleteStatus && u.IsUserActivated == true,
                    ct);
            }

            if (user == null)
                user = await FindPatientLoginByMobileAsync(digits, ct);

            if (user == null)
            {
                var model = new PatientModel
                {
                    PatientID = patient.Value.PatientId,
                    PatientName = patient.Value.PatientName,
                    MobileNo = digits,
                    Email = patient.Value.Email,
                    EntityType = PatientEntityTypes.PatientMobile
                };
                var createdId = await EnsurePatientLoginAsync(model, ct);
                if (!createdId.HasValue)
                    return null;
                user = await _context.UserMasters.FirstOrDefaultAsync(
                    u => u.UserId == createdId.Value && !u.DeleteStatus,
                    ct);
            }
            else
            {
                await EnsurePrimaryMapAsync(user.UserId, patient.Value.PatientId, user.UserId.ToString(), ct);
            }

            if (user == null || user.IsUserActivated != true)
                return null;

            return (user, patient.Value.PatientId, patient.Value.PatientName, patient.Value.Email ?? user.EmailId);
        }

        public async Task<bool> IsRegisteredOrClaimableAsync(string digits, CancellationToken ct = default)
        {
            if (await FindPatientLoginByMobileAsync(digits, ct) != null)
                return true;
            return await FindPatientByMobileAsync(digits, ct) != null;
        }

        private async Task EnsurePrimaryMapAsync(long userId, int patientId, string? enteredBy, CancellationToken ct)
        {
            var map = await _context.PatientUserMaps
                .FirstOrDefaultAsync(m => m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus, ct);
            if (map != null)
            {
                if (!map.IsPrimary)
                {
                    map.IsPrimary = true;
                    await _context.SaveChangesAsync(ct);
                }
                return;
            }

            _context.PatientUserMaps.Add(new PatientUserMap
            {
                UserId = userId,
                PatientId = patientId,
                IsPrimary = true,
                DeleteStatus = false,
                EnteredBy = enteredBy,
                EnteredDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(ct);
        }
    }
}

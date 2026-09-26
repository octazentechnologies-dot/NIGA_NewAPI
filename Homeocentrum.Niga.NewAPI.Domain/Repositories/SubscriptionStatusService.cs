using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
{
    public class SubscriptionStatusService : ISubscriptionStatusService
    {
        private readonly NIGACentrumContext _context;

        public SubscriptionStatusService(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task<SubscriptionStatusModel> GetForDoctorAsync(int doctorId)
        {
            var status = new SubscriptionStatusModel
            {
                IsPlanActive = false,
                IslastFiveDays = false,
                DaysRemaining = 0
            };

            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId || d.UserId == doctorId);

            var lookupIds = new List<int> { doctorId };
            if (doctor != null)
            {
                lookupIds.Add(doctor.DoctorId);
                if (doctor.UserId.HasValue)
                    lookupIds.Add(doctor.UserId.Value);
            }

            var userSubscription = await _context.PackageEntryDetails.AsNoTracking()
                .Where(p => p.IsActive == true && p.DoctorId != null && lookupIds.Contains(p.DoctorId.Value))
                .OrderByDescending(p => p.ExpiryDate)
                .FirstOrDefaultAsync();

            if (userSubscription?.ExpiryDate != null)
            {
                var expiryDate = Convert.ToDateTime(userSubscription.ExpiryDate);
                var daysRemaining = (int)Math.Floor((expiryDate - DateTime.UtcNow).TotalDays);

                if (daysRemaining > 0)
                {
                    status.IsPlanActive = true;
                    status.DaysRemaining = daysRemaining;
                    status.IslastFiveDays = daysRemaining <= 5;
                    status.ExpiryDate = expiryDate;
                }
            }

            if (!status.IsPlanActive)
            {
                string? userName = null;
                if (doctor?.UserId != null)
                {
                    userName = await _context.UserMasters.AsNoTracking()
                        .Where(u => u.UserId == doctor.UserId.Value)
                        .Select(u => u.UserName)
                        .FirstOrDefaultAsync();
                }

                if (IsDevClinicDoctor(userName))
                {
                    status.IsPlanActive = true;
                    status.DaysRemaining = Math.Max(status.DaysRemaining, 365);
                }
            }

            return status;
        }

        private static bool IsDevClinicDoctor(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;
            return userName.Equals("Tufan_Doctor", StringComparison.OrdinalIgnoreCase)
                || userName.Equals("NIGA HOMEOPATHY", StringComparison.OrdinalIgnoreCase)
                || userName.Equals("testdoctor", StringComparison.OrdinalIgnoreCase);
        }
    }
}

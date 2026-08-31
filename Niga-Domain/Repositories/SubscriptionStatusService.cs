using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Repositories
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

            var userSubscription = await _context.PackageEntryDetails
                .AsNoTracking()
                .Where(p => p.DoctorId == doctorId && p.IsActive == true)
                .OrderByDescending(p => p.ExpiryDate)
                .FirstOrDefaultAsync();

            if (userSubscription?.ExpiryDate == null)
            {
                return status;
            }

            var expiryDate = Convert.ToDateTime(userSubscription.ExpiryDate);
            var daysRemaining = (int)Math.Floor((expiryDate - DateTime.UtcNow).TotalDays);

            if (daysRemaining > 0)
            {
                status.IsPlanActive = true;
                status.DaysRemaining = daysRemaining;
                status.IslastFiveDays = daysRemaining <= 5;
                status.ExpiryDate = expiryDate;
            }

            return status;
        }
    }
}

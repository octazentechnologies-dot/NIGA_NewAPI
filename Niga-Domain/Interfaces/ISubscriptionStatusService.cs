using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces
{
    public interface ISubscriptionStatusService
    {
        Task<SubscriptionStatusModel> GetForDoctorAsync(int doctorId);
    }
}

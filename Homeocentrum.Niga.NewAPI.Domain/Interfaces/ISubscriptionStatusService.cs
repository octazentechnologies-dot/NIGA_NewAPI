using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    public interface ISubscriptionStatusService
    {
        Task<SubscriptionStatusModel> GetForDoctorAsync(int doctorId);
    }
}

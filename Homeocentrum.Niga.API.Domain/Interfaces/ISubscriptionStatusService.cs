using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Interfaces
{
    public interface ISubscriptionStatusService
    {
        Task<SubscriptionStatusModel> GetForDoctorAsync(int doctorId);
    }
}

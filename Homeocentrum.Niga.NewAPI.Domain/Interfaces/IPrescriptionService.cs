using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    /// <summary>
    /// Interface for prescription rubric and remedy operations.
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// Returns true when the appointment exists and is not deleted.
        /// </summary>
        Task<bool> AppointmentExistsAsync(int appointmentId);

        /// <summary>
        /// Gets paginated prescription rubric and remedy details for an appointment with joined names.
        /// </summary>
        Task<PrescriptionDetailsPaginatedResult> GetPrescriptionDetailsByAppointmentIdAsync(
            GetPrescriptionDetailsByAppointmentIdRequest request);
    }
}

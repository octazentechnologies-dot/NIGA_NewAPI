using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Interfaces
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

        #region Old API compatible methods
#nullable disable
        /// <summary>
        /// Method is used for get all the Clipboard Rubrics
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        /// 

        string SavePrescriptionDetail(PrescriptionDetailModel prescriptionDetail, ref ErrorResponseModel errorResponseModel);
        List<PrescriptionRemedyViewModel> GetPrescriptionRemedy(List<int?> rubricList, ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion
    }
}

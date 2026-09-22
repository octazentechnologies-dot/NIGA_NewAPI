using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces
{
    /// <summary>
    /// Interface for PatientAppointment operations
    /// </summary>
    public interface IPatientAppointmentService
    {
        /// <summary>
        /// Gets an PatientAppointment by its ID.
        /// </summary>
        PatientAppointmentModel GetPatientAppById(long PatientAppId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Saves or updates an PatientAppointment.
        /// </summary>
        string SavePatientApp(PatientAppointmentModel PatientAppointmentModel, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Gets all cases for a user.
        /// </summary>
        List<PatientModel> GetCasesByUser(long userId, ref ErrorResponseModel errorResponseModel);

        PatientAppointmentModel UpdateAppointmentStatus(UpdateAppointmentStatusModel model, ref ErrorResponseModel errorResponseModel);

        PatientAppointmentModel UpdateAppointmentTime(UpdateAppointmentTimeModel model, ref ErrorResponseModel errorResponseModel);

        Task<List<PatientAppointmentModel>> GetAppointmentsByDateAsync(GetAppointmentsByDateRequest request);

        /// <summary>
        /// Returns true when the patient exists and is not deleted.
        /// </summary>
        Task<bool> PatientExistsAsync(int patientId);

        /// <summary>
        /// Gets a paginated, sorted list of non-deleted appointments for a patient.
        /// </summary>
        Task<PaginatedResult<PatientAppointmentListItemModel>> GetAppointmentListByPatientIdAsync(
            GetAppointmentListByPatientIdRequest request);

        Task<DoctorDailyScheduleModel?> GetDailyScheduleAsync(GetDoctorDailyScheduleRequest request);

        Task<(DoctorDailyScheduleModel? Schedule, ErrorResponseModel? Error)> SaveDailyScheduleAsync(
            SaveDoctorDailyScheduleRequest request);

        Task<AppointmentSlotsResponse> GetAppointmentSlotsAsync(GetAppointmentSlotsRequest request);

        Task<AppointmentMutationResult> RescheduleAppointmentAsync(RescheduleAppointmentRequest request, long byUserId, string? byRole);

        Task<AppointmentMutationResult> CancelAppointmentAsync(CancelAppointmentRequest request, long byUserId, string? byRole);

        Task<List<AppointmentChangeLogItem>> GetChangeLogAsync(int patientAppId);

        Task<AppointmentMutationResult> PatchVisitTypeAsync(int patientAppId, string? visitType, string? consultMode);

        Task<AppointmentMutationResult> CallNextAsync(int doctorId);

        Task<List<PatientAppointmentModel>> GetQueueAsync(int doctorId);
    }
}
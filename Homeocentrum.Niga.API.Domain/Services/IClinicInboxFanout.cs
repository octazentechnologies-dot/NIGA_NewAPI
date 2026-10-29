namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Web-bell + FCM fan-out to clinic logins (doctor UserMaster id, reception staff JWT id,
/// pharmacy partner UserId) and patient portal UserId. Patient SMS/WhatsApp still go through
/// <see cref="IAppointmentRescheduleNotifier"/>.
/// </summary>
public interface IClinicInboxFanout
{
    Task NotifyDoctorAndReceptionAsync(
        int doctorId,
        string title,
        string body,
        string action,
        string? doctorDeepLink = "/doctordashboard",
        string? receptionDeepLink = "/reception/appointments",
        CancellationToken cancellationToken = default);

    Task NotifyPharmacyPartnerAsync(
        int? pharmacyPartnerId,
        string title,
        string body,
        CancellationToken cancellationToken = default);

    Task NotifyDoctorUserAsync(
        int doctorId,
        string title,
        string body,
        string action,
        string? deepLink,
        CancellationToken cancellationToken = default);

    Task NotifyPatientAsync(
        int patientId,
        string title,
        string body,
        string action,
        string? deepLink,
        CancellationToken cancellationToken = default);
}

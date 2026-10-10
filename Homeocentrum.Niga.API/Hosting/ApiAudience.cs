using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>Who an endpoint is built for. Drives the Swagger documents and tags.</summary>
public enum ApiAudience
{
    /// <summary>Only the Patient Mobile App calls it. Lives in MobilePatientController.</summary>
    MobilePatient,
    /// <summary>Only the Doctor Mobile App calls it. Lives in MobileDoctorController.</summary>
    MobileDoctor,
    /// <summary>Shared: called by a mobile app and the web, or by both mobile apps. Stays in its existing controller.</summary>
    Common,
    /// <summary>Web application and back office only.</summary>
    Web
}

[Flags]
public enum ApiClients
{
    None = 0,
    Web = 1,
    PatientApp = 2,
    DoctorApp = 4
}

/// <summary>Marks a controller whose every action is for one mobile app.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApiAudienceAttribute : Attribute
{
    public ApiAudienceAttribute(ApiAudience audience)
    {
        Audience = audience;
    }

    public ApiAudience Audience { get; }
}

/// <summary>
/// Shared endpoints that the Patient Mobile App or the Doctor Mobile App call, with every client that uses them.
/// Sources: the app source (store/api/new/*.ts), the Weeks 1-4 PAT/DMO mobile task list, and web UI page usage.
/// An endpoint not listed here and not in a Mobile* controller is a web endpoint.
/// Paths are in ApiExplorer form: no leading slash, no route constraints.
/// </summary>
public static class ApiAudienceCatalog
{
    private const ApiClients W = ApiClients.Web;
    private const ApiClients P = ApiClients.PatientApp;
    private const ApiClients D = ApiClients.DoctorApp;

    private static readonly (string Method, string Path, ApiClients Clients, string Use)[] Shared =
    {
        // Sign-in, session and device
        ("GET", "api/mastersAPI/GetLanguages", W | P | D, "PAT-01.02 language picker; doctor app first launch"),
        ("POST", "api/PatientAuth/RequestOtp", W | P, "PAT-03.02 patient OTP"),
        ("POST", "api/PatientAuth/VerifyOtp", W | P, "PAT-03.02 patient OTP"),
        ("POST", "api/Account/Login", W | D, "DMO-01.02 password login"),
        ("POST", "api/Account/ForgotPasswordAccounts", W | D, "Doctor app forgot password"),
        ("POST", "api/Account/ForgotPassword", W | D, "Doctor app forgot password"),
        ("POST", "api/Account/RefreshToken", P | D, "PAT-03.02 / DMO-11.02 token refresh"),
        ("POST", "api/Account/Logout", W | P | D, "DMO-11.02 logout"),
        ("POST", "api/Account/ConfirmMobile", W | D, "DMO-02.02 confirm number"),
        ("POST", "api/Otp/RequestOtp", W | D, "DMO-01.02 doctor OTP (action Login)"),
        ("POST", "api/Device/Register", P | D, "DMO-03.02 / DMO-06.02 push token"),
        ("POST", "api/Device/Unregister", P | D, "DMO-03.02 push token"),
        ("GET", "api/Device/Mine", P | D, "DMO-03.02 push token"),
        ("POST", "api/Devices/Register", P | D, "DMO-06.02 push token (alias of Device/Register)"),
        ("GET", "api/Notifications", W | P | D, "Notification inbox"),
        ("PATCH", "api/Notifications/{id}", W | P | D, "Notification inbox"),
        ("GET", "api/Help", W | P | D, "Help centre"),
        ("GET", "api/Help/{slug}", W | P | D, "Help centre"),

        // Patient profile, consent, family
        ("GET", "api/PatientProfile/Me", W | P, "PAT-01.02 / PAT-04.02 profile"),
        ("PUT", "api/PatientProfile/Me", W | P, "PAT-01.02 / PAT-04.02 profile"),
        ("GET", "api/Patient/Profile", W | P, "PAT-04.02 health profile"),
        ("PUT", "api/Patient/Profile", W | P, "PAT-04.02 health profile"),
        ("GET", "api/UserAddressLocation/Countries", W | P, "PAT-04.02 address"),
        ("GET", "api/UserAddressLocation/States/ByCountry/{countryId}", W | P, "PAT-04.02 address"),
        ("GET", "api/UserAddressLocation/Districts/ByState/{stateId}", W | P, "PAT-04.02 address"),
        ("GET", "api/UserAddressLocation/Cities/ByDistrict/{districtId}", W | P, "PAT-04.02 address"),
        ("GET", "api/UserAddressLocation/PinCodes/ByCity/{cityId}", W | P, "PAT-04.02 address"),
        ("GET", "api/Consent/PrivacyStatus", W | P, "PAT-05.02 privacy consent"),
        ("POST", "api/Consent/GrantPrivacy", W | P, "PAT-05.02 privacy consent"),
        ("GET", "api/Patient/Consents", W | P, "PAT-05.02 consents"),
        ("GET", "api/Family", W | P, "PAT-06.02 family"),
        ("POST", "api/Family", W | P, "PAT-06.02 family"),
        ("GET", "api/Family/{id}", W | P, "PAT-06.02 family"),
        ("PUT", "api/Family/{id}", W | P, "PAT-06.02 family"),
        ("PATCH", "api/Family/{id}", W | P, "PAT-06.02 family"),
        ("DELETE", "api/Family/{id}", W | P, "PAT-06.02 family"),
        ("GET", "api/Family/Relations", W | P, "PAT-06.02 family"),
        ("POST", "api/Family/Relations", W | P, "PAT-06.02 family"),
        ("POST", "api/Family/BookAs", W | P, "PAT-06.02 book for a family member"),
        ("POST", "api/Caregiver/Grant", W | P, "PAT-07.02 caregiver"),
        ("POST", "api/Caregiver/Revoke", W | P, "PAT-07.02 caregiver"),
        ("GET", "api/Caregiver/ListMine", W | P, "PAT-07.02 caregiver"),
        ("GET", "api/Caregiver/ListActingFor", W | P, "PAT-07.02 caregiver"),
        ("GET", "api/Caregiver/Lookup", W | P, "PAT-07.02 caregiver"),
        ("GET", "api/Caregiver/Me", W | P, "PAT-07.02 caregiver"),

        // Discovery and booking
        ("GET", "api/PatientPortal/CareCategories", W | P, "PAT-08.02 / PAT-09.02 care categories"),
        ("GET", "api/Public/Doctors", W | P, "PAT-09.02 to PAT-11.02 doctor search"),
        ("GET", "api/Public/Doctors/{id}", W | P, "PAT-12.02 / PAT-13.02 doctor profile"),
        ("GET", "api/Public/Doctors/{id}/Ranking", W | P, "PAT-14.02 ranking"),
        ("GET", "api/Public/Doctors/{id}/Slots", W | P, "PAT-16.02 slot picker"),
        ("POST", "api/Public/Doctors/{id}/Bookings", W | P, "PAT-17.02 booking"),
        ("GET", "api/Public/Articles", W | P, "PAT-15.02 health articles"),
        ("GET", "api/Public/Articles/{id}", W | P, "PAT-15.02 health articles"),
        ("GET", "api/Profile/Photo/{doctorId}", W | P | D, "PAT-10.02 doctor photo"),
        ("GET", "api/Fees/Public/{doctorId}", W | P, "PAT-12.02 / PAT-18.02 consult fee"),
        ("GET", "api/Reviews/Doctor/{doctorId}", W | P | D, "PAT-12.02 reviews"),
        ("POST", "api/Payments/ConsultOrders", W | P, "PAT-18.02 checkout"),
        ("POST", "api/Payments/Verify", P, "PAT-19.02 payment status (gateway verify)"),
        ("GET", "api/Payments/Appointments/{patientAppId}", P, "PAT-19.02 payment status"),
        ("GET", "api/Patient/Visits", W | P, "My appointments"),
        ("GET", "api/PatientAppointment/ChangeLog/{patientAppId}", W | P, "PAT-20.02 appointment detail"),
        ("POST", "api/PatientAppointment/RescheduleAppointment", W | P, "PAT-21.02 reschedule"),
        ("POST", "api/PatientAppointment/CancelAppointment", W | P, "PAT-22.02 cancel"),
        ("GET", "api/Refunds/Policy/{paymentOrderId}", W | P, "PAT-22.02 refund policy"),
        ("POST", "api/Waitlist/Join", W | P, "PAT-23.02 waitlist"),

        // Tele consultation
        ("POST", "api/Tele/Instant", W | P, "PAT-24.02 instant consult"),
        ("GET", "api/Tele/Instant/{requestId}", W | P, "PAT-25.02 instant consult status"),
        ("POST", "api/Tele/Instant/{requestId}/Cancel", W | P, "PAT-25.02 instant consult cancel"),
        ("GET", "api/Tele/DeviceCheck", W | P, "PAT-26.02 device check"),
        ("GET", "api/Tele/Sessions/{sessionId}", W | P | D, "PAT-27.02 waiting room"),
        ("POST", "api/Tele/Sessions/{sessionId}/Token", W | P | D, "PAT-28.02 / DMO-08.02 video token"),
        ("POST", "api/Tele/Sessions/{sessionId}/Rejoin", W | P | D, "PAT-33.02 / DMO-08.02 rejoin"),
        ("POST", "api/Tele/Sessions/{sessionId}/JoinFailure", W | P, "PAT-30.02 join failure"),
        ("POST", "api/Tele/Consent", W | P, "PAT-17.02 / PAT-29.02 consent"),
        ("POST", "api/Tele/Chat", W | P, "PAT-31.02 chat"),
        ("GET", "api/Tele/Chat/{sessionId}", W | P, "PAT-31.02 chat"),
        ("GET", "api/Tele/Summary/{patientAppId}", W | P, "PAT-20.02 / PAT-32.02 summary"),

        // Records and medicines
        ("GET", "api/Patient/Timeline", W | P, "PAT-34.02 records timeline"),
        ("GET", "api/Patient/Consultations/{patientAppId}/Note", W | P, "PAT-35.02 consultation note"),
        ("GET", "api/Erx/Patient/{id}", W | P, "PAT-36.02 / PAT-41.02 prescription"),
        ("GET", "api/Erx/{id}/Pdf", W | P, "PAT-36.02 prescription PDF"),
        ("POST", "api/Patient/Documents", W | P, "PAT-37.02 documents"),
        ("GET", "api/Patient/Documents", W | P, "PAT-37.02 documents"),
        ("GET", "api/Patient/Documents/{id}", W | P, "PAT-37.02 documents"),
        ("DELETE", "api/Patient/Documents/{id}", W | P, "PAT-37.02 documents"),
        ("GET", "api/Patient/FollowUps", W | P, "PAT-38.02 follow-ups"),
        ("POST", "api/Patient/FollowUps", W | P, "PAT-38.02 follow-ups"),
        ("POST", "api/Patient/FollowUps/{taskId}/Complete", W | P, "PAT-38.02 follow-ups"),
        ("GET", "api/Patient/Diary", W | P, "PAT-39.02 symptom diary"),
        ("POST", "api/Patient/Diary", W | P, "PAT-39.02 symptom diary"),
        ("PUT", "api/Patient/Diary/{id}", W | P, "PAT-39.02 symptom diary"),
        ("DELETE", "api/Patient/Diary/{id}", W | P, "PAT-39.02 symptom diary"),
        ("GET", "api/Patient/Progress", W | P, "PAT-40.02 progress"),
        ("GET", "api/Patient/MedicineOrders", P, "PAT-41.02 medicines tab (pharmacy role gets its queue)"),
        ("POST", "api/MedicineOrders", W | P, "PAT-42.02 order start"),
        ("GET", "api/Pharmacy/Sellers", W | P, "PAT-43.02 pharmacy selection"),
        ("POST", "api/MedicineOrders/{id}/AcceptQuote", W | P, "PAT-43.02 quote"),
        ("POST", "api/MedicineOrders/{id}/Consent", W | P, "PAT-44.02 order consent"),
        ("POST", "api/Payments/MedicineOrders", W | P, "PAT-44.02 medicine payment"),
        ("GET", "api/MedicineOrders/{id}/Tracking", W | P, "PAT-45.02 tracking"),
        ("POST", "api/Erx/Refills", W | P, "PAT-46.02 refill request"),
        ("POST", "api/MedicineOrders/Refill/{refillId}", W | P, "PAT-46.02 refill order"),

        // Doctor practice
        ("POST", "api/users/RegisterDoctorWithDocuments", W | D, "Doctor registration"),
        ("POST", "api/users/RegisterDoctor", W | D, "Doctor registration"),
        ("GET", "api/registration/qualifications", W | D, "Doctor registration"),
        ("GET", "api/Profile/Me", W | D, "Doctor profile"),
        ("PUT", "api/Profile/Me", W | D, "Doctor profile"),
        ("POST", "api/Profile/Me/Photo", W | D, "Doctor profile photo"),
        ("DELETE", "api/Profile/Me/Photo", W | D, "Doctor profile photo"),
        ("GET", "api/Profile/Me/Credentials", W | D, "Doctor credentials"),
        ("POST", "api/Profile/Me/CredentialDocuments", W | D, "Doctor credentials"),
        ("PUT", "api/Profile/Me/CredentialDocuments/{id}", W | D, "Doctor credentials"),
        ("DELETE", "api/Profile/Me/CredentialDocuments/{id}", W | D, "Doctor credentials"),
        ("GET", "api/Profile/CredentialDocuments/{id}/File", W | D, "Doctor credentials"),
        ("GET", "api/PatientAppointment/Queue", W | D, "DMO-04.02 today's queue"),
        ("POST", "api/PatientAppointment/CallNext", W | D, "DMO-04.02 queue"),
        ("GET", "api/PatientAppointment/GetAppointmentsByDate", W | D, "DMO-04.02 schedule"),
        ("GET", "api/PatientAppointment/GetDailySchedule", W | D, "DMO-04.02 schedule"),
        ("GET", "api/Tele/Queue", W | D, "DMO-04.02 tele queue"),
        ("POST", "api/Tele/Availability", W | D, "DMO-05.02 online toggle"),
        ("GET", "api/Availability/Me", W | D, "DMO-05.02 working hours"),
        ("PUT", "api/Availability/Me", W | D, "DMO-05.02 working hours"),
        ("GET", "api/DoctorMobile/Context/{patientAppId}", W | D, "DMO-07.02 patient context card"),
        ("GET", "api/Erx/Refills", W | D, "DMO-09.02 refills"),
        ("POST", "api/Erx/Refills/{id}/Approve", W | D, "DMO-09.02 refills"),
        ("POST", "api/Erx/Refills/{id}/Reject", W | D, "DMO-09.02 refills"),
        ("GET", "api/Earnings/Summary", W | D, "DMO-10.02 earnings"),
    };

    private static readonly Dictionary<string, (ApiClients Clients, string Use)> Index =
        Shared.ToDictionary(s => Key(s.Method, s.Path), s => (s.Clients, s.Use), StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<(string Method, string Path, ApiClients Clients, string Use)> SharedEndpoints => Shared;

    public static ApiAudience Classify(ApiDescription api)
    {
        if (api.ActionDescriptor is ControllerActionDescriptor action)
        {
            var marker = action.ControllerTypeInfo
                .GetCustomAttributes(typeof(ApiAudienceAttribute), true)
                .OfType<ApiAudienceAttribute>()
                .FirstOrDefault();
            if (marker != null)
                return marker.Audience;
        }
        return Index.ContainsKey(Key(api.HttpMethod, api.RelativePath)) ? ApiAudience.Common : ApiAudience.Web;
    }

    public static ApiClients ClientsOf(ApiDescription api)
    {
        return Classify(api) switch
        {
            ApiAudience.MobilePatient => ApiClients.PatientApp,
            ApiAudience.MobileDoctor => ApiClients.DoctorApp,
            ApiAudience.Common => Index[Key(api.HttpMethod, api.RelativePath)].Clients,
            _ => ApiClients.Web
        };
    }

    public static string? UseOf(ApiDescription api)
        => Index.TryGetValue(Key(api.HttpMethod, api.RelativePath), out var row) ? row.Use : null;

    public static LegacyRouteAttribute? LegacyAliasOf(ApiDescription api)
        => api.ActionDescriptor.EndpointMetadata
            .OfType<LegacyRouteAttribute>()
            .FirstOrDefault(l => l.Matches(api.RelativePath));

    public static string Describe(ApiClients clients)
    {
        var names = new List<string>();
        if (clients.HasFlag(ApiClients.Web)) names.Add("Web");
        if (clients.HasFlag(ApiClients.PatientApp)) names.Add("Patient Mobile App");
        if (clients.HasFlag(ApiClients.DoctorApp)) names.Add("Doctor Mobile App");
        return string.Join(", ", names);
    }

    private static string Key(string? method, string? path)
        => (method ?? "").ToUpperInvariant() + " " + (path ?? "").Trim('/');
}

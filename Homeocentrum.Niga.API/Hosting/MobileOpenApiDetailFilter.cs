using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Homeocentrum.Niga.API.Hosting;

/// <summary>
/// Adds the deployed host and fictitious request/response samples for the mobile clinic routes
/// so the OpenAPI download can be imported and used for development.
/// </summary>
public sealed class MobileOpenApiDetailFilter : IOperationFilter, IDocumentFilter
{
    public const string DeployedBase = "https://devapi.homeocentrum.com";

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Servers ??= new List<OpenApiServer>();
        if (swaggerDoc.Servers.Count == 0)
        {
            swaggerDoc.Servers.Add(new OpenApiServer
            {
                Url = DeployedBase,
                Description = "Deployed New API. Paths start with /api."
            });
        }
    }

    public static void ApplyServers(OpenApiDocument document, HttpRequest request)
    {
        var host = request.Host.Value ?? "";
        var here = string.IsNullOrEmpty(host) ? DeployedBase : $"{request.Scheme}://{host}";
        var onDeployed = host.Contains("devapi.homeocentrum.com", StringComparison.OrdinalIgnoreCase);
        document.Servers = new List<OpenApiServer>
        {
            new()
            {
                Url = DeployedBase,
                Description = "Deployed New API. Mobile apps and other clients call this host. Paths start with /api."
            }
        };
        if (!onDeployed && !string.Equals(here.TrimEnd('/'), DeployedBase, StringComparison.OrdinalIgnoreCase))
        {
            document.Servers.Add(new OpenApiServer
            {
                Url = here,
                Description = "The host that served this file. Mobile development uses the deployed server above."
            });
        }
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var api = context.ApiDescription;
        var key = (api.HttpMethod ?? "").ToUpperInvariant() + " " + (api.RelativePath ?? "").Trim('/');
        if (!Details.TryGetValue(key, out var detail))
            return;

        operation.Summary = detail.Summary;
        var body = (operation.Description ?? "").Trim();
        if (!body.Contains("Sample values are fictitious", StringComparison.Ordinal))
            operation.Description = detail.Notes + " Sample values are fictitious. " + body;

        if (detail.Request != null)
            SetRequestExample(operation, detail.Request);
        if (detail.Response != null)
            SetResponseExample(operation, detail.Response);
    }

    private static void SetRequestExample(OpenApiOperation operation, string json)
    {
        if (operation.RequestBody?.Content == null)
            return;
        foreach (var media in operation.RequestBody.Content)
        {
            if (!media.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
                continue;
            media.Value.Example ??= Parse(json);
        }
    }

    private static void SetResponseExample(OpenApiOperation operation, string json)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Success. The example is a fictitious sample of the JSON body.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType { Example = Parse(json) }
            }
        };
    }

    private static JsonNode? Parse(string json) => JsonNode.Parse(json);

    private sealed record RouteDetail(string Summary, string Notes, string? Request, string? Response);

    private static readonly Dictionary<string, RouteDetail> Details = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GET api/Public/AppConfig"] = new(
            "Public app settings",
            "No token. razorpayKeyId is the public key id from configuration, or null. siteUrl, supportPhone, supportWhatsApp, and supportEmail are null until those settings are stored. Same body as GET /api/App/Config.",
            null,
            """{"success":true,"data":{"razorpayKeyId":null,"siteUrl":"https://devapi.homeocentrum.com","supportPhone":null,"supportWhatsApp":null,"supportEmail":null}}"""),
        ["GET api/App/Config"] = new(
            "Public app settings",
            "Alias of GET /api/Public/AppConfig. No token.",
            null,
            """{"success":true,"data":{"razorpayKeyId":null,"siteUrl":"https://devapi.homeocentrum.com","supportPhone":null,"supportWhatsApp":null,"supportEmail":null}}"""),
        ["GET api/MobileDoctor/Registration/CheckAvailability"] = new(
            "Check username and email before registration",
            "No token. Query userName and emailId; either may be omitted. The matching side is null when that query value was omitted. 400 when both are omitted.",
            null,
            """{"success":true,"data":{"userNameAvailable":true,"emailAvailable":null}}"""),
        ["GET api/Doctor/PracticeStats"] = new(
            "Practice counts for the signed-in doctor",
            "Doctor token. Counts come from stored visits, reviews, and signed notes. yearsExperience, punctuality, and punctualityPct are null. completionRate, attendedPct, and completionPct are the same ratio, or null when there are no completed or cancelled visits. Same body as GET /api/Doctor/Insights.",
            null,
            """{"success":true,"data":{"doctorId":104,"averageRating":null,"reviewCount":0,"completedVisits":28,"cancelledVisits":9,"completionRate":0.757,"signedNoteCount":12,"accountAgeDays":24,"yearsExperience":null,"punctuality":null,"lifetimeConsultCount":28,"attendedPct":0.757,"punctualityPct":null,"completionPct":0.757,"noteSigningPct":0.324}}"""),
        ["GET api/Doctor/Insights"] = new(
            "Practice counts for the signed-in doctor",
            "Doctor token. Same body as GET /api/Doctor/PracticeStats. yearsExperience and punctuality stay null.",
            null,
            """{"success":true,"data":{"doctorId":104,"averageRating":null,"reviewCount":0,"completedVisits":28,"cancelledVisits":9,"completionRate":0.757,"signedNoteCount":12,"accountAgeDays":24,"yearsExperience":null,"punctuality":null,"lifetimeConsultCount":28,"attendedPct":0.757,"punctualityPct":null,"completionPct":0.757,"noteSigningPct":0.324}}"""),
        ["GET api/Doctor/FollowUps"] = new(
            "Follow-up tasks for the signed-in doctor",
            "Doctor token. Query status (for example OPEN), from, and to. Dates are yyyy-MM-dd. Omitted filters are not applied.",
            null,
            """{"success":true,"data":[{"followUpTaskId":2,"title":"Review cough after 7 days","dueDate":"2026-10-21T00:00:00","status":"OPEN","patientId":5012,"patientAppId":88120,"patientName":"Asha Kulkarni"}]}"""),
        ["GET api/Doctor/Activity"] = new(
            "Latest 30 appointments for the signed-in doctor",
            "Doctor token. kind is the lowercase word appointment. Ordered newest first.",
            null,
            """{"success":true,"data":[{"kind":"appointment","at":"2026-10-15T11:00:00","patientAppId":88120,"patientId":5012,"patientName":"Asha Kulkarni","status":"WAITING","consultMode":"Tele","paymentStatus":"UNPAID"}]}"""),
        ["GET api/PatientAppointment/GetAppointmentsByDateRange"] = new(
            "Visits between two dates for the token user",
            "Doctor token. Query from and to, yyyy-MM-dd. The range cannot exceed 62 days. Rows are limited to PatientAppointment.UserId of the token. consultFee is the stored tele or in-clinic fee. currency is the doctor fee currency, or null.",
            null,
            """{"success":true,"data":[{"patientAppId":88120,"patientId":5012,"patientName":"Asha Kulkarni","mobileNo":null,"doctorId":104,"appointmentDate":"2026-10-15T11:00:00","appointmentTime":"11:00","status":"WAITING","visitType":null,"consultMode":"Tele","isTele":true,"paymentStatus":"UNPAID","bookingChannel":null,"consultFee":500,"currency":null}]}"""),
        ["GET api/Doctor/TimeOff"] = new(
            "Time off for the signed-in doctor",
            "Doctor token. Same list as GET /api/Availability/TimeOff.",
            null,
            """{"success":true,"data":[{"doctorTimeOffId":1,"startsAt":"2026-12-02T09:00:00","endsAt":"2026-12-02T13:00:00","reason":"Camp"}]}"""),
        ["GET api/Availability/TimeOff"] = new(
            "Time off for the signed-in doctor",
            "Alias of GET /api/Doctor/TimeOff.",
            null,
            """{"success":true,"data":[{"doctorTimeOffId":1,"startsAt":"2026-12-02T09:00:00","endsAt":"2026-12-02T13:00:00","reason":"Camp"}]}"""),
        ["POST api/Doctor/TimeOff"] = new(
            "Save a time-off block",
            "Doctor token. Same body on POST /api/Availability/TimeOff. The response is the doctor's full time-off list.",
            """{"startsAt":"2026-12-01T09:00:00","endsAt":"2026-12-01T13:00:00","reason":"Camp"}""",
            """{"success":true,"data":[{"doctorTimeOffId":1,"startsAt":"2026-12-01T09:00:00","endsAt":"2026-12-01T13:00:00","reason":"Camp"}]}"""),
        ["POST api/Availability/TimeOff"] = new(
            "Save a time-off block",
            "Alias of POST /api/Doctor/TimeOff.",
            """{"startsAt":"2026-12-01T09:00:00","endsAt":"2026-12-01T13:00:00","reason":"Camp"}""",
            """{"success":true,"data":[{"doctorTimeOffId":1,"startsAt":"2026-12-01T09:00:00","endsAt":"2026-12-01T13:00:00","reason":"Camp"}]}"""),
        ["DELETE api/Doctor/TimeOff/{id}"] = new(
            "Delete one time-off row",
            "Doctor token. The id must belong to this doctor. Same on DELETE /api/Availability/TimeOff/{id}.",
            null,
            """{"success":true,"data":{"deleted":true}}"""),
        ["DELETE api/Availability/TimeOff/{id}"] = new(
            "Delete one time-off row",
            "Alias of DELETE /api/Doctor/TimeOff/{id}.",
            null,
            """{"success":true,"data":{"deleted":true}}"""),
        ["GET api/Doctor/BookingRules"] = new(
            "Slot gap and booking horizon",
            "Doctor token. Both values are null until the doctor saves them.",
            null,
            """{"success":true,"data":{"gapMinutes":10,"bookingHorizonDays":14}}"""),
        ["PUT api/Doctor/BookingRules"] = new(
            "Save slot gap and booking horizon",
            "Doctor token. Query gapMinutes (0 to 240) and bookingHorizonDays (1 to 90). There is no JSON body.",
            null,
            """{"success":true,"data":{"gapMinutes":10,"bookingHorizonDays":14}}"""),
        ["GET api/Doctor/AppointmentRequests"] = new(
            "Appointment requests for the signed-in doctor",
            "Doctor token. Query status, for example PENDING. Omit it to return every status.",
            null,
            """{"success":true,"data":[{"appointmentRequestId":1,"patientId":5012,"patientName":"Asha Kulkarni","requestedDate":"2026-10-21T00:00:00","requestedTime":"10:30","consultMode":"Tele","status":"PENDING","reason":"Fever for two days","proposedDate":null,"proposedTime":null,"patientAppId":null,"createdAt":"2026-10-15T10:00:00"}]}"""),
        ["POST api/Doctor/AppointmentRequests/{id}/Accept"] = new(
            "Accept a request and create the visit",
            "Doctor token. No body. The visit is created as NOT ARRIVED, payment UNPAID, booking channel Request. patientAppId is the new visit.",
            null,
            """{"success":true,"data":{"status":"ACCEPTED","patientAppId":88121}}"""),
        ["POST api/Doctor/AppointmentRequests/{id}/Decline"] = new(
            "Decline an open request",
            "Doctor token. reason may be null.",
            """{"reason":"I am in a camp that day."}""",
            """{"success":true,"data":{"status":"DECLINED"}}"""),
        ["POST api/Doctor/AppointmentRequests/{id}/Propose"] = new(
            "Propose another date and time",
            "Doctor token. proposedDate is a date. proposedTime is HH:mm.",
            """{"proposedDate":"2026-10-21","proposedTime":"11:00"}""",
            """{"success":true,"data":{"status":"PROPOSED"}}"""),
        ["GET api/Doctor/CareLinks"] = new(
            "Care-link requests for the signed-in doctor",
            "Doctor token. Sender or receiver. Same list as GET /api/CareLink. feeInr equals the stored feeAmount. expiry is always null.",
            null,
            """{"success":true,"data":[{"careLinkRequestId":1,"fromDoctorId":104,"toDoctorId":88,"patientId":5012,"patientName":"Asha Kulkarni","note":"Please see this child.","feeAmount":null,"feeInr":null,"opinion":null,"expiry":null,"status":"OPEN","createdAt":"2026-10-15T10:00:00"}]}"""),
        ["GET api/CareLink"] = new(
            "Care-link requests for the signed-in doctor",
            "Alias of GET /api/Doctor/CareLinks.",
            null,
            """{"success":true,"data":[{"careLinkRequestId":1,"fromDoctorId":104,"toDoctorId":88,"patientId":5012,"patientName":"Asha Kulkarni","note":"Please see this child.","feeAmount":null,"feeInr":null,"opinion":null,"expiry":null,"status":"OPEN","createdAt":"2026-10-15T10:00:00"}]}"""),
        ["POST api/Doctor/CareLinks"] = new(
            "Create a care-link request",
            "Doctor token. Same body on POST /api/CareLink. feeAmount is stored only when the sender sends it.",
            """{"toDoctorId":88,"patientId":5012,"note":"Please see this child.","feeAmount":null}""",
            """{"success":true,"data":{"status":"OPEN"}}"""),
        ["POST api/CareLink"] = new(
            "Create a care-link request",
            "Alias of POST /api/Doctor/CareLinks.",
            """{"toDoctorId":88,"patientId":5012,"note":"Please see this child.","feeAmount":null}""",
            """{"success":true,"data":{"status":"OPEN"}}"""),
        ["POST api/Doctor/CareLinks/{id}/Respond"] = new(
            "Accept or decline a care link",
            "Receiving doctor, while status is OPEN. status is ACCEPTED or DECLINED. Same path under /api/CareLink/{id}/Respond.",
            """{"status":"ACCEPTED"}""",
            """{"success":true,"data":{"status":"ACCEPTED"}}"""),
        ["POST api/CareLink/{id}/Respond"] = new(
            "Accept or decline a care link",
            "Alias of POST /api/Doctor/CareLinks/{id}/Respond.",
            """{"status":"ACCEPTED"}""",
            """{"success":true,"data":{"status":"ACCEPTED"}}"""),
        ["POST api/Doctor/CareLinks/{id}/Opinion"] = new(
            "Save a care-link opinion",
            "Sender or receiver. Same path under /api/CareLink/{id}/Opinion.",
            """{"opinion":"I agree with the current remedy."}""",
            """{"success":true,"data":{"careLinkRequestId":1,"opinion":"I agree with the current remedy."}}"""),
        ["POST api/CareLink/{id}/Opinion"] = new(
            "Save a care-link opinion",
            "Alias of POST /api/Doctor/CareLinks/{id}/Opinion.",
            """{"opinion":"I agree with the current remedy."}""",
            """{"success":true,"data":{"careLinkRequestId":1,"opinion":"I agree with the current remedy."}}"""),
        ["GET api/Doctor/PharmacyQueries"] = new(
            "Pharmacy questions for the signed-in doctor",
            "Doctor token. Same list as GET /api/PharmacyQuery. reply is null until answered.",
            null,
            """{"success":true,"data":[{"pharmacyQueryId":1,"patientId":5012,"question":"Is the 30C pack in stock?","reply":null,"status":"OPEN","createdAt":"2026-10-15T10:00:00"}]}"""),
        ["GET api/PharmacyQuery"] = new(
            "Pharmacy questions for the signed-in doctor",
            "Alias of GET /api/Doctor/PharmacyQueries.",
            null,
            """{"success":true,"data":[{"pharmacyQueryId":1,"patientId":5012,"question":"Is the 30C pack in stock?","reply":null,"status":"OPEN","createdAt":"2026-10-15T10:00:00"}]}"""),
        ["POST api/Doctor/PharmacyQueries"] = new(
            "Ask a pharmacy question",
            "Doctor token. Same body on POST /api/PharmacyQuery.",
            """{"patientId":5012,"question":"Is the 30C pack in stock?"}""",
            """{"success":true,"data":{"status":"OPEN"}}"""),
        ["POST api/PharmacyQuery"] = new(
            "Ask a pharmacy question",
            "Alias of POST /api/Doctor/PharmacyQueries.",
            """{"patientId":5012,"question":"Is the 30C pack in stock?"}""",
            """{"success":true,"data":{"status":"OPEN"}}"""),
        ["POST api/Doctor/PharmacyQueries/{id}/Reply"] = new(
            "Reply to a pharmacy question",
            "Doctor token. Same path under /api/PharmacyQuery/{id}/Reply.",
            """{"reply":"Yes, two bottles."}""",
            """{"success":true,"data":{"status":"REPLIED"}}"""),
        ["POST api/PharmacyQuery/{id}/Reply"] = new(
            "Reply to a pharmacy question",
            "Alias of POST /api/Doctor/PharmacyQueries/{id}/Reply.",
            """{"reply":"Yes, two bottles."}""",
            """{"success":true,"data":{"status":"REPLIED"}}"""),
        ["PUT api/Erx/Draft"] = new(
            "Save an eRx draft",
            "Doctor token. Writes remedy lines and notes. remedyId must exist. potencyCode is optional here. Sign still requires a potency on every line. An empty items list returns 400. A signed prescription returns 409.",
            """{"patientAppId":88120,"notes":"Review in one week","items":[{"remedyId":4401,"potencyCode":"30C","dose":"4 globules","frequency":"BD","duration":"3 days","instructions":"Away from food"}]}""",
            """{"success":true,"data":{"patientAppId":88120,"status":"DRAFT","itemCount":1}}"""),
        ["GET api/Notifications/Preferences"] = new(
            "Notification switches for the signed-in user",
            "Doctor or patient token. Before the first save: push, appointments, prescriptions, and medicines are true; sms, email, and marketing are false.",
            null,
            """{"success":true,"data":{"pushEnabled":true,"smsEnabled":false,"emailEnabled":false,"quietStart":null,"quietEnd":null,"appointments":true,"prescriptions":true,"medicines":true,"marketing":false,"quietHoursEnabled":false,"reminderOffsets":null}}"""),
        ["PUT api/Notifications/Preferences"] = new(
            "Save notification switches",
            "Doctor or patient token. quietStart and quietEnd are times. reminderOffsets is a comma-separated list of minutes, or null.",
            """{"pushEnabled":true,"smsEnabled":false,"emailEnabled":false,"quietStart":"22:00:00","quietEnd":"07:00:00","appointments":true,"prescriptions":true,"medicines":true,"marketing":false,"quietHoursEnabled":true,"reminderOffsets":"60,1440"}""",
            """{"success":true,"data":{"pushEnabled":true,"smsEnabled":false,"emailEnabled":false,"quietStart":"22:00:00","quietEnd":"07:00:00","appointments":true,"prescriptions":true,"medicines":true,"marketing":false,"quietHoursEnabled":true,"reminderOffsets":"60,1440"}}"""),
        ["POST api/Notifications/MarkAllRead"] = new(
            "Mark every notification read",
            "Doctor or patient token. marked is how many unread rows were updated.",
            null,
            """{"success":true,"data":{"marked":4}}"""),
        ["POST api/PatientAppointment/SwitchToTele"] = new(
            "Switch an in-clinic visit to tele",
            "Patient or the owning doctor. Send patientAppId as a query parameter or as a JSON body.",
            """{"patientAppId":88120}""",
            """{"success":true,"data":{"patientAppId":88120,"consultMode":"Tele","isTele":true}}"""),
        ["POST api/Tele/Sessions/{teleSessionId}/RunningLate"] = new(
            "Record that the caller is running late",
            "Patient or the owning doctor. Send minutes as a query parameter or as a JSON body. detail is null when minutes is omitted.",
            """{"minutes":10}""",
            """{"success":true,"data":{"teleSessionId":15,"code":"RUNNING_LATE","detail":"10 minutes"}}"""),
        ["POST api/Patient/Visits/{patientAppId}/CheckIn"] = new(
            "Check in for today's in-clinic visit",
            "Patient token. status is EARLY, WAITING, or READY. Send it as a query parameter or as a JSON body. A tele visit is rejected. readyAt is null.",
            """{"status":"WAITING"}""",
            """{"success":true,"data":{"patientAppId":88120,"status":"WAITING","queuePosition":2,"readyAt":null}}"""),
        ["GET api/Patient/Reminders"] = new(
            "Reminders for the signed-in patient",
            "Patient token. kind, timeOfDay, weekdays, and memberPatientId are null when they were not saved.",
            null,
            """{"success":true,"data":[{"patientReminderId":3,"patientId":5012,"title":"Evening dose","dueAt":"2026-10-16T20:00:00","isDone":false,"kind":"MEDICINE","timeOfDay":"20:00","weekdays":"Mon,Wed","memberPatientId":null}]}"""),
        ["POST api/Patient/Reminders"] = new(
            "Create a patient reminder",
            "Patient token. title and dueAt are required. The response is the patient's reminder list, not only the new id.",
            """{"title":"Evening dose","dueAt":"2026-10-16T20:00:00","kind":"MEDICINE","timeOfDay":"20:00","weekdays":"Mon,Wed","memberPatientId":null}""",
            """{"success":true,"data":[{"patientReminderId":3,"patientId":5012,"title":"Evening dose","dueAt":"2026-10-16T20:00:00","isDone":false,"kind":"MEDICINE","timeOfDay":"20:00","weekdays":"Mon,Wed","memberPatientId":null}]}"""),
        ["PUT api/Patient/Reminders/{id}"] = new(
            "Update a patient reminder",
            "Patient token. The id must belong to this user. The response is the reminder list.",
            """{"title":"Evening dose","dueAt":"2026-10-16T20:00:00","isDone":true,"kind":"MEDICINE","timeOfDay":"20:00","weekdays":"Mon,Wed","memberPatientId":null}""",
            """{"success":true,"data":[{"patientReminderId":3,"patientId":5012,"title":"Evening dose","dueAt":"2026-10-16T20:00:00","isDone":true,"kind":"MEDICINE","timeOfDay":"20:00","weekdays":"Mon,Wed","memberPatientId":null}]}"""),
        ["DELETE api/Patient/Reminders/{id}"] = new(
            "Delete a patient reminder",
            "Patient token. The id must belong to this user.",
            null,
            """{"success":true,"data":{"deleted":true}}"""),
        ["POST api/Patient/AppointmentRequests"] = new(
            "Ask a doctor for a visit",
            "Patient token. This does not create the appointment. The visit is created when the doctor accepts.",
            """{"doctorId":104,"requestedDate":"2026-10-20","requestedTime":"10:30","consultMode":"Tele","reason":"Fever for two days"}""",
            """{"success":true,"data":{"status":"PENDING"}}"""),
        ["GET api/Doctor/Reminders"] = new(
            "Doctor reminders between two dates",
            "Doctor token. Query from and to, yyyy-MM-dd. When omitted, both default to today. The range cannot exceed 62 days. contactNumber is null when it was not saved.",
            null,
            """{"success":true,"from":"2026-10-20T00:00:00","to":"2026-10-20T00:00:00","data":[{"doctorReminderId":4,"reminderDate":"2026-10-20T00:00:00","reminderTime":"09:30:00","title":"Call the lab","description":"Ask for the last report","contactNumber":null,"isDone":false,"createdAt":"2026-10-15T10:00:00"}]}"""),
        ["POST api/Doctor/Reminders"] = new(
            "Create a doctor reminder",
            "Doctor token. title is required. reminderTime is HH:mm, or null for an all-day reminder. contactNumber is optional.",
            """{"reminderDate":"2026-10-20","reminderTime":"09:30","title":"Call the lab","description":"Ask for the last report","contactNumber":null,"isDone":false}""",
            """{"success":true,"doctorReminderId":4}"""),
        ["PUT api/Doctor/Reminders/{id}"] = new(
            "Update a doctor reminder",
            "Doctor token. The id must belong to this doctor.",
            """{"reminderDate":"2026-10-20","reminderTime":"09:30","title":"Call the lab","description":"Ask for the last report","contactNumber":null,"isDone":true}""",
            """{"success":true,"doctorReminderId":4}"""),
        ["DELETE api/Doctor/Reminders/{id}"] = new(
            "Delete a doctor reminder",
            "Doctor token. The row is marked deleted.",
            null,
            """{"success":true,"doctorReminderId":4}"""),
        ["GET api/Earnings/Summary"] = new(
            "Earnings summary for the signed-in doctor",
            "Doctor token. unpaidReceivableAmount is the stored consult fee of visits that are not cancelled and not paid. It is separate from pendingPayoutAmount. currency is null when neither a payment nor the doctor fee currency is stored.",
            null,
            null),
        ["GET api/Profile/Me"] = new(
            "Signed-in doctor profile",
            "Doctor token. photoUrl is an absolute URL when a photo is stored, otherwise null. registrationNo repeats the stored certificate number. registrationValidUntil, issuingCouncil, and yearsExperience are null.",
            null,
            null),
        ["GET api/Profile/Me/Credentials"] = new(
            "Credential documents for the signed-in doctor",
            "Doctor token. Each document includes validUntil, verified, and issuingCouncil. Those three are null. Download the file from downloadUrl.",
            null,
            null),
        ["GET api/Patient/Visits"] = new(
            "Visits for the signed-in patient",
            "Patient token. Each item includes consultFee and currency from the doctor's stored fee, plus clinicPhone and clinicAddress when stored. reasonText is null on this list.",
            null,
            null),
        ["GET api/Reviews/Mine"] = new(
            "Reviews for the signed-in user",
            "A patient token returns that patient's reviews, including doctorName and createdAt. A doctor token returns reviews of that doctor.",
            null,
            null),
        ["GET api/Pharmacy/Sellers"] = new(
            "Pharmacy sellers",
            "priceTotal, deliveryFee, etaMinutes, fulfilPercent, distanceKm, and serviceable are null. Those facts are not stored.",
            null,
            null),
        ["GET api/Patient/DataRequests"] = new(
            "Data requests for the signed-in patient",
            "Patient token. Lists EXPORT and DELETE requests already recorded. A request is not executed automatically. There is no account-delete route.",
            null,
            """{"success":true,"data":[{"dataRequestId":12,"requestType":"EXPORT","status":"OPEN","createdAt":"2026-10-15T18:00:00"}]}"""),
        ["POST api/Tele/Instant/{requestId}/Accept"] = new(
            "Accept an instant consult and open the visit",
            "Doctor token. Creates the patient's visit and a waiting-room session. patientAppId and teleSessionId are the stored ids. A request with no patient record is rejected. Calling again returns the same ids.",
            null,
            """{"success":true,"instantConsultRequestId":18,"doctorOfferId":7,"status":"ACCEPTED","patientAppId":88120,"teleSessionId":44,"roomId":"room-44"}"""),
        ["GET api/Tele/Instant/{requestId}"] = new(
            "Instant consult status",
            "Patient token. After the doctor accepts, patientAppId and teleSessionId are set. Until then both are null.",
            null,
            """{"success":true,"data":{"instantConsultRequestId":18,"status":"ACCEPTED","queuePosition":null,"patientAppId":88120,"teleSessionId":44}}"""),
        ["GET api/Availability/Me"] = new(
            "Working hours for the signed-in doctor",
            "Doctor token. gapMinutes and bookingHorizonDays are included. Both are null until saved with PUT /api/Doctor/BookingRules.",
            null,
            null)
    };
}

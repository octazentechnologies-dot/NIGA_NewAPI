# Patient / Doctor App — Clinic patient roster + registration channel

**Backend status:** DONE (New-API). Recycle API after deploy. SQL `14_ClinicPatientRoster_And_RegistrationChannel.sql` (same as formerly `14_DoctorPatient_CareLink_And_RegistrationChannel.sql`) applied on Dev/Stage/Prod.

**Naming:** This is the doctor’s **clinic patient roster** (`CaseEntryDetail` → All Patients / New Appointment dropdown). It is **not** product **CareLink** (doctor-to-doctor second opinion).

## Problem fixed (API)

Doctor **All Patients** (`GET api/patient/getAllCases`) only lists patients with a `CaseEntryDetail` for that doctor.

- Doctor/Reception **Create patient** already created a case → visible.
- Patient App **book** only created `PatientAppointment` → **invisible**.

API now calls `ClinicPatientRoster.EnsureCase` on:

- Public / Patient App booking (`BookingChannel=PatientApp`)
- Clinic `SavePatientApp` (any booking channel)
- Appointment status → **Completed**

Historical rows backfilled from appointments.

## Registration / onboarding channel (stored)

| Column | Tables |
|--------|--------|
| `RegistrationChannel` NVARCHAR(40) | `UserMaster`, `Patient`, `Doctor` |

### Canonical values

| Value | Meaning |
|-------|---------|
| `PatientApp` | Patient mobile app |
| `DoctorApp` | Doctor mobile app |
| `WebPortal` | NIGA web UI |
| `Reception` | Reception desk flows |
| `PublicBooking` | Guest public book (if distinct) |
| `Admin` | Admin-created |
| `Staff` | Generic staff booking |

Doctor list returns:

- `registrationChannel`
- `registrationChannelLabel` (e.g. “Patient App”)

## Patient App — required

1. On **patient signup / profile create** (`POST api/patient` or account APIs): send  
   `entityType: "PatientMobile"` **or** `registrationChannel: "PatientApp"`.
2. Prefer header on all auth/register calls:  
   `X-Client-Platform: PatientApp`
3. Booking already uses public Slots/Bookings — clinic patient roster is server-side; **no Patient App change required** for doctor list visibility after book.
4. Do **not** invent doctor–patient roster rows client-side.

## Doctor App — required

1. On **create patient** (`POST api/patient`): send `entityType: "DoctorMobile"` (maps to `DoctorApp`).
2. On **doctor registration**: send `X-Client-Platform: DoctorApp` (or registrationChannel when API accepts it).
3. Patients tab (`getAllCases`) should show `registrationChannelLabel` badge when present (same as web).

## Acceptance

- [ ] Mobile patient registers → books Doctor A → appears in Doctor A All Patients (web + Doctor App) without killing apps.
- [ ] New patients show registration channel badge (Patient App / Doctor App / Web Portal).
- [ ] Doctor B does not see Doctor A–only patients.

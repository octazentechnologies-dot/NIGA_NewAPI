# Environment-specific / out-of-S1–S6 objects

## Dev-only tables not created by S1–S6

These exist on `HomeoCentrum_Dev` and not on `HomeoCentrum_Stage`. Source script is **outside** week folders:

`ScriptsAndFiles/Mobile/01_Mobile_Clinic_Tables.sql`

| Table | Classification |
|-------|----------------|
| dbo.UserNotificationPreference | Mobile / Dev-only unless Mobile package approved for Stage |
| dbo.PatientReminder | Mobile |
| dbo.DoctorTimeOff | Mobile |
| dbo.AppointmentRequest | Mobile |
| dbo.PharmacyQuery | Mobile |
| dbo.DoctorBookingRule | Mobile |
| dbo.DoctorCareCategory | Mobile (if present in Mobile script set) |
| dbo.PatientDeliveryAddress | Mobile |
| dbo.PublicBookingIdempotency | Mobile |

**Do not** invent Missing_Schema CREATE scripts that blindly copy Dev → Stage.

## S6 Dev-only (required on Stage via canonical S6)

| Table | Canonical script |
|-------|------------------|
| dbo.SmsOtpVerify | S6 `01_SmsOtpVerify_And_FirebaseNotificationLog.sql` |
| dbo.FirebaseNotificationLog | S6 `01_...` |
| dbo.CareLinkRequest | S6 `03_DoctorApp_Gap_Schema.sql` |
| dbo.CareLinkInfoRequest | S6 `03_...` |

## LOCAL_ONLY

| Script | Note |
|--------|------|
| S5 `10_Security_Test_Tenant_LOCAL_ONLY.sql` | Optional; not for Stage without explicit approval |

# 05 — Dev vs Stage Schema Comparison

**Date:** 2026-10-20

## Object counts

| Kind | Dev | Stage | Only Dev | Only Stage |
|------|----:|------:|---------:|-----------:|
| TABLE | 289 | 276 | 13 | 0 |
| VIEW | 1 | 1 | 0 | 0 |
| PROC | 41 | 41 | 0 | 0 |
| FN | 1 | 1 | 0 | 0 |
| TRIGGER | 4 | 4 | 0 | 0 |

## Only-Dev tables (13)

### Required by S6 (Stage PENDING)

- `dbo.SmsOtpVerify`  
- `dbo.FirebaseNotificationLog`  
- `dbo.CareLinkRequest`  
- `dbo.CareLinkInfoRequest`

### From Mobile package (out of S1–S6 — BLOCKED for blind copy)

- `dbo.AppointmentRequest`, `dbo.DoctorBookingRule`, `dbo.DoctorCareCategory`, `dbo.DoctorTimeOff`  
- `dbo.PatientDeliveryAddress`, `dbo.PatientReminder`, `dbo.PharmacyQuery`  
- `dbo.PublicBookingIdempotency`, `dbo.UserNotificationPreference`  

Source: `ScriptsAndFiles/Mobile/01_Mobile_Clinic_Tables.sql`

## Column-level Stage gaps (S6)

| Column / object | Dev | Stage | Class |
|-----------------|-----|-------|-------|
| DevicePushToken.DeviceType | OK | MISSING | MISSING_IN_STAGE |
| AppNotification.Type | OK (nvarchar **80**) | MISSING | MISSING_IN_STAGE |
| AppNotification.DeepLink | OK (nvarchar **600**) | MISSING | MISSING_IN_STAGE |
| InstantConsultRequest.PatientAppId | OK | MISSING | MISSING_IN_STAGE |
| InstantConsultRequest.TeleSessionId | OK | MISSING | MISSING_IN_STAGE |
| CareLinkRequest.Opinion | OK | MISSING (table missing) | MISSING_IN_STAGE |

## Definition note

After Stage runs S6 `03`, Stage Type/DeepLink will be nvarchar(40)/(300) while Dev remains wider (80/600) → **EXPECTED_ENVIRONMENT_DIFFERENCE / DEFINITION_MISMATCH** (wider-on-Dev). Do not shrink Dev.

## Reference data

District/City/Pin row counts differ (Dev higher). Class: **EXPECTED_ENVIRONMENT_DIFFERENCE**.

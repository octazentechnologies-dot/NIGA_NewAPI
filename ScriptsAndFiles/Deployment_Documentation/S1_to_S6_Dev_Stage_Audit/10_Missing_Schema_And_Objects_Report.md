# 10 — Missing Schema And Objects Report

## Stage missing (required by S6 / InstantConsult)

| Object | Expected source | Remediation |
|--------|-----------------|-------------|
| dbo.SmsOtpVerify | S6 01 | Execute S6 01 |
| dbo.FirebaseNotificationLog | S6 01 | Execute S6 01 |
| DevicePushToken.DeviceType | S6 02 | Execute S6 02 |
| dbo.CareLinkRequest (+ Opinion) | S6 03/04 | Execute S6 03–04 |
| dbo.CareLinkInfoRequest | S6 03/04 | Execute S6 03–04 |
| AppNotification.Type / DeepLink | S6 03/04 | Execute S6 03–04 |
| InstantConsultRequest.PatientAppId / TeleSessionId | S3 12 or S6 04 | Covered by S6 04 |

## Dev missing (S1–S6)

None for required markers.

## Not classified as S1–S6 missing on Stage

Mobile-only Dev tables — **BLOCKED** without Mobile package approval. Do not generate speculative CREATE scripts.

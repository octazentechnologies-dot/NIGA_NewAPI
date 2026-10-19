# S6 Week 6 — production takeaway pack

Copy this folder to the last production / UAT environment and run the SQL scripts **in order** on `HomeoCentrum_Dev` (or prod) before deploying the matching New API build.

## Database scripts (run in order)

| # | File | Purpose |
|---|------|---------|
| 01 | `Database scripts/01_SmsOtpVerify_And_FirebaseNotificationLog.sql` | `dbo.SmsOtpVerify` (Twilio/OTP hash store) + `dbo.FirebaseNotificationLog` |
| 02 | `Database scripts/02_DevicePushToken_DeviceType.sql` | `DevicePushToken.DeviceType` (Website / Android / iOS) |
| 03 | `Database scripts/03_DoctorApp_Gap_Schema.sql` | CareLink / AppNotification gap columns for doctor mobile |
| 04 | `Database scripts/04_Verify_And_Ensure_Gap_Schema.sql` | Idempotent verify + ensure for gap schema |

All scripts are **idempotent** (safe to re-run).

### Example (sqlcmd)

```bat
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "01_SmsOtpVerify_And_FirebaseNotificationLog.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "02_DevicePushToken_DeviceType.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "03_DoctorApp_Gap_Schema.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "04_Verify_And_Ensure_Gap_Schema.sql"
```

Or run `05_Verify_S6_Objects.sql` after 01–04.

## Already covered in earlier weeks (do not re-copy for S6)

- `OtpChallenge` / `OtpAuditLog` → S1  
- `VisitType` / `ConsultMode` (M05) / Reception profile check (M06) → S3  
- Pharmacy, FCM base tables, etc. → S4 / S5  

## Deploy with this week (code / config — not SQL)

| Item | Notes |
|------|--------|
| New API build | Includes Twilio WhatsApp ContentSid, `Sms:RealSms`, patient visits `isExpired`, PatientProfile PUT, etc. |
| `web.config` | WebDAV removed so IIS allows PUT/DELETE/PATCH (required for `PUT /api/PatientProfile/Me`) |
| `appsettings.json` | `Sms:RealSms` false = SMS + `devCode`; true = production OTP (no `devCode`). Twilio / WhatsApp ContentSid settings |

## AppConfig / API notes for mobile

- `GET /api/Public/AppConfig` → `realSms`, `smsMode` (`real` \| `dummy`)  
- `GET /api/Patient/Visits` → each row has `isExpired` (true when appointment date+time is before server local now)  

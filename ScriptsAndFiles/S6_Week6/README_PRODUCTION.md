# S6 Week 6 — production takeaway pack

Copy this folder to the last production / UAT environment and run the SQL scripts **in order** on `HomeoCentrum_Dev` (or prod) before deploying the matching New API build.

## Database scripts (run in order)

| # | File | Purpose |
|---|------|---------|
| 01 | `Database scripts/01_SmsOtpVerify_And_FirebaseNotificationLog.sql` | `dbo.SmsOtpVerify` (Twilio/OTP hash store) + `dbo.FirebaseNotificationLog` |
| 02 | `Database scripts/02_DevicePushToken_DeviceType.sql` | `DevicePushToken.DeviceType` (Website / Android / iOS) |
| 03 | `Database scripts/03_DoctorApp_Gap_Schema.sql` | CareLink / AppNotification gap columns for doctor mobile |
| 04 | `Database scripts/04_Verify_And_Ensure_Gap_Schema.sql` | Idempotent verify + ensure for gap schema |
| 06 | `Database scripts/06_Runtime_Dev_Schema_Gaps.sql` | **Required for other env:** `BookedByUserId`, `ErxSnapshot.LabOrdersJson`/`Notes`, delivery address, reception vitals, public booking tables, mobile clinic tables |
| 05 | `Database scripts/05_Verify_S6_Objects.sql` | Check only — should print `PASSED` (run **after** 01–04 and 06) |

All scripts are **idempotent** (safe to re-run).

### Why script 06?

Production logs (19-Oct) showed `Invalid column name 'BookedByUserId'` on Queue / slots / dashboard, and `LabOrdersJson` missing on Patient Timeline. Those columns exist in the API entity / FromSql but were only auto-created on some booking code paths. Script 06 applies them (and the rest of the runtime Ensure* schema) before deploy.

### Example (sqlcmd)

```bat
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "01_SmsOtpVerify_And_FirebaseNotificationLog.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "02_DevicePushToken_DeviceType.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "03_DoctorApp_Gap_Schema.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "04_Verify_And_Ensure_Gap_Schema.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "06_Runtime_Dev_Schema_Gaps.sql"
sqlcmd -S YOUR_SERVER -d HomeoCentrum_Dev -U sa -P YOUR_PASSWORD -C -i "05_Verify_S6_Objects.sql"
```

## Already covered in earlier weeks (do not re-copy for S6)

- `OtpChallenge` / `OtpAuditLog` → S1  
- `VisitType` / `ConsultMode` (M05) / Reception profile check (M06) → S3  
- Pharmacy / `ErxSnapshot` **base create** (includes LabOrdersJson on new installs) → S4  
- Pharmacy extras, FCM base, menus → S4 / S5  

Script 06 still **ALTERs** `LabOrdersJson` / `Notes` when an older `ErxSnapshot` table already exists without those columns.

## Deploy with this week (code / config — not SQL)

| Item | Notes |
|------|--------|
| New API build | Includes Twilio WhatsApp ContentSid, `Sms:RealSms`, patient visits `isExpired`, PatientProfile PUT, etc. |
| `web.config` | WebDAV removed so IIS allows PUT/DELETE/PATCH (required for `PUT /api/PatientProfile/Me`) |
| `appsettings.json` | `Sms:RealSms` false = SMS + `devCode`; true = production OTP (no `devCode`). Twilio / WhatsApp ContentSid settings |

## AppConfig / API notes for mobile

- `GET /api/Public/AppConfig` → `realSms`, `smsMode` (`real` \| `dummy`)  
- `GET /api/Patient/Visits` → each row has `isExpired` (true when appointment date+time is before server local now)  

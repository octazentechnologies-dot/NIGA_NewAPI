# 04 — S1 to S6 Execution Order

## Principles

- Prefer **actual dependencies** over filename alone.  
- Never re-execute byte-identical S3 duplicates.  
- Never run DEV-only seeds on Stage.  
- S6 is additive after S5 `AppNotification` (and InstantConsult from S3).

## S1 (10)

Security foundation → menus → family/caregiver → mobile menus → DEV seeds → verify → unit tests → more seeds → clinic menus → sample insert.  
Treat seeds as DEV-scoped.

## S2 dependency baseline (validated)

`01 02 03 04 05 06 07 08 09 10 11 12 13 15 14 16 17 19 22 23 24 25 26 27 28`  
Plus discovered: `00_DOC_DoctorDailySchedule_Table.sql` **before** `01`; both `25_*` unique files.

## S3 canonical (numbered) + InstantConsult

Historical numbered list through `14_M14_SUP_05_01_Enquiry_SLA.sql`, then:

`12_InstantConsult_VisitLink.sql` (PatientAppId/TeleSessionId) — may run after InstantConsultRequest exists (S3 schema).  
Unnumbered copies = DUPLICATE_NOT_REEXECUTED.

## S4

Historical: `01` Schema → `02` Menus → `03` Notification Outbox → `04` Demo.  
Extended: `05`→`13` address + doctor profile (required for Stage login / location).

## S5

`01` Schema → `02` Menus → `03` Demo → `04` Patient address lines → `05`–`09` → (`10` LOCAL_ONLY optional) → `11`–`12` → (`13` junk roles gated) → `14` → `15` FTS.

## S6 (Stage pending)

1. `01_SmsOtpVerify_And_FirebaseNotificationLog.sql`  
2. `02_DevicePushToken_DeviceType.sql`  
3. `03_DoctorApp_Gap_Schema.sql`  
4. `04_Verify_And_Ensure_Gap_Schema.sql`  
5. `05_Verify_S6_Objects.sql`

## Missing_Schema

No additional CREATE migrations (would duplicate S6). Verification: `Missing_Schema/11_Verification/001_Verify_S6_Parity_Markers.sql` after Stage S6.

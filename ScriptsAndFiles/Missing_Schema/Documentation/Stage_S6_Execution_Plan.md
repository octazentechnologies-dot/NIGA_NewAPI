# Stage S6 execution plan (canonical scripts — not Missing_Schema DDL)

**Status:** BLOCKED pending fresh Stage backup approval  
**Database:** `HomeoCentrum_Stage` on `Server1038\NIGA`  
**Do not use USE.** Confirm `@@SERVERNAME` and `DB_NAME()` before each script.

## Order

1. `S6_Week6/Database scripts/01_SmsOtpVerify_And_FirebaseNotificationLog.sql`
2. `S6_Week6/Database scripts/02_DevicePushToken_DeviceType.sql`
3. `S6_Week6/Database scripts/03_DoctorApp_Gap_Schema.sql`
4. `S6_Week6/Database scripts/04_Verify_And_Ensure_Gap_Schema.sql`
5. `S6_Week6/Database scripts/05_Verify_S6_Objects.sql` (verify-only; should PASS)
6. Optional verify: `Missing_Schema/11_Verification/001_Verify_S6_Parity_Markers.sql`

## Skip

- S5 `10` LOCAL_ONLY  
- S3 unnumbered duplicate copies  
- Mobile `01_Mobile_Clinic_Tables.sql` (not approved for Stage in this mission)  
- Any DEV-only seed scripts  

## Note on InstantConsult columns

`S3_Week3/.../12_InstantConsult_VisitLink.sql` and S6 `04` both add `PatientAppId` / `TeleSessionId` idempotently. Running S6 `04` is sufficient for Stage.

## Note on AppNotification lengths

Dev currently has `Type nvarchar(80)`, `DeepLink nvarchar(600)`. Canonical S6 `03` adds `nvarchar(40)` / `nvarchar(300)` on Stage. After Stage apply, lengths may differ (DEFINITION_MISMATCH / wider-on-Dev). Do **not** shrink Dev.

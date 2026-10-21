# Portable Parity Pack — Dev/Stage required schema gaps

**Purpose:** Idempotent scripts so any target DB (Stage, UAT, new env) gets the same **required schema objects** that were missing vs Dev after S4/S5.

**Order (run against the target database only — never Production without approval):**

1. `001_S6_SmsOtp_Fcm_DeviceType.sql`
2. `002_S6_CareLink_AppNotification_InstantConsult.sql`
3. `003_Mobile_Clinic_Tables.sql`
4. `004_Extra_DevParity_Tables.sql`
5. `006_ReceptionVisitVitals.sql`
6. `007_Missing_Columns_DevParity.sql`
7. `008_Seed_Reference_Data_From_Dev.sql` — SmsTemplate, HelpArticle, ModuleMaster, MenuMaster, RoleDetails
8. `005_Verify_Parity_Markers.sql` — must print `PASS`

**Does not copy:** patient/doctor/user/transaction/demo row data.  
**Safe to re-run:** all scripts use IF NOT EXISTS / COL_LENGTH guards.

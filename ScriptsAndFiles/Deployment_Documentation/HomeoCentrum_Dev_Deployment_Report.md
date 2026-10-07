# HomeoCentrum_Dev Deployment Report

Server: `103.196.187.99,1433` (`Server1038\NIGA`)
Database: `HomeoCentrum_Dev`
Environment: DEV
Recovery model: SIMPLE
State at start: ONLINE
Latest verified backup: Full backup finished `2026-09-30 08:57:09`, device `C:\SQL\NIGA_Dev_30-9-26.bak`
Earlier backups also recorded: `2026-09-17 08:59:06` (`C:\SQL\HomeoCentrum_Dev_Backup.bak`) and `2026-09-17 07:28:35` (`C:\SQL\HomeoCentrum_Dev_Backup`)
No backup was created, deleted, or restored by this deployment.
Credentials are not stored in this document.

## Counts

| Item | Count |
|---|---|
| Physical SQL files | 75 |
| Unique required scripts | 59 |
| Successful executions | 59 |
| Verified byte-identical duplicates, not re-executed | 16 |
| Failed | 0 |
| Blocked | 0 |
| Unknown | 0 |
| Final status | SUCCESS |

HomeoCentrum_Stage was not used. The session guard rejected any connection whose current database was not `HomeoCentrum_Dev`.

## Pre-flight

The Dev database already existed (created `2026-09-01`) and contained 203 tables and 40 procedures. Week 1 foundation tables such as `UserMaster`, `RoleMaster`, `PasswordResetToken`, `ConsentType`, `PatientUserMap`, `PatientFamilyMember`, and `CaregiverAuthorization` were already present. Later objects were absent, including `UserAppPreference`, `PolicyVersion`, `TeleSession`, `SupportTicket`, `NotificationOutbox`, and `PaymentOrder`. No `Tufan_*` UserMaster logins and no `Tufan_Reception` staff row were present. Scripts were therefore partially present, not fully applied, and were executed because they are written to add missing objects and seed rows.

Script `19_DEV_Seed_Role_Menus.sql` contains two filtered deletes only: Doctor role rows for `/enquiries` and `/admin/enquiries`, and duplicate `RoleDetails` rows (`rn > 1`) immediately before creating `UX_RoleDetails_RoleId_MenuId` when that index is absent. No `DROP DATABASE`, `TRUNCATE`, or unfiltered delete was found.

## Known script corrections kept

- `07_UNIT_TEST_S1_Week1_Guards.sql` UT-14 checks `UserName = Tufan_Patient` and `EmailId = tufanpowar001@gmail.com` with `DeleteStatus = 0`.
- `10_TEST_Sample_Data_Insert.sql` resolves the owner with those same two fields. No UserId was hard-coded.

## S2 execution-order exception

`14_DEV_Dashboard_Users_Verify.sql` is read-only and requires logins created by `15_DEV_Seed_Tufan_Role_Logins.sql`. Execution order was 13, then 15, then 14, then 16. Sequence numbers 18, 20, and 21 do not exist and were not created.

## S3 duplicate mapping

All 16 unnumbered files were rehashed before execution and remained byte-identical to the canonical numbered script. Only the canonical file was executed.

| Duplicate | Canonical |
|---|---|
| `M05_APT_02_01.sql` | `01b_M05_APT_02_01.sql` |
| `M05_APT_09_01.sql` | `04_M05_APT_09_01.sql` |
| `M06_REC_02_01.sql` | `05b_M06_REC_02_01.sql` |
| `M06_REC_08_01.sql` | `05_M06_REC_08_01.sql` |
| `M12_TEL_01_01.sql` | `06_M12_TEL_01_01.sql` |
| `M12_TEL_02_01.sql` | `08_M12_TEL_02_01.sql` |
| `M12_TEL_03_01.sql` | `07_M12_TEL_03_01.sql` |
| `M12_TEL_06_01.sql` | `08_M12_TEL_06_01.sql` |
| `M12_TEL_07_01.sql` | `06_M12_TEL_07_01.sql` |
| `M12_TEL_10_01.sql` | `09_M12_TEL_10_01.sql` |
| `M12_TEL_11_01.sql` | `10_M12_TEL_11_01.sql` |
| `M12_TEL_12_01.sql` | `06_M12_TEL_12_01.sql` |
| `M14_SUP_01_01.sql` | `10_M14_SUP_01_01.sql` |
| `M14_SUP_03_01.sql` | `11_M14_SUP_03_01.sql` |
| `M14_SUP_06_01.sql` | `12_M14_SUP_06_01.sql` |
| `M14_SUP_07_01.sql` | `13_M14_SUP_07_01.sql` |

Same-prefix files with different content were both executed: `01` then `01b`; the three `06` tele scripts in module order; `07` (`TeleSession`) before `08`; `10_M12_TEL_11` before `10_M14_SUP_01` so `SupportTicket` exists before script 11.

## Final read-only validation

Connected database remained `HomeoCentrum_Dev`. Eighteen expected tables were all present. `vw_TeleWaitingQueue` and `UX_RoleDetails_RoleId_MenuId` exist. Active logins `Tufan_Admin`, `Tufan_Doctor`, `Tufan_Account`, `Tufan_Pharmacy`, `Tufan_Patient`, `Tufan_Caregiver`, and `Tufan_NoMenu` exist with email `tufanpowar001@gmail.com` and `DeleteStatus = 0`. `Tufan_Reception` is present. Role menu grants exist for Admin (53), Doctor (11), Patient (4), Account (14), PharmacyPartner (4), and Reception (5).

## File results

| Week | Seq | Exec order | Filename | Classification | Pre-state | Status | Verification | Time | Notes |
|---|---|---|---|---|---|---|---|---|---|
| S1_Week1 | 01 | 1 | `01_SEC_M01_Foundation_Security_Server.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED t=7 c=11 i=8 v=0 | 9.37s | |
| S1_Week1 | 02 | 2 | `02_ADM_M02_W7_MenuMaster_Account_Pharmacy_Seed.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED (no durable DDL declared) | 2.02s | |
| S1_Week1 | 03 | 3 | `03_CON_M16_Family_Caregiver.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED t=4 c=11 i=7 v=0 | 5.87s | |
| S1_Week1 | 04 | 4 | `04_S1_Week1_Mobile_Menus_And_Prefs.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED t=3 c=5 i=1 v=0 | 2.62s | |
| S1_Week1 | 05 | 5 | `05_DEV_Seed_Patient_Portal_TufanPowar.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED t=0 c=3 i=0 v=0 | 1.51s | |
| S1_Week1 | 06 | 6 | `06_VERIFY_S1_Week1.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED (no durable DDL declared) | 9.04s | |
| S1_Week1 | 07 | 7 | `07_UNIT_TEST_S1_Week1_Guards.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED (no durable DDL declared) | 6.73s | |
| S1_Week1 | 08 | 8 | `08_DEV_Seed_S1_NewTables_Sample_Tested.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED (no durable DDL declared) | 6.34s | |
| S1_Week1 | 09 | 9 | `09_ADM_B04_Doctor_Clinic_Menus.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED (no durable DDL declared) | 1.17s | |
| S1_Week1 | 10 | 10 | `10_TEST_Sample_Data_Insert.sql` | Executed | Partially present foundation tables; scripts still applied missing objects | SUCCESS | PASSED (no durable DDL declared) | 4.36s | |
| S2_Week2 | 01 | 11 | `01_DOC_M04_Doctor_Profile_Kyc_Availability.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED t=1 c=11 i=2 v=0 | 2.64s | |
| S2_Week2 | 02 | 12 | `02_WEB_M10_Booking_Token_Policy.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED t=1 c=7 i=2 v=0 | 2.56s | |
| S2_Week2 | 03 | 13 | `03_WEB_CLN_Enquiry_CogRun_Activation.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED t=1 c=4 i=2 v=0 | 2.19s | |
| S2_Week2 | 04 | 14 | `04_CLN_M03_3D_Hotspot_SubSection.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED t=0 c=1 i=1 v=0 | 21.53s | |
| S2_Week2 | 05 | 15 | `05_VERIFY_S2_Week2.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 22.88s | |
| S2_Week2 | 06 | 16 | `06_UNIT_TEST_S2_Week2_Guards.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 3.16s | |
| S2_Week2 | 07 | 17 | `07_TEST_Sample_Data_Insert.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 9.31s | |
| S2_Week2 | 08 | 18 | `08_CARE_CATEGORIES_RECEPTION_ARTICLES.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 2.08s | |
| S2_Week2 | 09 | 19 | `09_WEB_TRU_Doctor_Credential_Documents.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED t=2 c=0 i=2 v=0 | 2.65s | |
| S2_Week2 | 10 | 20 | `10_TODAY_SLOTS_POLICY_DIRECTORY.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 1.48s | |
| S2_Week2 | 11 | 21 | `11_FAMILY_RELATION_MASTER.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED t=1 c=1 i=1 v=0 | 2.24s | |
| S2_Week2 | 12 | 22 | `12_DEV_Seed_Role_Users.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 1.01s | |
| S2_Week2 | 13 | 23 | `13_DOC_Reception_Profile_Menus.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 1.08s | |
| S2_Week2 | 15 | 24 | `15_DEV_Seed_Tufan_Role_Logins.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 4.47s | |
| S2_Week2 | 14 | 25 | `14_DEV_Dashboard_Users_Verify.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 1.4s | |
| S2_Week2 | 16 | 26 | `16_DEV_Tufan_Role_Logins_Verify.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 2.59s | |
| S2_Week2 | 17 | 27 | `17_DEV_Tufan_Contact_Email_Mobile.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 1.7s | |
| S2_Week2 | 19 | 28 | `19_DEV_Seed_Role_Menus.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED t=0 c=0 i=1 v=0 | 6.41s | |
| S2_Week2 | 22 | 29 | `22_DEV_Role_Menu_Consistency.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 3.28s | |
| S2_Week2 | 23 | 30 | `23_DEV_Rename_Tufan_Doctor.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 2.95s | |
| S2_Week2 | 24 | 31 | `24_DEV_Seed_Tufan_Doctor_Clinic.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 12.14s | |
| S2_Week2 | 25 | 32 | `25_DEV_Seed_Tufan_Doctor_Extra_Menus.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 2.04s | |
| S2_Week2 | 26 | 33 | `26_DEV_Replace_Tufanpowar_Email.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 0.97s | |
| S2_Week2 | 27 | 34 | `27_DEV_Tufan_All_Mobile_7768046064.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 3.83s | |
| S2_Week2 | 28 | 35 | `28_DEV_Tufan_Identity.sql` | Executed | Depends on S1; Tufan logins absent before script 15 | SUCCESS | PASSED (no durable DDL declared) | 2.08s | |
| S3_Week3 | 01 | 36 | `01_S3_Week3_Schema.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=15 c=13 i=7 v=0 | 2.14s | |
| S3_Week3 | 01b | 37 | `01b_M05_APT_02_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=0 c=2 i=0 v=0 | 0.92s | |
| S3_Week3 | 02 | 38 | `02_S3_Week3_Menus.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED (no durable DDL declared) | 1.58s | |
| S3_Week3 | 03 | 39 | `03_DEV_Seed_S3_Static.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED (no durable DDL declared) | 2.25s | |
| S3_Week3 | 04 | 40 | `04_M05_APT_09_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=0 c=3 i=0 v=0 | 5.71s | |
| S3_Week3 | 05 | 41 | `05_M06_REC_08_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=0 c=1 i=0 v=0 | 2.45s | |
| S3_Week3 | 05b | 42 | `05b_M06_REC_02_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED (no durable DDL declared) | 2.19s | |
| S3_Week3 | 06 | 43 | `06_M12_TEL_01_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=2 i=0 v=0 | 5.22s | |
| S3_Week3 | 06 | 44 | `06_M12_TEL_07_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=5 i=1 v=0 | 3.16s | |
| S3_Week3 | 06 | 45 | `06_M12_TEL_12_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=2 c=10 i=3 v=0 | 5.19s | |
| S3_Week3 | 07 | 46 | `07_M12_TEL_03_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=9 i=1 v=0 | 7.82s | |
| S3_Week3 | 08 | 47 | `08_M12_TEL_02_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=0 c=2 i=0 v=1 | 5.36s | |
| S3_Week3 | 08 | 48 | `08_M12_TEL_06_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=4 i=1 v=0 | 5.72s | |
| S3_Week3 | 09 | 49 | `09_M12_TEL_10_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=5 i=1 v=0 | 5.89s | |
| S3_Week3 | 10 | 50 | `10_M12_TEL_11_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=4 i=1 v=0 | 4.54s | |
| S3_Week3 | 10 | 51 | `10_M14_SUP_01_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=3 c=20 i=3 v=0 | 7.6s | |
| S3_Week3 | 11 | 52 | `11_M14_SUP_03_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=0 c=4 i=1 v=0 | 14.99s | |
| S3_Week3 | 12 | 53 | `12_M14_SUP_06_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=5 i=2 v=0 | 16.03s | |
| S3_Week3 | 13 | 54 | `13_M14_SUP_07_01.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED t=1 c=2 i=2 v=0 | 3.37s | |
| S3_Week3 | 14 | 55 | `14_M14_SUP_05_01_Enquiry_SLA.sql` | Executed | Later tele/support objects absent before this week | SUCCESS | PASSED (no durable DDL declared) | 0.55s | |
| S4_Week4 | 01 | 56 | `01_S4_Week4_Schema.sql` | Executed | Payment and outbox objects absent before this week | SUCCESS | PASSED t=33 c=7 i=16 v=0 | 4.61s | |
| S4_Week4 | 02 | 57 | `02_S4_Week4_Menus.sql` | Executed | Payment and outbox objects absent before this week | SUCCESS | PASSED (no durable DDL declared) | 6.99s | |
| S4_Week4 | 03 | 58 | `03_S4_Notification_Outbox.sql` | Executed | Payment and outbox objects absent before this week | SUCCESS | PASSED t=1 c=0 i=1 v=0 | 0.62s | |
| S4_Week4 | 04 | 59 | `04_S4_Week4_Demo_Data.sql` | Executed | Payment and outbox objects absent before this week | SUCCESS | PASSED (no durable DDL declared) | 7.76s | |
| S3_Week3 | — | — | `M05_APT_02_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `01b_M05_APT_02_01.sql` |
| S3_Week3 | — | — | `M05_APT_09_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `04_M05_APT_09_01.sql` |
| S3_Week3 | — | — | `M06_REC_02_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `05b_M06_REC_02_01.sql` |
| S3_Week3 | — | — | `M06_REC_08_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `05_M06_REC_08_01.sql` |
| S3_Week3 | — | — | `M12_TEL_01_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `06_M12_TEL_01_01.sql` |
| S3_Week3 | — | — | `M12_TEL_02_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `08_M12_TEL_02_01.sql` |
| S3_Week3 | — | — | `M12_TEL_03_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `07_M12_TEL_03_01.sql` |
| S3_Week3 | — | — | `M12_TEL_06_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `08_M12_TEL_06_01.sql` |
| S3_Week3 | — | — | `M12_TEL_07_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `06_M12_TEL_07_01.sql` |
| S3_Week3 | — | — | `M12_TEL_10_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `09_M12_TEL_10_01.sql` |
| S3_Week3 | — | — | `M12_TEL_11_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `10_M12_TEL_11_01.sql` |
| S3_Week3 | — | — | `M12_TEL_12_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `06_M12_TEL_12_01.sql` |
| S3_Week3 | — | — | `M14_SUP_01_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `10_M14_SUP_01_01.sql` |
| S3_Week3 | — | — | `M14_SUP_03_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `11_M14_SUP_03_01.sql` |
| S3_Week3 | — | — | `M14_SUP_06_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `12_M14_SUP_06_01.sql` |
| S3_Week3 | — | — | `M14_SUP_07_01.sql` | Verified byte-identical duplicate | Same bytes as canonical | NOT RE-EXECUTED | Byte-identical | — | Canonical `13_M14_SUP_07_01.sql` |

# Run 2 — 2026-10-08: S4 changes, S5, changed S2 scripts and extras

Same server and database. Credentials are not stored in this document.

Backup before any script: copy-only full backup `C:\SQL\HomeoCentrum_Dev_PreScripts_2026-10-08.bak` (WITH CHECKSUM, 1,695,785 pages, 112 s).

Every file ran with `sqlcmd -C -I -b -t 0 -f 65001` (QUOTED_IDENTIFIER on, stop on error). 33 executed, 0 failed, 3 skipped on purpose.

## Executed (in order)

| # | Week | Filename | Time | Why |
|---|---|---|---|---|
| 1 | S2 | `00_DOC_DoctorDailySchedule_Table.sql` | 5.8s | New since run 1 |
| 2 | S2 | `15_DEV_Seed_Tufan_Role_Logins.sql` | 4.8s | Changed since run 1 (15 before 14, as in run 1) |
| 3 | S2 | `14_DEV_Dashboard_Users_Verify.sql` | 1.7s | Changed since run 1 |
| 4 | S2 | `16_DEV_Tufan_Role_Logins_Verify.sql` | 2.6s | Changed since run 1 |
| 5 | S2 | `17_DEV_Tufan_Contact_Email_Mobile.sql` | 2.1s | Changed since run 1 |
| 6 | S2 | `22_DEV_Role_Menu_Consistency.sql` | 3.0s | Changed since run 1 |
| 7 | S2 | `25_WEB_Doctor_Credential_FilePath.sql` | 0.7s | New since run 1 |
| 8 | S2 | `28_DEV_Tufan_Identity.sql` | 2.4s | Changed since run 1 |
| 9 | S4 | `01_S4_Week4_Schema.sql` | 2.7s | Changed since run 1 |
| 10 | S4 | `02_S4_Notification_Outbox.sql` | 0.8s | Renumbered since run 1 |
| 11 | S4 | `03_S4_Week4_Menus.sql` | 0.9s | Renumbered since run 1 |
| 12 | S4 | `04_S4_Week4_Demo_Data.sql` | 0.7s | Re-run with the week |
| 13 | S4 | `05_S4_UserAddressLocation_Schema.sql` | 6.5s | New |
| 14 | S4 | `06_S4_CountryMaster_Data.sql` | 5.9s | New (inserts missing rows only) |
| 15 | S4 | `07_S4_StateMaster_Data.sql` | 31.2s | New (inserts missing rows only) |
| 16 | S4 | `08_S4_IndiaLocation_Data.sql` | 997.0s | New (inserts missing rows only) |
| 17 | S4 | `09_S4_AddressLocation_Other.sql` | 2.8s | New |
| 18 | S4 | `12_S4_AddressLocation_CheckAndApply.sql` | 11.2s | New; replaces 10 and 11 on a database that already had location data |
| 19 | S4 | `13_S4_DoctorProfile_Fields.sql` | 0.8s | New |
| 20–28 | S5 | `01` to `09` | 0.7–2.0s each | New week |
| 29 | S5 | `11_Consent_Notice_Version_Guardian.sql` | 3.1s | New week |
| 30 | S5 | `12_Rubric_Remedy_Author_Indexes.sql` | 8.5s | New week |
| 31 | S5 | `13_Remove_Junk_Roles.sql` | 1.3s | New week |
| 32 | S5 | `14_Required_Extras.sql` | 2.2s | Objects and data fixes no other script carries |
| 33 | S5 | `15_SubSection_SearchNormalized_FullText.sql` | 561.1s | Copy of Old API `Database\Scripts\SubSection_SearchNormalized_Setup.sql`. Adds `SearchNormalized` to the SubSectionMaster full-text index, which the remote lacked; without it Old API subsection search falls back to LIKE |

## Skipped on purpose

| Filename | Reason |
|---|---|
| S4 `10_S4_AddressLocation_EnteredBy.sql` | Overwrites EnteredBy/EnteredDate on every location row. `12` fills only empty values. |
| S4 `11_S4_AddressLocation_UserData.sql` | Sets every Patient, Doctor and UserMaster row to Kolhapur 416003, overwriting real addresses. `12` fills only rows with no location. |
| S5 `10_Security_Test_Tenant_LOCAL_ONLY.sql` | Marked local only. Creates second-tenant test logins (`Tufan_Doctor2`, `Tufan_Patient2`, `Tufan_Reception2`). |

## Validation after run 2

- Schema compared with the local database (tables, columns with type, size and nullability, indexes, keys, defaults, check constraints, full-text catalogs and columns, and the text of every procedure, view, function and trigger): nothing is missing, except SSMS diagram support (`sysdiagrams`, `sp_*diagram`, `fn_diagramobjects`), which the applications do not use. Nothing exists only on the remote.
- Now present: `ConsentNotice` (6 current v1.0 notices), `SecurityAuditLog` with `usp_SecurityAudit_Append` and its triggers, `DoctorReminder`, `MedicineOrderReview`, `PharmacyConfig`, the ConsentRecord guardian columns, the Review reply columns, `IX_RubricRemedyDetails_RemedyId_DeletedStatus`, `IX_ConsentRecord_GuardianRef` and `IX_UserMaster_UserName`.
- Every ConsentRecord row is linked to a notice. Every Patient, Doctor and UserMaster row has a location. No `string` placeholder values remain. FamilyRelationMaster has Child (17), Parent (18) and Sibling (19).
- Role menus: Account 14, Admin 54, Doctor 12, Patient 5, PharmacyPartner 4, Reception 5. Admin has `/admin/enquiries`, which the UI maps to `/enquiries`.
- Role `EmptyMenuProbe` was kept by `13_Remove_Junk_Roles.sql` because seven active team logins use it (`*_NoMenu`). `Tufan_NoMenu` and role `RoleName` were removed.
- Remote-only location data from before this run is unchanged: 13 extra older country names (for example `Korea, North`, `Ivory Coast`, three `Dominican Republic` rows), each with its own `Other` child rows.

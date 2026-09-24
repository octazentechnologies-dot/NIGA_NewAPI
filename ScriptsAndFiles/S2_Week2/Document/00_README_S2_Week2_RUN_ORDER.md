# S2 Week 2 — SQL run order

Scripts live in **New-API only**:

`NIGA_NewAPI/ScriptsAndFiles/S2_Week2/Database scripts`

Run on the shared SQL Server database (`HomeoCentrum_Dev`).  
Do **not** run from the app. SSMS / Azure Data Studio / `sqlcmd` is expected.

## Execution order

| # | File | Why |
|---|------|-----|
| 1 | `01_DOC_M04_Doctor_Profile_Kyc_Availability.sql` | Doctor clinic/fee/photo/online/directory/KYC. Existing practising doctors marked Verified + DirectoryVisible on Dev. |
| 2 | `02_WEB_M10_Booking_Token_Policy.sql` | PatientAppointment BookingToken, visit/consult/payment/tele, hold, PolicyVersion. |
| 3 | `03_WEB_CLN_Enquiry_CogRun_Activation.sql` | Enquiry TicketStatus/AssignedTo, CogRun log, UserMaster activation token TTL. |
| 4 | `04_CLN_M03_3D_Hotspot_SubSection.sql` | ThreeDBodyPartSectionHotspot.SubSectionId + name backfill. |
| 5 | `05_VERIFY_S2_Week2.sql` | Read-only proof queries. |
| 6 | `06_UNIT_TEST_S2_Week2_Guards.sql` | Guard unit tests + live schema assertions. |
| 7 | `07_TEST_Sample_Data_Insert.sql` | Dev/test sample on Tufan_Doctor 1010 and Tufan_Patient 3046 when those rows exist: 10 enquiries, 10 CogRun, 10 bookings (S2TESTED01–10), KYC, fees, hotspot map, Booking policy. |
| 8 | `08_CARE_CATEGORIES_RECEPTION_ARTICLES.sql` | HumanSystemMaster care categories, reception login `Tufan_Reception` / `123456`, S2-TESTED article body. |
| 9 | `09_WEB_TRU_Doctor_Credential_Documents.sql` | WEB-09.01 TRU-01 tables: DoctorVerification + DoctorCredentialDocument. Backfill one verification row per doctor. |
| 10 | `10_TODAY_SLOTS_POLICY_DIRECTORY.sql` | Dev-only: today + tomorrow DoctorDailySchedule for every Verified directory doctor; Booking policy 2026.09 if missing. |
| 11 | `11_FAMILY_RELATION_MASTER.sql` | FamilyRelationMaster + PatientFamilyMember.RelationId. Seed Spouse/Father/Mother/... |
| 12 | `12_DEV_Seed_Role_Users.sql` | Ensures `Tufan_Account` / `Tufan_Pharmacy` exist. Email `tufanpowar001@gmail.com`, mobile `7768046064`, password `123456`. |
| 13 | `13_DOC_Reception_Profile_Menus.sql` | Doctor menus: Reception Staff `/doctor/reception-staff`, Profile `/profile`. |
| 15 | `15_DEV_Seed_Tufan_Role_Logins.sql` | Team logins `Tufan_*` / `123456` for Admin, Doctor (`Tufan_Doctor`), Reception, Account, Pharmacy, Patient, Caregiver, plus API-only `Tufan_NoMenu`. Dev only. |
| 16 | `16_DEV_Tufan_Role_Logins_Verify.sql` | Read-only proof of Tufan_* logins + RoleDetails counts. |
| 17 | `17_DEV_Tufan_Contact_Email_Mobile.sql` | Sets email `tufanpowar001@gmail.com` and mobile `7768046064` on every Tufan_* login and Tufan_Reception. |
| 19 | `19_DEV_Seed_Role_Menus.sql` | RoleDetails for Admin/Reception/Pharmacy extras; revoke Doctor Enquiries; unique (RoleId, MenuId). Run after 15. |
| 22 | `22_DEV_Role_Menu_Consistency.sql` | Read-only proof: Tufan users, role-menu mappings, no Doctor Enquiries, Reception has no patientboard, Tufan_NoMenu has zero menus. |
| 23 | `23_DEV_Rename_Tufan_Doctor.sql` | Rename login Tufan_Doctore → Tufan_Doctor (already applied on Dev). |
| 24 | `24_DEV_Seed_Tufan_Doctor_Clinic.sql` | Dev clinic for Tufan_Doctor: KYC, schedule, credential stub, 8 cases, today appointments, notes, sample eRx. Do not run on production. |
| 25 | `25_DEV_Seed_Tufan_Doctor_Extra_Menus.sql` | Tufan_Doctor UserDetails = every available MenuMaster item. Other doctors stay on the RoleDetails clinic menus. Dev only. |
| 26 | `26_DEV_Replace_Tufanpowar_Email.sql` | Replace tufanpowar@gmail.com with tufanpowar001@gmail.com. |
| 27 | `27_DEV_Tufan_All_Mobile_7768046064.sql` | Replace leftover dummy 900000/910000/920000 mobiles on seeded rows with 7768046064. Dev only. |
| 28 | `28_DEV_Tufan_Identity.sql` | Force email tufanpowar001@gmail.com and mobile 7768046064 on Tufan/s1/s2 seed rows. Dev only. Run last. |

Scripts 07, 08, 10, 24, 27 and 28 are Dev/test data only. Do **not** run them on production.  
Script 01 marks practising doctors that are still Pending as Verified + DirectoryVisible.  
Script 07 fills empty fee/city on every doctor and sets DirectoryVisible = 1. Skip 07 on a database whose real doctors must stay hidden.  
Scripts 14, 16 and 22 are read-only. Script 15 is Dev team users only.

On another database, numeric ids (Doctor 1010, Patient 3046, UserId 10032) will differ. Seeds look up `Tufan_Doctor` / `Tufan_Patient` / `Tufan_Admin` by UserName. API samples in the docs show the Dev ids; mobile developers must use `data.userId` and `data.doctorId` from login.

## After SQL

1. Point New-API Development connection at `HomeoCentrum_Dev` (same as Old-API). Restart **NIGA_NewAPI** and **NIGA_OldAPI**.
2. Follow `S2_Week2_STATUS_REPORT.md` in this Document folder.
3. API contract: `../S2_Week2_API_DOC.txt`
   Combined Week 1+2 Excel catalog: `NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`

Login / Rx-write / Razorpay stay on Old-API. New HTTP for S2 is New-API only. No third API.

Existing endpoint patches that the UI still calls on `api` are on **Old-API**. See `S2_Week2_HOST_AUDIT.md`.

Doctor mobile must not get case-taking. Reception JWT includes `DoctorID` + `DoctorUserID` (owning doctor's UserMaster id) and is blocked from clipboard / COG / audio / lab mutate.

# S1 Week 1 — SQL run order (manual)

Scripts live in **New-API only**:

`NIGA_NewAPI/ScriptsAndFiles/S1_Week1/Database scripts`

Run on the shared SQL Server database (`HomeoCentrum_*`).  
Do **not** run from the app. SSMS / Azure Data Studio / `sqlcmd` is expected.

## Execution order

| # | File | Why |
|---|------|-----|
| 1 | `01_SEC_M01_Foundation_Security_Server.sql` | FND-01.02 RoleMaster (Patient / Account / PharmacyPartner). SEC-01 UserPassword NVARCHAR(500). ConsentType/ConsentRecord, OTP, AuditEvent, SecureDocument, PasswordResetToken. |
| 2 | `02_ADM_M02_W7_MenuMaster_Account_Pharmacy_Seed.sql` | ADM-B04.01 / SEC-04.03 Account + Pharmacy MenuMaster + RoleDetails. Fails if step 1 roles are missing. |
| 3 | `03_CON_M16_Family_Caregiver.sql` | CON-01 / CON-02 PatientUserMap, PatientFamilyMember, CaregiverAuthorization, FamilyRelationMaster. |
| 4 | `04_S1_Week1_Mobile_Menus_And_Prefs.sql` | Patient menus, WelcomeSlide, UserAppPreference, DevicePushToken. |
| 5 | `05_DEV_Seed_Patient_Portal_TufanPowar.sql` | Data seed (no schema change). Patient login Tufan Powar + `PatientUserMap` + case on NIGA HOMEOPATHY. Idempotent. |
| 6 | `06_VERIFY_S1_Week1.sql` | Read-only proof queries. |
| 7 | `07_UNIT_TEST_S1_Week1_Guards.sql` | Guard unit tests (create-if-missing, add-column, insert-if-absent) plus live schema assertions. |
| 8 | `08_DEV_Seed_S1_NewTables_Sample_Tested.sql` | Sample TESTED rows on Tufan_Patient (falls back to tufanpowar001@gmail.com). Family, caregiver, consent, OTP, audit, device, prefs. Run after 05 and Week 2 script 15. |
| 9 | `09_ADM_B04_Doctor_Clinic_Menus.sql` | Doctor RoleDetails + clinic MenuMaster so GetMenuByRole is 200 for Doctor. |
| 10 | `Test sample data insert.sql` (this folder) | **TEST/UAT only — do not run on production.** 10 sample rows in each new S1 table. |

No extra OTP table script: caregiver grant OTP reuses `OtpChallenge` from step 1 (`Action = GrantCaregiver`). Login OTP uses `Action = Login` (anonymous).

## After SQL

1. Restart **NIGA_NewAPI** and **NIGA_OldAPI**.
2. **SEC-01.01 bulk hash:** Admin `POST /api/Account/MigratePlaintextPasswords` on New-API (hashes remaining plaintext `UserMaster` passwords). Login also lazy-migrates one user at a time.
3. Follow `S1_Week1_STATUS_REPORT.md` in the `Document` folder.
4. Share these API text files (USE / HOST / TOKEN / SAMPLE / INPUTS / OUTPUT on every URL):
   - `../S1_Week1_API_DOC.txt` — full Week 1 web + security + dual-API hosts
   - `../S1_Week1_MOBILE_API_DOC.txt` — patient/doctor app HTTP contracts
   Combined Week 1+2 Excel catalog: `NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`
   Copies also live in this `Document` folder and repo `Documents/`.

Login / Rx-write / Razorpay stay on classic (Old-API) paths. New HTTP for S1 security/family/OTP/menu is New-API.

**Do not run on production:** `07_UNIT_TEST_S1_Week1_Guards.sql`, `08_DEV_Seed_S1_NewTables_Sample_Tested.sql`, `Test sample data insert.sql`.

## FND notes (S1)

- **FND-01.01 shared keys:** `DoctorId` (JWT `DoctorID` + appointment/patient rows), `PatientId`, `PatientAppId` / appointment id, JWT `UserId` (`NameIdentifier`). Family members are real `Patient` rows. Caregiver is `CaregiverUserId` → `UserMaster`.
- **FND-01.02 roles:** `Patient`, `Account`, `PharmacyPartner` seeded in script 01. Do not rename existing Admin/Doctor/Reception roles.
- **FND-01.03 dual API:** New domain HTTP on New-API (.NET 8). Do not add a third API. Existing classic writes stay on Old-API.
- **FND-02.01 Account menus:** Seeded in script 02; SPA consumes `GetMenuByRole` with hardcoded Account nav fallback (`/accountdashboard`, ledger, earnings, payouts, invoices, reports). Real login is `s2.account` / `123456` on Old-API Login (no dummy token).
- **FND-02.04 five portals (PDF count):** Patient Website (public landing `/`, `/book`, `/contact`); Doctor Web Portal (`/doctordashboard`); Reception Portal (same doctor chrome until Phase 5, login `s2.reception`); Admin Portal (`/dashboard`); Account Department (`/accountdashboard`). Pharmacy console `/pharmacydashboard` is HomeoMeds, not a 6th portal.

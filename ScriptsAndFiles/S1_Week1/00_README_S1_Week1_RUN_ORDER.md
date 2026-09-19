# S1 Week 1 — SQL run order (manual)

Run these on the **same SQL Server database** both APIs use (`HomeoCentrum_*`).  
Do **not** run from the app. SSMS / Azure Data Studio / `sqlcmd` is expected.

## Order

| # | File | Why |
|---|------|-----|
| 1 | `01_SEC_M01_Foundation_Security_Server.sql` | FND-01.02 RoleMaster (Patient / Account / PharmacyPartner). SEC-01 UserPassword NVARCHAR(500). ConsentType/ConsentRecord, OTP, AuditEvent, SecureDocument, PasswordResetToken. |
| 2 | `02_ADM_M02_W7_MenuMaster_Account_Pharmacy_Seed.sql` | ADM-B04.01 / SEC-04.03 Account + Pharmacy MenuMaster + RoleDetails. Fails if step 1 roles are missing. |
| 3 | `03_CON_M16_Family_Caregiver.sql` | CON-01 / CON-02 PatientUserMap, PatientFamilyMember, CaregiverAuthorization. |
| 4 | `05_S1_Week1_Mobile_Menus_And_Prefs.sql` | Patient menus, WelcomeSlide, UserAppPreference, DevicePushToken. |
| 5 | `04_VERIFY_S1_Week1.sql` | Read-only proof queries. |

No extra OTP table script: caregiver grant OTP reuses `OtpChallenge` from step 1 (`Action = GrantCaregiver`). Login OTP uses `Action = Login` (anonymous).

## After SQL

1. Restart **NIGA_NewAPI** and **NIGA_OldAPI**.
2. **SEC-01.01 bulk hash:** Admin `POST /api/Account/MigratePlaintextPasswords` on New-API (hashes remaining plaintext `UserMaster` passwords). Login also lazy-migrates one user at a time.
3. Follow `S1_Week1_STATUS_REPORT.md` test/proof section.

Login / Rx-write / Razorpay stay on classic (Old-API) paths. New HTTP for S1 security/family/OTP/menu is New-API.

## FND notes (S1)

- **FND-01.01 shared keys:** `DoctorId` (JWT `DoctorID` + appointment/patient rows), `PatientId`, `PatientAppId` / appointment id, JWT `UserId` (`NameIdentifier`). Family members are real `Patient` rows. Caregiver is `CaregiverUserId` → `UserMaster`.
- **FND-01.02 roles:** `Patient`, `Account`, `PharmacyPartner` seeded in script 01. Do not rename existing Admin/Doctor/Reception roles.
- **FND-01.03 dual API:** New domain HTTP on New-API (.NET 8). Do not add a third API. Existing classic writes stay on Old-API.
- **FND-02.01 Account menus:** Seeded in script 02; SPA consumes `GetMenuByRole` with hardcoded Account nav fallback (`/accountdashboard`, ledger, earnings, payouts, invoices, reports).


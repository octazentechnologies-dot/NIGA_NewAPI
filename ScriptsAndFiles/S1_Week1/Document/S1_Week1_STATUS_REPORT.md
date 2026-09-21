# S1 Week 1 status report

Excel was **not** edited. This file is the delivery record for S1 Week 1 (API + database + in-scope web).

## Scope applied

- **Skipped:** QA, PRE/client gates, Mobile UI / React Native screens (PAT-*.01/*.03, DMO-*.01/*.03).
- **In scope:** Web API (existing on Old-API, new HTTP on New-API), SQL you run manually, family/caregiver SPA, GetMenuByRole consume, **Mobile APIs** (PAT-01.02 … PAT-05.02, DMO-01.02 … DMO-03.02).
- **Not rebuilt:** Admin clinical masters. No third API. Login / Rx-write / Razorpay stay on classic (Old-API).

Shareable API text (give these to test / mobile):

- `NIGA_NewAPI/ScriptsAndFiles/S1_Week1/S1_Week1_API_DOC.txt`
- `NIGA_NewAPI/ScriptsAndFiles/S1_Week1/S1_Week1_MOBILE_API_DOC.txt`
- Combined Week 1+2 catalog: `NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`

## SQL pack (you run)

All Week 1 SQL is **New-API only**:

`NIGA_NewAPI/ScriptsAndFiles/S1_Week1/Database scripts`

Docs: `NIGA_NewAPI/ScriptsAndFiles/S1_Week1/Document`

1. `01_SEC_M01_Foundation_Security_Server.sql`
2. `02_ADM_M02_W7_MenuMaster_Account_Pharmacy_Seed.sql`
3. `03_CON_M16_Family_Caregiver.sql`
4. `04_S1_Week1_Mobile_Menus_And_Prefs.sql`
5. `05_DEV_Seed_Patient_Portal_TufanPowar.sql`
6. `06_VERIFY_S1_Week1.sql`

Database is shared. Run once on `HomeoCentrum_*`.

## Code shipped this week

| ID | Change | Where |
|----|--------|-------|
| SEC-05.02 | Removed `UseDirectoryBrowser` on `/attachments` and `/Blogs` | New-API `Program.cs` |
| CON-02.04 | Caregiver Grant requires OTP (`Action=GrantCaregiver`) unless AdminPortal | New-API `CaregiverController` |
| SEC-05.01 | JWT `DoctorID` / caller userId checks on patient, appointment, Rx, notes, labs, case history | Old-API controllers + `DoctorOwnership` |
| ADM-B04.03 | SPA calls `GET /api/mastersAPI/GetMenuByRole`; hardcoded fallback | `LayoutMenuContext.js` |
| CON-01.03 | Family members SPA | `/family` |
| CON-02.03 | Caregiver grant/revoke + OTP SPA | `/caregiver` |
| FND-* | Shared-key / dual-API / role notes in README + this report | scripts pack |

Both APIs compile (0 errors) to a temp output folder. Running `Niga-Web` / Old-API processes lock `bin/` until restart.

## Per-task table (all 178 S1 rows)

| Sub Task ID | Track | Module | Excel BE / API / FE / DB | S1 status | Notes |
|-------------|-------|--------|--------------------------|-----------|-------|
| PRE-01.01 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-01.02 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-01.03 | Web | M00 | Not Started/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-02.01 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-02.02 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-02.03 | Web | M00 | Not Started/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-03.01 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-03.02 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-03.03 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-03.04 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| PRE-03.05 | Web | M00 | N/A/N/A/N/A/N/A | SKIPPED | PRE / client gate (skipped this week) |
| FND-01.01 | Web | M01 | Not Started/N/A/N/A/Not Started | DONE | Implemented this week (code + docs) |
| FND-01.02 | Web | M01 | Not Started/N/A/Not Started/Not Started | DONE | Implemented this week (code + docs) |
| FND-01.03 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Implemented this week (code + docs) |
| FND-01.04 | QA | M01 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| FND-02.01 | UI | M01 | N/A/N/A/Not Started/N/A | DONE | Implemented this week (code + docs) |
| FND-02.02 | UI | M01 | N/A/N/A/Not Started/N/A | DONE | Documented in S1_Week1 README / this report |
| FND-02.03 | Mobile | M01 | N/A/N/A/N/A/N/A | SKIPPED | Mobile / mobile frontend skipped |
| FND-02.04 | Web | M01 | N/A/N/A/N/A/N/A | DONE | Documented in S1_Week1 README / this report |
| SEC-01.01 | Web | M01 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| SEC-01.02 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Verified already in repo |
| SEC-01.03 | UI | M01 | N/A/N/A/Not Started/N/A | DONE | Verified already in repo |
| SEC-01.04 | QA | M01 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| SEC-02.01 | Web | M01 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| SEC-02.02 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Verified already in repo |
| SEC-02.03 | UI | M01 | N/A/Not Started/Not Started/N/A | DONE | Covered by M01 security pack + New-API Account/Consent/OTP/Audit |
| SEC-02.04 | QA | M01 | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| SEC-03.01 | Web | M01 | Not Started/Not Started/N/A/Not Started | DONE | Verified already in repo |
| SEC-03.02 | UI | M01 | N/A/Not Started/Not Started/N/A | DONE | Verified already in repo |
| SEC-03.03 | Web | M01 | N/A/Not Started/N/A/N/A | DONE | Covered by M01 security pack + New-API Account/Consent/OTP/Audit |
| SEC-03.04 | QA | M01 | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| SEC-04.01 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Verified already in repo |
| SEC-04.02 | UI | M01 | N/A/N/A/Not Started/N/A | DONE | Verified already in repo |
| SEC-04.03 | Web | M01 | Not Started/N/A/N/A/Not Started | DONE | Verified already in repo |
| SEC-04.04 | QA | M01 | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| SEC-05.01 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Implemented this week (code + docs) |
| SEC-05.02 | Web | M01 | Not Started/N/A/N/A/N/A | DONE | Implemented this week (code + docs) |
| SEC-05.03 | QA | M01 | Not Started/Not Started/N/A/N/A | SKIPPED | QA track skipped |
| SEC-06.01 | Web | M01 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| SEC-06.02 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Verified already in repo |
| SEC-06.03 | Web | M01 | Not Started/N/A/N/A/N/A | DONE | Verified already in repo |
| SEC-06.04 | QA | M01 | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| SEC-07.01 | Web | M01 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| SEC-07.02 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Verified already in repo |
| SEC-07.03 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Verified already in repo |
| SEC-07.04 | QA | M01 | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| SEC-08.01 | Web | M01 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| SEC-08.02 | Web | M01 | Not Started/N/A/N/A/N/A | DONE | Verified already in repo |
| SEC-08.03 | QA | M01 | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| SEC-09.01 | Web | M01 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| SEC-09.02 | Web | M01 | Not Started/Not Started/N/A/N/A | DONE | Verified already in repo |
| SEC-09.03 | QA | M01 | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| ADM-3D1.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-3D1.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-3D1.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-3D2.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-3D2.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-3D2.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-3D3.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-3D3.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-3D3.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-A01.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-A01.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-A01.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-A02.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-A02.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-A02.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-A03.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-A03.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-A03.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-B01.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-B01.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-B01.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-B02.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-B02.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-B02.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-B03.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-B03.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-B03.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-B04.01 | Web | M02 | Done/N/A/N/A/In Progress | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| ADM-B04.02 | Web | M02 | Done/Done/N/A/In Progress | DONE | Verified already in repo |
| ADM-B04.03 | UI | M02 | N/A/Not Started/Not Started/N/A | DONE | Implemented this week (code + docs) |
| ADM-B04.04 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-D01.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-D01.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-D01.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-D02.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-D02.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-D02.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-D03.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-D03.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-D03.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M01.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-M01.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M01.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M02.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-M02.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M02.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M03.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-M03.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M03.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M04.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-M04.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-M04.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-Q01.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-Q01.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-Q01.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-Q02.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-Q02.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-Q02.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R01.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R01.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R01.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R02.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R02.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R02.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R03.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R03.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R03.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R04.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R04.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R04.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R05.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R05.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R05.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R06.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R06.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R06.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R07.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R07.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R07.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R08.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R08.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R08.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R09.01 | QA | M02 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| ADM-R09.02 | Web | M02 | Done/Done/Done/N/A | DONE | M02 ACL / dual-API verified in repo |
| ADM-R09.03 | Web | M02 | Done/Done/N/A/N/A | DONE | M02 ACL / dual-API verified in repo |
| CON-01.01 | Web | M16 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| CON-01.02 | Web | M16 | Not Started/Not Started/N/A/Not Started | DONE | Verified already in repo |
| CON-01.03 | UI | M16 | N/A/Not Started/Not Started/N/A | DONE | Implemented this week (code + docs) |
| CON-01.04 | QA | M16 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CON-02.01 | Web | M16 | Not Started/N/A/N/A/Not Started | SQL READY | Script in ScriptsAndFiles/S1_Week1/Database scripts — run manually |
| CON-02.02 | Web | M16 | Not Started/Not Started/N/A/Not Started | DONE | Verified already in repo |
| CON-02.03 | UI | M16 | N/A/Not Started/Not Started/N/A | DONE | Implemented this week (code + docs) |
| CON-02.04 | Web | M16 | Not Started/Not Started/N/A/N/A | DONE | Implemented this week (code + docs) |
| CON-02.05 | QA | M16 | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-01.01 | Mobile | M17 | N/A/N/A/Done/N/A | SKIPPED | RN UI skipped |
| PAT-01.02 | Mobile | M17 | N/A/Not Started/Not Started/N/A | API DONE | GET /mastersAPI/GetLanguages (anon) + PatientProfile language |
| PAT-01.03 | Mobile | M17 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| PAT-01.04 | QA | M17 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-02.01 | Mobile | M17 | N/A/N/A/Done/N/A | SKIPPED | RN UI skipped |
| PAT-02.02 | Mobile | M17 | N/A/Not Started/Not Started/N/A | API DONE | GET /Welcome/Patient + welcomeVersionSeen on profile PUT |
| PAT-02.03 | Mobile | M17 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| PAT-02.04 | QA | M17 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-03.01 | Mobile | M17 | N/A/N/A/Done/N/A | SKIPPED | RN UI skipped |
| PAT-03.02 | Mobile | M17 | N/A/Not Started/Not Started/N/A | API DONE | RequestOtp Action=Login + LoginWithOtp |
| PAT-03.03 | Mobile | M17 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| PAT-03.04 | QA | M17 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-04.01 | Mobile | M17 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| PAT-04.02 | Mobile | M17 | N/A/Not Started/Not Started/N/A | API DONE | GET/PUT /PatientProfile/Me |
| PAT-04.03 | Mobile | M17 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| PAT-04.04 | QA | M17 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-05.01 | Mobile | M17 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| PAT-05.02 | Mobile | M17 | N/A/Not Started/Not Started/N/A | API DONE | PrivacyStatus + GrantPrivacy + Public/Policies/Privacy |
| PAT-05.03 | Mobile | M17 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| PAT-05.04 | QA | M17 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DMO-01.01 | Mobile | M18 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI / Keychain skipped |
| DMO-01.02 | Mobile | M18 | N/A/Not Started/Not Started/N/A | API DONE | Same Old-API POST /Account/Login. Keychain is RN. |
| DMO-01.03 | Mobile | M18 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| DMO-01.04 | QA | M18 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DMO-02.01 | Mobile | M18 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| DMO-02.02 | Mobile | M18 | N/A/Not Started/Not Started/N/A | API DONE | GET /Profile/Me + POST /Account/ConfirmMobile |
| DMO-02.03 | Mobile | M18 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| DMO-02.04 | QA | M18 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DMO-03.01 | Mobile | M18 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| DMO-03.02 | Mobile | M18 | N/A/Not Started/Not Started/N/A | API DONE | POST /Device/Register + Unregister + GET /Device/Mine |
| DMO-03.03 | Mobile | M18 | N/A/N/A/Not Started/N/A | SKIPPED | RN UI skipped |
| DMO-03.04 | QA | M18 | N/A/Not Started/Not Started/N/A | SKIPPED | QA track skipped |

Counts: SKIPPED RN/QA/PRE, DONE web/SQL, API DONE for PAT-01.02…05.02 and DMO-01.02…03.02 (total 178). Excel was not edited.

## Test / proof guide

Restart New-API and Old-API after SQL + this code (file locks otherwise).

### Database

1. Run scripts 01 → 02 → 03 → 04 → 05 then `06_VERIFY_S1_Week1.sql`.
2. Proof: `UserPassword` length 500; tables Consent/OTP/Audit/SecureDocument/Family; RoleMaster Patient/Account/PharmacyPartner; Account/Pharmacy MenuMaster rows.

### Auth (classic login stays Old-API)

UI `login` posts to Old-API `:5001` `/api/Account/Login` (`api`, not nigahomeoAPI). Do not switch this host.
Confirm JWT contains `RoleId`, `RoleName`/`role`, `DoctorID` for doctors, `NameIdentifier` = UserId.
Login once so plaintext hashes migrate to `PBKDF2$v1$…`.

### SEC-05.02 directory browsing

- `GET http://localhost:5038/attachments/` must **not** list files (no directory index).
- Direct file URL still works if the file exists and static files are enabled.

### SEC-05.01 ownership

Old-API (`:5001`) with Doctor A JWT:

- `GET /api/patientApp/{id}` for Doctor B appointment → 403.
- `POST /api/patientApp` with another DoctorId → 403 (JWT DoctorId bound when missing).
- `GET /api/patient/{otherUserId}` → 403 unless AdminPortal.
- Rx `PrescriptionRubricDetail?appointmentId=` for another doctor → 403.
- Labs `GET /api/PatientLab/GetPatientLabOrder` (all) → 403 unless Admin; JWT required.
- Admin/Management JWT still allowed.

New-API: `GET /api/PatientApp/{id}` and `GetCasesByUser` now also enforce ownership.

### CON-02.04 caregiver OTP

1. Patient JWT: `POST /api/Family/LinkPrimary` `{ "patientId": N }`.
2. `POST /api/Otp/RequestOtp` `{ "action":"GrantCaregiver", "entityType":"Patient", "entityId":"N", "destination":"9999999999" }`.
3. In Development, response `data.devCode` is the stub code.
4. `POST /api/Caregiver/Grant` with `otpChallengeId` + `otpCode` → 200.
5. Same Grant without OTP → 400. Wrong doctor/patient OTP → 400. AdminPortal may omit OTP.

### CON-01.03 / CON-02.03 SPA

1. Login as Patient (or Doctor — Family/Caregiver also on More menu).
2. Open `/family`: link primary PatientId, add/edit/delete member.
3. Open `/caregiver`: request OTP, grant, list mine, revoke.

### ADM-B04.03 menu

1. After SQL 02, login as Account user.
2. Network: `GET /api/mastersAPI/GetMenuByRole?userId=` returns Account menus.
3. Top nav uses those items (URLs like `/account/home` mapped to `/accountdashboard`).
4. If API 404/empty, hardcoded Account/Pharmacy/admin menus still show.

### Dual-API reminder

- New domain HTTP: New-API `:5038` (`nigahomeoAPI`).
- Classic write paths that were already Old-API stay there.
- Shared keys: `DoctorId`, `PatientId`, `PatientAppId` / appointment id, JWT `UserId`.


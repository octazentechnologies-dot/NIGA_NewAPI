# S2 Week 2 status report

Excel **S2_Week2** sheet was updated for in-scope layer/overall statuses.
Ignore prior Excel Done/Not Started when judging the work — this file is the delivery record.

## Scope applied

- **Skipped:** QA, PRE/client gates, Mobile UI / React Native screens.
- **In scope:** Web Frontend UI, Web API, Database, Security, Mobile APIs.
- **Host rule:** new HTTP on New-API `:5038`; existing URLs stay on the live UI host. Login / Rx-write / Razorpay stay Old-API `:5001`.
- **Doctor mobile** must not call case-taking (clipboard, Center of Gravity, audio, lab mutate).

Excel rows: **187**. In-scope: **107**. Skipped: **80**.

Shareable API text:

- `NIGA_NewAPI/ScriptsAndFiles/S2_Week2/S2_Week2_API_DOC.txt`
- `NIGA_NewAPI/ScriptsAndFiles/S2_Week2/S2_Week2_MOBILE_API_DOC.txt`
- Combined Week 1+2 catalog: `NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`

## SQL pack (you run)

`NIGA_NewAPI/ScriptsAndFiles/S2_Week2/Database scripts` on `HomeoCentrum_Dev` (`localhost\MSSQLSERVER25`).
Do **not** run 07 / 08 / 10 sample scripts on production.

1. `01_DOC_M04_Doctor_Profile_Kyc_Availability.sql`
2. `02_WEB_M10_Booking_Token_Policy.sql`
3. `03_WEB_CLN_Enquiry_CogRun_Activation.sql`
4. `04_CLN_M03_3D_Hotspot_SubSection.sql`
5. `05_VERIFY_S2_Week2.sql`
6. `06_UNIT_TEST_S2_Week2_Guards.sql`
7. `07_TEST_Sample_Data_Insert.sql` (Dev 10-row samples, date 18-09-2026)
8. `08_CARE_CATEGORIES_RECEPTION_ARTICLES.sql`
9. `09_WEB_TRU_Doctor_Credential_Documents.sql`
10. `10_TODAY_SLOTS_POLICY_DIRECTORY.sql`
11. `11_FAMILY_RELATION_MASTER.sql`
12. `12_DEV_Seed_Role_Users.sql`
13. `13_DOC_Reception_Profile_Menus.sql`
14. `14_DEV_Dashboard_Users_Verify.sql` (read-only dashboard logins)

## Re-audit 21 Sep 2026 (after upper-branch pull)

Excel Done/Not Started ignored. In-scope 107 rows checked against code (skip QA / Mobile UI / Mobile Frontend / PRE). Dummy Account/Pharmacy SPA tokens removed; use `s2.account` / `s2.pharmacy` on Old-API Login. Dashboard users verified on `HomeoCentrum_Dev`. S3–S5 week sheets are not in this branch delivery.

## Re-audit 21 Sep 2026 (S1+S2 full inventory + live test)

- No additional S2 feature gaps found in code vs tracker (Excel Done ignored). CON-02.04 caregiver Grant OTP is in New-API (`Action=GrantCaregiver`).
- Mobile developer sheet: **`Mobile_API_Reference`** in `Homeocentrum_All_New_And_Updated+APIs.xlsx` (all Old-API + New-API HTTP, not S1/S2-only).
- UI verified locally: `/book` directory + profile fees, `/pricing` labelled SaaS vs consult, doctor dashboard chrome (availability / tele queue / unpaid), reception-staff page, Account portal login.
- Regenerator + smoke: `Documents/_build_mobile_api_reference.py`, `NIGA_NewAPI/ScriptsAndFiles/S2_Week2/15_S1_S2_Human_Smoke.py`.

## Code shipped this week
| ID | Change | Where |
|----|--------|-------|
| WEB-03/04/05 | Public /book list, slots, confirm, pay, success/failure | Landing HomeoJobLanding |
| WEB-09 | Register multipart docs + pending + status | Register.js + New-API Users |
| WEB-10 | `/activate?token=` + expiry copy | ActivateAccount.js / Login.js |
| DOC-09 | `/doctor/reception-staff` live CRUD | ReceptionStaffPage + New-API |
| DOC-10 | Doctor `/profile` clinic/fee/photo/hours/bank tabs | user-profile.js |
| CLN-13 | Center of Gravity | PatientBoard + New-API Repertorization |
| WEB-02 | /pricing labelled SaaS vs consult fees | PricingPage.js |
| WEB-06 | /contact confirmation + /enquiries inbox | ContactForm + EnquiryInboxPage |

## Per-task table (all S2_Week2 rows)

| Sub Task ID | Track | Module | Bifurcation | Excel FE / BE / API / DB | S2 status | Notes |
|-------------|-------|--------|-------------|--------------------------|-----------|-------|
| CLN-01.01 | A-ClinicWeb | M03 | UI | Done/N/A/N/A/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-01.02 | C-Mobile | M03 | Web Other | N/A/N/A/N/A/N/A | DONE | Implemented this week (code + docs) |
| CLN-01.03 | D-QA | M03 | QA | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| CLN-02.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-02.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-02.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-03.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-03.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-03.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-04.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-04.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-04.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-05.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-05.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-05.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-06.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-06.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-06.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-07.01 | A-ClinicWeb | M03 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| CLN-07.02 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-07.03 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-07.04 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-08.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-08.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-08.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-09.01 | A-ClinicWeb | M03 | Web API | N/A/Done/N/A/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-09.02 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-09.03 | D-QA | M03 | QA | N/A/Not Started/N/A/N/A | SKIPPED | QA track skipped |
| CLN-10.01 | A-ClinicWeb | M03 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| CLN-10.02 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-10.03 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-10.04 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-11.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-11.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-11.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-12.01 | A-ClinicWeb | M03 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| CLN-12.02 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-12.03 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-12.04 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-13.01 | A-ClinicWeb | M03 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| CLN-13.02 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-13.03 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-13.04 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-14.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-14.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-14.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-15.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-15.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-15.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-16.01 | A-ClinicWeb | M03 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| CLN-16.02 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-16.03 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-16.04 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-17.01 | A-ClinicWeb | M03 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| CLN-17.02 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-17.03 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-17.04 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-18.01 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-18.02 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-18.03 | D-QA | M03 | QA | Not Started/Not Started/N/A/N/A | SKIPPED | QA track skipped |
| CLN-19.01 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| CLN-19.02 | A-ClinicWeb | M03 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| CLN-19.03 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-20.01 | A-ClinicWeb | M03 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| CLN-20.02 | A-ClinicWeb | M03 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| CLN-20.03 | A-ClinicWeb | M03 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| CLN-20.04 | D-QA | M03 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-01.01 | A-ClinicWeb | M04 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| DOC-01.02 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-01.03 | A-ClinicWeb | M04 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| DOC-01.04 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-02.01 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-02.02 | A-ClinicWeb | M04 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| DOC-02.03 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-03.01 | A-ClinicWeb | M04 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| DOC-03.02 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-03.03 | A-ClinicWeb | M04 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| DOC-03.04 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-04.01 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-04.02 | A-ClinicWeb | M04 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| DOC-04.03 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-05.01 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-05.02 | A-ClinicWeb | M04 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| DOC-05.03 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-06.01 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-06.02 | A-ClinicWeb | M04 | UI | Done/N/A/N/A/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| DOC-06.03 | D-QA | M04 | QA | Not Started/Not Started/N/A/N/A | SKIPPED | QA track skipped |
| DOC-07.01 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-07.02 | A-ClinicWeb | M04 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| DOC-07.03 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-08.01 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| DOC-08.02 | A-ClinicWeb | M04 | Security | Done/Done/Done/N/A | DONE | DoctorOnly / ownership / reception block on live host |
| DOC-08.03 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-09.01 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/N/A | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-09.02 | A-ClinicWeb | M04 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| DOC-09.03 | D-QA | M04 | QA | Not Started/Not Started/N/A/N/A | SKIPPED | QA track skipped |
| DOC-10.01 | A-ClinicWeb | M04 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| DOC-10.02 | A-ClinicWeb | M04 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| DOC-10.03 | A-ClinicWeb | M04 | UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| DOC-10.04 | D-QA | M04 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| WEB-01.01 | B-PatientEco | M10 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| WEB-01.02 | B-PatientEco | M10 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| WEB-01.03 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-01.04 | D-QA | M10 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| WEB-02.01 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-02.02 | B-PatientEco | M10 | Web UI | Done/N/A/N/A/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-02.03 | D-QA | M10 | QA | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| WEB-03.01 | B-PatientEco | M10 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| WEB-03.02 | B-PatientEco | M10 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| WEB-03.03 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-03.04 | D-QA | M10 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| WEB-04.01 | B-PatientEco | M10 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| WEB-04.02 | B-PatientEco | M10 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| WEB-04.03 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-04.04 | D-QA | M10 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| WEB-05.01 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-05.02 | D-QA | M10 | QA | Not Started/Not Started/N/A/N/A | SKIPPED | QA track skipped |
| WEB-06.01 | B-PatientEco | M10 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| WEB-06.02 | B-PatientEco | M10 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| WEB-06.03 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-06.04 | D-QA | M10 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| WEB-07.01 | B-PatientEco | M10 | Web UI | Done/N/A/N/A/Done | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-07.02 | D-QA | M10 | QA | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| WEB-08.01 | B-PatientEco | M10 | Web UI | Done/N/A/N/A/Done | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-08.02 | D-QA | M10 | QA | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| WEB-09.01 | B-PatientEco | M10 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| WEB-09.02 | B-PatientEco | M10 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| WEB-09.03 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-09.04 | D-QA | M10 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| WEB-10.01 | B-PatientEco | M10 | Database | N/A/Done/N/A/Done | DONE | SQL pack ScriptsAndFiles/S2_Week2/Database scripts (run 01–13 on HomeoCentrum_Dev) |
| WEB-10.02 | B-PatientEco | M10 | Web API | N/A/Done/Done/Done | DONE | New HTTP on New-API; existing URLs patched on the live UI host |
| WEB-10.03 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-10.04 | D-QA | M10 | QA | Not Started/Not Started/Not Started/N/A | SKIPPED | QA track skipped |
| WEB-12.01 | B-PatientEco | M10 | Web UI | Done/N/A/Done/N/A | DONE | Web UI on NIGAHomeopathy_UI (landing /board /profile /reception-staff) |
| WEB-12.02 | D-QA | M10 | QA | Not Started/N/A/N/A/N/A | SKIPPED | QA track skipped |
| PAT-06.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-06.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-06.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-06.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-07.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-07.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-07.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-07.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-08.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-08.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-08.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-08.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-09.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-09.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-09.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-09.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-10.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-10.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-10.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-10.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-11.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-11.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-11.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-11.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-12.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-12.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-12.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-12.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-13.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-13.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-13.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-13.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-14.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-14.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-14.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-14.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| PAT-15.01 | C-Mobile | M17 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-15.02 | C-Mobile | M17 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| PAT-15.03 | C-Mobile | M17 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| PAT-15.04 | D-QA | M17 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| DMO-04.01 | C-Mobile | M18 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| DMO-04.02 | C-Mobile | M18 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| DMO-04.03 | C-Mobile | M18 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| DMO-04.04 | D-QA | M18 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| DMO-05.01 | C-Mobile | M18 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| DMO-05.02 | C-Mobile | M18 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| DMO-05.03 | C-Mobile | M18 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| DMO-05.04 | D-QA | M18 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |
| DMO-06.01 | C-Mobile | M18 | Mobile UI | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| DMO-06.02 | C-Mobile | M18 | API Mobile | Done/N/A/Done/N/A | DONE | Mobile API contract on New-API; documented in S2_Week2_MOBILE_API_DOC.txt |
| DMO-06.03 | C-Mobile | M18 | Mobile Frontend | Done/N/A/N/A/N/A | SKIPPED | Mobile UI / RN skipped; Mobile API is documented separately |
| DMO-06.04 | D-QA | M18 | QA | Not Started/N/A/Not Started/N/A | SKIPPED | QA track skipped |

## Not committed

No `git commit` / push from the agent. You commit locally.

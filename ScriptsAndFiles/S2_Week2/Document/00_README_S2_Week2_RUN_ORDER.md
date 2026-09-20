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
| 7 | `07_TEST_Sample_Data_Insert.sql` | Dev/test sample: 10 enquiries, 10 CogRun, 10 bookings (S2TESTED01–10), KYC, fees, hotspot map, Booking policy. |
| 8 | `08_CARE_CATEGORIES_RECEPTION_ARTICLES.sql` | HumanSystemMaster care categories, Dev reception login `s2.reception`, S2-TESTED article body. |
| 9 | `09_WEB_TRU_Doctor_Credential_Documents.sql` | WEB-09.01 TRU-01 tables: DoctorVerification + DoctorCredentialDocument. Backfill one verification row per doctor. |

Scripts are idempotent (IF COL_LENGTH / IF NOT EXISTS). Safe to re-run. Scripts 07 and 08 are Dev/test data only.

## After SQL

1. Point New-API Development connection at `HomeoCentrum_Dev` (same as Old-API). Restart **NIGA_NewAPI** and **NIGA_OldAPI**.
2. Follow `S2_Week2_STATUS_REPORT.md` in this Document folder.
3. API contract: `../S2_Week2_API_DOC.txt`

Login / Rx-write / Razorpay stay on Old-API. New HTTP for S2 is New-API only. No third API.

Existing endpoint patches that the UI still calls on `api` are on **Old-API**. See `S2_Week2_HOST_AUDIT.md`.

Doctor mobile must not get case-taking. Reception JWT includes `DoctorID` + `DoctorUserID` (owning doctor's UserMaster id) and is blocked from clipboard / COG / audio / lab mutate.

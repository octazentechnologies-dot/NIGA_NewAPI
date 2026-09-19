# S2 Week 2 — host audit (FND-01.03)

Rule: new HTTP on New-API (`nigahomeoAPI`). Existing changes on the host the UI already calls. Do not silently switch hosts. Login / Rx-write / Razorpay stay Old-API.

UI source of truth: `NIGAHomeopathy_UI/src/helpers/realbackend_helper.js`

## New HTTP (New-API only) — complete

| SubID | API | Status |
|---|---|---|
| WEB-03.02 | GET /api/Public/Doctors + profile + ranking | Done |
| WEB-04.02 | Slots, Bookings, PatientAuth OTP | Done |
| WEB-01.02 | Public doctor/fee reuse | Done (same Public DTOs) |
| CLN-13.02 | POST /api/RepertorizationPage/CenterOfGravity (+ alias /api/Repertorization/CenterOfGravity) | Done |
| DOC-10.02 | GET/PUT /api/Profile/Me + photo | Done |
| DMO-05 | GET/PUT /api/Availability/Me | Done |
| PAT-08/09 | GET /api/PatientPortal/Home, CareCategories | Done |
| PAT-06–07 | Family / Caregiver (S1 URLs) | Reused |
| PAT-10–15 | Public doctors/articles | Reused Public |
| DMO-04 | Existing dashboard list on New-API | Reused |
| DMO-06 | POST /api/Device/Register | Reused |
| WEB-10.02 | ActivateByToken / ResendActivation | Done (Users already on New-API in UI) |

## Existing changes — placed on the live host

| SubID | What | Live UI host | Where patched |
|---|---|---|---|
| CLN-02–06, 11, 14, 15 | DoctorOnly ACL (reception 403) | Old-API `api` for clipboard, questions, subsection, MM, lab, Rx, notes | **Old-API** controllers. New-API copies also have DoctorOnly but UI does not call them for these. |
| CLN-02.03 / 03.03 / 04.03 / 05.03 / 06.03 / 11.03 | Confirm host unchanged | Old-API | Confirmed. No UI helper switch. |
| CLN-07.02 | Backup intensity | New-API (nigahomeoAPI) | New-API PatientBoardBackup |
| CLN-08 | Audio consent + queue | New-API | New-API AudioCaseTaking (DoctorOnly) |
| CLN-10.02 | GetMateriaMedicaByRemedy | Old-API MM | **Old-API** (was wrongly New-API-only) |
| CLN-12.02 | Keep repertorization APIs | Old-API | Host unchanged; DoctorOnly; COG is new HTTP on New-API |
| CLN-14.03 | Classic PatientLab authoritative | Old-API | Confirmed. DoctorOnly on Old-API PatientLab. |
| CLN-15.03 | Classic notes no dual-write | Old-API | Confirmed. |
| CLN-16.02 | Keep SaveComplaints; add GET | Old-API POST; GET added on Old-API | **Old-API** GetComplaints / GetCaseDetails |
| CLN-17.02 | Timeline payment flags | New-API GetAppointmentListByPatientId (history UI) + Old-API GetPatientBackHostory / dashboard POST | Both |
| CLN-18.01 | Export Excel + clinical PDF | New-API ExportPatients already nigahomeoAPI | New-API ExportCaseToPdf |
| CLN-19 | PatientBoardBackup .NET 8 | New-API | Confirmed. DoctorUserID claim from Old-API login. |
| CLN-20.02 | 3D hotspot SubSectionId | New-API | New-API threeDBodyPart |
| DOC-01.02 | Dashboard DTO flags | New-API GetCountApp (UI) + Old-API GetCountApp | Both |
| DOC-02 | Reception may share dashboard | Both | No DoctorOnly on dashboard |
| DOC-03.02 | lastVisitAt | UI list = New-API GetCasesByUser | **New-API GetCasesByUser** (was missing; only /api/patient had it). Also Old-API GetCases / GetCasesByUser |
| DOC-06 | Export PDF | New-API | Existing ExportPatients PDF |
| DOC-09 | ReceptionStaff JWT bind | New-API CRUD; login Old-API | New-API CRUD + Old-API token DoctorUserID |
| WEB-06.02 | Existing enquiry POST | Landing `API_BASE/EnquiryDetail` Old-API | **Old-API** TicketStatus=New. Admin GET list already existed; now [Authorize] + TicketStatus fields. Extra New-API /api/Enquiry is additive new HTTP. |
| WEB-09.02 | RegisterDoctor Pending | New-API (UI registerDoctor) | New-API |

## Login claim (Old-API) required by New-API backup

Reception token now includes `RoleName=Reception` and `DoctorUserID` (owning doctor's UserMaster id). PatientBoardBackup on New-API keys backups with that claim.

## Skipped (out of this week's agreed scope)

- QA rows
- New Mobile UI screens (PAT/DMO UI)
- New web screens: public /book, COG panel, reception-staff pages, doctor profile tab rewrite, contact/admin enquiry inbox, MM tab UX, 3D viewer UX, complaints POST form, privacy/terms copy, landing redesign

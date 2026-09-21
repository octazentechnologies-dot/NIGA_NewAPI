# S2 Week 2 — host audit (FND-01.03)

Rule: new HTTP on New-API (`nigahomeoAPI`). Existing changes on the host the UI already calls. Do not silently switch hosts. Login / Rx-write / Razorpay stay Old-API.

Combined Week 1+2 catalog: `NIGA_NewAPI/ScriptsAndFiles/Homeocentrum_All_New_And_Updated+APIs.xlsx`

UI source of truth: `NIGAHomeopathy_UI/src/helpers/realbackend_helper.js` and `publicBookingApi.js`.

## New HTTP (New-API only)

| SubID | API | Status |
|---|---|---|
| WEB-03.02 | GET /api/Public/Doctors + profile + ranking | Done |
| WEB-04.02 | Slots, Bookings, PatientAuth OTP | Done |
| WEB-01.02 | Public doctor/fee reuse | Done |
| CLN-13.02 | POST /api/Repertorization/CenterOfGravity | Done |
| DOC-10.02 | GET/PUT /api/Profile/Me + photo + credentials | Done |
| DMO-05 | GET/PUT /api/Availability/Me | Done |
| PAT-08/09 | GET /api/PatientPortal/Home, CareCategories | Done |
| PAT-06–07 | Family / Caregiver | Reused S1 |
| PAT-10–15 | Public doctors/articles | Reused Public |
| DMO-04 | Dashboard list | Reused |
| DMO-06 | POST /api/Device/Register | Reused |
| WEB-09.02/03 | RegisterDoctor + RegisterDoctorWithDocuments + RegistrationStatus | Done |
| WEB-10.02 | ActivateByToken / ResendActivation | Done |
| DOC-09 | ReceptionStaff CRUD (JWT bind) | Done |

## Existing changes — live host

| SubID | What | Live UI host | Where patched |
|---|---|---|---|
| CLN-02–06, 11, 14, 15 | DoctorOnly ACL | Old-API `api` | Old-API controllers |
| CLN-07.02 | Backup intensity | New-API | PatientBoardBackup |
| CLN-08 | Audio consent | New-API | AudioCaseTaking |
| CLN-10.02 | GetMateriaMedicaByRemedy | Old-API | Old-API MM |
| CLN-16.02 | SaveComplaints + GET | Old-API | Patient |
| CLN-17.02 | Timeline payment flags | New-API + Old-API | Both |
| CLN-18.01 | Export PDF | New-API | ExportCaseToPdf |
| CLN-19 | Board backup | New-API | Unchanged host |
| CLN-20.02 | 3D hotspot SubSectionId | New-API | threeDBodyPart |
| DOC-01.02 | Dashboard flags | New-API GetCountApp | UI nigahomeoAPI |
| DOC-03.02 | lastVisitAt | New-API GetCasesByUser | UI nigahomeoAPI |
| WEB-06.02 | Enquiry POST | Old-API landing | TicketStatus=New. Admin list New-API /api/Enquiry |

## Skipped

- QA rows
- New Mobile UI / React Native screens (PAT/DMO *.01/*.03). Mobile **APIs** are in scope and documented.

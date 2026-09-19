# S2 Week 2 — status report

Branch: `S1_Week2_Tufan_V1`  
Database: `HomeoCentrum_Dev` on `localhost\MSSQLSERVER25`  
Host rule: new HTTP on New-API; existing changes on the host the UI already calls (FND-01.03). Login / Rx / Razorpay stay Old-API.

## Host correction (this pass)

Earlier work put some **existing** patches only on New-API while the UI still calls Old-API `api`. That is now fixed:

- DoctorOnly ACL on classic clipboard / questions / subsection / MM / lab / Rx / notes / repertorization (**Old-API**).
- GetMateriaMedicaByRemedy on classic MM (**Old-API**).
- GetComplaints / GetCaseDetails on classic Patient (**Old-API**).
- lastVisitAt / dashboard flags / enquiry TicketStatus / timeline payment fields on **Old-API** counterparts.
- lastVisitAt on the **live** patient list: New-API `GET /api/patientApp/GetCasesByUser/{userId}` (UI uses nigahomeoAPI this URL).
- Reception login JWT `DoctorUserID` + `RoleName` on **Old-API** TokenService (login stays classic).

See `S2_Week2_HOST_AUDIT.md` for the one-by-one SubID map.

## Done (in-scope)

- SQL pack 01–06 executed on Dev.
- New-API: Public doctors, booking token, PatientAuth OTP, policies, articles, Profile, Availability, PatientPortal, COG, RegisterDoctor Pending, ActivateByToken, ExportCaseToPdf, 3D SubSectionId, backup intensity, ReceptionStaff JWT bind, audio consent.
- Old-API existing: DoctorOnly, lastVisitAt, dashboard/timeline flags, MM get-by-remedy, complaints GET, enquiry TicketStatus, reception claims.
- Existing UI function changes: last-visit column, dashboard Online/Tele/Unpaid slots, export labels, board visit/consult placeholders, clipboard delete confirm, history payment badge.

## Confirm-host (no silent switch)

| Call | Host |
|---|---|
| Login, Rx save, Razorpay, MM, clipboard, questions, subsection, lab, history notes, landing enquiry | Old-API `api` |
| RegisterDoctor, activate, dashboard counts, patient list GetCasesByUser, export patients, 3D, board backup, audio | New-API `nigahomeoAPI` |
| New Public / PatientAuth / Profile / Availability / COG / PatientPortal | New-API only |

## Not this sprint

- New web screens (COG panel, /book, reception-staff pages, profile tab rewrite, enquiry inbox, landing/privacy/terms).
- New mobile app screens. APIs above are the contracts.
- QA rows.

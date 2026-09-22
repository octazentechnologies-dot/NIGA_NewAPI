# S2 Week 2 — Demo proves: present and tested

Checked: 18-09-2026 on branch `S1_Week2_Tufan_V1`.  
Tracker: 187 compact SubIDs (Excel **Demo proves** = compact `SubTask`).  
Database: `HomeoCentrum_Dev` on `localhost\MSSQLSERVER25`.  
Live processes: New-API `:5038` (swagger **339** paths, pre-Week-2). Old-API `:5000`.  
Isolated compile (not loaded by those PIDs): `.tmp/newapi_out/Niga-Web.dll`, `.tmp/oldapi_out/NIGA.Centrum.API.dll`.

## Verdict

**Schema Demo proves are present and SQL-tested (`06` PASS=11 FAIL=0).**  
**Source + existing-UI Demo proves for this week’s APIs, ACL, DTOs, and function changes are present.**  
**Most new HTTP Demo proves are not in the running `:5038` / `:5000` processes.** Isolated DLLs already contain `api/Public`, `PatientAuth`, `CenterOfGravity`, `GetComplaints`, `GetMateriaMedicaByRemedy`, `DoctorOnly`, `ForbidIfReception`. Restart both APIs before a mentor live demo.

Counts: **187** Demo-prove tasks. **54** QA skipped. **26** new mobile UI/frontend skipped. **17** new web screens skipped. Remainder in-scope: present in source/schema/UI; live HTTP mixed (S1 routes pass, Week-2 routes 404 until restart).

---

## Tests that ran

| Test | Result |
|---|---|
| `05_VERIFY_S2_Week2.sql` | PASS — Doctor directory columns, `DoctorPayeeKyc`, **8** VisibleVerified doctors, booking columns (`BookingToken`, `VisitType`, `ConsultMode`, `PaymentStatus`, `IsTele`, `HoldExpiresAt`), `PolicyVersion` Privacy+Terms **2026.09**, `EnquiryDetails.TicketStatus/AssignedTo`, `CogRun`, `UserMaster.ActivationTokenHash/ExpiresAt`, hotspot `SubSectionId`, existing clipboard/MM/3D/schedule/reception tables |
| `06_UNIT_TEST_S2_Week2_Guards.sql` | **PASS=11 FAIL=0** |
| Appointment flag data | Columns exist; **0** rows currently have `PaymentStatus` / `VisitType` / `ConsultMode` / `IsTele` (nullable until later phases) |
| 3D hotspot name backfill | **0 / 5** exact matches |
| `AudioCaseFastPathGuardsTests` | **Passed 4 / 4** |
| Live New-API swagger | 339 paths; **no** Public, PatientAuth, CenterOfGravity, Availability, PatientPortal, ExportCaseToPdf, GetComplaints |
| Live `GET /api/Public/Doctors` | **404** |
| Live `POST` CenterOfGravity (both routes) | **404** |
| Live `GET /api/Family` (patient JWT) | **200** `[]` |
| Live `POST /api/Device/Register` | **200** |
| Live audio health | **200** `Healthy` |
| Live `RegisterDoctor` | route present (swagger + OPTIONS 405) |
| Live `POST /api/users/ActivateUser` | **400** (route present; dummy token) |
| Live `POST ActivateByToken` / `ResendActivation` | **405** — live `{userId}` GET captures that path |
| Live `GetCasesByUser/{other}` | **403** ownership |
| Live `GetCasesByUser/{self}` | **200** `[]` (no lastVisitAt sample) |
| Live Old-API enquiry GET, no JWT | **200** `[]` (source now `[Authorize]` — process stale) |
| Live Old-API `GetComplaints` / `GetCaseDetails` / MM-by-remedy | **404** (process stale) |
| Live clipboard / clinical questions, no JWT | **401** |
| Isolated DLL strings | Public, PatientAuth, CenterOfGravity, GetComplaints, GetMateriaMedicaByRemedy, DoctorOnly, ForbidIfReception **present** |
| PatientAuth OTP live call | **not sent** (would insert OTP) |
| Enquiry POST live call | **not sent** (SMTP side effect) |
| Reception ACL 403 | **not proven live** (no reception JWT; `DoctorOnly` blocks `RoleName=Reception` only, not Patient) |

---

## One-by-one (all 187)

Legend: **P** present in this week’s source/schema/UI. **T** tested as noted. **SKIP** out of agreed build scope (QA / new mobile screens / new web screens). **RESTART** present in isolated build, missing on live PID.

### M03 Clinical workspace

| SubID | Demo proves | Present | Test |
|---|---|---|---|
| CLN-01.01 | Patient Board header appointment / visit / consult placeholders | **P** always-visible chips `Appointment:` / `Visit:` / `Consult:` in `PatientBoard.js`; empty shows `—`; dashboard path forwards query params | UI + dashboard path |
| CLN-01.02 | Do not split board; doctor mobile will NOT get case-taking | **P** one `PatientBoardRoute`; alias `/patientboard`; no RN doctor app here; `docs/CLN-01.02_SINGLE_CLINICAL_BOARD.md` | source + architecture doc |
| CLN-01.03 | Regression: dashboard remounts via PatientBoardRoute | SKIP QA | — |
| CLN-02.01 | Regression: create case, rubrics, repertorize | SKIP QA | — |
| CLN-02.02 | ACL: only treating doctor (not Reception) | **P** `[DoctorOnly]` + `ForbidIfReception` on Old-API clipboard/questions/MM/lab/Rx/notes/repertorize | source+DLL; live reception 403 **unproven** (need restart + reception JWT) |
| CLN-02.03 | Clipboard/repertorization still on correct host | **P** UI `api.post` `/clipboardRubrics/...` and elimination stay Old-API | helper grep |
| CLN-03.01 | Regression: body part → rubrics; intensity | SKIP QA | — |
| CLN-03.02 | ACL doctor-only | **P** same DoctorOnly on intensity/body-part controllers | RESTART for live |
| CLN-03.03 | API host unchanged | **P** | helper grep |
| CLN-04.01 | Regression: question drill-down | SKIP QA | — |
| CLN-04.02 | ACL doctor-only | **P** QuestionSection/Group/SubGroup/ClinicalQuestions `[DoctorOnly]` | RESTART |
| CLN-04.03 | API host unchanged | **P** UI still `api` | helper grep |
| CLN-05.01 | Regression: diagnosis + therapeutics | SKIP QA | — |
| CLN-05.02 | ACL doctor-only | **P** | RESTART |
| CLN-05.03 | API host unchanged | **P** diagnosis still `api` | helper grep |
| CLN-06.01 | Regression: tree + search + clipboard | SKIP QA | — |
| CLN-06.02 | ACL doctor-only | **P** SubSection `[DoctorOnly]` | RESTART |
| CLN-06.03 | Subsection search host documented | **P** classic `api` + 3D search on nigahomeoAPI (unchanged) | helper grep |
| CLN-07.01 | No new clipboard table | **P** `ClipboardRubrics` still exists | SQL 05 |
| CLN-07.02 | Persist intensity on backup payload | **P** `BoardBackupIntensityMerger` | source |
| CLN-07.03 | Clipboard UX: confirm delete; intensity; count | **P** Swal `Remove this rubric from clipboard?` + COG note | UI grep |
| CLN-07.04 | QA remove/restore | SKIP QA | — |
| CLN-08.01 | Regression: audio record/poll/approve | SKIP QA | — |
| CLN-08.02 | Consent writes AudioCaseConsentLog (also ConsentRecord) | **P** dual-write in `AudioCaseTakingService` | source |
| CLN-08.03 | Audio queue/worker healthy | **P** | LIVE health **200 Healthy**; latest empty OK |
| CLN-09.01 | Follow AUDIO_CASE_TAKING accuracy docs | **P** | **unit 4/4 PASS** |
| CLN-09.02 | Admin metaphor/alias approve-reject UX | **P** `admin/listrubricmetaphors` | UI route |
| CLN-09.03 | QA golden transcripts | SKIP QA | — |
| CLN-10.01 | MM: no schema change | **P** `MateriaMedicaMaster` exists | SQL 05 |
| CLN-10.02 | Lightweight get-by-remedy | **P** Old-API `GET .../GetMateriaMedicaByRemedy/{id}` | LIVE **404**; isolated DLL **has** string |
| CLN-10.03 | MM tab faster head navigation | SKIP new UX | — |
| CLN-10.04 | QA open MM | SKIP QA | — |
| CLN-11.01 | Regression: drug → side effects | SKIP QA | — |
| CLN-11.02 | ACL doctor-only | **P** AllopathicDrug `[DoctorOnly]` | RESTART |
| CLN-11.03 | API host unchanged | **P** | helper grep |
| CLN-12.01 | Repertorize: no schema change | **P** | SQL 05 |
| CLN-12.02 | Keep repertorization APIs | **P** host unchanged; COG is **new** HTTP on New-API only | helper + source |
| CLN-12.03 | Repertorize tab stability; COG beside it | **P** partial — clipboard note `COG uses this clipboard`; full COG panel skipped as new screen | UI grep |
| CLN-12.04 | QA known case top remedies | SKIP QA | — |
| CLN-13.01 | COG optional CogRun; no mandatory table | **P** `CogRun` table exists (0 rows yet) | SQL 05 |
| CLN-13.02 | POST CenterOfGravity ranked + reasons | **P** intensity × GradeNo; empty clipboard message; alias `/api/Repertorization/CenterOfGravity` | LIVE **404**; isolated DLL **has** CenterOfGravity |
| CLN-13.03 | COG sub-panel UI | SKIP new screen | — |
| CLN-13.04 | QA COG vs clipboard | SKIP QA | — |
| CLN-14.01 | Regression lab order/result | SKIP QA | — |
| CLN-14.02 | ACL doctor-only | **P** PatientLab `[DoctorOnly]` | RESTART |
| CLN-14.03 | Classic PatientLab authoritative | **P** UI still `api` lab order/entry | helper grep |
| CLN-15.01 | Regression notes | SKIP QA | — |
| CLN-15.02 | ACL doctor-only | **P** AppointmentHistoryNote `[DoctorOnly]` | RESTART |
| CLN-15.03 | Do not dual-write; classic notes | **P** | helper grep |
| CLN-16.01 | Complaints: existing tables | **P** | SQL |
| CLN-16.02 | Keep SaveComplaints; add GET | **P** Old-API `GetComplaints` / `GetCaseDetails` | LIVE **404**; isolated DLL **has** GetComplaints |
| CLN-16.03 | Board form that POSTs complaints | SKIP remaining form UX | — |
| CLN-16.04 | QA save/reload/second doctor | SKIP QA | — |
| CLN-17.01 | History: no new table | **P** compose appointment + notes + Rx | SQL |
| CLN-17.02 | Timeline DTO including payment status | **P** source maps `PaymentStatus`/`IsTele`/`VisitType`/`ConsultMode` | LIVE list **200** 15 rows but **no** those keys (stale). DB values all NULL |
| CLN-17.03 | History panel payment badge | **P** `[Payment pending]` in `patient_history_helper.js` | UI grep |
| CLN-17.04 | QA multi-visit / empty | SKIP QA | — |
| CLN-18.01 | Export Excel + clinical PDF | **P** `ExportCaseToPdf` on New-API | LIVE **404** |
| CLN-18.02 | Board toolbar Export Excel/PDF | SKIP new toolbar | — |
| CLN-18.03 | QA export + 403 | SKIP QA | — |
| CLN-19.01 | Regression backup/restore stack | SKIP QA | — |
| CLN-19.02 | ACL backup scoped to DoctorId | **P** ownership + `DoctorUserID` claim | source |
| CLN-19.03 | PatientBoardBackup on .NET 8 unchanged | **P** UI still nigahomeoAPI backup URLs | helper grep |
| CLN-20.01 | 3D: no new tables | **P** mesh/section/hotspot masters exist | SQL 05 |
| CLN-20.02 | Fix hotspot mapping gaps | **PARTIAL** column + index + DTO/service; exact-name backfill **0/5** | SQL mapped=0. LIVE hotspot list **200**. Needs curated map (LIKE would attach thousands of rubrics) |
| CLN-20.03 | Viewer UX incomplete/fallback | SKIP remaining UX | — |
| CLN-20.04 | QA hotspot → subsection | SKIP QA | — |

### M04 Doctor ops

| SubID | Demo proves | Present | Test |
|---|---|---|---|
| DOC-01.01 | No new dashboard table | **P** | SQL |
| DOC-01.02 | DTO payment/tele flags | **P** `teleQueueCount` / `unpaidCount` / `isOnline` | LIVE Old GetCountApp JSON **lacks** those keys (stale) |
| DOC-01.03 | Chrome: Online / tele queue / unpaid | **P** `Widgets.js` | UI grep |
| DOC-01.04 | QA buckets + reception layout | SKIP QA | — |
| DOC-02.01 | Regression bucket counts | SKIP QA | — |
| DOC-02.02 | Reception sharing this view is OK | **P** no DoctorOnly on dashboard counts | source |
| DOC-02.03 | Existing dashboard count APIs | **P** UI `nigahomeoAPI` GetCountApp | LIVE route **401** without doctor JWT |
| DOC-03.01 | Search DTO fields, existing Patient+Appointment | **P** | SQL + source `LastVisitAt` |
| DOC-03.02 | Search DTO includes lastVisitAt | **P** New-API `GetCasesByUser` binds `LastVisitAt`; 196 dated appointments exist | LIVE self list `[]` so field not observed; restart to prove JSON |
| DOC-03.03 | List column last visit | **P** `Last visit` column in `BestSellingProducts.js` | UI grep |
| DOC-03.04 | QA search/open case | SKIP QA | — |
| DOC-04.01 | Regression add patient | SKIP QA | — |
| DOC-04.02 | ACL doctor/reception of that clinic | **P** ownership helpers (S1) | source |
| DOC-04.03 | API unchanged | **P** create patient still nigahomeoAPI | helper grep |
| DOC-05.01 | Regression import | SKIP QA | — |
| DOC-05.02 | ACL doctor-only (or reception if already allowed) | **P** | source |
| DOC-05.03 | API unchanged | **P** | helper grep |
| DOC-06.01 | Audit export; PDF if missing | **P** ExportPatients already on New-API | swagger lists route; one probe 404 on query shape |
| DOC-06.02 | UI Excel / CSV / PDF labelled | **P** `Excel, CSV, or PDF` | UI grep |
| DOC-06.03 | QA another doctor cannot export | SKIP QA | — |
| DOC-07.01 | Regression activity | SKIP QA | — |
| DOC-07.02 | ACL scoped to doctor | **P** | source |
| DOC-07.03 | API unchanged | **P** | helper grep |
| DOC-08.01 | Regression stats charts | SKIP QA | — |
| DOC-08.02 | ACL doctor-only | **P** | source |
| DOC-08.03 | API unchanged | **P** | helper grep |
| DOC-09.01 | Authorise doctor owns staff on existing CRUD | **P** + Old-API login JWT `DoctorUserID` | source |
| DOC-09.02 | Build /doctor/reception-staff pages | SKIP new screen | — |
| DOC-09.03 | QA reception cannot manage other staff | SKIP QA | — |
| DOC-10.01 | Doctor / DoctorPayeeKyc fields | **P** table exists (0 KYC rows yet) | SQL 05 |
| DOC-10.02 | GET/PUT /api/Profile/Me + photo | **P** New-API `DoctorProfileController` | LIVE **404** (swagger only `PatientProfile/Me`) |
| DOC-10.03 | Replace Velzon /profile tabs | SKIP new screen | — |
| DOC-10.04 | QA reception cannot edit bank | SKIP QA | — |

### M10 Patient website

| SubID | Demo proves | Present | Test |
|---|---|---|---|
| WEB-01.01 | Home: no schema | **P** | SQL |
| WEB-01.02 | Reuse public doctor/fee APIs | **P** Public DTOs | LIVE Public **404** |
| WEB-01.03 | HomeLandingPage book/doctors/pricing | SKIP new landing | — |
| WEB-01.04 | QA mobile viewport /book | SKIP QA | — |
| WEB-02.01 | Public card in-clinic + tele fee | SKIP new UI (API fields on Doctor) | SQL fees exist |
| WEB-02.02 | /pricing labelled vs consult fees | SKIP new UI | — |
| WEB-02.03 | QA fee vs checkout | SKIP QA | — |
| WEB-03.01 | Public doctor search indexes | **P** DirectoryVisible + Verified (**8** doctors) | SQL |
| WEB-03.02 | GET /api/Public/Doctors + filters + profile | **P** `PublicController` | LIVE **404**; isolated DLL **has** api/Public |
| WEB-03.03 | /book list + profile pages | SKIP new screen | — |
| WEB-03.04 | QA unverified excluded | SKIP QA | — |
| WEB-04.01 | BookingToken + PatientAuth OTP schema | **P** | SQL 05 |
| WEB-04.02 | Public slots/create/token + PatientAuth OTP | **P** | LIVE **404**; OTP **not fired** |
| WEB-04.03 | /book slots/confirm UI | SKIP new screen | — |
| WEB-04.04 | QA double-book / OTP | SKIP QA | — |
| WEB-05.01 | /book/pay pages | SKIP new screen | — |
| WEB-05.02 | QA pay-at-clinic / failed pay | SKIP QA | — |
| WEB-06.01 | Enquiry Status/AssignedTo | **P** `TicketStatus` + `AssignedTo` | SQL 05 |
| WEB-06.02 | Existing enquiry POST; admin list | **P** Old-API POST sets `TicketStatus=New`; GET `[Authorize]`; additive New-API `/api/Enquiry` | LIVE unauth GET still **200** (stale). POST not fired (SMTP) |
| WEB-06.03 | /contact + /admin/enquiries | SKIP new screen | — |
| WEB-06.04 | QA guest submit → admin row | SKIP QA | — |
| WEB-07.01 | Replace /privacy; store PolicyVersion | **P** DB `PolicyVersion` Privacy **2026.09** current; UI copy skipped | SQL |
| WEB-07.02 | QA booking consent version | SKIP QA | — |
| WEB-08.01 | Replace /terms; version stamp | **P** DB Terms **2026.09**; UI copy skipped | SQL |
| WEB-08.02 | QA terms checkbox | SKIP QA | — |
| WEB-09.01 | Register docs via TRU-01 tables | **P** | source/S1 tables |
| WEB-09.02 | RegisterDoctor sets Pending; not directory-live | **P** `VerificationStatus = "Pending"` | LIVE route present; Pending in source |
| WEB-09.03 | /register pending UX | SKIP remaining register UX | — |
| WEB-09.04 | QA register → admin queue | SKIP QA | — |
| WEB-10.01 | Activation token TTL | **P** `ActivationTokenHash` + `ActivationExpiresAt` | SQL 05 |
| WEB-10.02 | Resend activation; not TRU-04 gate | **P** `ActivateByToken` + `ResendActivation` | LIVE ActivateUser **400**; new POSTs **405** until restart |
| WEB-10.03 | Login activation query-param UX | SKIP remaining UX | — |
| WEB-10.04 | QA used/expired/resend | SKIP QA | — |
| WEB-12.01 | Public articles list/detail UI | SKIP new UI (API `Public/Articles` in source) | LIVE Articles **404** |
| WEB-12.02 | QA unpublished hidden | SKIP QA | — |

### M17 Patient app APIs (UI skipped)

| SubID | Demo proves | Present | Test |
|---|---|---|---|
| PAT-06.01/03 | Family screens / a11y | SKIP mobile UI | — |
| PAT-06.02 | Wire Family API | **P** `/api/Family` | LIVE GET **200** `[]` |
| PAT-06.04 | QA family | SKIP QA | — |
| PAT-07.01/03 | Caregiver screens | SKIP mobile UI | — |
| PAT-07.02 | Wire Caregiver API | **P** `/api/Caregiver` | LIVE GET **404** (stale); source present |
| PAT-07.04 | QA caregiver | SKIP QA | — |
| PAT-08.01/03 | Home dashboard screens | SKIP mobile UI | — |
| PAT-08.02 | Wire PatientPortal Home | **P** `GET /api/PatientPortal/Home` | LIVE **404** |
| PAT-08.04 | QA home | SKIP QA | — |
| PAT-09.01/03 | Search/care category screens | SKIP mobile UI | — |
| PAT-09.02 | Wire CareCategories | **P** `GET /api/PatientPortal/CareCategories` | LIVE **404** |
| PAT-09.04 | QA categories | SKIP QA | — |
| PAT-10–14 *.01/*.03 | Discovery/profile/badge/ranking screens | SKIP mobile UI | — |
| PAT-10–14 *.02 | Wire Public doctors/filters/profile/ranking | **P** `PublicController` | LIVE **404** |
| PAT-10–14 *.04 | QA those flows | SKIP QA | — |
| PAT-15.01/03 | Articles screens | SKIP mobile UI | — |
| PAT-15.02 | Wire Public articles | **P** | LIVE **404** |
| PAT-15.04 | QA articles | SKIP QA | — |

### M18 Doctor app APIs (UI skipped)

| SubID | Demo proves | Present | Test |
|---|---|---|---|
| DMO-04.01/03 | TodayQueue screen / pull-to-refresh | SKIP mobile UI | — |
| DMO-04.02 | Existing appointment list APIs | **P** New-API dashboard/appointment | LIVE GetCountApp route exists (**401** without doctor JWT) |
| DMO-04.04 | QA parity with web counts | SKIP QA | — |
| DMO-05.01/03 | Availability screens / offline banner | SKIP mobile UI | — |
| DMO-05.02 | TEL-01 + hours APIs | **P** `GET/PUT /api/Availability/Me` | LIVE **404** |
| DMO-05.04 | QA web/app heartbeat | SKIP QA | — |
| DMO-06.01/03 | Push handling / in-app banner | SKIP mobile UI | — |
| DMO-06.02 | Device register | **P** `POST /api/Device/Register` | LIVE **200** |
| DMO-06.04 | QA deep links | SKIP QA | — |

---

## Gaps before a live mentor demo

1. **Restart New-API `:5038` and Old-API `:5000`** (file locks on current PIDs). After restart, swagger must list `/api/Public/Doctors`, `/api/PatientAuth/*`, `/api/Repertorization/CenterOfGravity`, `/api/Profile/Me`, `/api/Availability/Me`, `/api/PatientPortal/Home`; Old-API must **401** enquiry GET without JWT and expose `GetComplaints` + `GetMateriaMedicaByRemedy`.
2. **CLN-20.02** — `SubSectionId` is on the table/API; **0 of 5** hotspots mapped because names (`Cervical region`, …) do not equal `SubSectionName`. Do not LIKE-backfill (thousands of hits). Needs a curated map.
3. **Reception ACL** — prove with a reception login after restart. Patient JWT returning 200 on clipboard is expected: `DoctorOnly` only blocks `Reception`.
4. **Dashboard/timeline new fields** serialize null until `PaymentStatus` / `IsTele` / `VisitType` are populated (later phases).

# S2 Week 2 — sequential task review

Rule: one in-scope SubID at a time. Excel Done is ignored. Skip QA, Mobile UI, Mobile Frontend.

Host: New-API `:5038`, Old-API `:5001`, UI `:3000`. Clinic login Old-API. Dev user for this row: `Tufan_Doctor` / `123456`.

---

## CLN-01.01 — Patient Board header placeholders

| Field | Value |
|-------|--------|
| Track / Module | A-ClinicWeb / M03 |
| Bifurcation | UI (doctor web portal) |
| Work type | Existing Improvement |
| Spec | Patient Board header shows appointment, visit type, consult mode **placeholders** (wired in Phases 4 and 6) |
| Out of scope | Full booking CRUD, live VisitType/ConsultMode API bind, mobile case-taking, QA |

### Code vs description

**Before this pass (partial):**

- Visit and Consult chips existed, but defaulted to `In-clinic` / `Clinic` even when no appointment data was passed (looked like live values).
- Appointment date rendered only when `?appointmentDate=` was on the URL. Opening `/doctor/patientboard` hid the appointment slot.
- Dashboard `buildPatientBoardPath` did not forward `visitType` / `consultMode` / `isTele`.
- `No Upcoming Appointment` still showed when an appointment date was already in the header.

**After this pass (Week 2 Done):**

- Header always shows three labelled chips: `Appointment:`, `Visit:`, `Consult:`.
- Missing values display `—` (honest placeholder until Phase 4/6).
- `?appointmentDate=` still formats as `Do MMMM, YYYY` when present.
- `?visitType=` / `?consultMode=` override Visit/Consult; `?isTele=true` maps Consult to `Tele`.
- Dashboard and session resume paths forward those query fields when the appointment row has them.
- Route stays Doctor-only (`DOCTOR_CASE_ROUTE_ROLES`). Reception cannot open case-taking.
- No new CRUD screen. Due Amount chip left as existing chrome (not this ticket).

Files: `NIGAHomeopathy_UI/src/pages/Doctor/PatientBoard/PatientBoard.js`, `helpers/patientBoardSessionHelper.js`, `hooks/usePatientBoardSessionPersistence.js`.

### Manual check (22 Sep 2026, `Tufan_Doctor` / `123456`)

- Dashboard → Tufan Patient → Manual case taking → header: `Appointment: 22nd September, 2026` / `Visit: First` / `Consult: InClinic`. URL carried `visitType=First&consultMode=InClinic`. `No Upcoming Appointment` hidden.
- Same patient with only `patientId`+`caseId`: `Appointment: —` / `Visit: —` / `Consult: —` and `No Upcoming Appointment` shown.

### Inputs / confirmation needed from you (Phase 4/6, not blocking Week 2)

1. **Canonical labels** — booking confirm currently writes `VisitType`/`ConsultMode` as `InClinic` / `Tele`. Confirm production chips should show `In-clinic` vs `InClinic` vs `Walk-in`, and Consult as `Clinic` vs `In-clinic` vs `Tele` vs `Audio`.
2. **Live bind** — confirm header should read `PatientAppointment.VisitType` / `ConsultMode` / `IsTele` in Phase 4/6 (web booking + clinic walk-in). Week 2 must not invent that fetch.
3. **Which appointment** — when a patient has several rows, confirm the header uses the dashboard-selected `patientAppId` (current) rather than “next upcoming”.
4. **Time on the chip** — appointment time exists on the dashboard row but is not on the header. Confirm whether Phase 4 should add `hh:mm A` next to the date.
5. **Due Amount ₹ 0.00** — always shown; not CLN-01.01. Confirm whether billing should replace this later or hide it when not in scope.

### Suggestions (senior)

- Keep placeholders as `—` until a real value exists. Do not default Visit to In-clinic; that trains doctors on fake data.
- Do not fetch appointment flags from a new API for this ticket. Phase 4 booking already has the columns; wire the existing GET into the header then.
- Reception must stay off this screen (already Doctor-only). Do not add a Reception-safe “view only” board in Week 2.

### Verdict

**DONE for Week 2** (placeholders on the existing Patient Board header). Full-proof live values need the Phase 4/6 confirmations above — no further Week 2 coding unless those answers change the placeholder contract.

---

## CLN-01.02 — Do not split the board; doctor mobile will NOT get case-taking

| Field | Value |
|-------|--------|
| Track / Module | C-Mobile listed in Excel / M03 — work is **Web Other** (architecture + docs) |
| Bifurcation | Web Other (not Mobile UI — that track is skipped) |
| Work type | Existing (Tech Lead / documentation) |
| Spec | Do not split the board into multiple apps; mobile doctor app will NOT get case-taking (PDF §3) |
| Out of scope | Building a doctor mobile app; QA CLN-01.03; splitting Patient Board |

### Code vs description

**Was partial as a ticket:** the board was already one SPA screen, and mobile API text already said “do not call case-taking”, but the intern spec requires a dedicated architecture markdown under the UI docs folder. That file did not exist.

**Now:**

- One component: `PatientBoard.js` via `PatientBoardRoute`.
- `/doctor/patientboard` and `/patientboard` are aliases (`allRoutes.js` comment). Not two apps.
- No React Native / Flutter doctor app in this workspace.
- Doctor-only route guard. Reception cannot open the board.
- Canonical doc: `NIGAHomeopathy_UI/docs/CLN-01.02_SINGLE_CLINICAL_BOARD.md` (copy under `NIGA_NewAPI/ScriptsAndFiles/S2_Week2/Document/`).
- Forbidden mobile URLs remain listed in `S2_Week2_MOBILE_API_DOC.txt`.

`[DoctorOnly]` on classic case-taking APIs blocks Reception/Patient. A **Doctor** JWT from a phone would still be accepted. PDF §3 is “do not ship case-taking on doctor mobile”, not User-Agent blocking.

### Inputs / confirmation needed from you

1. **Doctor mobile repo** — if a separate React Native / native doctor app exists outside this workspace, confirm its path so case-taking screens can be audited there. Nothing in these three repos is a doctor case-taking app.
2. **Hard reject on API** — confirm you do **not** want a `client=doctor-mobile` claim that 403s clipboard/COG for Doctor JWTs. Default: keep web working; mobile team simply never calls those URLs.
3. **Alias URL** — confirm `/patientboard` stays forever as a bookmark alias (recommended) vs redirect-only to `/doctor/patientboard`.

### Suggestions

- Do not split the board for tablet vs desktop. Responsive CSS on the same page is enough.
- Do not add case-taking to patient mobile either (patient app is Family / portal, not repertory).

### Verdict

**DONE for Week 2.** Statement is true in this workspace; the missing intern doc is now in place. Full-proof against a hidden doctor-mobile repo needs confirmation (1).

Next in-scope row (skip CLN-01.03 QA): **CLN-02.02**.

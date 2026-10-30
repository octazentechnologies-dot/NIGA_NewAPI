# NIGA — Notifications & Real-time Audit (evidence-based)

**Date:** 2026-10-29 (Production-readiness verification)  
**Scope:** New-API, Web UI, Doctor-App, Patient-App  
**Rule:** Reuse existing architecture; no duplicate transports. **No production changes were made.**

---

## 0. Executive verdict

**DEV SMOKE: PASS (with known residual gaps)** — authenticated Dev smoke completed. Staging/Production **not** approved. Not a 100% guarantee for Prod.

| Gate | Result |
|---|---|
| Unit tests (notification/realtime) | **42 passed** |
| Dev outbox schema + worker | **Pass** |
| Seeded Dev users | `NigaSmoke_Doctor` / `NigaSmoke_Patient` (pwd `123456`, Dev only) |
| Login + book + cancel | **Pass** (appt `29199` → CANCELLED) |
| Inbox doctor+patient | **Pass** (after duplicate-push fix) |
| Preference Appointments=false | **Pass** — patient inbox `PushStatus=SKIPPED` (id 10035) |
| Hubs negotiate (auth) | **Pass** patient+doctor; unauth **401** |
| Clinic hub rejects non-doctor join | **Hardened** — `OnConnected` requires DoctorID claim |
| Firebase provider `SENT` | **Not claimed** — Dev Firebase off/not configured → doctor outbox `FAILED` / retry (expected) |
| Real-device / iOS plist | **Still blocked** |
| Staging / Production | **NOT RUN / NOT APPROVED** |

### Bugs found & fixed

| # | Severity | Issue | Fix |
|---|---|---|---|
| 1 | Medium | Duplicate patient inbox on **book** (Confirm push + fanout) | `skipPush: true` on confirm; fanout owns inbox/push/realtime |
| 2 | Medium | Cancel: patient got push but **no patient SignalR**; inconsistent with book | Cancel uses `skipPush` + `NotifyPatientAsync` |
| 3 | Medium | Appointment-request accept paths: confirm push without fanout realtime | Same skipPush + `NotifyPatientAsync` on MobileClinic accept |
| 4 | Medium | Patient could start clinic hub session | Hub `OnConnected` requires DoctorID; UI `clinicRealtime` skips non-Doctor/Reception |
| 5 | Low | Accidental `Domain/nul` file | Removed |
| 6 | Low | Vague Firebase worker error when JSON file missing | Clearer outbox `LastError` (Enabled / config / file missing) |

### Residual blockers for “100% smoke” (need from ops/you — not inventable)

1. **Firebase service-account JSON** on the API host at `Firebase:CredentialsPath` (file is **missing** in this workspace; do not commit secrets).  
2. **Restart API** after placing credentials so worker can reach `SENT`.  
3. **Physical Android device** (or emulator with FCM) logged in as smoke doctor/patient + token registered.  
4. **iOS**: `GoogleService-Info.plist` for Doctor-App + Patient-App (still missing).  
5. **Manual browser check**: two tabs doctor dashboard + patient dashboard while booking (UI debounce already coded).  
6. **Stage** (optional next): apply `07_*.sql`, deploy, repeat smoke — separate from Dev.  
7. **Production**: separate explicit approval only.

Dev accounts for retest: `NigaSmoke_Doctor` / `NigaSmoke_Patient` / `123456` on `HomeoCentrum_Dev` only.

---

## 1. Repository baseline (inspected)

| Project | Path | Branch | HEAD (at audit) | Notes |
|---|---|---|---|---|
| New-API | `New-API/NIGA_NewAPI` | `Dev_V2.0` | `f237f59d…` | Phase 2 files largely **uncommitted** + staged delete of accidental `Domain/nul` |
| Web UI | `UI/NIGAHomeopathy_UI` | `Dev_V3.0` | `d7c54e69…` | Realtime helpers / pages modified, uncommitted |
| Patient-App | `Patient-App` | `Devlopment_1.1` | local mods to notifications | Android `google-services.json` present; **iOS plist missing** |
| Doctor-App | `Doctor-App` | `one_shot_api_md_give_to_integrate` | local notification mods | Same iOS gap |

UI `config.js` currently points at **local** `http://localhost:5050/api` (Dev/Stage URLs commented).  
API `appsettings.json` targets host fragment `103.196.187.99` (Dev/Stage farm — **not** production). Firebase Enabled=true in config (credentials not logged).

### Code present (verified)

- `FirebasePushNotificationSender`, `PushNotificationOutboxWorker`, `PushOutboxBackoff`, `IPushOutboxProcessor`
- `07_PushNotificationOutbox.sql` (+ idempotent secondary index)
- `ClinicRealtimeHub` `/hubs/clinic`, `PatientRealtimeHub` `/hubs/patient`
- `RealtimeGroupResolver` (claim-only groups; client ids ignored)
- `ClinicInboxFanout` + patient/doctor realtime publish
- UI: `clinicRealtime.js`, `patientRealtime.js`, Widgets, schedule, requests, NotificationDropdown, logout stops
- Docs: this file, `docs/FCM-Mobile-API.md`

### Defects fixed in this verification pass

1. Removed accidental tracked file `Homeocentrum.Niga.API.Domain/nul` (MSBuild temp path junk).
2. Added `RealtimeGroupResolver` + hub wiring so forged client user/doctor ids cannot select another group.
3. Added `RealtimeGroupAuthorizationTests` (negative auth).
4. Aligned runtime `EnsureSchemaAsync` + SQL script for `IX_PushNotificationOutbox_User_Created` when table already exists.

---

## 2. Historical test-count reconciliation (34 vs 37)

Both totals were **real**, from **different filters** — not double-counting and not a reporting mistake.

| Command / filter | Discovered | Passed | Failed | Skipped | Exit | Explanation |
|---|---:|---:|---:|---:|---:|---|
| `--filter "…PushOutboxBackoffTests\|…AppNotificationInboxTests"` | 34 | **34** | 0 | 0 | 0 | Inbox (28 cases) + backoff (6) |
| Same + `\|…RealtimeHubGroupTests` | 37 | **37** | 0 | 0 | 0 | +3 hub contract tests |
| Same + `\|…RealtimeGroupAuthorizationTests` (**current**) | 42 | **42** | 0 | 0 | 0 | +5 negative-auth tests |
| Full `Domain.Tests` (no filter) | 379 | **373** | **6** | 0 | 1 | Failures all under `RubricIntelligence.*` — pre-existing / out of scope |

**Do not add 34+37.** 37 = 34 + 3 hub tests. Current notification-relevant suite = **42**.

Commands used (Dev machine):

```bash
cd New-API/NIGA_NewAPI
VSTEST_CONNECTION_TIMEOUT=300 dotnet test Homeocentrum.Niga.API.Domain.Tests/Homeocentrum.Niga.API.Domain.Tests.csproj \
  --filter "FullyQualifiedName~PushOutboxBackoffTests|FullyQualifiedName~AppNotificationInboxTests|FullyQualifiedName~RealtimeHubGroupTests|FullyQualifiedName~RealtimeGroupAuthorizationTests"
# → Passed: 42, Failed: 0, exit 0

VSTEST_CONNECTION_TIMEOUT=300 dotnet test Homeocentrum.Niga.API.Domain.Tests/Homeocentrum.Niga.API.Domain.Tests.csproj
# → Passed: 373, Failed: 6 (RubricIntelligence only)
```

Unrelated failures (not fixed here):  
`DualLanguageMeaningGraphTests…`, `CorrectnessBugsAtoDTests.BugA_*` (3), `CausationDetectionEngineTests…`, `FastClinicalAccuracyPackTests.SpeakerNegation…`.

---

## 3. Architecture (verified)

| Layer | Technology | Evidence |
|---|---|---|
| Backend | ASP.NET Core New-API | `Homeocentrum.Niga.API` |
| DB | SQL Server | Dev/Stage host only in this audit |
| Push | Firebase Admin → FCM | `FirebasePushNotificationSender` |
| Device tokens | `dbo.DevicePushToken` | S6 + EnsureSchema |
| Inbox | `dbo.AppNotification` | separate from push status |
| Push outbox | `dbo.PushNotificationOutbox` | worker drain |
| Push log | `dbo.FirebaseNotificationLog` | title/body stored (may include appointment copy) |
| SMS outbox | `dbo.NotificationOutbox` | **uncoupled** from FCM |
| Preferences | `dbo.UserNotificationPreference` | enqueue + send re-check |
| Clinic realtime | `/hubs/clinic` → `doctor:{DoctorID claim}` | reception JWT includes DoctorID |
| Patient realtime | `/hubs/patient` → `user:{UserId claim}` | |
| Tele waiting room | Polling | unchanged by design |

---

## 4. Backend / SQL / outbox findings

### Correctness (verified in code)

- Inbox written immediately; push queued (`queued` / worker `SENT` = provider acceptance only).
- `DeliveryId` unique; concurrent enqueue races treated as idempotent.
- Claim: `READPAST, UPDLOCK, ROWLOCK`; stale lock reclaim ~5 minutes.
- Backoff: 15s…30m, max 8 → `DEAD`.
- Multi-token send; permanent FCM errors soft-delete token.
- Preferences at enqueue and again at send (`SKIPPED` if opted out later).
- Missing preference row: all on except MARKETING. QuietHours flag has **no window** → does not suppress.

### Crash window (residual — High for reliability, accepted for this phase)

Business TX commits (e.g. appointment save) **then** fanout/`SendToUserAsync` enqueues.  
**Not** a same-transaction outbox. Crash between commit and enqueue can lose push intent.  
Safe atomic outbox across all writers = **separate scoped project**; not silently done here.

### Schema

- Script: `ScriptsAndFiles/S6_Week6/Database scripts/07_PushNotificationOutbox.sql` (idempotent create + index align).
- Runtime ensure creates table if missing; worker logs drain failures (does not silently swallow forever — retries until DEAD / backoff).
- Growth: outbox + FirebaseNotificationLog need retention policy (Medium) — not implemented.

### FirebaseNotificationLog / PHI

- Logs store Title/Body from business copy (may include patient name / visit time). Treat as sensitive operational data; restrict DB access (Medium).

### JWT / SignalR

- `?access_token=` accepted only under `/hubs` path.
- `RequireHttpsMetadata = false` in Program — acceptable for local/dev; **must be true in production** (High config).
- Groups from claims only; forged hub method args ignored (`RealtimeGroupResolver`).

---

## 5. Web / mobile findings

### Web (code review)

- Doctor Widgets: debounced `appointmentChanged` → `refreshDashboardCounts` (400ms).
- Schedule / appointment requests / patient dashboard / bell: subscribed; logout stops both hubs.
- Deep links: relative paths; clinic users redirected away from `/patient/*` opens.
- Multi-tab: shared hub connection per tab; eventId client dedupe.
- Staging smoke of live UI: **not run** (UI config localhost; staging deploy not performed).

### Mobile

- Expo FCM registration + `addPushTokenListener` present (Doctor + Patient).
- Android `google-services.json` present; **`GoogleService-Info.plist` missing** on both apps (app.json references it).
- Real-device push: **not verified**.

---

## 6. Dev smoke evidence (executed 2026-10-29) + Stage checklist

### Environment identity (Dev only)

- SQL: `103.196.187.99` / database **`HomeoCentrum_Dev`** (asserted before write; Prod host refused).
- API: local rebuild `http://localhost:5050` against Dev connection string.
- UI: `http://localhost:3000` → `API_Base_URL=http://localhost:5050/api`.
- **No Production writes.**

### Checklist results (Dev) — completed with seeded users

| # | Step | Result |
|---|---|---|
| 1 | Env = Dev | **Pass** — `HomeoCentrum_Dev` |
| 2 | Outbox schema | **Pass** |
| 3 | Seed users | **Pass** — `NigaSmoke_Doctor` / `NigaSmoke_Patient` / `123456` |
| 4 | Login | **Pass** — both 200 + JWT |
| 5 | Hubs | **Pass** — patient+clinic negotiate 200 with JWT; unauth 401 |
| 6 | Book | **Pass** — PatientAppId `29199`, inbox 10030/10031 (1 doctor + 1 patient after fix) |
| 7 | Outbox worker | **Pass** — tokenless → SKIPPED; doctor+fake token → FAILED “Firebase … not configured” (retry) |
| 8 | Cancel | **Pass** — status CANCELLED; cancel inbox both roles |
| 9 | Preference Appointments=false | **Pass** — rebook inbox id `10035` PushStatus **SKIPPED** |
| 10 | Notifications API | **Pass** — GET 200 doctor+patient |
| 11 | UI compile | **Pass** — :3000 |
| 12 | Firebase real-device SENT | **Blocked** — Firebase off in this Dev process / no physical device |
| 13 | Browser multi-tab visual | **Not automated** — API/DB path proven |

Sanitized cycle ids: AppNotification `10030–10035`; DeliveryId examples `2FA388C3`, `E99B3E35`, `5D66290C`.

### Staging checklist — still NOT RUN

1. Backup Stage DB.  
2. Apply `07_*.sql` if missing.  
3. Deploy API+UI to Stage hosts.  
4. Test accounts + devices.  
5. Full book → inbox/outbox/UI path.  
… (same as prior plan)

---

## 7. Remaining risks

| Rank | Risk |
|---|---|
| **Critical** | None confirmed in code for auth bypass of hub groups (negative unit tests pass). Staging unproven. |
| **High** | Post-commit enqueue crash window; Prod HTTPS metadata / secrets hardening; iOS push blocked without plist; staging smoke not done |
| **Medium** | Title/body PHI in push log; outbox/log retention; QuietHours incomplete; full Domain.Tests Rubric failures (unrelated) |
| **Low** | Doctor schedule page subscribe is keepalive (hours UI not appointment calendar); uncommitted Phase 2 work on Dev branches |

---

## 8. Production deployment plan — **NOT EXECUTED / NOT APPROVED BY THIS DOCUMENT**

Requires **separate explicit approval** and named operator.

1. Approvals: Product + Eng lead + release owner.  
2. Code review; pin exact commit SHAs after Phase 2 is committed/merged (API `Dev_V2.0` / UI `Dev_V3.0` currently dirty).  
3. Production DB full backup + restore validation.  
4. Apply `07_PushNotificationOutbox.sql` on Prod **only after approval** (schema additive).  
5. Deploy API (worker + hubs) then UI; old clients ignore unknown hub methods.  
6. Server Firebase prod credentials; add iOS plists before claiming mobile push.  
7. Worker health: logs show drain; no sustained PENDING backlog.  
8. Monitor: PENDING/FAILED age, DEAD, AttemptCount, invalid-token deletes, SignalR errors, table growth.  
9. Post-deploy smoke with **test** accounts only.  
10. Abort if DEAD spike / worker crash loop / auth failures.  
11. Rollback: revert API/UI binaries; **do not drop** outbox while PENDING exists; leave table orphaned if needed.  
12. Distinguish code rollback vs schema leave-in-place vs ops drain.

---

## 9. Explicit non-production statement

**No production database, API, UI, Firebase project, or patient data was modified, migrated, restarted, or notified by this audit.** Staging smoke was not executed. Production deployment is **not** approved by this prompt or document.

---

## 10. APIs / contracts (do not duplicate)

- `POST /api/Device/Register` (+ unregister)  
- Notification preferences GET/PUT  
- Inbox list / mark read  
- Hubs: `/hubs/clinic`, `/hubs/patient`  
- Client methods: `appointmentChanged`, `appointmentRequestChanged`, `inboxChanged`  

See `docs/FCM-Mobile-API.md`.

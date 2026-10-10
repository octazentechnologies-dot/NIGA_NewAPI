# DPDP Act 2023 compliance mapping — health data in Homeocentrum

Scope: patient, caregiver, doctor, reception and pharmacy personal data processed by the UI, API, Old API, doctor/patient apps and the SQL Server databases.
Homeocentrum (the clinic / platform operator) is the **Data Fiduciary**. Service providers that process data on its behalf are **Data Processors**.

This is an engineering mapping, not legal advice. Have counsel confirm it against the Act, the Digital Personal Data Protection Rules 2025 (notified November 2025, most obligations commence 18 months after notification) and the medical-records rules that apply to homoeopathy practice.

Status key: **Done** = in the code today; **Partial** = some support, gaps listed; **Gap** = not built.

## 1. Requirement map

| DPDP provision | What it requires | Homeocentrum today | Status | Action |
|---|---|---|---|---|
| s.4, s.6 Consent | Process only with free, specific, informed, unambiguous consent by clear affirmative action (or a legitimate use) | `ConsentRecord` (type, subject, granted by, granted at, withdrawn at, IP, user agent, notes) plus the **notice it was given against**: `ConsentNoticeId`, `NoticeVersion`, `NoticeLanguage`, `NoticeSha256`. Notices live in `ConsentNotice` (versioned; triggers block edits and deletes of published text). `POST /api/Consent/Grant` and `GrantPrivacy` refuse a stale version (409 `NOTICE_OUTDATED`); every other insert path (consent centre, caregiver, tele and audio recording) is stamped with the current notice on save. A newer notice published with `RequiresReconsent` makes older consents count as not given until renewed. The profile page shows the notice text and version before the consent button | Done | One consent type per purpose (treatment, reminders/WhatsApp, marketing, AI transcription) — no bundled consent |
| s.5 Notice | Itemised notice: data collected, purpose, how to withdraw, how to complain, how to reach the Board; available in English or any Eighth Schedule language | Privacy consent screen | Partial | Publish a privacy notice page in the UI and apps with the itemised list in section 2, grievance contact, and Board complaint route; offer Hindi and the clinic's regional language |
| s.6(4) Withdrawal | Withdrawing must be as easy as giving consent; processing must then stop within a reasonable time | `POST /api/Consent/Withdraw`, `GET ListMine` | Partial | Add a "Withdraw" button next to each consent in the patient profile; on withdrawal stop the dependent processing (e.g. WhatsApp reminders, AI transcription) and start the erasure check in section 3 |
| s.6(7)–(9) Consent managers | Data principals may act through a registered consent manager | Not supported | Gap (low priority) | Revisit once consent managers are operating; design consent APIs so an external manager can call them |
| s.7(f), s.7(g) Legitimate uses | Medical emergency, and treatment during an epidemic or public-health threat, may proceed without consent | Not distinguished | Gap | Add a "legitimate use: medical emergency" reason on records created without consent and audit it |
| s.8(1)–(2) Accountability and processors | The fiduciary is responsible, including for processors, which may only act under a valid contract | Processors listed in section 4 | Partial | Sign data-processing terms with each processor; keep the register in section 4 current |
| s.8(3) Accuracy | Keep data complete, accurate and consistent when used for decisions or shared | Patients and staff can edit profiles; audit middleware records changes | Done | — |
| s.8(4)–(5) Security safeguards | Reasonable technical and organisational measures to prevent a breach (penalty up to ₹250 crore) | Section 5 lists the October 2026 hardening: object-level access checks with cross-tenant tests, strict JWT validation, login throttle, generic errors, masked logs, upload scanning, CORS without credentials, ops email alerts, tamper-evident audit trail, CodeQL, gitleaks, dependency scans | Partial | Rotate all exposed secrets; move secrets out of git; encrypt backups; enforce HTTPS + HSTS; migrate the end-of-life Old API. Reception staff passwords are now PBKDF2 (same as `UserMaster`); legacy rows are re-hashed at API startup and on login in either API |
| s.8(6) Breach notification | Inform the Board and each affected person (Rules: without delay, detailed report to the Board within 72 hours) | `INCIDENT_RESPONSE_AND_KEY_ROTATION_RUNBOOK.md` sections 4 and 6; `SecurityAuditLog` gives who/when/what for exports and logins | Done (process) | Run a tabletop drill; keep contact table filled |
| s.8(7)–(8) Erasure and retention | Erase when consent is withdrawn or the purpose is served, unless law requires retention; make processors erase too | Patients and users are **soft-deleted** (`DeleteStatus = 1`) — data stays. Audio has `AudioRetentionDays` (default 0 = keep forever) with a background purge job | Gap | Build the deletion-request workflow in section 3 and the retention schedule in section 6 |
| s.8(9) Contact | Publish the business contact of a person who answers data questions (DPO if significant) | Not published | Gap | Name a grievance officer; show their contact in the app, website and privacy notice |
| s.8(10), s.13 Grievance redressal | Effective grievance mechanism; person must use it before going to the Board | General support only | Gap | Add a "Privacy request / complaint" form that creates a tracked request with an SLA (Rules: respond within 90 days at most; aim for 30) |
| s.9 Children | Verifiable parental consent for under-18s; no tracking, behavioural monitoring or targeted ads | Age from `Patient.DateOfBirth` (else `Age`). A minor's own consent is refused (403 `GUARDIAN_CONSENT_REQUIRED`) in `/api/Consent` and the patient consent centre. A parent or guardian consents after declaring they are the legal guardian and 18+, verified one of three ways: **FamilyAccount** (signed-in adult owns the child's family record or an active caregiver grant), **InClinic** (staff record guardian name, relationship and the ID document type checked — never the number), **Otp** (a verified `GuardianConsent` OTP for that patient, used once, within 30 minutes). The row stores `GrantedForMinor`, guardian user/name/relationship, method, reference and masked mobile; each guardian consent is written to `SecurityAuditLog` as `GUARDIAN_CONSENT`. Guardian consent stops counting when the patient turns 18. Family page has date of birth and a guardian consent dialog | Partial | OTP proof is weak until SMS is live, because `RequestOtp` still returns `devCode`. Never use minors' data for marketing or analytics profiling (no technical block yet on the Marketing type for minors). In-clinic audio and tele-recording consents for minors are stamped with the notice but do not capture guardian details yet |
| s.10 Significant Data Fiduciary | If notified (volume and sensitivity of health data make this possible): DPO in India, independent audit, periodic DPIA | Not notified | Watch | Prepare a DPIA for AI transcription and WhatsApp messaging now; it is good practice regardless |
| s.11 Right to access | Summary of personal data and processing, and identities of fiduciaries/processors it was shared with | Patients see their own records in the app; no single export | Gap | Add `GET /api/Privacy/MyData` (patient/caregiver, OTP re-check) returning a summary plus a downloadable copy; audit as `PATIENT_DATA_EXPORT` |
| s.12 Correction and erasure | Correct, complete, update and erase on request | Profile edit exists; no erasure | Partial | Section 3 |
| s.14 Nomination | Nominate someone to exercise rights on death or incapacity | Caregiver link exists but is not a legal nomination | Gap | Add a nominee field with consent from the patient; allow the nominee to raise requests |
| s.16 Cross-border transfer | Allowed except to countries the Government restricts | OpenAI / Azure OpenAI (AI transcription/analysis), Meta (WhatsApp), Twilio / Daily / 100ms (video), Razorpay may process outside India | Watch | Record the hosting region of each processor in section 4; check the restricted-country list when notified; prefer India regions where offered |

## 2. Personal data inventory (for the notice and the register)

| Category | Examples | Where | Purpose |
|---|---|---|---|
| Identity and contact | Name, mobile, email, address, photo, date of birth, gender | `Patient`, `UserMaster`, `Doctor`, `DoctorReceptionStaff` | Account, appointments, communication |
| Health data | Complaints, case history, rubrics, prescriptions, lab orders and results, uploaded reports, audio of case-taking and transcripts | `CaseEntryDetails`, case/complaint tables, `PatientLab*`, upload folders, audio store | Diagnosis and treatment |
| Financial | Payments, subscriptions, payouts, bank details for payees | Order/subscription/payout tables, Razorpay | Billing and payouts |
| Credentials and security | Password hashes, OTP events (no OTP values in logs), session token ids, masked client IPs | `UserMaster`, `SecurityAuditLog` | Security and fraud prevention |
| Consent and requests | Consent grants and withdrawals, privacy requests | `ConsentRecord`, `AudioCaseConsentLog` | Proof of lawful processing |

## 3. Deletion-request workflow (to build)

1. Patient (or guardian / nominee) submits a request in the app; identity re-checked by OTP to the registered mobile.
2. A `DataPrincipalRequest` row is created (type: access, correction, erasure, grievance; status; due date) and acknowledged.
3. Legal-hold check: medical-records retention rules may require keeping clinical records for a minimum period after the last consultation. Where retention is required, **restrict** processing (hide from search, block marketing/AI, keep only for legal purposes) and tell the person the date when erasure will happen.
4. Otherwise erase: anonymise identity fields on `Patient`/`UserMaster` (name, mobile, email, address, photo), delete uploaded files and audio, delete or anonymise free-text notes that identify the person, revoke sessions, withdraw consents.
5. Ask processors to delete (WhatsApp/SMS message logs, video recordings, AI provider data if retained).
6. Record completion in `SecurityAuditLog` with the request id only — never the erased data.
7. Close the request and inform the person.

## 4. Processor register (complete with contracts and regions)

| Processor | Data shared | Purpose | Contract / DPA | Region |
|---|---|---|---|---|
| Razorpay | Name, contact, payment details | Payments | | |
| Meta (WhatsApp Business) | Mobile, message content (reminders, receipts) | Messaging | | |
| SMS provider (`Sms:*`) | Mobile, OTP / message text | OTP and alerts | | |
| Twilio / Daily / 100ms (`TeleVideo:*`) | Names, video/audio streams | Teleconsultation | | |
| OpenAI (`OpenAI`) or Azure OpenAI (`AzureOpenAI`) | Case-taking audio and text | Transcription and rubric assistance | | Azure offers India regions |
| Mail provider (`smtp`) | Email address, message content | Notifications, error alerts | | |
| Hosting / backups | All data | Infrastructure | | |

## 5. Security safeguards already in place (s.8(5) evidence)

- Object-level authorization on patient, case, lab, user and doctor routes; cross-tenant test suite (66 checks) proves one doctor, patient or reception user cannot read another's data.
- JWT issuer, audience, lifetime and signature validation; login throttle; retired insecure login endpoint; logout token denylist.
- Generic 500 responses with an error id; details only in server logs; no console logging.
- Personal data and secrets masked in logs (`LogRedactor`): mobiles, OTPs, JWTs, emails, passwords, request bodies.
- Uploads checked by content (magic bytes), executables and scripts blocked, antivirus scan, random file names.
- Tamper-evident `SecurityAuditLog` (HMAC chain, append-only trigger) for logins, failed logins, token issuance, payout OTPs and patient-data exports.
- CORS open to any origin but never with credentials; security headers; Swagger behind sign-in; developer exception page never outside Development.
- CI: CodeQL static analysis, gitleaks secret scanning, dependency vulnerability audits for all three repositories.

## 6. Retention schedule (proposal — confirm with counsel and the clinic)

| Data | Keep for | Then | Mechanism |
|---|---|---|---|
| Clinical records (cases, prescriptions, lab results) | Minimum period required by the applicable medical-records rules after the last consultation (confirm; commonly 3 years, longer for minors and medico-legal cases) | Erase or anonymise | Scheduled job (to build) |
| Case-taking audio | Until the transcript is reviewed, at most 90 days | Delete file | Set `AudioCaseTaking:AudioRetentionDays` (currently 0 = forever) |
| Uploaded documents | Same as clinical records | Delete | Scheduled job (to build) |
| Security audit log | At least 1 year (DPDP Rules require processing logs for one year; CERT-In requires ICT logs for 180 days) — proposed 3 years | Archive offline | Append-only table; archive by `OccurredAtUtc` |
| Application logs (`Logs/`) | 180 days | Delete | Scheduled task on the server (to build) |
| Message outbox (stores OTP and message text) | 24 hours for OTP messages, 30 days for others | Delete | Scheduled job (to build) |
| Inactive accounts with no clinical record | Notify after 2 years of inactivity, erase 48 hours after notice if no response | Erase | Scheduled job (to build) |
| Consent records | For as long as the related data is kept, plus the limitation period | Delete | With the related data |
| Backups | 35 days rolling, encrypted | Expire | Backup job settings |

## 7. Priority actions

1. Rotate every exposed secret and remove real secrets from `appsettings.json` in git (runbook section 5).
2. Purge OTP text from the message outbox after use (reception passwords are hashed — done).
3. Publish the grievance contact and a public privacy notice page (versioned notices and per-consent version — done).
4. Build the data-principal request workflow (access export, correction, erasure with legal hold) and the retention jobs.
5. Stop returning OTP `devCode` once SMS is live, so guardian OTP verification is real (guardian consent itself — done).
6. Processor contracts and the region register; DPIA for AI transcription.
7. Migrate the Old API off ASP.NET Core 2.2.

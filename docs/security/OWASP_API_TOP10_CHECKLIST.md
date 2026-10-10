# OWASP API Security Top 10 (2023) checklist — Homeocentrum API and Old API

How to re-check locally (both APIs running):

```powershell
cd NIGA_API\ScriptsAndFiles\Security
.\Test-OwaspApiTop10.ps1 -ReportPath .\owasp-report.md   # runs Test-CrossTenantAccess.ps1 too
.\Test-UploadGuard.ps1
```

Dynamic scan of staging: `ZAP\Run-ZapStaging.ps1` with `ZAP\zap-staging-plan.yaml` (needs Docker; refuses production hosts).

Last local run: 2026-10-07, Development, API 5002 and Old API 5001. 57 pass, 0 fail, 4 informational. Cross-tenant suite 66/66. Upload suite 6/6.

| Risk | Control in the code | Evidence | Status | Remaining work |
|---|---|---|---|---|
| API1 Broken Object Level Authorization | `PatientAccessGuard` (New), `PatientAccess` and `DoctorOwnership` (Old) on patient, case, lab, clipboard, user, doctor, menu and sub-section routes | `Test-CrossTenantAccess.ps1`: 66 checks across doctor, patient, reception and pharmacy users, own data allowed and other tenant's data denied | Pass | Add new per-patient routes to the guard and to the suite as they are built |
| API2 Broken Authentication | JWT issuer, audience, lifetime and signature validated in both APIs; login throttle (5 per user name / 30 per IP in 15 minutes, then 429); Old `/api/login/authenticate` retired (410); identical response for unknown user and wrong password | Probe: no token, garbage, tampered payload, `alg=none`, wrong issuer, wrong audience, missing issuer/audience, expired and wrong-key tokens all return 401; sixth failed login returns 429 | Pass | Throttle is in memory per API instance. With several instances behind a load balancer, move it to a shared store |
| API3 Broken Object Property Level Authorization | Firm details no longer return the mail password, connection path or backup path; user and login responses carry no password hash | Probe API3 rows (both APIs) | Pass | Review new DTOs for over-exposure; prefer explicit response models over entities |
| API4 Unrestricted Resource Consumption | JSON depth capped at 64; upload size and type limits; OTP request limit; login throttle; request and header timeouts; optional global `RateLimit` | Probe API4 rows; `Test-UploadGuard.ps1` | Pass | Turn on `RateLimit:Enabled` in production after load testing; cap `PageSize` on list endpoints |
| API5 Broken Function Level Authorization | Admin portal policy on admin routes (users list, delete, audit verify, Old blog/news/subscription saves); `Tufan_Doctor` bypass is Development only | Probe: doctor, reception and patient get 403 on admin routes; admin control gets 200 | Pass | Keep admin routes under the `AdminPortal` policy; never rely on the UI hiding a button |
| API6 Unrestricted Access to Sensitive Business Flows | Subscriptions require a valid Razorpay signature, an unused payment id and a matching amount; payout OTP is audited; OTP verify locks after 5 attempts | Probe: fake payment returns 400; Old subscription save returns 403 | Pass | Add bot protection (captcha) on public patient sign-up and OTP request if abuse is seen |
| API7 Server Side Request Forgery | No endpoint fetches a caller-supplied URL; outbound hosts (Razorpay, Meta, SMS, mail, video, AI) come from configuration | Code review | Info | Re-review if a "fetch from URL" or link-preview feature is added |
| API8 Security Misconfiguration | CORS open to any origin (Bearer tokens only, credentials never allowed; `Cors:AllowedOrigins` can restrict it); `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, CSP on API responses; no `Server` banner; TRACE refused; Swagger behind sign-in; generic 500 with `errorId`; developer exception page only in Development outside IIS; no console logging; forwarded headers behind IIS | Probe API8 rows (both APIs) | Pass | Serve HTTPS only with HSTS at IIS; set `Cors:AllowedOrigins` if CORS should be narrowed to real UI hosts |
| API9 Improper Inventory Management | Retired endpoint returns 410; Swagger gated; this checklist and the QA matrix list the surface | Probe API9 rows | Partial | Old API runs ASP.NET Core 2.2, which is end of life and has an unpatched Critical Kestrel advisory. Plan its migration into the API |
| API10 Unsafe Consumption of APIs | WhatsApp receipts need a Meta `X-Hub-Signature-256` HMAC (closed with 503 when no secret is set); Razorpay payments verified by signature and amount lookup | Probe: unsigned and wrongly signed receipts refused | Pass | Set `WhatsAppMeta:AppSecret` on every environment that should receive receipts |

## Not covered by the scripted probe

- A full crawler-based dynamic scan (ZAP/Burp). Run `ZAP\Run-ZapStaging.ps1` against staging and triage the HTML/SARIF reports. It was not run from this machine: Docker, Java and ZAP are not installed here, and staging must not be scanned without a restorable database.
- Business-logic abuse that needs real payments or real SMS/WhatsApp delivery.
- The React UI itself (XSS in rendered content). CodeQL `javascript-typescript` covers the source; ZAP covers the served pages when pointed at the UI host.

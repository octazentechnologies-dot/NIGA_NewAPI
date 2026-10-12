# QA security matrix

One clinic. Each doctor's patients, cases and staff are private to that doctor. Status below is what the APIs do in this tree.

| Control | Where | What QA checks |
|---|---|---|
| Authentication | JWT Bearer on both APIs, issuer, audience, lifetime and signature validated | Missing, tampered, `alg=none`, wrong-issuer, wrong-audience or expired token returns 401 |
| Authorization | Admin, Account, Doctor, Reception, Patient, Pharmacy policies | A patient or doctor token on an admin route returns 403 |
| Object ownership (IDOR) | `PatientAccessGuard` (New), `PatientAccess` / `DoctorOwnership` (Old) | Doctor A cannot read doctor B's patient, case, lab or user record; `Test-CrossTenantAccess.ps1` |
| Login throttle | `[LoginThrottle]` on both `/api/Account/Login` | Five failed logins for one user name in 15 minutes, or 30 from one IP, return 429 with `Retry-After` |
| Rate limit | `RateLimit` in appsettings. Off while `Enabled` is false | When enabled, 1000 calls in 60 seconds is one bucket for all APIs, per user, or per IP if anonymous. `/health` and `/swagger` are not counted |
| OTP abuse | `POST /api/Otp/RequestOtp` | A fourth request for the same mobile and action inside one minute returns 429 |
| CORS | Any origin. If `Cors:AllowedOrigins` is set, only those origins. Credentials are never allowed | Any origin gets its own `Access-Control-Allow-Origin` and no `Access-Control-Allow-Credentials`. A cross-origin call without a token still returns 401 |
| Ops email alerts | `ErrorAlert:Recipients` + `smtp`. API alerts: `OpsAlert:Enabled` (default: on under IIS, off locally). Watchdog: `ScriptsAndFiles\Ops\Watch-Homeocentrum.ps1` scheduled task | Deploy started/ready and API stopping (IIS only); API crash; sign-in blocked for a user name or IP; malware upload; antivirus unavailable; audit trail failed verification (daily). Watchdog: W3SVC/WAS, app pool, site or HTTP down / still down (hourly) / recovered, low disk, IIS or ASP.NET Core Module error events, server restarted (flags power loss). `-DryRun` sends nothing |
| Forwarded headers | `UseForwardedHeaders` behind IIS | Audit rows and the login throttle see the client IP, not 127.0.0.1 |
| CSRF | Cookie-only POST/PUT/PATCH/DELETE | A Bearer call is allowed. A cookie call without Bearer is 403 |
| Timeout | Five minutes on the API. Keep-alive two minutes. Header timeout 30 seconds | A hung request ends with 408 |
| Input validation | Model validation returns `success`, `message`, `traceId`, `errors` | A blank required field returns 400 in that shape. JSON nested deeper than 64 returns 400 |
| Security headers | Every response except Swagger's content policy | `nosniff`, `DENY`, `no-referrer`, `X-Trace-Id`; no `Server` banner |
| SQL | EF and parameterized SQL | Queries do not concatenate user text into SQL |
| Exceptions | 500 returns a generic message plus `errorId`; details go to the error log only | The response has no exception text or stack trace |
| Developer exception page | Old API, Development and not under IIS only | Production and IIS never show it |
| Swagger | Gate plus operation notes | Swagger and `swagger.json` redirect to the developer sign-in |
| File upload | `UploadGuard`: magic bytes, extension match, executables/scripts blocked, Defender scan, random names | `.exe` as `.jpg`, PNG as `.pdf`, `.php`, HTML as `.txt` return 415; EICAR returns 422; `Test-UploadGuard.ps1` |
| Webhooks | WhatsApp receipts need `X-Hub-Signature-256` with `WhatsAppMeta:AppSecret` | Unsigned or wrongly signed receipt returns 401; without the secret the webhook returns 503 |
| Payments | Subscriptions need a verified Razorpay signature, unused payment id and matching amount | A fake payment returns 400 |
| Audit | Mutating requests are written by the audit middleware | A payment or profile change has an audit row |
| Security audit trail | `SecurityAuditLog`, HMAC chain, append-only trigger | Logins, failed logins, token issue, payout OTP and exports are recorded; `GET /api/Admin/SecurityAudit/Verify` returns `intact` |
| Privacy | `GET /api/Consent/PrivacyStatus` | The patient profile shows grant only when `granted` is false |
| Consent notice version | `ConsentNotice`, `GET /api/Consent/Notice/{code}`, admin `POST /api/Consent/Notice` | Each consent row has notice id, version and SHA-256; a stale `noticeVersion` returns 409 `NOTICE_OUTDATED`; editing or deleting a published notice fails in SQL |
| Guardian consent | `POST /api/Consent/Grant` with `guardian`; Family page dialog | Consent for an under-18 patient without a guardian returns 403; an unrelated adult gets 403; a parent with the child in their family list succeeds and the row shows `GrantedForMinor`; one guardian OTP backs one consent; `Test-ConsentAndGuardian.ps1` |
| Reception passwords | `DoctorReceptionStaff.Password` | Every row starts `PBKDF2$v1$`; a legacy encoded password still logs in on both APIs and is re-hashed |
| Logs | `Logs/{day}` files, `LogRedactor` | An error line includes the trace id. Mobile numbers, OTPs, JWTs, passwords and request bodies are masked |
| Console | No console logging in either API; UI console silenced unless `REACT_APP_DEBUG_CONSOLE=true` | Browser console shows no tokens or API responses |
| Metrics | `GET /api/Ops/Metrics` as Admin | `requests` and `serverErrors` increase after traffic |
| Traces | `X-Trace-Id` and `traceId` in error JSON | The header matches the error body |
| Deploy mail | IIS only, same recipients as ErrorAlert | App-pool start sends "deployment started", then "deployment done" |
| Dependencies | `dotnet list package --vulnerable`, `npm audit`, `dependency-audit.yml` | AutoMapper 13.0.1 is the accepted high finding (fixed versions need a commercial licence) |
| Static and secret scanning | `codeql.yml`, `gitleaks.yml` in each repo | A new secret in a push or PR fails the build |
| Tests | `Homeocentrum.Niga.API.Domain.Tests`, `Test-OwaspApiTop10.ps1` | `HostSecurityTests` and `SecurityHardeningTests` pass; the probe reports 0 failures |

The cross-tenant tests use a second doctor, patient and reception user created by `10_Security_Test_Tenant_LOCAL_ONLY.sql`. Never run that script on production.

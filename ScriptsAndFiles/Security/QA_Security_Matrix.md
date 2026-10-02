# QA security matrix

One clinic. There is no second tenant. Status below is what the APIs do in this tree.

| Control | Where | What QA checks |
|---|---|---|
| Authentication | JWT Bearer on both APIs | Missing token on a protected route returns 401 |
| Authorization | Admin, Account, Doctor, Reception, Patient, Pharmacy policies | A patient token on an admin route returns 403 |
| Rate limit | `RateLimit` in appsettings. Off while `Enabled` is false | When enabled, 1000 calls in 60 seconds is one bucket for all APIs, per user, or per IP if anonymous. `/health` and `/swagger` are not counted |
| OTP abuse | `POST /api/Otp/RequestOtp` | A fourth request for the same mobile and action inside one minute returns 429 |
| CORS | Named policy, any browser origin, no credentials | The site on port 80 and `localhost:3000` can call the API. `X-Trace-Id` is visible to the browser |
| CSRF | Cookie-only POST/PUT/PATCH/DELETE | A Bearer call is allowed. A cookie call without Bearer is 403 |
| Timeout | Five minutes on the New API. Keep-alive two minutes. Header timeout 30 seconds | A hung request ends with 408 |
| Input validation | Model validation returns `success`, `message`, `traceId`, `errors` | A blank required field returns 400 in that shape |
| Security headers | Every response except Swagger's content policy | `nosniff`, `DENY`, `no-referrer`, `X-Trace-Id` |
| SQL | EF and parameterized SQL | Queries do not concatenate user text into SQL |
| Exceptions | Unhandled errors become 500 with `traceId` and an email when ErrorAlert is on | The response does not include a stack trace |
| Swagger | Gate plus operation notes | Swagger asks for the developer sign-in. Each operation lists auth, roles, and error codes. Samples live in the API workbook |
| File upload | Credential files are PDF, JPG, or PNG only | A .exe upload is refused |
| Audit | Mutating requests are written by the audit middleware | A payment or profile change has an audit row |
| Privacy | `GET /api/Consent/PrivacyStatus` | The patient profile shows grant only when `granted` is false |
| Logs | `Logs/{day}` files | An error line includes the trace id |
| Metrics | `GET /api/Ops/Metrics` as Admin | `requests` and `serverErrors` increase after traffic |
| Traces | `X-Trace-Id` and `traceId` in error JSON | The header matches the error body |
| Deploy mail | IIS only, same recipients as ErrorAlert | App-pool start sends "deployment started", then "deployment done" |
| Dependencies | `dotnet list package --vulnerable` | Known package warnings are recorded; AutoMapper 13.0.1 is the current high finding |
| Tests | `Homeocentrum.Niga.NewAPI.Domain.Tests` | `HostSecurityTests` covers the user/IP bucket, CSRF, error shape, and counters |

Single clinic means a user token cannot be pointed at another clinic id. Do not add a second tenant for a test.

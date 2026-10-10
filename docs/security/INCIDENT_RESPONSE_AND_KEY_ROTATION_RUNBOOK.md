# Incident response and key rotation runbook

Applies to the Homeocentrum UI, API (`newapi`, port 5002), Old API (`oldapi`, port 5001) and the SQL Server databases.
Keep a printed copy. Never paste secret values into tickets, chat, email or AI tools. Refer to secrets by their configuration name.

## 1. Contacts (fill in and keep current)

| Role | Name | Phone | Backup |
|---|---|---|---|
| Incident lead (decides severity, owns the timeline) | | | |
| Technical lead (APIs, IIS, database) | | | |
| Grievance officer / data protection contact (DPDP s.8(9)) | | | |
| Clinic owner / management sign-off | | | |
| Legal counsel | | | |
| Hosting / network provider | | | |
| Razorpay support, Meta (WhatsApp), SMS provider, Twilio / video providers | | | |

Regulators: Data Protection Board of India (DPDP Act 2023, s.8(6)); CERT-In (`incident@cert-in.org.in`, within 6 hours of noticing a reportable incident under the CERT-In Directions of 28 April 2022).

## 2. Severity

| Level | Examples | Response |
|---|---|---|
| SEV1 | Patient data read or exported by someone not entitled; database or signing key leaked; ransomware; audit chain not `intact` | Start now, any hour. Lead plus technical lead. Regulator clock starts |
| SEV2 | Account takeover of one user; malware upload caught after storage; secret leaked in git but no sign of use | Same working day |
| SEV3 | Brute-force or scan traffic blocked by controls; vulnerable package with no known exploit path | Next working day |

## 3. Where incidents show up

- ErrorAlert email (unhandled 500s with an `errorId`; the matching detail is in `Logs/errors` on the server).
- Ops alert email ("Homeocentrum ALERT - ..."), sent to `ErrorAlert:Recipients` by both APIs under IIS (`OpsAlert:Enabled` overrides):
  API stopping (app pool stop, recycle, IIS stop, deploy), API crashed, sign-in blocked for a user name or client IP (brute force),
  malware upload blocked, antivirus scanning unavailable, security audit trail failed its daily verification.
- Watchdog email from the `Homeocentrum Watchdog` scheduled task (`ScriptsAndFiles\Ops\Watch-Homeocentrum.ps1`, register with
  `Register-HomeocentrumWatchdog.ps1` as administrator): W3SVC/WAS, app pool, site or HTTP check down, still down (hourly), recovered;
  disk below 5 GB; IIS / ASP.NET Core Module error events; server restarted, flagged when the previous shutdown was unexpected.
  A machine that is off cannot mail; use an external uptime monitor on the public URL for that. Logs: `C:\ProgramData\Homeocentrum\Watchdog`.
- `dbo.SecurityAuditLog`: logins, failed and throttled logins, token issuance, payout OTPs, patient-data and finance exports, blocked and malware uploads.
- `GET /api/Admin/SecurityAudit/Verify` (admin) — anything other than `intact` means rows were altered or keys are missing.
- GitHub: gitleaks (secret pushed), CodeQL alerts, `dependency-audit` failures.
- Defender alerts on the IIS host; IIS logs under `C:\inetpub\logs\LogFiles`.
- User reports (wrong patient visible, unexpected OTP, unknown login).

Useful queries (run read-only; the audit table is append-only):

```sql
-- Failed or throttled logins by client IP in the last 24 hours
SELECT ClientIp, Outcome, COUNT(*) AS Attempts, MIN(OccurredAtUtc) AS FirstSeen, MAX(OccurredAtUtc) AS LastSeen
FROM dbo.SecurityAuditLog
WHERE EventType = 'LOGIN' AND Outcome IN ('DENIED','FAILURE') AND OccurredAtUtc > DATEADD(HOUR,-24,SYSUTCDATETIME())
GROUP BY ClientIp, Outcome ORDER BY Attempts DESC;

-- Everything one account did (tokens, exports, payouts)
SELECT OccurredAtUtc, EventType, Outcome, SourceApi, ClientIp, Subject, Detail
FROM dbo.SecurityAuditLog WHERE ActorUserId = @UserId ORDER BY OccurredAtUtc DESC;

-- Patient-data and finance exports in a window
SELECT OccurredAtUtc, ActorUserId, ActorRole, Outcome, Subject, Detail
FROM dbo.SecurityAuditLog
WHERE EventType IN ('PATIENT_DATA_EXPORT','FINANCE_EXPORT') AND OccurredAtUtc BETWEEN @FromUtc AND @ToUtc;
```

Client IPs are stored masked (/24 for IPv4, /48 for IPv6). Request bodies, OTPs, tokens and mobile numbers are never logged; do not turn on body logging during an incident.

## 4. Response steps

1. **Open a timeline.** UTC times, who did what, what was seen. Every later step goes on it.
2. **Preserve evidence before changing anything.** Copy `Logs\` from both API folders, IIS logs, Windows Security/Defender event logs, and export the relevant `SecurityAuditLog` rows. Take a database backup (`COPY_ONLY`). Store copies outside the affected server with restricted access.
3. **Contain** (pick what applies):
   - Account takeover: set `UserMaster.IsUserActivated = 0` and `UserStatus = 0` for the account (blocks sign-in and token refresh) and reset its password. Access tokens already issued stay valid until they expire; `/api/Account/Logout` only revokes the caller's own token. To cut every session at once, rotate the signing key (section 5.1).
   - Leaked secret: rotate it now (section 5). A secret that ever reached git is leaked even if the commit was removed.
   - Data exfiltration through an API bug: block the route at IIS (URL Rewrite deny rule) or stop the app pool, then fix and redeploy.
   - Malicious upload: quarantine the stored file (keep a copy for analysis), run a full Defender scan of the upload folders, check `UPLOAD_BLOCKED`/`UPLOAD_MALWARE` rows for the same user.
   - Brute force from many IPs: block ranges at the firewall; consider enabling `RateLimit:Enabled`.
4. **Eradicate.** Remove the cause (patch, revert, remove backdoor accounts, rotate every secret the attacker could have read). If the server itself was compromised, rebuild it from clean media rather than cleaning it.
5. **Recover.** Redeploy known-good builds, confirm `/health`, run `Test-OwaspApiTop10.ps1` against staging, and check the audit chain is `intact`. Watch logs closely for 72 hours.
6. **Notify** (lead and legal decide; section 6).
7. **Review** within 10 working days: root cause, what detected it, what slowed the response, actions with owners and dates.

## 5. Key rotation

Generate new random secrets on a trusted machine. Do not print them to shared screens or logs:

```powershell
$b = New-Object byte[] 64; [Security.Cryptography.RandomNumberGenerator]::Fill($b); Set-Clipboard ([Convert]::ToBase64String($b))
```

Edit `appsettings.json` on the server (`C:\inetpub\homeocentrum\newapi` and `...\oldapi`), recycle the app pool, confirm `/health`, then record on the timeline which key was rotated and when — never the value. Update staging and developer machines separately with their own values; production values must not be reused anywhere else.

| # | Secret (configuration name) | Where | Effect of rotating | Steps |
|---|---|---|---|---|
| 5.1 | `TokenKey` (API signing key) **and** `JWT:Secret` (Old API signing key) | New and Old `appsettings.json` | Every signed-in user must sign in again | These two must hold the **same value**: the UI uses Old API tokens on the API, and both APIs derive the default audit key (`tk1`) from it. First do 5.2 if `SecurityAudit:HmacKey` is not set yet. Then put the old value in New `SecurityAudit:PreviousKeys:tk1`, set the new value in both files, recycle both pools, sign in, and check audit Verify is `intact` |
| 5.2 | `SecurityAudit:HmacKey` with `SecurityAudit:KeyId` | Both `appsettings.json` | None for users | Same value and id in both APIs. Choose a new id (`k2`, `k3`...). Move the previous key to New `SecurityAudit:PreviousKeys:<oldId>`; never delete retired keys while their rows exist, or Verify reports an unknown key |
| 5.3 | `Razorpay:KeyId`, `Razorpay:KeySecret`, `Razorpay:WebhookSecret` | Both APIs (Old API now reads `Razorpay:*` instead of a hardcoded value) and the UI checkout key id | Payments fail until both sides use the new pair | Regenerate in the Razorpay dashboard, update both APIs and the UI key id, redeploy the UI, make a test payment in test mode first |
| 5.4 | `ConnectionStrings:DefaultConnection` passwords | Both APIs, any scripts, backups jobs | API down for the minutes between change and recycle | Change the SQL login password, update both files, recycle. Prefer Windows/managed identity over SQL passwords |
| 5.5 | `smtp` passwords (mail and ErrorAlert) | Both APIs | Mail stops until updated | Change at the mail provider (use an app password), update, recycle, send a test alert |
| 5.6 | `SwaggerAuth:Password` | Both APIs | Developers sign in to Swagger again | Update and recycle |
| 5.7 | `WhatsAppMeta:AccessToken`, `WhatsAppMeta:AppSecret` | API | WhatsApp sends and receipts stop until updated | Regenerate in Meta Business; the receipt webhook stays closed (503) while `AppSecret` is empty |
| 5.8 | `Sms:AuthKey`, `Twilio:AccountSid`/`AuthToken`, `TeleVideo:*` keys (Twilio API key, Daily, 100ms) | API | SMS / video calls fail until updated | Regenerate at each provider and update |

Old `GlobalConstants.AuthKey` was removed from the code on 2026-10-07 together with the retired `/api/login/authenticate` endpoint. It remains in git history and must be treated as public.

### Secrets known to be exposed (rotate before the next production release)

Gitleaks (October 2026) found secrets in the current `appsettings.json` files and in git history of the API and UI repositories, including signing keys, SQL and mail passwords, the Razorpay key pair (also hardcoded in the Old API `OrderService.cs` history), API documents with tokens, and keys in UI sources. Several values were also displayed in an AI-assisted tooling session. Treat all of rows 5.1 to 5.8 as compromised and rotate them, then:

- Remove real secrets from `appsettings.json` in git. Keep placeholders in the file and supply values from server-only configuration (environment variables on the app pool, or a secrets store).
- Do not rewrite git history unless every clone can be re-cloned; rotation is what makes the old values useless.
- Restrict the Google Maps key in `GoogleMaps.js` to the site's HTTP referrers and the needed APIs.

### Routine schedule

| What | How often |
|---|---|
| Signing keys (5.1), audit key (5.2) | Yearly, and at once when a staff member with server access leaves |
| Payment, mail, provider keys (5.3, 5.5, 5.7, 5.8) | Yearly, or when the provider recommends |
| Database passwords (5.4) | Every 6 months |
| Restore test of a backup to a scratch database | Quarterly |
| Tabletop drill of this runbook | Twice a year |

## 6. Notification

| Who | When | What |
|---|---|---|
| Data Protection Board of India | Personal data breach: intimate without delay, detailed report within 72 hours of becoming aware (DPDP Act s.8(6) and DPDP Rules 2025, Rule 7) | Nature, extent, timing, likely impact, mitigation, findings about the cause, measures to prevent recurrence, notifications made |
| Each affected Data Principal (patient, doctor, staff) | Without delay, in plain language | What happened, likely consequences, what we are doing, what they should do (e.g. watch for phishing calls), contact person |
| CERT-In | Within 6 hours of noticing a reportable incident (data breach, unauthorised access, compromise of critical systems, etc.) | CERT-In incident form |
| Payment processor (Razorpay) | When payment keys or payment flows are involved | Per their merchant terms |
| Clinic management | SEV1 and SEV2 immediately | Summary and next update time |

Confirm the current text of the DPDP Rules and CERT-In Directions with legal counsel at the time of the incident; deadlines run from the time the organisation became aware.

## 7. After the incident

- File the timeline, evidence index, notifications sent and the review in a restricted folder.
- Add a regression check to `Test-OwaspApiTop10.ps1` or `Test-CrossTenantAccess.ps1` for the exploited weakness.
- Update this runbook with anything that was missing.

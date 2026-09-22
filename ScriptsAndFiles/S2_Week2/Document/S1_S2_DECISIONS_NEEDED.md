# S1 Week 1 + S2 Week 2 — decisions needed from you

Date: 22 Sep 2026  
Scope: in-scope rows only (S1 **102**, S2 **107**). Skipped: QA, Mobile UI, Mobile Frontend. Excel Done ignored.

Reply in chat with **item numbers** and your comment. Example: `1 In-clinic / Tele` · `8 leave disabled` · `12 send MSG91 later`.  
If you skip an item, I keep the **default** in the last column.

Secrets: do not paste production keys in chat. Put them in appsettings or a secrets store and say “keys are in appsettings”.

---

## Confirmations (product)

| # | SubIDs | Question | Default if you skip |
|---|--------|----------|---------------------|
| 1 | CLN-01.01 | Canonical **Visit** / **Consult** labels on Patient Board. Dev seed uses `First` / `InClinic`. Public booking uses `InClinic` / `Tele`. | Keep raw stored values (`First`, `InClinic`, `Tele`). Empty stays `—`. |
| 2 | CLN-01.01 | Wire header from `PatientAppointment.VisitType` / `ConsultMode` / `IsTele` **now**, or wait Phase 4/6? | Wait Phase 4/6. Query-string + dashboard pass-through only. |
| 3 | CLN-01.01 | Which appointment when several exist? Show **time** on the chip? | Selected `patientAppId`. Date only, no time. |
| 4 | CLN-01.01 | Dummy **Due Amount ₹ 0.00** on the board header? | Leave as chrome until billing. |
| 5 | CLN-01.02 | Is there a **doctor mobile repo** outside these three projects? | None here. No extra audit. |
| 6 | CLN-01.02 | Should Doctor JWT from a phone **403** clipboard / COG / audio mutate? | No. PDF §3 = do not ship those screens. Same Doctor JWT still used by web. |
| 7 | CLN-01.02 | Keep `/patientboard` as a bookmark alias? | Keep alias. |
| 8 | DOC-02.02 | Split Reception chrome **now** (Phase 5), or keep sharing `/doctordashboard`? | Keep share until Phase 5. Reception still blocked from Patient Board. |
| 9 | dashboard UX | Enable **Add Case Notes** on the appointment row? It is hardcoded off (`IS_ADD_CASE_NOTES_ENABLED = false`). | **Done 22 Sep 2026:** enabled for Doctor. Saves via Old-API `AppointmentHistoryNote`. Reception stays blocked. |
| 10 | DMO-05.02 | Doctor **mobile** may edit working hours (`PUT /api/Availability/Me` exists), or web-only? | API stays; mobile UI skipped this week. Web dashboard/profile already edit hours. |
| 11 | SEC-01.02 FND-01.03 | Cut clinic **password login** over to New-API `:5038` now? | No. Login / Rx-write / Razorpay stay Old-API `:5001`. |
| 12 | FND-02.01 | Build Account ledger/payouts and Pharmacy order screens in this sprint? | No. Layout stubs / Coming Soon stay until later weeks. |
| 13 | WEB-07.01 WEB-08.01 | Is the copy on `/privacy` and `/terms` the **signed** legal text, or do you have a lawyer PDF to replace it? PolicyVersion in DB is `2026.09`. | Keep current pages; bump version only when you send new copy. |

## Inputs (I cannot invent)

| # | SubIDs | What to send | Today |
|---|--------|--------------|-------|
| 14 | SEC-07.02 PRE-03 PAT-03.02 CON-02.04 WEB-04.02 | SMS vendor (MSG91 / Twilio / Exotel), API key, sender ID, **India DLT template IDs**, OTP text. | `StubSmsSender`. OTP is Dev/challenge / `devCode`. Test mobile `7768046064`. |
| 15 | S5 WhatsApp | Confirm Meta `AccessToken` / `PhoneNumberId` still valid, or new token in appsettings. | Token already in New-API `WhatsAppMeta`. Not used for S1/S2 case-taking. |
| 16 | WEB-05 Razorpay | Confirm live key in `OrderService.cs` + `Widgets.js` is intended, **or** test keys in appsettings (recommended). | Live key hardcoded in source. |
| 17 | CLN-08 CLN-09 audio | OpenAI or Azure OpenAI key + deployments. | Audio/embeddings mock or skip when key empty. |
| 18 | go-live | Production SQL server, database, auth. Production UI URL, Old-API URL, New-API URL. Production mailbox if Gmail is Dev-only. | Dev: `localhost\MSSQLSERVER25` / `HomeoCentrum_Dev`. SiteUrl `http://localhost:3000`. SMTP Gmail `tufanpowar001@gmail.com`. |
| 19 | SEC-09 WEB-09 | Production document storage: local disk vs Azure Blob / S3. | Local disk `SecureDocument`. |
| 20 | WEB-03 map | Confirm Google Maps browser key in UI is yours and billing-enabled, or replacement. | Key in `GoogleMaps.js`. |
| 21 | DMO-06.02 PAT Device | FCM + APNs for push. | Device register API exists. Not required for clinic web. |

## Suggestions (optional)

| # | Topic | Suggestion |
|---|--------|------------|
| 22 | Razorpay | Move keys out of C#/JS into appsettings. Do not leave `rzp_live_*` in git. |
| 23 | JWT leftovers | New-API `ValidAudience` `localhost:4200` / `ValidIssuer` `localhost:5000` are old Angular ports. Clinic SPA is `:3000`, New-API `:5038`. Clean on go-live. |
| 24 | Old-API welcome mail | **Done 22 Sep 2026:** `UserService` builds `/login?UserId=` from `ConfigurationModel:SiteUrl`, then `HostName`, then `AppSettings:UiBaseUrl`, then `http://localhost:3000`. Change SiteUrl per env. |
| 25 | 3D hotspots CLN-20 | SQL backfill had **0 / 5** exact name matches. Send the correct SubSection names if you want 100% hotspot mapping this week. |
| 26 | ErrorAlert | Recipients = `tufanpowar001@gmail.com`, cooldown 10 min, `Enabled=true`. Change only if production should use another mailbox. |

## Second pass (22 Sep 2026 pm) — extras beyond 1–26

Nothing else is required to close S1/S2 if you only answer 1–26. These are the only additions after a full re-scan of intern rows, `appsettings`, `config.js`, CORS, OTP/hold TTLs, and leftover UI.

### More confirmations

| # | Area | Question | Default if you skip |
|---|------|----------|---------------------|
| 27 | WEB-05 public book | Keep **pay at clinic** (slot hold) this week, or force Razorpay online pay now? | Keep pay-at-clinic hold. Online pay stays the existing `/book/pay` path. |
| 28 | SEC-07 OTP | OTP lifetime? Hardcoded **10 minutes**. | 10 minutes. |
| 29 | WEB-10 | Doctor activation email lifetime? Hardcoded **48 hours**. | 48 hours. |
| 30 | SEC-01 JWT | Clinic JWT lifetime? Old-API **7 days** (`ExpiryInMinutes` 10080). | 7 days. |
| 31 | Patient Board | Max open cases at once? Hardcoded **5**. | 5. |
| 32 | CLN-08 audio | Delete recordings automatically? `AudioRetentionDays` = **0** (keep forever). | Keep forever until you set a day count. |
| 33 | Leftover UI | Dummy WhatsApp/call and Patient Board Global Search **done 22 Sep 2026** (real mobile or `0000000000`; header Global Search runs repertory subsection search). Landing store badges and Velzon sample activity still later. | Store badges + Velzon sample activity later. |

### More configuration (go-live; Dev can stay as-is)

| # | Area | Question | Default if you skip |
|---|------|----------|---------------------|
| 34 | CORS | Production APIs currently allow **any origin**. Lock to your UI host? | Dev: any origin. Prod: lock when you give the UI URL in **18**. |
| 35 | TokenKey | Same signing key is in both `appsettings.json` files (clinic JWT). Rotate for production? | Keep current key on Dev. Rotate only when you provide a new key out of chat (user-secrets / server appsettings). |
| 36 | Timezone | Clinic appointment dates: **IST** (`Asia/Kolkata`) vs UTC? | IST. |
| 37 | Dial code | WhatsApp/SMS default country code? | `91` (already in `WhatsAppMeta`). |
| 38 | HTTPS | Local APIs run **http** `:5038` / `:5001`. Production HTTPS host names? | Covered by **18**. Local stays http. |

`OpenAI` / `AzureOpenAI` sections are **missing** from New-API `appsettings.json` — that is item **17**, not a new item. UI `config.js` hosts (`localhost:5038` / `:5001`) are item **18**.

Not a new ask: Gmail SMTP password already in Dev appsettings (do not paste a replacement in chat). Swagger on local launch profiles. `AllowedHosts: *`. File logs under the API process `Logs/` folder. Audio files under `Data/AudioCaseTaking`. New-API `JWT:Secret` is unused (`TokenKey` is used); leftover Angular ports are item **23**.

Already decided (not asking again): dual-API; never agent-commit; Tufan_* / `123456`; ErrorAlert + FileLog flags; `tufanpowar001@gmail.com` not `tufanpowar@gmail.com`; Tufan_Doctor extra menus in existing More dropdown; Family helper text removed; doctor mobile must not get case-taking; Reception not on Patient Board.

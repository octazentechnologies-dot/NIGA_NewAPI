# Messaging + Tele video (keys later)

Orchestration APIs stay live; carriers and A/V media are pluggable.

## Why Device check / waiting room / rejoin / join-failure / chat / summary exist

Those are **NIGA session orchestration** (who may join, status, audit, consent, chat, summary). They do **not** require Agora/Twilio/Daily or SignalR.

Clients **poll** `GET Tele/Session/{id}`, queue, chat list — no SignalR hub.

Live A/V media only appears when `TeleVideo:Vendor` is set and vendor keys are filled. Until then Token/Rejoin return a **Stub** token + `clientConfig` so SPA/mobile share one contract.

## appsettings (`Homeocentrum.Niga.NewAPI`)

```json
"Sms": {
  "Provider": "Stub",          // Stub | Msg91 | Twilio
  "Enabled": true,
  "DefaultCountryDialCode": "91",
  "Msg91": { "AuthKey": "", "SenderId": "", "Route": "4", "OtpTemplateId": "", "DltEntityId": "" },
  "Twilio": { "AccountSid": "", "AuthToken": "", "FromNumber": "" }
},
"TeleVideo": {
  "Vendor": "Stub",            // Stub | Agora | Twilio | Daily
  "TokenTtlMinutes": 60,
  "Agora": { "AppId": "", "AppCertificate": "" },
  "Twilio": { "AccountSid": "", "ApiKeySid": "", "ApiKeySecret": "" },
  "Daily": { "ApiKey": "", "Domain": "" }
},
"WhatsAppMeta": { /* AccessToken + PhoneNumberId — already used by WhatsApp APIs */ }
```

Leave keys empty → Stub logs (OTP/reschedule/cancel still succeed). Fill keys later without code changes (except Twilio Video / Daily JWT may need a follow-up once you choose the vendor).

## Scenarios wired

| Event | SMS | WhatsApp | Notes |
|-------|-----|----------|--------|
| OTP request | Yes (code in body) | — | `devCode` only in Development |
| Appointment reschedule | Yes | If opt-in + Meta | Push later |
| Appointment cancel | Yes | If opt-in + Meta | Push later |
| Waitlist offer after cancel | Yes | Best-effort | No auto-book |
| Tele session Start → Active | Yes | If opt-in + Meta | Patient still polls waiting room |

## Go live checklist (when you have keys)

1. Set `Sms:Provider` to `Msg91` or `Twilio` and fill that block.
2. Confirm `WhatsAppMeta` token still valid (or replace).
3. Set `TeleVideo:Vendor` + matching keys; wire client SDK (Agora Web SDK / Twilio Video / Daily) using Token response `token` + `clientConfig`.
4. For India OTP SMS, set Msg91 DLT `OtpTemplateId` / `DltEntityId` as required by your DLT registration.

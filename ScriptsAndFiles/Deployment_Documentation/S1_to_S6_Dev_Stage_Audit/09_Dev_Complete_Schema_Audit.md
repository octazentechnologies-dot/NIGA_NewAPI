# 09 — Dev Complete Schema Audit

## Scope

Independent audit of `HomeoCentrum_Dev` against canonical S1–S6 script intents (object/column evidence). Not a claim that every historical script hash was executed.

## Summary

| Domain | Result |
|--------|--------|
| Security / OTP foundation (OtpChallenge) | Present |
| Address location stack | Present + large master data |
| S5 notifications / pharmacy / consent / FTS SearchNormalized | Present |
| S6 SMS OTP / FCM log / DeviceType / CareLink / notification Type+DeepLink | Present — verify PASSED |
| InstantConsult PatientAppId / TeleSessionId | Present |
| FollowUpTask.CompletedAt | Present |

## Missing relative to S1–S6 scripts

**None critical.** No Missing_Schema CREATE required for Dev.

## Drift notes

- `AppNotification.Type` / `DeepLink` lengths **wider** than S6 script literals (80/600 vs 40/300). Class: DRIFTED (compatible / non-shrinking).  
- Filename `12_InstantConsult_VisitLink.sql` does not add VisitLink/VisitUrl columns (those remain absent on Dev and Stage — script content is PatientAppId/TeleSessionId).

## Extra objects (not from S1–S6)

Mobile clinic tables present — see doc 10 and Missing_Schema Mobile notes.

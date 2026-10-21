# 02 — Dev Preflight And Deployment

## Identity

| Check | Evidence |
|-------|----------|
| Server | `Server1038\NIGA` |
| Database | `HomeoCentrum_Dev` |
| Status / Recovery | ONLINE / SIMPLE |
| Collation | SQL_Latin1_General_CP1_CI_AS |
| Compatibility | 150 |
| User tables | 289 |
| Latest full backup (msdb) | `C:\SQL\HomeoCentrum_Dev_PreScripts_2026-10-08.bak` finish **2026-10-07 12:17:44** |

## S4–S6 object markers (2026-10-20)

All critical markers **OK**, including: UserMaster location FKs, District/City/Pin masters, S5 messaging/security tables, S6 SmsOtpVerify / FirebaseNotificationLog / DeviceType / CareLink* / AppNotification Type+DeepLink / InstantConsult PatientAppId+TeleSessionId / FollowUpTask.CompletedAt.

`05_Verify_S6_Objects.sql` on Dev: **S6 verify PASSED**.

## Location master counts (active)

Districts **5910**, Cities **100027**, Pins **194144** (higher than Stage — EXPECTED_ENVIRONMENT_DIFFERENCE / seed volume).

## Classification

| Scope | Dev state | Action this engagement |
|-------|-----------|------------------------|
| S1–S4 historical | Objects present | UNVERIFIED_HISTORY / VERIFY_ONLY — do not rerun non-idempotent seeds |
| S4 address 05–13 | Present | ALREADY_APPLIED_VERIFIED |
| S5 01–15 (except optional 10) | Core objects present | ALREADY_APPLIED_VERIFIED |
| S5 10 LOCAL_ONLY | Optional | Not executed |
| S6 01–04 | Present | ALREADY_APPLIED_VERIFIED — **no execute** |
| S6 05 | Verify | PASSED |
| Mobile-only tables | Present on Dev | Out of S1–S6; not altered |

## Deployment actions executed this engagement

**None** (read-only audit + documentation). No Dev DDL required for S1–S6 completeness.

## Verdict

**Dev: PASS** for required S1–S6 schema/object evidence (provenance of older weeks remains UNVERIFIED_HISTORY without checksum deploy log).

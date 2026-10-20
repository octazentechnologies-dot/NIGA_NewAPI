# 03 — Stage Preflight And Deployment

## Identity

| Check | Evidence |
|-------|----------|
| Server | `Server1038\NIGA` |
| Database | `HomeoCentrum_Stage` |
| Status / Recovery | ONLINE / SIMPLE |
| Collation | SQL_Latin1_General_CP1_CI_AS |
| Compatibility | 150 |
| User tables | 276 |
| Last full backup (msdb) | `C:\SQL\HomeoCentrum_Stage_PreS4Address_2026-10-13_125331.bak` finish **2026-10-13 00:25:45** |

## Post-2026-10-13 context

Stage already received S4 address `05`–`13` and S5 core after that backup (prior engagement). Backup image is **stale relative to current Stage**.

## Markers (2026-10-20)

| Area | Stage |
|------|-------|
| S4 address / masters | OK (Districts 5736 / Cities 99853 / Pins 193970) |
| S5 core tables | OK |
| S6 SmsOtpVerify / FirebaseNotificationLog | **MISSING** |
| DevicePushToken.DeviceType | **MISSING** |
| CareLinkRequest / CareLinkInfoRequest / Opinion | **MISSING** |
| AppNotification.Type / DeepLink | **MISSING** |
| InstantConsult PatientAppId / TeleSessionId | **MISSING** |
| FollowUpTask.CompletedAt | OK |

## Classification

| Scope | Stage state | Action |
|-------|-------------|--------|
| S1–S4 core + address | Present | VERIFY_ONLY / UNVERIFIED_HISTORY |
| S5 core | Present | ALREADY_APPLIED_VERIFIED |
| S5 10 LOCAL_ONLY | Not required | BLOCKED_OPTIONAL |
| S6 01–04 | **PENDING** | **EXECUTE after fresh backup approval** |
| S6 05 | Pending | VERIFY_ONLY after 01–04 |
| Mobile Dev-only tables | Absent | BLOCKED — needs separate Mobile approval |

## Deployment this engagement

**STOPPED** — safety gate. No Stage DDL executed.

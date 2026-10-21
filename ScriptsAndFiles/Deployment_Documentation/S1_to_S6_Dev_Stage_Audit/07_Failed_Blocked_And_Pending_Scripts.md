# 07 — Failed, Blocked, And Pending Scripts

## FAILED

None this engagement (no Stage/Dev DDL executed after read-only audit).

## BLOCKED

| Item | Reason |
|------|--------|
| Stage S6 `01`–`04` | Fresh Stage backup required (current backup pre-dates post-backup S4/S5 changes) |
| Mobile tables → Stage | Not part of S1–S6; Dev-only Mobile script |
| Shrink Dev AppNotification lengths | Drift is wider-than-script; destructive risk |
| Dual SUCCESS claim | Stage S6 incomplete |

## PENDING (Stage) — after `APPROVE STAGE S6 WITH BACKUP`

1. S6 `01` → `04` execute  
2. S6 `05` + Missing_Schema verify script  

## PENDING (optional)

- S5 `10` LOCAL_ONLY on either env  
- Mobile package for Stage parity  

## APPLIED / VERIFIED (evidence)

| Item | Dev | Stage |
|------|-----|-------|
| S4 address + S5 core objects | Yes | Yes |
| S6 objects | Yes (verify PASSED) | **No** |
| S3 InstantConsult PatientAppId/TeleSessionId | Yes | **No** |

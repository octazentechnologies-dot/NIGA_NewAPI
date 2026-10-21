# 14 — Backup, Rollback, And Recovery Plan

## Current backup evidence (msdb)

| Database | Latest full backup path | Finish (server) | Suitable for pending work? |
|----------|-------------------------|-----------------|----------------------------|
| HomeoCentrum_Dev | `C:\SQL\HomeoCentrum_Dev_PreScripts_2026-10-08.bak` | 2026-10-07 12:17:44 | Yes for **read-only** / no new Dev DDL this engagement |
| HomeoCentrum_Stage | `C:\SQL\HomeoCentrum_Stage_PreS4Address_2026-10-13_125331.bak` | 2026-10-13 00:25:45 | **No** for S6 — Stage changed after this backup |

## Required before Stage S6

1. Full backup of **current** `HomeoCentrum_Stage` (Express: omit COMPRESSION if unsupported).  
2. Record finish time + device path.  
3. Reply `APPROVE STAGE S6 WITH BACKUP`.  
4. Execute S6 `01`→`05` with identity checks.  
5. Re-run verify scripts.

## Rollback

| Scenario | Action |
|----------|--------|
| S6 script fails mid-batch | Stop; do not retry blindly; inspect messages; restore from **pre-S6** backup if DB left inconsistent |
| Need to undo S6 | Prefer restore; see `Missing_Schema/12_Rollback_And_Recovery/Stage_S6_Rollback_Notes.md` for high-risk reverse DDL |

## Production

Never connect. Never restore Stage/Dev over Production.

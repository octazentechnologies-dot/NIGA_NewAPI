# 02 — Production Backup And Recovery

## Pre-change full backup (mandatory)

| Field | Value |
|-------|-------|
| Path | `D:\SQL_Backups\HomeoCentrum_Production\HomeoCentrum_Production_Full_2026-10-20_101652.bak` |
| Start | 2026-10-20 10:16:54 (server) |
| Finish | 2026-10-20 10:19:14 (server) |
| Size (backupset) | ~13316 MB |
| COPY_ONLY | Yes |
| CHECKSUM | Yes |
| COMPRESSION | Not supported on Express — omitted |
| Pages | 1704457 |
| RESTORE VERIFYONLY | **The backup set on file 1 is valid.** |

## Disk

- D: free before backup ≈ 168 GB  
- DB size estimate ≈ 15184 MB  

## Recovery notes

- Preferred rollback: restore this COPY_ONLY full backup to a **non-production** name first if validating; production restore only under incident process.  
- RESTORE VERIFYONLY confirms media readability, not a full restore drill.  
- No TDE observed in preflight options checked this engagement.

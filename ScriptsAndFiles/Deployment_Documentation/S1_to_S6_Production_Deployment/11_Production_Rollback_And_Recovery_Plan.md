# 11 — Production Rollback And Recovery Plan

## Primary rollback artifact

`D:\SQL_Backups\HomeoCentrum_Production\HomeoCentrum_Production_Full_2026-10-20_101652.bak`  
Finish: 2026-10-20 10:19:14 · COPY_ONLY · VERIFYONLY valid · ~13316 MB

## Procedure (incident only)

1. Stop writers / take maintenance window.  
2. Restore backup to a side database first when possible.  
3. Only after validation, restore over Production under change control.  
4. Re-test app login and critical reads.

Do not drop newly created tables ad-hoc as primary rollback when a verified full backup exists.

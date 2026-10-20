# 01 — Production Connection And Preflight

**Date:** 2026-10-20  
**Connectivity:** VERIFIED

## Identity evidence

| Property | Value |
|----------|-------|
| Reachable host (from deploy workstation) | `100.124.149.15,1433` |
| RDP host | `100.124.149.15` (ping OK; RDP 3389 open) |
| `@@SERVERNAME` | `DESKTOP-I7AMAS6` |
| MachineName | `DESKTOP-I7AMAS6` |
| InstanceName | default (NULL) |
| Edition | Express Edition (64-bit) |
| ProductVersion | 17.0.1000.7 |
| Database | `HomeoCentrum_Production` |
| database_id | 5 |
| Status | ONLINE |
| Recovery | SIMPLE |
| Compatibility | 150 |
| Collation | SQL_Latin1_General_CP1_CI_AS |
| User tables (pre-deploy) | 193 |

## Permissions (smoke)

- CREATE TABLE / ALTER DATABASE: granted  
- Backup: succeeded (COPY_ONLY full backup)

## Notes

- `Data Source=.` is for clients running **on** the RDP box; from Mac, TCP `100.124.149.15,1433` was verified.  
- Credentials are not recorded in this document.

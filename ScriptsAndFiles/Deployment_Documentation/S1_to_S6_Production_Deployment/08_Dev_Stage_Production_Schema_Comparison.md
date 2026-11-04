# 08 — Dev / Stage / Production Schema Comparison

**Last verified:** 2026-11-04

| Kind | Dev | Stage | Production |
|------|----:|------:|-----------:|
| Tables | 293 | 293 | 293 |
| Columns | 2614 | 2614 | 2614 |
| Named indexes (excl. system `PK__%`) | aligned | aligned | aligned |

Missing-in-Stage vs Dev: **none**  
Missing-in-Production vs Dev (2026-11-04): applied  
- `12_Appointment_RescheduleCount.sql` → `PatientAppointment.RescheduleCount`  
- `11_RubricRemedyDetails_Repertorize_Index.sql` → `IX_RubricRemedyDetails_SubSection_Active`  

Pre-sync Production backup:  
`D:\SQL_Backups\HomeoCentrum_Production\HomeoCentrum_Production_PreSchemaSync_2026-11-04_203724.bak`

Business row data intentionally not synchronized.

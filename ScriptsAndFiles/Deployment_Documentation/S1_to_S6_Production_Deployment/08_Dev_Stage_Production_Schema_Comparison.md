# 08 — Dev / Stage / Production Schema Comparison

**Last verified:** 2026-11-05 (afternoon re-sync)

| Kind | Dev | Stage | Production |
|------|----:|------:|-----------:|
| Tables | 293 | 293 | 293 |
| Columns | 2619 | 2619 | 2619 |
| Named indexes (excl. system `PK__%`) | 205 | 205 | 205 |

Missing-in-Stage vs Dev: **none** (0 column / 0 table diffs)  
Missing-in-Production vs Dev: **none** (0 column / 0 table diffs)  
No Production pre-sync backup this pass — no DDL gaps; only idempotent `14` re-run (0 new rows).

### Recent markers (all OK on Dev / Stage / Production)

| Marker | Script |
|--------|--------|
| `PatientAppointment.RescheduleCount` | `12_Appointment_RescheduleCount.sql` |
| `MedicineOrder.DeliveryFee` / `MedicineQuote.DeliveryFee` | `13_MedicineOrder_DeliveryFee.sql` |
| `UserMaster` / `Patient` / `Doctor`.`RegistrationChannel` | `14_ClinicPatientRoster_And_RegistrationChannel.sql` |
| `PatientAppointment.BookingChannel` / `BookedByUserId` | earlier S6 runtime gaps |

`14_ClinicPatientRoster_And_RegistrationChannel.sql` re-applied 2026-11-05 on all three (idempotent; 0 new backfill rows — prior backfill already complete). Remaining appointment pairs without an active case are deleted/missing patients only — intentionally skipped.

Pre-sync Production backup (prior full sync):  
`D:\SQL_Backups\HomeoCentrum_Production\HomeoCentrum_Production_PreSchemaSync_2026-11-04_203724.bak`

Business row data intentionally not synchronized.

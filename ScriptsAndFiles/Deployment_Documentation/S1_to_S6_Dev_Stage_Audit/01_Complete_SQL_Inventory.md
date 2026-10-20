# 01 — Complete SQL Inventory (S1–S6)

**Audit date:** 2026-10-20  
**Root:** `ScriptsAndFiles/`  
**Machine inventory JSON:** `_inventory.json` (same folder)

## Actual file counts (discovered)

| Week | Files | Unique SHA-256 | Duplicate copies | Historical baseline | Delta |
|------|------:|---------------:|-----------------:|--------------------:|------:|
| S1_Week1 | **10** | 10 | 0 | 10 | 0 |
| S2_Week2 | **27** | 27 | 0 | 25 | **+2** |
| S3_Week3 | **37** | 21 | **16** | 36 | **+1** |
| S4_Week4 | **13** | 13 | 0 | 4 | **+9** |
| S5_Week5 | **15** | 15 | 0 | *(not in baseline)* | discovered |
| S6_Week6 | **5** | 5 | 0 | *(not in baseline)* | discovered |
| **Total** | **107** | **91** | **16** | 75 (S1–S4 hist.) | — |

Historical “59 unique / 16 duplicates” referred to S1–S4 only. Current unique SHA across S1–S6 = **91**.

## Historical vs current differences

| Change | Detail |
|--------|--------|
| S2 +2 | `00_DOC_DoctorDailySchedule_Table.sql`; second `25_*` file (`25_WEB_Doctor_Credential_FilePath.sql`) — same prefix, **different content**, both unique |
| S3 +1 | `12_InstantConsult_VisitLink.sql` (adds PatientAppId/TeleSessionId; filename ≠ VisitLink columns) |
| S3 dups | Same 16 byte-identical unnumbered ↔ numbered pairs as prior audit |
| S4 +9 | Address-location stack `05`–`13` (beyond historical core `01`–`04`) |
| S5 | **15** discovered |
| S6 | **5** discovered |

## S6 files (5)

| # | File | Category |
|---|------|----------|
| 01 | `01_SmsOtpVerify_And_FirebaseNotificationLog.sql` | Schema |
| 02 | `02_DevicePushToken_DeviceType.sql` | Schema |
| 03 | `03_DoctorApp_Gap_Schema.sql` | Schema |
| 04 | `04_Verify_And_Ensure_Gap_Schema.sql` | Schema + verify |
| 05 | `05_Verify_S6_Objects.sql` | Verification |

## Duplicate groups (S3 only — 16 extra copies)

Unnumbered `M05_*` / `M06_*` / `M12_*` / `M14_*` files are byte-identical to numbered counterparts. **DUPLICATE_NOT_REEXECUTED.**

## Same-prefix different content (must both be tracked)

- S2: `25_DEV_Seed_Tufan_Doctor_Extra_Menus.sql` vs `25_WEB_Doctor_Credential_FilePath.sql`
- S3: multiple `06_*`, `08_*`, `10_*`, `12_*` prefixes with **different** hashes (not duplicates)

## Missing_Schema generated (this engagement)

No duplicate S6 DDL. Generated: README, Stage plan, Mobile notes, rollback notes, `11_Verification/001_Verify_S6_Parity_Markers.sql`. See hashes in `_inventory.json`.

# 03 — Complete S1–S6 And Parity Pack Inventory

**Audit date:** 2026-10-20

| Source | Files | Notes |
|--------|------:|-------|
| S1_Week1 | 10 | |
| S2_Week2 | 27 | +2 vs historical 25 |
| S3_Week3 | 37 | 16 byte-identical duplicate copies |
| S4_Week4 | 13 | includes address stack 05–13 |
| S5_Week5 | 15 | discovered |
| S6_Week6 | 5 | discovered |
| Portable_Parity_Pack | 7 | 001–007 (+ verify); overlaps S6 for 001/002/005 |
| **Total** | **114** | **98 unique SHA**; **16** duplicate copies |

Machine inventory: `/tmp/niga_prod/inventory.json`

## Production execution set (this engagement)

**71** production-safe scripts selected (DEV seeds, demos, LOCAL_ONLY, unit/test, junk-role delete, and S3 unnumbered duplicates excluded). List: `/tmp/niga_prod/exec_list.txt`.

# 04 — Production Execution Order

## Applied this engagement (71 production-safe scripts)

Order file: `/tmp/niga_prod/exec_list.txt`

1. S1 schema/menus/verify (skip DEV seeds / unit / sample insert)
2. S2 schema + selected menus (skip DEV Tufan/role seed scripts)
3. S3 numbered scripts + InstantConsult (skip unnumbered duplicates + DEV seed)
4. S4 schema/menus/address 05–13 (skip demo)
5. S5 schema/menus/address lines/reminders/security/consent/indexes/extras/FTS (skip demo, LOCAL_ONLY, Remove_Junk_Roles)
6. S6 01–05
7. Portable pack 003, 004, 006, 007 (001/002/005 covered by S6)

## Explicitly NOT executed on Production

- All `DEV_Seed*` / Tufan identity / demo data scripts
- S5 `10_Security_Test_Tenant_LOCAL_ONLY.sql`
- S5 `13_Remove_Junk_Roles.sql`
- S3 byte-identical unnumbered duplicates
- Unit test / sample insert scripts

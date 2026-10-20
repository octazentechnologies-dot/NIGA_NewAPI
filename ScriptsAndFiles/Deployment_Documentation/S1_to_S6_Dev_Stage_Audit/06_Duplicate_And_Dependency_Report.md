# 06 — Duplicate And Dependency Report

## Byte-identical duplicates (16 extra copies — S3)

Each unnumbered `M##_*.sql` matches its numbered twin. Canonical = numbered path. **Never execute both.**

## Same-prefix, different content

| Files | Note |
|-------|------|
| S2 `25_DEV_Seed_Tufan_Doctor_Extra_Menus.sql` + `25_WEB_Doctor_Credential_FilePath.sql` | Both unique; both required if environment needs menus + FilePath |
| S3 multiple `06_*` / `08_*` / `10_*` / `12_*` | Distinct scripts sharing numeric prefix |

## Overlapping intent (idempotent)

| Scripts | Shared effect |
|---------|---------------|
| S3 `12_InstantConsult_VisitLink.sql` + S6 `04` | Add InstantConsultRequest.PatientAppId / TeleSessionId |
| S6 `03` + S6 `04` | CareLink* + AppNotification Type/DeepLink ensure |

Safe to run both due to IF NOT EXISTS / COL_LENGTH guards; prefer single pass of S6 `01`–`04` on Stage.

## Risk scripts

| Script | Risk |
|--------|------|
| S5 `10_Security_Test_Tenant_LOCAL_ONLY.sql` | Environment-specific |
| S5 `13_Remove_Junk_Roles.sql` | Conditional deletes; keeps EmptyMenuProbe if users remain |
| S5 `14_Required_Extras.sql` | Unique index may fail on duplicate UserName |
| S5 `15` FTS | Large backfill / FTS |
| S4 `08` India location | Large data load |

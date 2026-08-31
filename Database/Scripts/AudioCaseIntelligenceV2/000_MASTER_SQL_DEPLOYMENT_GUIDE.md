# HomeoCentrum AI Engine V2 — Master SQL Deployment Guide

**Database:** `HomeoCentrum_Production`  
**Policy:** Execute scripts **manually** in SSMS. Do not auto-apply from the application.  
**Folder:** `New_API/Database/Scripts/AudioCaseIntelligenceV2/`

**One-click deploy (Phases 0–6):** run **`000_DEPLOY_ALL_Phases_0_to_6.sql`** — consolidates all 21 scripts below in order (~1000 lines). Individual scripts remain available if you prefer step-by-step execution.

---

## Before You Start

1. Take a **full database backup**.
2. Run on a **staging copy first** if available.
3. Execute scripts **in the exact order** below.
4. After each phase, verify with the checklist at the end of that phase.
5. Set `RubricIntelligence:EnableV2` in `appsettings.json` only after Phase 1 + 102 are applied.

---

## Phase 0 — Audio Case Taking V1 (if not already done)

| Step | Script | Purpose |
|------|--------|---------|
| 0.1 | `../AudioCaseTaking_CreateTables.sql` | Core audio session + audit tables |

---

## Phase 1 — Case Understanding (REQUIRED for V2)

| Step | Script | Purpose |
|------|--------|---------|
| 1.1 | `001_Create_AudioCaseClinicalConcept.sql` | Clinical concepts per session |
| 1.2 | `002_Create_AudioCaseIntelligenceLog.sql` | Pipeline stage audit log |
| 1.3 | `102_Alter_AudioCaseSession_V2Columns.sql` | `ClinicalConceptsJson`, `IntelligenceEngineVersion` on session |

**Verify:**
```sql
SELECT TOP 1 * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AudioCaseClinicalConcept';
SELECT TOP 1 * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AudioCaseSession' AND COLUMN_NAME = 'ClinicalConceptsJson';
```

---

## Phase 2 — Metaphors + Aliases + Admin Audit (THIS PHASE)

| Step | Script | Purpose |
|------|--------|---------|
| 2.1 | `003_Create_RubricMetaphorDictionary.sql` | Patient expression → clinical meaning |
| 2.2 | `004_Create_RubricAlias.sql` | Patient phrases → SubSectionId |
| 2.3 | `014_Create_RubricAdminAuditLog.sql` | Admin CRUD audit trail |
| 2.4 | `501_Seed_MetaphorDictionary_EN_HI_MR.sql` | Seed metaphors (EN/HI/MR) |
| 2.5 | `502_Seed_RubricAlias_Batch001.sql` | Seed aliases (dynamic SubSection lookup) |

**Verify:**
```sql
SELECT COUNT(*) AS MetaphorCount FROM dbo.RubricMetaphorDictionary WHERE IsActive = 1;
SELECT COUNT(*) AS AliasCount FROM dbo.RubricAlias WHERE IsActive = 1;
SELECT TOP 5 * FROM dbo.RubricMetaphorDictionary WHERE ApprovalStatus = 'Approved';
```

---

## Phase 3 — Causation + Weight Rules (THIS PHASE)

| Step | Script | Purpose |
|------|--------|---------|
| 3.1 | `006_Create_HomeopathicWeightRule.sql` | Configurable homeopathic weight hierarchy |
| 3.2 | `007_Create_AudioCaseCausationLink.sql` | Persist cause→effect links per session |
| 3.3 | `103_Alter_AudioCaseSession_CausationLinksJson.sql` | Cache causation JSON on session for fast GET /concepts |
| 3.4 | `503_Seed_HomeopathicWeightRule.sql` | Seed default SRP/mental/causation weights |

**Verify:**
```sql
SELECT COUNT(*) AS WeightRuleCount FROM dbo.HomeopathicWeightRule WHERE IsActive = 1;
SELECT TOP 1 * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AudioCaseCausationLink';
SELECT TOP 1 * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AudioCaseSession' AND COLUMN_NAME = 'CausationLinksJson';
SELECT RuleCode, Category, WeightValue FROM dbo.HomeopathicWeightRule ORDER BY WeightValue DESC;
```

---

## Phase 4 — JSON Embeddings + Hybrid Search (THIS PHASE)

| Step | Script | Purpose |
|------|--------|---------|
| 4.1 | `005_Create_RubricEmbeddings.sql` | JSON embedding vectors keyed by SubSectionId |
| 4.2 | `201_Indexes_All.sql` | Performance indexes for embeddings + intelligence tables |

**Verify:**
```sql
SELECT COUNT(*) AS EmbeddingCount FROM dbo.RubricEmbeddings;
SELECT TOP 5 RubricId, ModelName, SourceType, LEN(EmbeddingJson) AS JsonLength
FROM dbo.RubricEmbeddings ORDER BY CreatedDate DESC;
SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.RubricEmbeddings');
```

**After deploy:**
1. Deploy API with Phase 4 code.
2. Trigger initial index: `POST /api/AudioCaseIntelligence/embeddings/reindex?maxRubrics=500` (admin auth).
3. Check status: `GET /api/AudioCaseIntelligence/embeddings/status`.
4. Nightly indexer runs automatically when `EnableV2` + `EnableEmbeddingSearch` are true.

**Hybrid scoring (locked):** 40% embedding + 30% alias + 20% clinical + 10% keyword LIKE proxy.

---

## Phase 5 — Inference + Explainability (THIS PHASE)

| Step | Script | Purpose |
|------|--------|---------|
| 5.1 | `008_Create_AudioCaseClinicalInferenceLog.sql` | Audit log for inferred rubrics |
| 5.2 | `101_Alter_AudioCaseRubricMatchLog_V2Columns.sql` | Explainability columns on match log |

**Verify:**
```sql
SELECT TOP 1 * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AudioCaseClinicalInferenceLog';
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AudioCaseRubricMatchLog'
  AND COLUMN_NAME IN ('ClinicalMeaning','WhySuggested','ConfidenceScore','RubricTier','MatchLayer','ExplainabilityJson');
```

**UI (after API deploy):**
- Expandable rubric rows show patient statement → clinical → homeopathic meaning
- Approve / Reject actions when V2 manual approval is enabled
- Inference rubrics tagged `RubricTier = Inference` and require doctor review

---

## Phase 6 — Feedback + Benchmark + Gold Library (THIS PHASE)

| Step | Script | Purpose |
|------|--------|---------|
| 6.1 | `009_Create_AudioCaseRubricFeedback.sql` | Doctor accept/reject/correct feedback |
| 6.2 | `010_Create_AudioCaseRubricBenchmark.sql` | Per-session precision/recall/acceptance metrics |
| 6.3 | `015_Create_GoldCaseLibrary.sql` | Internal gold standard benchmark library |
| 6.4 | `504_Seed_GoldCaseLibrary_Batch001.sql` | Starter gold cases (expand to 250+) |

**Verify:**
```sql
SELECT TOP 1 * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AudioCaseRubricFeedback';
SELECT TOP 1 * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AudioCaseRubricBenchmark';
SELECT COUNT(*) AS GoldCaseCount FROM dbo.GoldCaseLibrary WHERE IsActive = 1;
SELECT TOP 5 Category, LEFT(Transcript, 80) AS TranscriptPreview FROM dbo.GoldCaseLibrary;
```

**API (after deploy):**
- Doctor: `POST /api/AudioCaseTaking/{id}/rubrics/feedback`
- Admin: `GET /api/AudioCaseIntelligence/benchmark/summary`
- Admin: `GET /api/AudioCaseIntelligence/benchmark/trends`
- Admin: `GET /api/AudioCaseIntelligence/feedback/queue`

**UI (after deploy):**
- Approve/Reject in case taking calls feedback API (updates alias/metaphor acceptance when learning enabled)
- Admin benchmark dashboard: `/admin/rubric-intelligence-benchmark`

---

## Phase 7 — Repertory Kent + Complete (THIS PHASE)

| Step | Script | Purpose |
|------|--------|---------|
| 7.1 | `011_Create_RepertorySource.sql` | Repertory source registry (Kent, Complete) |
| 7.2 | `012_Create_RubricRepertoryMap.sql` | SubSectionId ↔ repertory source mapping |
| 7.3 | `505_Seed_RepertorySource_Kent_Complete.sql` | Seed Kent/Complete from AuthorMaster links |

**One-click deploy:** `000_DEPLOY_Phase_7.sql` (3 scripts above)

**Verify:**
```sql
SELECT SourceCode, SourceName, PriorityOrder FROM dbo.RepertorySource WHERE IsActive = 1;
SELECT COUNT(*) AS MappedRubrics FROM dbo.RubricRepertoryMap WHERE IsActive = 1;
SELECT TOP 5 m.SubSectionId, s.SourceCode, m.SourceRubricKey
FROM dbo.RubricRepertoryMap m
JOIN dbo.RepertorySource s ON s.RepertorySourceId = m.RepertorySourceId
WHERE m.IsActive = 1;
```

**After deploy:**
- Rubric suggestions show **Primary / Secondary / Confirmatory / Inference** tier sections
- Kent/Complete badges appear on mapped rubrics
- Orchestrator stage: `RepertoryMapping`

---

## Phase 8 — Production Rollout (THIS PHASE)

No new SQL required. Deploy API + UI, then use admin controls.

| Item | How |
|------|-----|
| Enable V2 all doctors | Admin dashboard → **Enable V2** or `PUT /api/AudioCaseIntelligence/config` |
| Instant rollback | **Rollback to V1** button or `RollbackToV1Only: true` |
| Monitoring | `/api/AudioCaseIntelligence/health` + benchmark dashboard |
| Go-live gates | `/api/AudioCaseIntelligence/rollout/status` (6 gates) |

**Production checklist:**
1. Phase 0–7 SQL applied
2. Deploy New_API + NigaHomeopathy-UI
3. `POST /api/AudioCaseIntelligence/embeddings/reindex?maxRubrics=500`
4. Pilot with `EnableV2: false`, use runtime toggle for selected testing
5. When gates pass → Enable V2 for all doctors
6. Monitor acceptance ≥95% and primary-in-top-5 ≥95% for 2 weeks
7. Senior homeopath sign-off on 30 live cases

**Persist rollout:** set `"EnableV2": true` in `appsettings.json` after validation (runtime override is for emergency/testing only).

---

## Optional Stored Procedures (Phase 2+)

| Step | Script | Status |
|------|--------|--------|
| SP.1 | `301_SP_SearchRubricAlias.sql` | **Optional — app uses EF queries** |
| SP.2 | `302_SP_SearchMetaphorDictionary.sql` | **Optional — app uses EF queries** |

---

## Rollback (Emergency)

Run **`401_Rollback_All_V2.sql`** to drop V2 objects.  
Also set `RubricIntelligence:RollbackToV1Only = true` in appsettings for instant API fallback.

---

## Quick Start — Minimum to Enable V2 Today

Execute **in order**:

```
001_Create_AudioCaseClinicalConcept.sql
002_Create_AudioCaseIntelligenceLog.sql
102_Alter_AudioCaseSession_V2Columns.sql
003_Create_RubricMetaphorDictionary.sql
004_Create_RubricAlias.sql
014_Create_RubricAdminAuditLog.sql
501_Seed_MetaphorDictionary_EN_HI_MR.sql
502_Seed_RubricAlias_Batch001.sql
006_Create_HomeopathicWeightRule.sql
007_Create_AudioCaseCausationLink.sql
103_Alter_AudioCaseSession_CausationLinksJson.sql
503_Seed_HomeopathicWeightRule.sql
```

Then deploy API + set `"EnableV2": true`.

---

## Full Manual Runbook — Phases 0–3 (Execute in Order)

Run each script **one at a time** in SSMS against `HomeoCentrum_Production`:

| # | Script | Phase |
|---|--------|-------|
| 0 | `../AudioCaseTaking_CreateTables.sql` | 0 — V1 base (skip if already done) |
| 1 | `001_Create_AudioCaseClinicalConcept.sql` | 1 |
| 2 | `002_Create_AudioCaseIntelligenceLog.sql` | 1 |
| 3 | `102_Alter_AudioCaseSession_V2Columns.sql` | 1 |
| 4 | `003_Create_RubricMetaphorDictionary.sql` | 2 |
| 5 | `004_Create_RubricAlias.sql` | 2 |
| 6 | `014_Create_RubricAdminAuditLog.sql` | 2 |
| 7 | `501_Seed_MetaphorDictionary_EN_HI_MR.sql` | 2 |
| 8 | `502_Seed_RubricAlias_Batch001.sql` | 2 |
| 9 | `006_Create_HomeopathicWeightRule.sql` | 3 |
| 10 | `007_Create_AudioCaseCausationLink.sql` | 3 |
| 11 | `103_Alter_AudioCaseSession_CausationLinksJson.sql` | 3 |
| 12 | `503_Seed_HomeopathicWeightRule.sql` | 3 |
| 13 | `005_Create_RubricEmbeddings.sql` | 4 |
| 14 | `201_Indexes_All.sql` | 4 |
| 15 | `008_Create_AudioCaseClinicalInferenceLog.sql` | 5 |
| 16 | `101_Alter_AudioCaseRubricMatchLog_V2Columns.sql` | 5 |
| 17 | `009_Create_AudioCaseRubricFeedback.sql` | 6 |
| 18 | `010_Create_AudioCaseRubricBenchmark.sql` | 6 |
| 19 | `015_Create_GoldCaseLibrary.sql` | 6 |
| 20 | `504_Seed_GoldCaseLibrary_Batch001.sql` | 6 |
| 21 | `011_Create_RepertorySource.sql` | 7 |
| 22 | `012_Create_RubricRepertoryMap.sql` | 7 |
| 23 | `505_Seed_RepertorySource_Kent_Complete.sql` | 7 |

**Phase 8** has no SQL scripts — deploy code and use admin rollout controls.

---

## Admin UI Routes (after Phase 2 deploy)

- `/admin/listrubricmetaphors` — manage metaphors
- `/admin/listrubricaliases` — manage aliases
- `/admin/rubric-intelligence-benchmark` — acceptance & benchmark dashboard

---

**Last updated:** Phases 0–8 complete. Full SQL runbook: steps 0–23 + Phase 8 rollout via admin UI.

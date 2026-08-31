# Audio Case Version History

Do **not** treat these as marketing version names. They are engine stamps found in source, `appsettings.json`, telemetry, and existing dated docs.

Exact git SHAs for every milestone were **not fully enumerated** in this audit. Use:

```bash
cd NigaHomeopathy-API
git log --all --oneline --decorate -- Niga-Domain/Repositories/AudioCaseTakingService.cs
git log --follow -- Niga-Domain/Services/AudioCaseIntelligence/Orchestration/FastClinicalRetrievalOrchestrator.cs
```

---

## Current production

| Field | Value | Evidence |
| ----- | ----- | -------- |
| Engine stamp | `fast-f` | `RubricIntelligence:FastPipelineEngineVersion` in `Niga-Web/appsettings.json` |
| Gate | `EnableFastClinicalRetrievalPipeline: true` | same file; `AudioCaseTakingService` Stage C comment |
| Rollback | set flag `false` | `RubricIntelligenceOptions.EnableFastClinicalRetrievalPipeline` |

CONFIRMED: this is the path `MatchRubricsWithIntelligenceAsync` takes today.

---

## Chronological evolution (from code + existing docs)

### Phase — Initial audio case (V1)

- **Problem:** Capture consultation audio and suggest repertory rubrics.
- **Implementation:** Upload → Whisper → GPT extraction → `SearchSubSectionsByHotspotAsync` LIKE/FTS → optional GPT rubric names.
- **Files:** `AudioCaseTakingController`, `AudioCaseTakingService`, `AudioCaseAiProcessor`, `Database/Scripts/AudioCaseTaking_CreateTables.sql`
- **Database:** `AudioCaseSession`, event/AI/consent/match/doctor-action logs.
- **AI:** `whisper-1` + `gpt-4o` extraction JSON.
- **Status:** Still implemented as `MatchRubricsV1Async`. Used as one **parallel channel** on the fast path, or as the only path if `RollbackToV1Only`.

### Phase — V2 Rubric Intelligence

- **Problem:** Keyword-only matching missed synonyms/metaphors; no explainability.
- **Implementation:** `RubricIntelligenceOrchestrator` + concept extraction, alias, embedding hybrid, doctor feedback learning, clinical validation V2.1.
- **Config:** `EnableV2: true` (still true; required for fast path because `IsV2Active` must be true).
- **Status:** LEGACY for discovery when fast path is on. Settings/approval flags still apply.

### Phase — V3 Concept Graph (M1–M4)

- **Problem:** Need patient-meaning → clinical → homeopathic concept graph before search.
- **Implementation:** `ConceptGraphOrchestrator` + GPT models M1–M4 in `V3/Engines`.
- **Config:** `EnableV3ConceptGraph: true` (still true, but skipped on fast path).
- **Status:** LEGACY / rollback path.

### Phase — V3.5 recall + fast V3 internals

- **Problem:** Missed multi-symptom coverage; sequential GPT too slow.
- **Implementation:** M0 / M1b / M4b in `ConceptGraphV35Engines.cs`; `EnableV35RecallEngine`, `EnableV35FastPipeline`.
- **Status:** LEGACY / rollback path.

### Phase — V4 / V5 enterprise discovery

- **Problem:** Merge multiple sources; per-concept slots; hybrid completion.
- **Implementation:** `EnableEnterpriseRubricDiscoveryEngine`, `EnableHybridCompletionEngine`, knowledge graph flags.
- **Status:** LEGACY / rollback path. Flags remain `true` in appsettings but are not executed on fast-f.

### Phase — V6 SQL-authoritative reasoning

- **Problem:** Stop AI from inventing rubrics; SQL as authority.
- **Implementation:** `Services/AudioCaseIntelligence/V6/*`
- **Config:** `EnableV6ClinicalReasoningEngine: false`
- **Status:** Implemented but **disabled**.

### Phase — V7 Repertory Intelligence

- **Problem:** Modular vocabulary / search / ranking / completion with latency budget.
- **Implementation:** `Services/AudioCaseIntelligence/RepertoryIntelligence/*`
- **Config:** `EnableV7RepertoryIntelligenceEngine: true` (still true, skipped on fast path).
- **Measured:** Existing doc `AI_RUBRIC_ENGINE_VS_FAST_PIPELINE.md` (2026-08-13): legacy case engine `v7.0`, **562.9 s** total, discovery **314.6 s**.
- **Status:** LEGACY / rollback path. Accuracy table still has a tiny n=4 acceptance sample.

### Phase — ECI v8

- **Problem:** New enterprise clinical intelligence pipeline.
- **Implementation:** `Services/AudioCaseIntelligence/ECI/V8/*`
- **Config:** `EnableEciV8Engine` default **false** (not set true in appsettings).
- **Status:** Implemented, **not active**.

### Phase — Stage C Fast Clinical Retrieval (`fast-c`)

- **Problem:** 9–20 minute cases; V7+Enterprise+EnsureAll dominated post-Whisper time.
- **Implementation:** `FastClinicalRetrievalOrchestrator` — skip V3/V7/Enterprise; parallel V1 + Keyword FTS; no semantic-cache wait.
- **Measured (case_5, 2026-08-13):** total **151.7 s**; Whisper **139.2 s**; discovery **1.9 s**; LLM calls **1**; final **8** DB-backed rubrics.
- **Status:** Superseded by later fast-* stamps; same orchestrator.

### Phase — Stage D embedding on fast path (`fast-d` / `fast-e`)

- **Problem:** Fast-c had 0 embedding calls; recall risk.
- **Implementation:** Parallel embedding channel with `FastPipelineEmbeddingTimeoutSeconds` (8s); alias; catalog.
- **Telemetry:** `AI_RUBRIC_ENGINE_PERFORMANCE_BENCHMARK.md` lists FastClinicalRetrieval `fast-d` 3039 ms, `fast-e` 2673 ms (n=1 each).
- **Status:** Intermediate stamps.

### Phase — Stage F ranking + accuracy pack (`fast-f`) — CURRENT

- **Problem:** Need canonical score, MMR diversity, hallucination/gender/hierarchy gates, doctor-learning soft boost, multi-query blocks.
- **Implementation:** `FastClinicalRanking`, `FastClinicalEvidenceGate`, `FastClinicalSymptomBlockBuilder`, query embedding cache.
- **Config:** `FastPipelineEngineVersion: "fast-f"`, `FastPipelineMaxFinalRubrics: 12`, `FastPipelineMmrLambda: 0.80`, `FastPipelineMinCanonicalScore: 0.45`.
- **Accuracy:** `AI_RUBRIC_ENGINE_ACCURACY_BENCHMARK.md` — **no doctor feedback rows yet** for fast-* engines.
- **Status:** CURRENT.

---

## Timeline template (as requested)

```text
V1 hotspot search
↓ Problem: low synonym recall
↓ V2 hybrid + embeddings + approval
↓ Files: RubricIntelligenceOrchestrator, HybridRetrievalEngine
↓ DB: AudioCaseIntelligenceV2 scripts, RubricEmbeddings
↓ AI: extra GPT concept engines
↓ Performance: slower
↓ Status: LEGACY discovery / ACTIVE flags

V7 repertory intelligence
↓ Problem: accuracy + modular search
↓ Implementation: RepertoryIntelligence/*
↓ Performance impact: ~5 min discovery (measured 314 s)
↓ Status: LEGACY (skipped when fast path on)

Stage C–F fast pipeline
↓ Problem: 9–20 min wall clock
↓ Implementation: FastClinicalRetrievalOrchestrator
↓ DB: none required beyond existing FTS + embeddings
↓ AI: typically 1 GPT extraction (+ Whisper)
↓ Performance: discovery ~2–3 s; Whisper still ~2 min
↓ Accuracy: unmeasured at fleet scale
↓ Status: CURRENT (fast-f)
```

---

## Related existing docs (may lag source)

| File | Date / note | Discrepancy vs source |
| ---- | ----------- | --------------------- |
| `AI_RUBRIC_ENGINE_VS_FAST_PIPELINE.md` | 2026-08-13, engine `fast-c` | Current stamp is `fast-f` |
| `AI_RUBRIC_ENGINE_FAST_PIPELINE_ARCHITECTURE.md` | describes `fast-f` | Aligns with current orchestrator |
| `AI_RUBRIC_ENGINE_SYSTEM_DOCUMENTATION.md` (repo root) | older full-engine writeup | Do not treat as current hot path |
| `AI_RUBRIC_ENGINE_ACCURACY_BENCHMARK.md` | tiny N; no fast-* feedback | Still accurate as “unproven” |

---

## Abandoned / duplicate implementations

| Item | Location | Still referenced? | Safe to delete? |
| ---- | -------- | ----------------- | --------------- |
| V6 engine | `AudioCaseIntelligence/V6` | DI registered; flag off | No — rollback / tests |
| ECI v8 | `AudioCaseIntelligence/ECI/V8` | DI registered; flag off | No |
| V3/V7 orchestrators | `V3/Orchestration`, `RepertoryIntelligence` | Rollback path | No |
| Legacy `RubricEmbeddings` table | `005_Create_RubricEmbeddings.sql` | `KeepLegacyRubricEmbeddingsActive: true` | No |
| `NIGA_Latest_Code_API` (`NIGA.Centrum` netcoreapp2.2) | sibling repo | Classic SubSection FTS/CRUD only; no AI | Keep as admin API | N/A |

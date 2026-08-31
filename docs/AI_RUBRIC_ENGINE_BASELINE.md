# AI Rubric Engine — Baseline (Stage A) — MEASURED

**Date:** 2026-08-13  
**Source workbooks:** `Result for the Ai case_4.xlsx`, `Result for the Ai case_5.xlsx`  
**Legacy session:** `a245ba0f-9beb-4524-b75f-a2c8a5ae0c0d` (`v7.0`, 562.9 s)  
**Fast-C session:** `c680488d-32b5-4884-aa13-61a2627c6ea5` (`fast-c`, 151.7 s)  
**Comparison:** `docs/AI_RUBRIC_ENGINE_VS_FAST_PIPELINE.md`  
**EngineVersion tag (telemetry):** `base-a`

---

## 1. Verdict

### Legacy (a245…)

| Metric | Measured | Target | Gap |
|---|---|---|---|
| End-to-end total | **562.9 s (~9.4 min)** | P50 ≤ 120 s | **~4.7× over** |
| MatchRubrics | **314.6 s** | ≤ ~60 s | Fail |
| Cache wait | **120 s Timeout** | no block | Fail |

### Fast-C (c680…, flag true) — Stage C verified

| Metric | Measured | Target | Gap |
|---|---|---|---|
| End-to-end total | **151.7 s (~2.5 min)** | P50 ≤ 120 s | Whisper ~139 s dominates |
| MatchRubrics / FastClinical | **1.9 s / 1.5 s** | ≤ ~60 s | **PASS** |
| Cache wait | **0 s Skipped** | no block | **PASS** |
| LLM / Emb | **1 / 0** | 1 extract | **PASS** |
| Final | **8 DB-backed / 0 AI-only** | quality over pad | **PASS** |

User ~10 min complaint matches legacy; fast path fixes rubric wait to **~12.5 s after Whisper**.

---

## 2. Session timeline (a245ba0f… legacy)
| Stage | Latency | Notes |
|---|---|---|
| WorkerDequeued | 0 ms | queueDepth=0; cacheReady=False |
| SemanticCacheWait | **120,015 ms** | **Timeout**; proceedingDegraded=true |
| Whisper (Translation whisper-1) | **116,451 ms** | Audio-bound |
| GptExtraction (gpt-4o) | 11,645 ms | 3245 prompt / 1478 completion tokens |
| DualLanguageWhisper | 3 ms | Built (no extra Whisper cost) |
| ConceptExtraction_PatientMeaning | 6,995 ms | 16 meanings |
| ConceptExtraction_MultiConcept | 7,111 ms | 10 homeopathic concepts |
| EnterpriseRubricDiscovery | **89,674 ms** | **100 candidates** |
| V7RepertoryIntelligence | **167,840 ms** | **4 candidates** after ~2.8 min |
| EnsureAllConceptsDiscovered | 17,499 ms | 34 candidates |
| ClinicalValidation | 118 ms | 34 → 16 accepted; ~18 rejected |
| ConceptGraphAnalyze (wall) | **310,216 ms** | rejected=23; engine=v7.0; out=12 |
| PerConceptKeywordDiscovery | **417 ms** | 10 concepts; 9 withHits; **92 raw → 27 kept** |
| FinalizeRubricsForResponse | 3,875 ms | final=20 |
| MatchRubricsWithIntelligence | **314,584 ms** | dbBacked=20; aiOnly=0 |
| Finalization | 39 ms | |
| ProcessSessionTotal | **442,845 ms** | |
| PipelineBaselineSummary | **562,918 ms** | llm=6; emb=2; sql≈10; cacheMiss=1 |

**Additive non-overlapping critical path (approx):**

`120s cache + 116s Whisper + 12s extract + 7s meaning + 7s multi + 90s Enterprise + 168s V7 + 17s EnsureAll + 0.4s Keyword + 4s finalize ≈ **~9.0 min**`  
(matches measured total).

---

## 3. OpenAI call log (this session)

| ServiceType | Model | Calls | SumLatencyMs | Tokens |
|---|---|---|---|---|
| Translation | whisper-1 | 1 | 116,325 | — |
| ChatCompletion | gpt-4o | 1 | 11,607 | 3245 / 1478 |

Telemetry `llmCallCount=6` includes additional Intelligence GPT stages (Meaning, MultiConcept, and others) that are **not** all mirrored in `AudioCaseAiRequestLog` the same way — treat **6 sequential LLM-ish stages** as the real cost model.

---

## 4. Historical wall-clock sample (xlsx R34–R53, n=20)

Completed sessions (EnteredDate → CompletedAtUtc):

| Stat | Seconds | Minutes |
|---|---|---|
| Min | 229 | ~3.8 |
| Median (P50) | **772.5** | **~12.9** |
| Mean | 807 | ~13.5 |
| P90 (approx) | **1212** | **~20.2** |
| Max | 1360 | ~22.7 |

Latest row = this gold session **563 s (~9.4 min)**.  
Engines in sample: mostly `v7.0`; older `v6.0` / `v5.2` / `v4.0` also present.

**Targets:** P50 ≤ 120 s, P90 ≤ 180 s — **not met** on current production path.

---

## 5. Candidate funnel (this session)

| Stage | Candidates in/out |
|---|---|
| Enterprise discovery | **100** |
| V7 discovery | **4** (very low recall for cost) |
| EnsureAllConcepts | **34** |
| ClinicalValidation | 34 → **16** accepted (~18 rejected) |
| ConceptGraph final tiers | **12** |
| Keyword FTS supplement | **92 raw / 27 kept** in **0.4 s** |
| Final suggested | **20** DB-backed |

Keyword path alone found more usable hits in **&lt;0.5 s** than V7 produced in **~168 s**.

---

## 6. Concept quality signal (same session API)

User sample concepts for this session include clinically shaky normalizations, e.g.:

- Raw: “I feel afraid for the feet.” → Homeopathic: **“Fear Before Convulsion”** (unsupported organ/disease leap)

Accuracy work remains mandatory; Stage B focuses on **engine overlap / latency**, not full clinical rewrite.

---

## 7. Slowest stages (ranked)

1. **V7RepertoryIntelligence — 167.8 s**  
2. **SemanticCacheWait timeout — 120.0 s**  
3. **Whisper — 116.5 s**  
4. **EnterpriseRubricDiscovery — 89.7 s**  
5. **EnsureAllConceptsDiscovered — 17.5 s**  
6. GPT Meaning + MultiConcept — ~14 s combined  
7. GptExtraction — 11.6 s  

Validation and Keyword are **not** the bottleneck.

---

## 8. Stage A instrumentation status

- [x] Telemetry produced `PipelineBaselineSummary` for this session  
- [x] Numbers filled from xlsx (no PHI copied)  
- [x] Hotfix `EngineVersion` length addressed (`base-a` + script 728)  
- [x] Stage B overlap report: `docs/AI_RUBRIC_ENGINE_ENGINE_OVERLAP.md`

---

## 9. Next

**Stage C done.** Enable with `EnableFastClinicalRetrievalPipeline: true` (see `AI_RUBRIC_ENGINE_FAST_PIPELINE_ARCHITECTURE.md`).  
Legacy path remains when flag is `false`.

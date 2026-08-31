# AI Rubric Engine — Legacy vs Fast Pipeline (measured)

**Date:** 2026-08-13  
**Source:** `NigaHomeopathy-UI/Result for the Ai case_5.xlsx` (all non-empty rows reviewed)  
**Flag:** `EnableFastClinicalRetrievalPipeline: true`

| | Legacy (case_4) | Fast-C (case_5) | Improvement |
|---|---|---|---|
| SessionId | `a245ba0f-9beb-4524-b75f-a2c8a5ae0c0d` | `c680488d-32b5-4884-aa13-61a2627c6ea5` | |
| Engine | `v7.0` | **`fast-c`** | Fast path active |
| **Total wall clock** | **562.9 s (~9.4 min)** | **151.7 s (~2.5 min)** | **−73%** |
| SemanticCacheWait | 120.0 s (Timeout) | **0 s (Skipped)** | **−120 s** |
| Whisper | 116.5 s | 139.2 s | Audio variance (not pipeline) |
| GptExtraction | 11.6 s | 10.5 s | Similar |
| **MatchRubrics / discovery** | **314.6 s** | **1.9 s** | **−99.4%** |
| FastClinicalRetrieval | — | **1.5 s** (v1=20, kw=24, concepts=8→12) | New |
| V7 | 167.8 s | **not run** | Removed |
| Enterprise | 89.7 s | **not run** | Removed |
| EnsureAllConcepts | 17.5 s | **not run** | Removed |
| LLM calls (telemetry) | 6 | **1** | −5 |
| Embedding calls | 2 | **0** | −2 |
| Final rubrics | 20 DB-backed | **8 DB-backed** | Fewer; no AI-only |
| Post-Whisper work | ~326 s | **~12.5 s** | **~26× faster** |

---

## Stage timeline (fast-c session)

| Stage | Ms | Notes |
|---|---|---|
| WorkerDequeued | 0 | cacheReady=False |
| SemanticCacheWait | 0 | Skipped; fastPipeline=True |
| Whisper | 139,161 | Dominates total |
| GptExtraction | 10,475 | Only GPT call |
| DualLanguageWhisper | 3 | |
| FastClinicalRetrieval | 1,521 | parallel V1+Keyword |
| MatchRubricsWithIntelligence | 1,880 | includes finalize gates |
| Finalization | 15 | |
| ProcessSessionTotal | 151,617 | |
| PipelineBaselineSummary | 151,666 | llm=1; emb=0; final=8 |

OpenAI log: Whisper translation 1×; gpt-4o ChatCompletion 1× (3031/1499 tokens).

---

## Verdict

| Goal | Target | Fast-C result |
|---|---|---|
| P50 total ≤ 120 s | Hard | **Not yet** (Whisper ~139 s alone) |
| Rubric intelligence ≪ 5–10 min | Soft | **~12.5 s post-Whisper — PASS** |
| DB-backed finals | ≥95% | **8/8 (100%)** |
| No V7/Enterprise on fast path | Required | **PASS** |
| Prefer quality over padding to 12 | Required | **8 rubrics — PASS** |

**Conclusion:** Stage C meets the **rubric-intelligence** speed goal. Remaining total latency is almost entirely **Whisper**. Next: Stage D (embedding/alias parallel) + Stage E (evidence prompt — started with fit≠convulsion fix) + UX that shows “Transcribing…” vs “Matching rubrics…”.

---

## Measurement methodology (Pack 3)

Do **not** invent Precision@10 / doctor acceptance.

1. Run `docs/sql/730_Golden_Set_Benchmark_Queries.sql`.  
2. Fill `AI_RUBRIC_GOLDEN_SET.md` with SessionIds only (no PHI).  
3. Paste aggregate acceptance into `AI_RUBRIC_ENGINE_ACCURACY_BENCHMARK.md`.  
4. Paste P50/P90 into `AI_RUBRIC_ENGINE_PERFORMANCE_BENCHMARK.md`.  
5. Use `RubricBenchmarkMetrics.ComputeAtK` for per-session Precision@K.

Until N≥50 golden sessions, accuracy targets remain **unproven**.

---

## Accuracy note (case_5 concepts)

API showed `searchTerms` including `"convulsion"` for phrases that only said `"fit"`.  

**Patch applied (same deploy cycle):**

1. Extraction prompt: do not auto-add epilepsy/convulsion unless spoken.  
2. `ConceptGraphConceptMapper.FromSymptoms` strips unsupported disease-inference terms unless present in the patient phrase.

Redeploy and reanalyze to confirm concepts no longer inject `convulsion` from `fit` alone.

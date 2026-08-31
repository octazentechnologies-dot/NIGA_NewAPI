# Audio Case Performance and Accuracy

**Do not mix this with the current-implementation sections of the complete doc.**  
Recommendations are at the end.

---

## Measured timings (existing repo evidence)

Source: `docs/AI_RUBRIC_ENGINE_VS_FAST_PIPELINE.md` (2026-08-13) and `docs/AI_RUBRIC_ENGINE_PERFORMANCE_BENCHMARK.md`.

These are **historical session measurements**, not live profiling from this audit.

| Stage | Legacy V7 case | Fast-c case_5 | Notes |
| ----- | -------------- | ------------- | ----- |
| SemanticCacheWait | 120.0 s (timeout) | 0 s | Fast path skips wait |
| Whisper | 116.5 s | 139.2 s | Dominates wall clock |
| GptExtraction | 11.6 s | 10.5 s | 1 GPT call |
| DualLanguageWhisper | — | 3 ms | English session; no extra Whisper |
| Match / discovery | 314.6 s | 1.9 s | V7+Enterprise removed |
| FastClinicalRetrieval | — | 1.5 s | v1=20, kw=24, concepts 8→12 |
| Total | **562.9 s (~9.4 min)** | **151.7 s (~2.5 min)** | |

SQL 730-C averages (tiny N≈4, mixed engines):

| Stage | Avg | Min | Max |
| ----- | --- | --- | --- |
| Whisper | 115 s | 97 s | 139 s |
| GptExtraction | 11 s | 10 s | 12 s |
| FastClinicalRetrieval | 1.5–3.0 s | 1.5 s | 3.0 s |
| ProcessSessionTotal | 207 s | 111 s | 443 s |
| PipelineBaselineSummary | 237 s | 111 s | **563 s** |

CONFIRMED from code (not from a new run):

- Frontend auto-poll budget ≈ **8 minutes** (`MAX_POLL_ATTEMPTS = 192` × 2500 ms).
- Backend job timeout **20 minutes** (`AudioCaseTaking:MaxProcessingMinutes`).
- OpenAI HttpClient timeout **10 minutes**.
- Desired product target **2–3 minutes** is **not guaranteed**. Whisper alone was ~2.3 minutes on the measured fast-c case.

UNCERTAIN: current production P50/P90 for `fast-f`. Existing 730-D NTILE query returned NULL (N too small). `fast-f` was “not in export yet” in the performance doc.

---

## Current-path stage analysis

| Stage | Sequential / parallel | API calls | DB | Bottleneck? |
| ----- | --------------------- | --------- | -- | ----------- |
| Upload + disk write | Sequential | 1 HTTP | 1 insert session + consent + events | Small |
| Queue wait | Sequential, single worker | 0 | 0 | **Yes if jobs pile up** (one reader) |
| Semantic cache wait | Skipped on fast path (`FastPipelineSemanticCacheMaxWaitSeconds: 0`) | 0 | 0 | Was 120 s; now 0 |
| Whisper `audio/translations` | Sequential | 1 OpenAI | 1 AI log | **Primary bottleneck** |
| Dual-language Whisper | Conditional | 0 or 1 | log | Extra Whisper if non-English + sensation-bearing |
| GPT extraction | Sequential | 1 chat/completions, max_tokens 8192 | persist JSON | Secondary (~10 s) |
| Fast retrieval | **Parallel** V1 + Keyword + Alias + Embedding + Catalog | 0–1 embedding batch; V1 may add GPT rubric suggestion | many SubSection queries | Usually 2–3 s |
| Clinical validation | Sequential in-process | 0 | 0 | Small |
| Finalize + SaveChanges | Sequential | 0 | match logs, concepts, session | Small |

---

## Duplicate / wasted work on the current path

CONFIRMED:

1. **V1 AI rubric suggestion can still fire** inside `MatchRubricsV1Async` when `EnableAiSuggestedRubrics: true` and fewer than 20 DB hits. Fast path then **drops** `SubSectionId <= 0` before finalize, so those GPT names are often discarded.
2. **Dual-language context is built** (`DualLanguageForSensationSegments: true`) even though `MatchRubricsFastClinicalAsync` **does not receive** `dualLanguage`. Extra Whisper only if language is not English.
3. **V1 and Keyword both hit** `SearchSubSectionsByHotspotAsync` (FTS/Contains) for overlapping terms.
4. **Enterprise validation still runs** after fast retrieval (`EnableEnterpriseClinicalValidation: true`) — extra in-process scoring, not extra GPT.

NOT IMPLEMENTED on fast path: V3 M0–M5 GPT chain, V7 extraction GPT, EnsureAllConcepts.

---

## Caching (actual)

| Cache | Key | TTL | Used on audio path? |
| ----- | --- | --- | ------------------- |
| `RubricEmbeddingMemoryCache` | process memory | until refresh | Yes (embedding channel) |
| `FastClinicalQueryEmbeddingCache` | query text + model | process lifetime | Yes if `FastPipelineEnableQueryEmbeddingCache` |
| `AiEnterpriseRubricEmbeddingMemoryCache` / concept cache | warmup | 30 min config | Fast path proceeds without waiting |
| Redis | — | — | **Not found** |
| Transcript cache | — | — | **Not found** |
| GPT result cache | — | — | **Not found** |

---

## Parallelization (actual vs opportunity)

**Actual (fast-f):** V1, keyword, alias, embedding, catalog run via `Task.WhenAll`.

**Still sequential:** Whisper then GPT extraction then retrieval. Symptoms are not each sent to GPT; one extraction call covers the transcript.

**Opportunity (recommendation only):** stream Whisper; skip V1 AI suggestion on fast path; skip dual-language when fast path is on; consider a second worker (queue is single-reader).

---

## Accuracy — measured vs targets

Targets from the audit brief are **goals**, not claims.

| Target | Current measured value | Gap | How to measure | Potential solution |
| ------ | ---------------------- | --- | -------------- | ------------------ |
| Overall rubric accuracy 90–100% | **Not automatically measured** | Unknown | Golden set + doctor accept | Keep DB-backed-only finals |
| Doctor acceptance ~95% | v2 **0.50** (n=14); v7 **1.00** (n=4); fast-* **no rows** | Unproven | `AudioCaseRubricFeedback` + SQL 730 | Collect fast-f feedback |
| Primary Top-5 ~95% | TBD | Unknown | `RubricBenchmarkMetrics.ComputeAtK` | Ranking/MMR tuning |
| False positive &lt;5% | TBD | Unknown | Rejects / suggested | Hallucination + gender + hitchhiker gates already exist |
| 10–12 rubrics when clinically available | Fast-c returned **8**; MMR stops on score cliff after 5 | May return fewer than 12 **by design** | Count `SuggestedRubricsJson` | Do not pad; improve recall |

CONFIRMED: `SelectWithMmr` **never fabricates** to fill `targetCount`. If 7 pass the score floor, 7 are returned.

NOT IMPLEMENTED: automated Precision/Recall dashboard that is populated in production without running SQL 730.

---

## Recommended Performance Improvements

*(Separate from current behavior.)*

### P0 — Whisper dominates wall clock

- **Problem:** 97–139 s of a ~2.5 min case is transcription.
- **Current:** Full-file `audio/translations`, HttpClient 10 min timeout.
- **Proposed:** Shorter recordings; compress/convert client-side; consider chunked transcription; show “Transcribing…” (UI already has stage labels).
- **Expected:** Largest cut toward 2–3 min total.
- **Risk:** Accuracy of translation on long mixed-language audio.
- **Complexity:** Medium–high.

### P0 — Disable wasted GPT on V1 during fast path

- **Problem:** `SuggestAiRubricsAsync` can run inside V1; fast path drops non-DB ids.
- **Proposed:** Skip `EnableAiSuggestedRubrics` when fast pipeline is on.
- **Expected:** Avoid 5–20 s extra GPT + hallucination risk.
- **Risk:** Low if DB recall is adequate.
- **Complexity:** Low.

### P1 — Skip dual-language work on fast path

- **Problem:** Dual-language is built then ignored.
- **Proposed:** Gate `TryBuildDualLanguageContextAsync` on `!EnableFastClinicalRetrievalPipeline`.
- **Expected:** Avoid extra Whisper on non-English cases.
- **Risk:** Sensation nuance for Marathi/Hindi on fast path.
- **Complexity:** Low.

### P1 — Multi-worker queue

- **Problem:** Unbounded channel, `SingleReader = true`.
- **Proposed:** Bounded parallel workers with session gate (gate already exists).
- **Expected:** Removes head-of-line blocking.
- **Risk:** OpenAI rate limits; DB load.
- **Complexity:** Medium.

### P2 — Prompt / JSON size

- **Problem:** Extraction `max_tokens = 8192`; conversation array can be large.
- **Proposed:** Cap conversation turns; keep “do not echo transcript” (already in prompt).
- **Expected:** Lower truncation risk and latency.
- **Risk:** Missing exchanges.
- **Complexity:** Low.

### P3 — Persistent job queue

- **Problem:** In-memory queue dies on restart (mitigated by requeue of Uploaded).
- **Proposed:** SQL/Hangfire jobs.
- **Expected:** Survive recycles mid-Whisper.
- **Risk:** Duplicate processing (session gate helps).
- **Complexity:** High.

---

## Recommended Accuracy Improvements

### P0 — Instrument fast-f acceptance

- Run SQL `730_Golden_Set_Benchmark_Queries.sql` after doctors Accept/Reject.
- Fill `AI_RUBRIC_GOLDEN_SET.md` with SessionIds only (no PHI).

### P1 — Recall without padding

- Fast-c had 8 rubrics vs 10–12 goal. Tune multi-query / catalog / embedding timeout rather than lowering `FastPipelineMinCanonicalScore` blindly.
- Risk: more false positives.

### P1 — Extraction completeness

- Extraction prompt already lists must-extract items (salt, sleep-talk, thirst, etc.). Misses are likely Whisper or prompt coverage, not ranking.

### P2 — Keep hallucination hard gate

- `FastPipelineMinEvidenceScore = 0.15` + `SubSectionId > 0` is the main anti-hallucination design. Do not re-enable unmapped AI names on the default UI path.

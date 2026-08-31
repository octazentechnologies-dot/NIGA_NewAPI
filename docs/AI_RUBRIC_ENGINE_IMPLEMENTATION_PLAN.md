# AI Rubric Engine — Implementation Plan (Phase 0 Audit)

**Date:** 2026-08-13  
**Repo of record:** `NigaHomeopathy-API` (not `NIGA_Latest_Code_API`)  
**Status:** Audit complete. **No functional pipeline rewrite yet.**  
**Rule:** Accuracy > quantity; DB-backed > AI-invented; patient evidence > doctor questions; one fast production path.

---

## 1. Current production path (code, not docs)

```
Upload → AudioCaseTakingQueue (SingleReader)
  → BackgroundService (SemanticCache wait ≤2 min, then proceed degraded)
  → ProcessSessionAsync
      → Whisper (TranslateAudioToEnglish / Transcribe)
      → ExtractCaseDataAsync (1 GPT call)
      → optional DualLanguageForSensationSegments (+1 Whisper)
      → ConceptGraphOrchestrator.AnalyzeAsync
          → Decomposition ∥ Meaning (V35 fast)
          → MultiSymptom / Category / Metaphor / MultiConcept (mostly sequential GPT)
          → EnterpriseRubricDiscovery (+ RubricCandidateEngine embeddings)
          → V7 RepertoryIntelligence (primary; Hybrid else-if skipped when V7 on)
          → EnsureAllConceptsDiscoveredAsync (per-concept Hybrid/Hierarchical)
          → Enterprise validation / quality / RepertoryMapping
      → StrictConceptGatedDiscovery → ConceptGraphOnly early return
          → PerConceptKeywordDiscovery supplement (FTS CONTAINS → Contains fallback)
      → Finalize: reconcile → RubricCandidateQualityGate → modality group → SaveDisplayedRubrics
  → Status = Completed
```

**Key files:**  
`AudioCaseTakingService.cs`, `AudioCaseTakingBackgroundService.cs`, `ConceptGraphOrchestrator.cs`, `RubricResultMerger.cs`, `ConceptKeywordDiscoveryEngine.cs`, `SubSectionRepository.SearchSubSectionsByHotspotAsync`, `EnterpriseClinicalValidationPipeline` / `RubricCandidateQualityGate`.

**UI:** poll every 2.5s (~8 min cap) — `thunk.js`, `AudioCaseProcessingStatus.js`, Section A/B in `AudioCaseRubricSuggestions.js`.

**DB:** FTS on `SubSectionName` verified via scripts `725`/`726` on `HomeoCentrum_Production` (~185k rows).

---

## 2. What already exists (do not rebuild blindly)

| Asset | Status |
|---|---|
| FTS `CONTAINS` on SubSectionName | Ready; API wired with Contains fallback |
| LatencyGap1/2/3 logs | Partial stage telemetry |
| `RubricCandidateQualityGate` | Word-boundary, hitchhiker, citation lock, A/B dedupe |
| `ConceptIdentity` + strict evidence chain | Partial Phase 3–6 correctness |
| Semantic embedding memory cache | Exists; can still block startup |
| Golden tests | `NearZeroDiscoveryGoldCaseTests`, `CorrectnessBugsAtoDTests` |
| `FastClinicalRetrievalPipeline` flag | **Does not exist** |

---

## 3. Measured / structural bottlenecks (pre-baseline)

| Rank | Bottleneck | Why |
|---|---|---|
| 1 | **~8–12 sequential GPT round-trips** | Meaning → category → metaphor → multi-concept → optional V7 GPT extraction |
| 2 | **SemanticCache wait** | Up to 2 minutes before any work |
| 3 | **Whisper (+ dual-language)** | Audio-bound; separate from rubric intelligence budget |
| 4 | **V7 + EnsureAllConceptsDiscovered** | Symptom search then full per-concept pass |
| 5 | **Overlapping engines** | Enterprise + V7 + Keyword + Hybrid supplement all active via flags |

**Config reality:** almost every engine flag is `true` except V6. Production path is **not** a single fast path today.

---

## 4. Target production path (one clear fast path)

```
Audio → Whisper (1)
     → ONE structured clinical extraction (1 GPT)
     → Symptom blocks (patient-evidence only)
     → Parallel DB retrieval: Exact | FTS | Alias | Embedding(batch) | Ontology
     → Candidate union (high recall)
     → Cheap gates → Evidence → Domain/Gender/Hierarchy
     → Canonical score (0–1)
     → Dedup + diversity (MMR)
     → Top ≤12 DB-backed rubrics (never fabricate)
     → Doctor review
```

**LLM budget target (normal case):** 1 Whisper + 1 extraction + ≤1 embedding batch.  
**No GPT for scoring 100 candidates. No invented final rubric names.**

---

## 5. Staged delivery (must follow order)

### STAGE A — Baseline only (no behavior change)
1. Unified stage telemetry DTO persisted to `AudioCaseIntelligenceLog` / metrics fields.  
2. Capture: Whisper, Extraction, each GPT stage, Embedding batch, SQL/FTS, Merge, Validation, Finalize, Total.  
3. Write `docs/AI_RUBRIC_ENGINE_BASELINE.md` from **3+ real gold sessions** (numbers required).  
4. **Stop if P50 rubric-intelligence portion cannot be measured.**

### STAGE B — Instrumentation complete + engine overlap report
1. `docs/AI_RUBRIC_ENGINE_ENGINE_OVERLAP.md` — candidates unique/overlap/latency per engine.  
2. Decide minimum engine set for production.

### STAGE C — Feature flag + skeleton fast path
1. Add `RubricIntelligence:EnableFastClinicalRetrievalPipeline` (default **false**).  
2. Wire entry in `MatchRubricsWithIntelligenceAsync`: if flag → new orchestrator; else existing path.  
3. Rollback = flip flag.

### STAGE D — Fast retrieval core
1. Process-level immutable rubric catalog snapshot (SubSectionId, name, parents, aliases, embedding version).  
2. Parallel Exact/FTS/Alias/Embedding/Ontology with bounded concurrency.  
3. Batch query embeddings; never rebuild index per request.  
4. Cache wait: never block analysis > few seconds; proceed degraded.

### STAGE E — Evidence-first extraction
1. Rewrite extraction prompt: doctor≠patient; negation; no organ/disease invention.  
2. Symptom blocks with patientStatement + normalizedMeaning + confidence.  
3. Speaker/negation unit tests (Phase 51 list).

### STAGE F — Canonical scoring + gates
1. One FinalScore (0–1): Evidence 0.30, Clinical 0.25, Semantic 0.20, Exact/Alias 0.10, Hierarchy 0.05, Modality 0.05, Learning 0.05 — **tune via benchmark**.  
2. Hard gates: SubSectionId>0, active row, patient evidence, gender, location, hierarchy specificity ≤ evidence.  
3. Diversity (MMR) top 10–12 with evidence-based stop.  
4. Section B AI concepts only when no DB match.

### STAGE G — Collapse redundant engines on fast path
1. Production fast path does **not** run V1∪V2∪V3∪Enterprise∪V7∪Keyword serially.  
2. Keep old path for A/B and rollback.  
3. Port proven pieces: FTS hotspot, QualityGate, Causation softening, modality grouping.

### STAGE H — Benchmark + enable
1. Golden set (≥50 cases if available; start with NearZero + CorrectnessBugs + vibration/hands case).  
2. Compare old vs new → `docs/AI_RUBRIC_ENGINE_VS_FAST_PIPELINE.md`.  
3. Enable flag only if **both** accuracy and latency improve.

---

## 6. Accuracy non-negotiables (hard gates)

- Doctor question ≠ patient symptom; patient “No” → reject that symptom.  
- No fit→epilepsy / chest→heart / vibration→convulsion without evidence.  
- Gender mismatch reject; unknown gender → no gender-specific guess.  
- Final doctor-facing list: `SubSectionId > 0` + active `SubSectionMaster`.  
- Prefer 7 strong over 12 fabricated.  
- Learning boost never overrides hard evidence/gender/location gates.

---

## 7. Performance targets

| Scope | Target |
|---|---|
| Total normal case | P50 ≤ 120s, P90 ≤ 180s |
| Rubric intelligence (post-Whisper) | Must not consume 10+ minutes |
| Retrieval | ≤ 5s after cache warm |
| Scoring + validation + finalize | ≤ 10s combined |

Whisper time scales with audio length — report separately.

---

## 8. Risks & missing information (stop points)

| Risk | Mitigation |
|---|---|
| No measured baseline yet | Stage A first — do not tune weights blindly |
| Golden set may lack 50+ de-identified cases | Start with existing gold + feedback-approved IDs; don’t invent PII |
| Embedding JSON + in-process cosine may be enough | Optimize cache first; ANN only if measured slow |
| DualLanguage second Whisper | Keep optional; default off on fast path unless needed |
| FTS on wrong DB | Always verify API connection string DB matches 726 |

**If unavailable, stop and ask:** (1) 3+ recent Completed SessionIds with timings, (2) whether patient gender is reliably on Patient record, (3) count of doctor Accepted/Rejected feedback rows usable for benchmark.

---

## 9. Immediate next action

**Stages A–F + Accuracy/Retrieval packs delivered for production.** Flag ON (`fast-e`):
speaker/negation → multi-query → parallel V1+FTS+Alias+Embedding+Catalog → accuracy gates →
canonical score → MMR → evidence contract.  
Deploy API+UI; run SQL 728 if needed; smoke session; rollback = flag false.  
See `AI_RUBRIC_ENGINE_PRODUCTION_DELIVERY.md`.


---

## 10. Acceptance (recap)

Complete only when: build+tests green; upload/reanalyze/feedback/repertorize work; DB-backed finals; evidence on every rubric; doctor≠patient; P50/P90 measured; precision/acceptance measured; old path behind flag for rollback; no production data deleted; no secrets logged.

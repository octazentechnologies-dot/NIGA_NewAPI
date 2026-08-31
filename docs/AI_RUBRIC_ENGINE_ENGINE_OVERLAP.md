# AI Rubric Engine — Engine Overlap Report (Stage B)

**Date:** 2026-08-13  
**Evidence:** `NigaHomeopathy-UI/Result for the Ai case_4.xlsx` + session `a245ba0f-9beb-4524-b75f-a2c8a5ae0c0d`  
**Baseline:** `docs/AI_RUBRIC_ENGINE_BASELINE.md`  
**Rule:** Prefer measured overlap over feature-flag folklore.

---

## 1. Engines active on this production run

| Engine / stage | Enabled in practice | Latency | Candidates produced | Notes |
|---|---|---|---|---|
| Semantic cache gate | Yes | **120.0 s** (timeout) | — | Cold miss; blocks user before any clinical work |
| Whisper translation | Yes | **116.5 s** | — | Audio-length bound (separate budget) |
| GPT case extraction | Yes | 11.6 s | symptoms/summary | Necessary once |
| PatientMeaning (GPT) | Yes | 7.0 s | 16 meanings | Overlaps extraction clinically |
| MultiConcept (GPT) | Yes | 7.1 s | 10 concepts | Overlaps meaning |
| **EnterpriseRubricDiscovery** | Yes | **89.7 s** | **100** | High recall, expensive |
| **V7RepertoryIntelligence** | Yes | **167.8 s** | **4** | Lowest recall / highest cost |
| **EnsureAllConceptsDiscovered** | Yes (post-V7) | **17.5 s** | **34** | Third retrieval pass over same concepts |
| ClinicalValidation | Yes | 0.1 s | 34 → 16 | Cheap; keep |
| **PerConceptKeywordDiscovery (FTS)** | Yes (ConceptGraphOnly supplement) | **0.4 s** | **92 → 27** | Best cost/recall ratio observed |
| Finalize / quality gate | Yes | 3.9 s | 20 final | Keep |

Total session: **562.9 s**. Rubric path alone ≈ **314.6 s**.

---

## 2. Overlap analysis

### 2.1 Retrieval overlap (same job, three expensive passes)

```
Concepts (10)
   │
   ├─► Enterprise discovery ──► 100 candidates  (~90 s)
   │
   ├─► V7 repertory ──────────►   4 candidates  (~168 s)  ← mostly redundant / low yield
   │
   ├─► EnsureAllConcepts ─────►  34 candidates  (~17.5 s) ← backfill because V7 missed concepts
   │
   └─► Keyword FTS ───────────►  92→27 hits     (~0.4 s) ← high recall AFTER graph path
```

**Finding:** Enterprise + V7 + EnsureAll are **serial and overlapping**. V7 did not remove the need for EnsureAll; Keyword still found additional hits afterward.

**Unique value of V7 on this case:** only **4** candidates for **168 s** — unacceptable ROI.

**Unique value of Keyword:** **27** kept candidates in **0.4 s** — should be a **first-class** retrieval channel, not a late supplement.

### 2.2 LLM overlap

| Call family | Purpose overlap |
|---|---|
| GptExtraction | Patient symptoms / summary |
| PatientMeaning | Re-interprets transcript into meanings |
| MultiConcept | Maps meanings → clinical/homeopathic concepts |
| (Other Intelligence GPT inside V7/category/metaphor as configured) | More sequential chat |

Measured telemetry: **6 LLM increments** vs target **1 extraction**.

Meaning + MultiConcept (~14 s) are cheaper than V7/Enterprise but still duplicate “understand the case” work already done in extraction.

### 2.3 Embedding overlap

| Signal | Count |
|---|---|
| Embedding HTTP calls | **2** |
| Cache | **Miss** (degraded path) |

Enterprise discovery likely did embedding work while cache was cold — explains part of the **90 s** Enterprise cost.

---

## 3. Acceptance / rejection funnel

| Gate | Count |
|---|---|
| Enterprise candidates | 100 |
| After V7 | 4 (then merged/chained) |
| After EnsureAll | 34 |
| Validation rejected (approx) | **18** (message) / ConceptGraph rejected **23** |
| Validation accepted | **16** |
| ConceptGraph displayed tiers | **12** |
| Keyword kept | **27** |
| Final doctor-facing | **20** DB-backed |

Validation is **not** slow; discovery is. High rejection after expensive discovery means we pay full retrieval cost for many doomed candidates — Stage C should apply **cheap gates earlier** (evidence/gender/location) before deep engines.

---

## 4. Minimum engine set for production fast path

### KEEP on fast path

1. **Whisper** ( unavoidable; report separately )  
2. **One structured GPT extraction** (symptom blocks + evidence)  
3. **Parallel DB retrieval:** Exact + **FTS/Keyword** + Alias + Ontology + **cached** Embedding  
4. **Cheap clinical gates** + evidence + hierarchy + gender  
5. **Canonical score + MMR diversity** → ≤12 DB rubrics  
6. **Doctor approval** unchanged  

### MOVE off production path (feature-flag / A/B only)

| Engine | Reason |
|---|---|
| **V7RepertoryIntelligence** | 168 s / 4 candidates on measured case |
| **EnterpriseRubricDiscovery as mandatory serial stage** | 90 s; overlaps Keyword+Embedding |
| **EnsureAllConceptsDiscovered after V7** | Exists because V7 under-covers; not needed if Keyword/FTS is primary |
| Multi sequential GPT meaning/category/metaphor/multiConcept chain | Fold into one extraction where possible |

### OPTIMIZE immediately (even before full fast pipeline)

| Change | Expected save on this case |
|---|---|
| Do not wait full **120 s** on cold semantic cache | **~120 s** |
| Skip V7 on fast path | **~168 s** |
| Skip or demote Enterprise serial pass | **~90 s** |
| Promote Keyword/FTS to primary retrieval | Already **0.4 s** for strong recall |
| Keep Whisper budget separate in UX (“transcribing…”) | Honest progress |

**Illustrative residual after those cuts (same case):**  
Whisper 116 s + Extract 12 s + Meaning/Multi 14 s (until folded) + FTS/Keyword+Ensure-style retrieval &lt;20 s + finalize &lt;5 s ≈ **~2.5–3.5 min** with Whisper, or **~1–1.5 min** post-Whisper if Meaning/Multi folded and cache non-blocking.

Still above 120 s **total** when Whisper is ~2 min — document Whisper separately; **rubric intelligence must leave the 5+ minute zone**.

---

## 5. Overlap matrix (qualitative)

|  | Enterprise | V7 | EnsureAll | Keyword/FTS | Embedding |
|---|---|---|---|---|---|
| Enterprise | — | High purpose overlap | High | Medium | High (emb inside) |
| V7 | High | — | High (V7 incomplete → EnsureAll) | Medium | Medium |
| EnsureAll | High | High | — | Medium | Medium |
| Keyword/FTS | Medium | Medium | Medium | — | Low (complementary) |
| Embedding | High | Medium | Medium | Low | — |

**Conclusion:** Running Enterprise **and** V7 **and** EnsureAll on every case is duplicate spend. Production should pick **one high-recall retrieval stack** (Keyword/FTS + Alias + cached Embedding), not three.

---

## 6. Accuracy note tied to this session

Concepts API for the same session shows over-inference risk (e.g. feet fear → “Fear Before Convulsion”).  
Stage B does **not** fix that; Stage C/E evidence gates must.  
Fast path must not equal “keep V7 because it feels smarter” — measured yield is poor.

---

## 7. Decision for Stage C

| Decision | Value |
|---|---|
| Feature flag | `RubricIntelligence:EnableFastClinicalRetrievalPipeline` default **false** |
| Fast path engines | Extraction + parallel Exact/FTS/Alias/Embedding + gates + MMR |
| Legacy path | Current V7+Enterprise+EnsureAll+Keyword (rollback) |
| Success criteria vs this baseline | Total ≪ 563 s; MatchRubrics ≪ 314 s; ≥ DB-backed rate; no worse gold acceptance |

---

## 8. Acceptance (Stage B)

- [x] Measured latency per engine from real session  
- [x] Candidate counts per engine  
- [x] Overlap identified (Enterprise ∩ V7 ∩ EnsureAll)  
- [x] Keyword proven as high-ROI channel  
- [x] Minimum production engine set proposed  
- [x] Historical P50/P90 wall-clock documented (~13 / ~20 min)

**Do not delete old engines yet.** Flag-gate the fast path first.

# Audio Case Rubric Engine

Production engine stamp: **`fast-f`**.  
Orchestrator: `Niga-Domain/Services/AudioCaseIntelligence/Orchestration/FastClinicalRetrievalOrchestrator.cs`.  
Entry: `AudioCaseTakingService.MatchRubricsWithIntelligenceAsync` → `MatchRubricsFastClinicalAsync` when `EnableFastClinicalRetrievalPipeline`.

---

## Actual stages (fast-f)

```text
Transcript (already English)
    ↓
GPT extraction → symptoms + summary + conversation
    ↓
Speaker / negation filter (FastClinicalEvidenceGate)
    ↓
Concepts from symptoms (ConceptGraphConceptMapper.FromSymptoms)
    ↓
Multi-query expand (FastClinicalSymptomBlockBuilder) if FastPipelineEnableMultiQueryBlocks
    ↓
PARALLEL:
    V1 MatchRubricsV1Async (hotspot FTS/Contains, ≤12 symptoms, pageSize 8)
    ConceptKeywordDiscoveryEngine (per-concept, concurrency 6, timeout 15s)
    RubricAliasEngine
    EmbeddingSearchEngine (topK 50, cosine ≥ 0.74, timeout 8s)
    FastClinicalRubricCatalog token lookup (max 40)
    ↓
Merge funnel cap FastPipelineCandidateFunnelSize = 80
    ↓
Keep SubSectionId > 0
    ↓
RubricCandidateQualityGate (substring collision, hitchhikers, citation lock, AI/DB dup)
    ↓
Gender gate
    ↓
Hierarchy specificity gate
    ↓
Hallucination hard gate (evidence overlap ≥ 0.15)
    ↓
CanonicalScore (FastClinicalRanking)
    ↓
Doctor learning soft boost (optional)
    ↓
MMR select ≤ 12, min canonical 0.45, score cliff after 5
    ↓
Evidence contract attach
    ↓
ClinicalValidationEngine (enterprise 8-step) if RequiresStrictValidation
    ↓
Take maxFinal 5–20 (config 12), SubSectionId > 0
    ↓
ApplyRubricMetadata (manual approval)
    ↓
AiSuggestedRubricReconciler + QualityGate + unified contract
```

Stages **not** on this path: V3 graph GPT, V7 search, ECI v8, knowledge-graph ranking.

---

## Database search (V1 + Keyword)

**Repository:** `SubSectionRepository.SearchSubSectionsByHotspotAsync`

1. Build FTS query from hotspot; `CONTAINS(SubSectionName, {ftsQuery})`.
   Audio Case (NigaHomeopathy): `"term*"` or `"a*" AND "b*"` (`725_FullText_SubSectionMaster_SubSectionName.sql`).
   Classic admin API (`NIGA_Latest_Code_API`): `CONTAINSTABLE(..., SearchNormalized)` with `"word*" OR ...` ordered by SQL `RANK` — **not** on the audio path.
2. On FTS failure → `ApplySubSectionByHotspotSearch` (Contains/LIKE scan).
3. Word-boundary filter via `WordBoundaryMatcher` (drops drop/dropsy-style collisions).
4. V1: `PageSize = 8`, take 12 symptoms; score:

```text
keywordScore = max(0.45, domainScore)   // domainScore < 0.35 → skip
semanticScore = AudioCaseAiProcessor.ComputeTextSimilarity(phrase, name)  // Jaccard-like, not embeddings
finalScore = EnableSemanticRubricMatch ? 0.6*keyword + 0.4*semantic : keyword
```

`EnableSemanticRubricMatch` is **true** — this is **string similarity**, not vector search.

V1 then may call GPT `SuggestAiRubricsAsync` to fill up to 20 combined (config `MaxAiSuggestedRubrics: 25` but V1 caps combined at 20). Fast path drops non-DB ids afterward.

Keyword engine: same hotspot search per concept search term, with domain scoring and traces in `AudioCaseIntelligenceLog`.

---

## Embedding search

```text
Feature: Rubric semantic search
File: Niga-Domain/Services/AudioCaseIntelligence/Engines/EmbeddingSearchEngine.cs
Function: SearchAsync
Database: RubricEmbeddings / memory cache (legacy + V4 infra)
Status: IMPLEMENTED (optional channel; timeout 8s; skip if cache empty)
```

| Parameter | Current value | File |
| --------- | ------------- | ---- |
| Model | `text-embedding-3-small` | `OpenAI:EmbeddingModel` |
| Dimensions | 1536 | `AiEmbeddingInfrastructure:DefaultDimensionCount` |
| Top K | 50 | `EmbeddingTopK` / `V7EmbeddingTopK` |
| Min cosine | 0.74 | `MinEmbeddingCosineForCandidate` |
| Max query texts | 12 | `MaxEmbeddingConceptsPerPass` |
| Similarity | cosine (`EmbeddingVectorMath`) | |
| Fallback | Jaccard over cache texts if embed fails | `FallbackJaccardSearch` |

Query text: built from concept (`BuildQueryText`). Results mapped to `MatchSource = "Embedding"`.

Refresh: `RubricEmbeddingIndexerBackgroundService` + V4 `AiIncrementalEmbeddingRefreshBackgroundService` / builder. Fast path **does not wait** for cache (`FastPipelineSemanticCacheMaxWaitSeconds: 0`).

---

## Ranking formula (ACTUAL — fast-f)

`FastClinicalRanking.ScoreWeights` defaults (hardcoded, not appsettings):

```text
CanonicalScore =
    0.30 * Evidence
  + 0.25 * ClinicalMatch
  + 0.20 * Semantic
  + 0.15 * ExactAlias
  + 0.10 * Keyword
```

- Evidence: token overlap of rubric name vs concept haystack (`ComputeEvidenceOverlap`).
- If evidence &lt; 0.15: evidence forced to 0; clinical ×0.4; semantic ×0.3.
- ClinicalMatch: existing confidence/matchScore scaled to 0–1.
- Semantic component boosted if `MatchSource` contains `"embed"`.
- ExactAlias boosted if source contains alias/exact/database.
- Keyword boosted if source contains keyword/fts/concept.

Display: `MatchScore = CanonicalScore * 100`.

**V2 HybridWeights** apply to the **legacy** `HybridRetrievalEngine` only (skipped on fast-f):

```text
hybridScore =
    Embedding * embeddingScore          // 0.40
  + Alias * aliasScore                  // 0.30
  + ClinicalMeaning * clinicalScore     // 0.20
  + KeywordLike * keywordScore          // 0.10
  + 0.15 * domainScore                  // hardcoded extra term
confidence = min(0.99, hybrid * 0.92 + 0.08)   // Calibrate
```

Domain reject: skip if `domainScore < 0.25` and embedding>0 and alias<0.4.

**ECI v8 weights** (ClinicalMatch 30, Evidence 20, …) apply only if ECI is enabled (it is not).

**ECI v8 weights** (ClinicalMatch 30, Evidence 20, …) apply only if ECI is enabled (it is not).

### MMR

```text
mmr = λ * relevance − (1−λ) * maxNameSimilarity(selected)
λ = FastPipelineMmrLambda = 0.80
minCanonicalScore = 0.45
scoreCliffRatio = 0.70 after ≥5 selected
targetCount = FastPipelineMaxFinalRubrics = 12 (clamped 5–20)
```

Never fabricates rubrics to fill 12.

### Doctor learning boost (after canonical)

If acceptance rate known: `score = 0.95*score + 0.05*rate`.  
Concept-rubric map: `+ cappedWeight * 0.05`.  
Cannot rescue below evidence/canonical floors.

---

## Top-K

| Layer | Count |
| ----- | ----- |
| V1 symptoms | 12 |
| V1 page size | 8 |
| V1 combined cap | 20 |
| Embedding topK | 50 then cosine filter |
| Catalog tokens | 40; results 40 |
| Funnel merge | 80 |
| Final | ≤12 |
| Min required | **None** — empty list is allowed (`Success` if rubrics or concepts &gt; 0) |

If requested 10–12 and found 7: **return 7**. CONFIRMED.

---

## Validation

| # | Check | Implemented on fast-f? |
| - | ----- | ---------------------- |
| 1 | DB existence | Yes — `SubSectionId > 0` hard filter |
| 2 | Exact/normalized match | Partial — FTS/Contains + word boundary; reconciler exact name |
| 3 | Semantic similarity | Yes if embedding channel succeeds |
| 4 | Clinical relevance | Enterprise pipeline + canonical evidence |
| 5 | Context (location/sensation/…) | Hierarchy specificity gate; not a full Kent analysis |
| 6 | Duplicates | Merge by SubSectionId; MMR; QualityGate AI/DB dup |
| 7 | Confidence | CanonicalScore 0–1 **is** implemented on fast-f |

Enterprise steps (`EnterpriseValidationStepNames`): Evidence, Clinical, Gender, Age, Domain, Hallucination, Duplicate, Confidence.  
Quality floors: `MinRubricQualityScore` 70, Tier3 58, review fallback 50, `MinEnterpriseRubricConfidenceScore` 62.

---

## Hallucination prevention

```text
AI Candidate with SubSectionId <= 0
    ↓ FastClinicalEvidenceGate.ApplyHallucinationHardGate
    ↓ dropped (NotDbBacked)
```

Also: evidence token overlap &lt; `FastPipelineMinEvidenceScore` (0.15) dropped.  
Reconciler can promote AI names **only if they still exist in the list** and fuzzy/exact match ≥ `AiReconciliationMinConfidence` (0.7). Fast-f usually never gives them that chance.

AI **can** invent names inside `SuggestAiRubricsAsync`. They are **not** shown as repertory rubrics on the fast path unless mapped and kept. `EnableAiClinicalConceptSuggestions` is for unmatched concepts as `resultKind=AiClinicalConcept` on other engines.

---

## Decision tree (actual fast-f)

```text
Transcript
   ↓
GPT: clinically extract symptoms? (model may omit items — no second pass on fast path)
   ↓
Filter doctor-only / negated
   ↓
Search DB (5 parallel channels)
   ↓
Candidate SubSectionId > 0?
   ├── NO  → drop
   └── YES → quality/gender/hierarchy/evidence
                  ↓
             CanonicalScore ≥ 0.45?
                  ├── NO  → drop from MMR pool
                  └── YES → MMR until 12 or cliff
```

---

## Quality control mapping

| Issue | Mechanism |
| ----- | --------- |
| False positive | Evidence gate, gender, hitchhiker filter, domain score 0.35, word boundary |
| False negative | Multi-query, catalog, embedding, alias; MMR cliff / min score can drop valid ones |
| Hallucination | DB id required |
| Duplicate | SubSectionId group + MMR name similarity |
| Over-generalization | Hierarchy specificity prefers evidence-backed leaves |
| Under-specificity | Extraction prompt asks to preserve location/side/timing; not separately scored |

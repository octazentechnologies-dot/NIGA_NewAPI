# AI Rubric Engine — Fast Pipeline Architecture (Accuracy + Retrieval + Packs 3/5)

**Superseding audit (2026-08-16):** [AUDIO_CASE_AI_RUBRIC_COMPLETE_DOCUMENTATION.md](./AUDIO_CASE_AI_RUBRIC_COMPLETE_DOCUMENTATION.md). This file remains a short fast-path sketch; the audit is the source of truth when they differ.

```
Audio
  → Whisper (1)
  → GptExtraction (1)  [evidence-first prompt]
  → FastClinicalRetrieval (engine fast-f)
       Speaker/negation filter
       Multi-query symptom blocks
       parallel:
         V1 Exact/LIKE/Hotspot
         Keyword/FTS
         Alias
         Embedding (≤8s, query-vector cache)
         Catalog token funnel (immutable snapshot + aliases, DeleteStatus filtered)
       → QualityGate (Bugs A–D)
       → Gender + Hierarchy + Hallucination hard gate
       → CanonicalScore 0–1
       → Doctor learning soft boost (never overrides evidence)
       → MMR diversity ≤12
       → Evidence contract
  → ClinicalValidation (existing)
  → Doctor approval
```

**Background:** session processing gate prevents duplicate in-flight jobs.  
**UI:** stage label + progress strip (Transcribe → Extract → Match → Finalize).

**Benchmark (Pack 3):**  
- `AI_RUBRIC_GOLDEN_SET.md` (SessionId registry — fill via SQL 730)  
- `AI_RUBRIC_ENGINE_ACCURACY_BENCHMARK.md` / `PERFORMANCE_BENCHMARK.md` (framework; metrics TBD until measured)  
- SQL `730_Golden_Set_Benchmark_Queries.sql` (read-only)

**Flag:** `EnableFastClinicalRetrievalPipeline` (default **true**).  
**Engine:** `FastPipelineEngineVersion` = `fast-f`.  
**Rollback:** set flag `false`.
